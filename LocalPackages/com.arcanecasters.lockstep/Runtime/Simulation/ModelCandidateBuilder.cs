using System;
using System.Collections.Generic;
using System.Text;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    // 원본 llm_candidate_builder.rs의 고정 소수점 이관이다. 한 봇 관측을 모델 요청이 고르는 완전한 선택지와 board 상태로 바꾼다.
    // 선택지는 규칙 brain이 쓰는 helper로 값을 매기고 겨눈다: ThreatAssessment, BotSpellStats, BlastTargetSelector, PlacementPlanner.
    public sealed class ModelCandidateBuilder : IModelCandidateBuilder {
        public const int MaxPlays = 16, MaxStateEnemies = 12, MaxStateAllies = 8, UnpricedCost = 9999, MaxStandardReason = 90;
        public const string StandardTactic = "STANDARD TACTIC";
        public static readonly Fix64 UrgentPressure = (Fix64)0.3m, UrgentDistance = 6, SameTargetDistance = 1, CounterThreshold = 1;
        // 소환을 둘 수 있는 세 갈래 z 좌표와, "기지 근처"가 기지 앞으로 나오는 거리다.
        private static readonly Fix64[] Lanes = { 2, 5, 8 };
        private static readonly Fix64 NearOffset = 3;
        private readonly IBotMagicCatalog catalog;
        private readonly IBotCounterScorer counters;
        private readonly Func<JavaRandom> random;
        private readonly bool cardMenu;
        private readonly MagicDescriptions descriptions;
        public ModelCandidateBuilder(IBotMagicCatalog catalog, IBotCounterScorer counters, Func<JavaRandom> random,
            bool cardMenu = false, MagicDescriptions descriptions = null) {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.counters = counters;
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            // 기본값은 묶인 카드 설명문이다. 원본 MagicDescriptions::bundled()와 같은 생산 경로다.
            this.cardMenu = cardMenu; this.descriptions = descriptions ?? MagicDescriptions.Bundled;
        }
        public static ModelCandidateBuilder Cards(IBotMagicCatalog catalog, IBotCounterScorer counters, Func<JavaRandom> random,
            MagicDescriptions descriptions = null) => new ModelCandidateBuilder(catalog, counters, random, true, descriptions);

        public ModelCandidateMenu Build(BotEye eye, IDictionary<string, Fix64> parameters, Master side, InputDecision standard) {
            if (cardMenu) return BuildCards(eye, parameters, side);
            var turn = new Turn(this, eye, parameters, side);
            var drafts = new List<Draft>();
            if (standard != null) {
                // 규칙 brain은 낼 수 없는 카드를 고르지 않지만, 오래되거나 다른 곳에서 온 판단은 그럴 수 있다.
                BotMagic chosen = null;
                foreach (var magic in turn.Affordable) if (magic.Id == standard.MagicId) { chosen = magic; break; }
                if (chosen != null) {
                    var plain = turn.Draft(chosen, standard.Target);
                    drafts.Add(new Draft(chosen, standard.Target,
                        StandardTactic + " (what the rule bot would do: " + Shorten(standard.Reason) + "): " + plain.Description, plain.Impact));
                }
            }
            var alternatives = new List<Draft>();
            foreach (var magic in turn.Affordable) {
                if (magic.Summoning.HasValue) {
                    var spot = PlacementPlanner.Plan(turn.OwnBase, turn.Stats.CastRange(magic), side, turn.Threats, random());
                    drafts.Add(turn.Draft(magic, spot));
                    continue;
                }
                var best = BlastTargetSelector.BestImpact(turn.Threats.Threats, turn.OwnBase, turn.Stats.CastRange(magic),
                    turn.Stats.BlastRadius(magic), turn.Stats.DamagePerTarget(magic));
                if (!best.HasValue) continue;
                var center = best.Value.Center;
                drafts.Add(turn.Draft(magic, center));
                foreach (var extra in turn.ExtraTargets(magic, center)) alternatives.Add(turn.Draft(magic, extra));
            }
            drafts.AddRange(alternatives);
            var plays = new List<ModelCandidatePlay>();
            var kept = new List<Draft>();
            foreach (var draft in drafts) {
                if (plays.Count >= MaxPlays) break;
                bool duplicate = false;
                foreach (var other in kept) if (other.SameAs(draft)) { duplicate = true; break; }
                if (duplicate) continue;
                kept.Add(draft);
                plays.Add(new ModelCandidatePlay("p" + plays.Count, draft.Magic.Id, draft.Magic.Name, turn.Stats.ManaCost(draft.Magic),
                    draft.Target, draft.Description, draft.Magic.Summoning.HasValue, draft.Impact.BaseDamage, draft.Impact.UnitDamage));
            }
            return new ModelCandidateMenu(plays, turn.WaitDescription(), turn.State());
        }

        // 낼 수 있는 카드마다 하나씩, 마법 이름을 키로 제시한다. 어느 카드를 낼지는 모델이, 어디에 둘지는 코드가 정한다.
        public ModelCandidateMenu BuildCards(BotEye eye, IDictionary<string, Fix64> parameters, Master side) {
            var turn = new Turn(this, eye, parameters, side);
            var keys = new List<string>();
            var plays = new List<ModelCandidatePlay>();
            foreach (var magic in turn.Affordable) {
                if (plays.Count >= MaxPlays) break;
                int cost = turn.Stats.ManaCost(magic);
                var text = new StringBuilder(magic.Name);
                Vector3 target;
                var impact = Impact.None;
                IReadOnlyList<ModelCandidateAim> aims;
                bool summon = magic.Summoning.HasValue;
                if (summon) {
                    target = PlacementPlanner.Plan(turn.OwnBase, turn.Stats.CastRange(magic), side, turn.Threats, random());
                    var summoning = magic.Summoning.Value;
                    text.Append(" (summon, ").Append(cost).Append(" mana): ").Append(CardText(magic))
                        .Append(" Puts ").Append(summoning.Quantity).Append(' ').Append(summoning.Prefab)
                        .Append(". It ").Append(turn.Guide.Role(summoning.Prefab))
                        .Append(". Right now: ").Append(turn.SummonNow(summoning.Prefab, target)).Append('.');
                    aims = turn.SummonAims(magic, summoning.Prefab, target);
                } else {
                    var radius = turn.Stats.BlastRadius(magic);
                    var damage = turn.Stats.DamagePerTarget(magic);
                    var best = BlastTargetSelector.BestImpact(turn.Threats.Threats, turn.OwnBase, turn.Stats.CastRange(magic), radius, damage);
                    if (!best.HasValue) continue;
                    target = best.Value.Center;
                    impact = turn.MeasureImpact(target, radius, damage);
                    if (impact.Hit.Count == 0 && !impact.BaseHit) continue;
                    text.Append(" (spell, ").Append(cost).Append(" mana, range ").Append(ModelText.Number(turn.Stats.CastRange(magic)))
                        .Append(", radius ").Append(ModelText.Number(radius)).Append(", ").Append(ModelText.Number(damage))
                        .Append(" damage to each target in the radius): ").Append(CardText(magic)).Append(" Right now its best target ");
                    turn.AppendImpact(text, impact, cost);
                    text.Append('.');
                    // 주문은 코드가 겨눈다. 어디에 쏠지 물으면 모델은 기지 대신 유닛을 때려 승률이 떨어졌다.
                    aims = Array.Empty<ModelCandidateAim>();
                }
                var play = new ModelCandidatePlay(magic.Name, magic.Id, magic.Name, cost, target, text.ToString(), summon,
                    impact.BaseDamage, impact.UnitDamage, aims);
                // LinkedHashMap.put: 같은 이름이 다시 오면 그 자리에서 값만 바뀐다.
                int existing = keys.IndexOf(magic.Name);
                if (existing >= 0) plays[existing] = play;
                else { keys.Add(magic.Name); plays.Add(play); }
            }
            string wait = plays.Count == 0 ? null : turn.KeepManaDescription();
            return new ModelCandidateMenu(plays, wait, turn.State());
        }

        private string CardText(BotMagic magic) {
            string text = descriptions.Describe(magic.Name);
            if (text == null) return "";
            return text.EndsWith(".", StringComparison.Ordinal) ? text : text + ".";
        }

        // 손해 주문이 겨눈 자리에 떨어뜨리는 것이다. 소환은 모두 0이다.
        private readonly struct Impact {
            public readonly IReadOnlyList<string> Hit;
            public readonly Fix64 UnitDamage, BaseDamage;
            public readonly int Kills;
            public readonly bool BaseHit;
            public Impact(IReadOnlyList<string> hit, Fix64 unitDamage, Fix64 baseDamage, int kills, bool baseHit) {
                Hit = hit; UnitDamage = unitDamage; BaseDamage = baseDamage; Kills = kills; BaseHit = baseHit;
            }
            public static Impact None => new Impact(Array.Empty<string>(), Fix64.Zero, Fix64.Zero, 0, false);
        }
        private readonly struct Draft {
            public readonly BotMagic Magic;
            public readonly Vector3 Target;
            public readonly string Description;
            public readonly Impact Impact;
            public Draft(BotMagic magic, Vector3 target, string description, Impact impact) {
                Magic = magic; Target = target; Description = description; Impact = impact;
            }
            public bool SameAs(Draft other) => Magic.Id == other.Magic.Id && Vector3.Distance(Target, other.Target) <= SameTargetDistance;
        }

        // 한 번의 build가 관측에서 읽는 전부를 한 번만 계산한다.
        private sealed class Turn {
            private readonly ModelCandidateBuilder builder;
            private readonly Master side;
            private readonly BotEye eye;
            private readonly List<BotVisibleObject> enemyUnits, ownUnits;
            private readonly Dictionary<int, BotVisibleObject> objects = new Dictionary<int, BotVisibleObject>();
            private readonly Dictionary<long, string> counterText = new Dictionary<long, string>();
            public readonly Vector3 OwnBase, EnemyBase;
            public readonly BotSpellStats Stats;
            public readonly ModelUnitGuide Guide;
            public readonly ThreatAssessment Threats;
            public readonly List<BotMagic> Hand = new List<BotMagic>(), Affordable = new List<BotMagic>();
            public Turn(ModelCandidateBuilder builder, BotEye eye, IDictionary<string, Fix64> parameters, Master side) {
                this.builder = builder; this.eye = eye; this.side = side;
                var enemySide = side == Master.LeftPlayer ? Master.RightPlayer : Master.LeftPlayer;
                OwnBase = BasePosition(side); EnemyBase = BasePosition(enemySide);
                Stats = new BotSpellStats(parameters); Guide = new ModelUnitGuide(parameters);
                Threats = ThreatAssessment.Observe(eye.Objects, enemySide, OwnBase, eye.EnemyPlayerHp);
                foreach (var item in eye.Objects) if (!objects.ContainsKey(item.Id)) objects[item.Id] = item;
                var seen = new HashSet<long>();
                foreach (long magicId in eye.Cards) {
                    if (!seen.Add(magicId)) continue;
                    var magic = builder.catalog.GetMagic(magicId);
                    if (magic == null) continue;
                    int cost = Stats.ManaCost(magic);
                    Hand.Add(magic);
                    if (cost <= eye.Mana && cost < UnpricedCost) Affordable.Add(magic);
                }
                enemyUnits = Units(eye, enemySide, OwnBase); ownUnits = Units(eye, side, EnemyBase);
            }
            public static Vector3 BasePosition(Master master) => new Vector3(master == Master.LeftPlayer ? 1 : 17, 0, 5);

            public Draft Draft(BotMagic magic, Vector3 target) {
                var impact = magic.Summoning.HasValue ? Impact.None : MeasureImpact(target, Stats.BlastRadius(magic), Stats.DamagePerTarget(magic));
                return new Draft(magic, target, Describe(magic, target, impact), impact);
            }

            private string Describe(BotMagic magic, Vector3 target, Impact impact) {
                int cost = Stats.ManaCost(magic);
                string at = " at " + Coordinates(target) + ", " + ModelText.Number(Vector3.Distance(target, OwnBase)) + " from our base";
                var text = new StringBuilder(magic.Name);
                if (magic.Summoning.HasValue) {
                    var summoning = magic.Summoning.Value;
                    text.Append(" (summon, cost ").Append(cost).Append(')').Append(at)
                        .Append(": puts ").Append(summoning.Quantity).Append(' ').Append(summoning.Prefab);
                    if (summoning.Airborne) text.Append(" (flying)");
                    if (Guide.Stats(summoning.Prefab).Known) text.Append(" [").Append(Guide.Describe(summoning.Prefab)).Append(']');
                    text.Append(" in lane z=").Append(ModelText.Number(target.Z)).Append("; ");
                    if (enemyUnits.Count == 0) text.Append("no enemy unit is on the field, so it starts an advance");
                    else text.Append("blocks the nearest enemy ").Append(enemyUnits[0].Prefab)
                        .Append(" (").Append(ModelText.Number(Vector3.Distance(enemyUnits[0].Position, OwnBase))).Append(" from our base)");
                    text.Append("; pressure on our base is ").Append(PressureLabel()).Append(". ");
                } else {
                    var radius = Stats.BlastRadius(magic);
                    var damage = Stats.DamagePerTarget(magic);
                    text.Append(" (damage spell, cost ").Append(cost).Append(", ").Append(ModelText.Number(damage))
                        .Append(" damage in radius ").Append(ModelText.Number(radius)).Append(')').Append(at).Append(": ");
                    AppendImpact(text, impact, cost);
                    text.Append(". ");
                }
                return text.Append(Counter(magic)).ToString();
            }

            public Impact MeasureImpact(Vector3 target, Fix64 radius, Fix64 damage) {
                var hit = new List<string>();
                Fix64 unitDamage = Fix64.Zero, baseDamage = Fix64.Zero;
                int kills = 0; bool baseHit = false;
                foreach (var threat in Threats.Threats) {
                    if (Vector3.Distance(threat.Position, target) > radius) continue;
                    if (threat.PlayerCore) {
                        baseHit = true; baseDamage += MathHelper.Min(damage, threat.Hp);
                        continue;
                    }
                    // 날아가는 투사체는 규칙 brain에게는 위협이지만 읽는 사람에게는 유닛이 아니다.
                    if (!objects.TryGetValue(threat.ObjectId, out var item) || !item.Mob) continue;
                    hit.Add(ThreatName(item));
                    unitDamage += MathHelper.Min(damage, threat.Hp);
                    if (damage >= (Fix64)threat.Hp) kills++;
                }
                return new Impact(hit, unitDamage, baseDamage, kills, baseHit);
            }

            public void AppendImpact(StringBuilder text, Impact impact, int cost) {
                var hit = impact.Hit;
                if (hit.Count == 0 && !impact.BaseHit) { text.Append("hits no enemy unit"); return; }
                if (hit.Count > 0)
                    text.Append("hits ").Append(hit.Count).Append(" enemy unit").Append(hit.Count == 1 ? "" : "s")
                        .Append(" (").Append(string.Join(", ", hit)).Append(')');
                if (impact.BaseHit) text.Append(hit.Count == 0 ? "hits the enemy base" : " and the enemy base");
                var total = impact.UnitDamage + impact.BaseDamage;
                text.Append(" for ").Append(ModelText.Number(total)).Append(" total damage (")
                    .Append(ModelText.Number(cost > 0 ? total / cost : total)).Append(" per mana), kills ").Append(impact.Kills);
            }

            private long CopiesInHand(BotMagic magic) {
                long copies = 0;
                foreach (long id in eye.Cards) if (id == magic.Id) copies++;
                return copies;
            }

            // 소환이나 건물이 설 수 있는 자리다: 밀려오는 적을 막는 지점, 세 갈래마다 기지 근처와 닿는 가장 앞, 그리고 우리 선두 유닛 옆이다.
            public IReadOnlyList<ModelCandidateAim> SummonAims(BotMagic magic, string unit, Vector3 block) {
                var range = Stats.CastRange(magic);
                var aims = new List<ModelCandidateAim> {
                    new ModelCandidateAim("block", "Between our base and the incoming enemies: " + SummonNow(unit, block), block)
                };
                int forward = side == Master.LeftPlayer ? 1 : -1;
                foreach (var lane in Lanes) {
                    string laneName = LaneName(lane);
                    var near = WithinRange(new Vector3(OwnBase.X + forward * NearOffset, OwnBase.Y, lane), range);
                    AddSpot(aims, "near_" + laneName, "Near our base in the " + laneName + " lane (z=" + ModelText.Number(lane) + "): ", unit, near);
                    var laneOffset = lane - OwnBase.Z;
                    var depth = Fix64.Sqrt(MathHelper.Max(Fix64.Zero, range * range - laneOffset * laneOffset)) * (Fix64)0.95m;
                    var far = new Vector3(OwnBase.X + forward * depth, OwnBase.Y, lane);
                    AddSpot(aims, "forward_" + laneName,
                        "As far toward the enemy base as it reaches in the " + laneName + " lane (z=" + ModelText.Number(lane) + "): ", unit, far);
                }
                foreach (var ally in ownUnits) {
                    if (Vector3.Distance(ally.Position, OwnBase) > range) continue;
                    AddSpot(aims, "ally", "Next to our " + ally.Prefab + " (" + ally.Hp + " hp): ", unit, ally.Position);
                    break;
                }
                return aims;
            }
            private void AddSpot(List<ModelCandidateAim> aims, string key, string label, string unit, Vector3 spot) {
                foreach (var aim in aims) if (Vector3.Distance(aim.Target, spot) <= SameTargetDistance) return;
                aims.Add(new ModelCandidateAim(key, label + SummonNow(unit, spot), spot));
            }
            private Vector3 WithinRange(Vector3 spot, Fix64 range) {
                var distance = Vector3.Distance(spot, OwnBase);
                if (distance <= range || distance == Fix64.Zero) return spot;
                return OwnBase + Vector3.Normalize(spot - OwnBase) * range;
            }

            // 소환이 설 자리와 그 자리에 가까운 적이다.
            public string SummonNow(string unit, Vector3 spot) {
                var text = new StringBuilder("it would be placed ")
                    .Append(ModelText.Number(Vector3.Distance(spot, OwnBase))).Append(" from our base");
                BotVisibleObject? nearest = null; Fix64 nearestDistance = Fix64.Zero;
                foreach (var enemy in enemyUnits) {
                    var distance = Vector3.Distance(enemy.Position, spot);
                    if (nearest.HasValue && distance >= nearestDistance) continue;
                    nearest = enemy; nearestDistance = distance;
                }
                if (!nearest.HasValue) return text.Append(", and no enemy unit is on the field").ToString();
                text.Append("; the nearest enemy unit to that spot is a ").Append(nearest.Value.Prefab)
                    .Append(' ').Append(ModelText.Number(nearestDistance)).Append(" away");
                var reach = Guide.Reach(unit);
                if (Guide.Stats(unit).Speed <= Fix64.Zero && reach > Fix64.Zero) {
                    int inReach = 0;
                    foreach (var enemy in enemyUnits) if (Vector3.Distance(enemy.Position, spot) <= reach) inReach++;
                    text.Append("; ").Append(inReach).Append(" enemy unit").Append(inReach == 1 ? " is" : "s are")
                        .Append(" within its reach of ").Append(ModelText.Number(reach)).Append(" now");
                }
                return text.ToString();
            }

            // 기다리는 선택지와 그것이 모으는 카드다.
            public string KeepManaDescription() {
                var later = new List<string>();
                foreach (var magic in Hand) {
                    int cost = Stats.ManaCost(magic);
                    if (cost > eye.Mana && cost < UnpricedCost) later.Add(magic.Name + " (" + cost + " mana)");
                }
                bool full = eye.Cards.Count >= PlayerEconomy.MaxHand;
                return "Cast nothing now and keep the mana; mana is " + eye.Mana
                    + (later.Count == 0 ? ", and every card in hand is already affordable."
                        : ", and these cards in hand are not affordable yet: " + string.Join(", ", later) + ".")
                    + (full ? " The hand is full, so no new card is drawn while nothing is played." : "");
            }

            // 가장 앞선 적 유닛과 적 기지다. 사거리 안이고 최선의 폭발에 들어 있지 않을 때만이다.
            public IReadOnlyList<Vector3> ExtraTargets(BotMagic magic, Vector3 center) {
                var range = Stats.CastRange(magic);
                var radius = Stats.BlastRadius(magic);
                var extras = new List<Vector3>();
                foreach (var unit in enemyUnits) {
                    if (Vector3.Distance(unit.Position, OwnBase) > range) continue;
                    if (Vector3.Distance(unit.Position, center) > radius) extras.Add(unit.Position);
                    break;
                }
                foreach (var threat in Threats.Threats) {
                    if (!threat.PlayerCore || Vector3.Distance(threat.Position, OwnBase) > range
                        || Vector3.Distance(threat.Position, center) <= radius) continue;
                    extras.Add(threat.Position); break;
                }
                return extras;
            }

            private string Counter(BotMagic magic) {
                if (counterText.TryGetValue(magic.Id, out var cached)) return cached;
                string text;
                if (enemyUnits.Count == 0) text = "No enemy units on the field to counter.";
                else if (builder.counters == null) text = "Neutral against the enemy units on the field.";
                else {
                    var net = builder.counters.Evaluate(magic, enemyUnits) - builder.counters.EvaluateVulnerability(magic, enemyUnits);
                    string label = net >= CounterThreshold ? "Strong" : net <= -CounterThreshold ? "Weak" : "Neutral";
                    text = label + " against the enemy units on the field (counter score "
                        + (net > Fix64.Zero ? "+" : "") + ModelText.Number(net) + ").";
                }
                counterText[magic.Id] = text;
                return text;
            }

            public bool Urgent() {
                if (Threats.Pressure >= UrgentPressure) return true;
                return enemyUnits.Count > 0 && Vector3.Distance(enemyUnits[0].Position, OwnBase) <= UrgentDistance;
            }

            // 급한 일이 없고 손패의 가장 비싼 마법을 아직 낼 수 없을 때만 제시한다.
            public string WaitDescription() {
                if (Urgent()) return null;
                BotMagic dearest = null; int dearestCost = 0;
                foreach (var magic in Hand) {
                    int cost = Stats.ManaCost(magic);
                    if (cost >= UnpricedCost) continue;
                    if (dearest != null && cost <= dearestCost) continue;
                    dearest = magic; dearestCost = cost;
                }
                if (dearest == null || dearestCost <= eye.Mana) return null;
                string nearest = enemyUnits.Count == 0 ? "no enemy unit is on the field"
                    : "the nearest enemy unit is " + ModelText.Number(Vector3.Distance(enemyUnits[0].Position, OwnBase)) + " from our base";
                return "Cast nothing now and save mana for " + dearest.Name + " (cost " + dearestCost + "), mana is "
                    + eye.Mana + " now; nothing is urgent: " + nearest + ".";
            }

            public ModelJsonMap State() {
                var state = new ModelJsonMap();
                state.Put("side", side == Master.LeftPlayer ? "left" : "right");
                int? ownHp = OwnHp();
                state.Put("ourHp", ownHp.HasValue ? ModelJson.Of(ownHp.Value) : ModelJson.Null);
                state.Put("enemyHp", eye.EnemyPlayerHp);
                state.Put("mana", eye.Mana);
                state.Put("pressure", ModelJson.Round2(Threats.Pressure));
                state.Put("enemyNearOurBase", Urgent());
                var hand = new List<ModelJson>();
                foreach (var magic in Hand) {
                    var card = new ModelJsonMap();
                    card.Put("magic", magic.Name); card.Put("cost", Stats.ManaCost(magic)); card.Put("copies", CopiesInHand(magic));
                    hand.Add(ModelJson.Of(card));
                }
                state.Put("hand", ModelJson.Of(hand));
                state.Put("handSlotsUsed", eye.Cards.Count + " of " + PlayerEconomy.MaxHand);
                state.Put("enemyUnits", ModelJson.Of(Summarise(enemyUnits, MaxStateEnemies, OwnBase, "toOurBase", Guide)));
                state.Put("ourUnits", ModelJson.Of(Summarise(ownUnits, MaxStateAllies, EnemyBase, "toEnemyBase", Guide)));
                state.Put("unitGuide", ModelJson.Of(UnitGuideMap()));
                return state;
            }

            // 전장에 있거나 손패에서 소환할 수 있는 모든 유닛 종류를 이름 순으로 적는다.
            private ModelJsonMap UnitGuideMap() {
                var types = new SortedDictionary<string, string>(StringComparer.Ordinal);
                foreach (var unit in enemyUnits) if (!types.ContainsKey(unit.Prefab)) types[unit.Prefab] = Guide.Describe(unit.Prefab);
                foreach (var unit in ownUnits) if (!types.ContainsKey(unit.Prefab)) types[unit.Prefab] = Guide.Describe(unit.Prefab);
                foreach (var magic in Hand) {
                    if (!magic.Summoning.HasValue) continue;
                    string prefab = magic.Summoning.Value.Prefab;
                    if (!types.ContainsKey(prefab)) types[prefab] = Guide.Describe(prefab);
                }
                var guide = new ModelJsonMap();
                foreach (var pair in types) guide.Put(pair.Key, pair.Value);
                return guide;
            }

            // 폭발에 들어간 적 유닛의 "WindSpirit (hits our base for 300 in 2.3s)"다.
            private string ThreatName(BotVisibleObject item) {
                var unit = Guide.Stats(item.Prefab);
                var eta = unit.SecondsToReach(Vector3.Distance(item.Position, OwnBase));
                if (!eta.HasValue || unit.Damage <= Fix64.Zero) return item.Prefab;
                return item.Prefab + " (hits our base for " + ModelText.Number(unit.Damage) + " in " + ModelText.Number(eta.Value) + "s)";
            }

            private int? OwnHp() {
                foreach (var item in eye.Objects) if (item.Master == side && item.Prefab == "Player") return item.Hp;
                return null;
            }

            private string PressureLabel() {
                var pressure = Threats.Pressure;
                string label = pressure >= (Fix64)0.7m ? "high" : pressure >= UrgentPressure ? "medium" : "low";
                return label + " (" + ModelText.Decimal(ModelText.Round2Hundredths(pressure), 2) + ")";
            }
        }

        // 한쪽의 살아 있는 유닛을 from에 가까운 순으로. 플레이어 본체는 뺀다.
        private static List<BotVisibleObject> Units(BotEye eye, Master master, Vector3 from) {
            var units = new List<BotVisibleObject>();
            foreach (var item in eye.Objects)
                if (item.Master == master && item.Targetable && item.Mob && item.Prefab != "Player") units.Add(item);
            // Stream.sorted처럼 안정 정렬이다.
            var keys = new Fix64[units.Count];
            for (int i = 0; i < units.Count; i++) keys[i] = Vector3.Distance(units[i].Position, from);
            for (int i = 1; i < units.Count; i++) {
                var unit = units[i]; var key = keys[i];
                int j = i - 1;
                while (j >= 0 && keys[j] > key) { units[j + 1] = units[j]; keys[j + 1] = keys[j]; j--; }
                units[j + 1] = unit; keys[j + 1] = key;
            }
            return units;
        }

        private static List<ModelJson> Summarise(IReadOnlyList<BotVisibleObject> objects, int limit, Vector3 from,
            string distanceName, ModelUnitGuide guide) {
            var rows = new List<ModelJson>();
            for (int i = 0; i < objects.Count && i < limit; i++) {
                var item = objects[i];
                var row = new ModelJsonMap();
                var distance = Vector3.Distance(item.Position, from);
                row.Put("type", item.Prefab);
                row.Put("hp", item.Hp);
                row.Put(distanceName, ModelJson.Round1(distance));
                var eta = guide.Stats(item.Prefab).SecondsToReach(distance);
                if (eta.HasValue) row.Put("secondsToHitBase", ModelJson.Round1(eta.Value));
                row.Put("z", ModelJson.Round1(item.Position.Z));
                rows.Add(ModelJson.Of(row));
            }
            return rows;
        }

        private static string Shorten(string reason) {
            if (reason == null || reason.Trim().Length == 0) return "no reason given";
            string text = ModelText.CollapseWhitespace(reason.Trim());
            if (text.EndsWith(".", StringComparison.Ordinal)) text = text.Substring(0, text.Length - 1);
            return text.Length <= MaxStandardReason ? text : text.Substring(0, MaxStandardReason);
        }
        private static string Coordinates(Vector3 position) => "(" + ModelText.Number(position.X) + ", " + ModelText.Number(position.Z) + ")";
        private static string LaneName(Fix64 lane) => lane < (Fix64)4 ? "low" : lane > (Fix64)6 ? "high" : "middle";
    }
}
