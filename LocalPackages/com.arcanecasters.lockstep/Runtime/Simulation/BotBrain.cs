using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class InputDecision {
        public readonly long MagicId;
        public readonly Vector3 Target;
        public readonly string RuleId, Reason;
        public InputDecision(long id, Vector3 target, string rule, string reason) { MagicId = id; Target = target; RuleId = rule; Reason = reason; }
    }
    public sealed class BotBrain {
        public const string ValueRule = "score.best-value", CycleRule = "cycle.low-utility";
        private readonly IBotMagicCatalog catalog;
        private readonly IBotCounterScorer counters;
        private readonly BotPersona persona;
        private readonly JavaRandom random;
        private readonly MotionTracker motion = new MotionTracker();
        private readonly Func<IReadOnlyList<long>> deckCards;
        private DeckProfile? deck;
        private readonly HospitalityDirector hospitality;
        public BotSkill Skill { get; }
        // 모델 봇의 후보 메뉴가 같은 마법 데이터와 상성 평가를 읽는다.
        public IBotMagicCatalog Catalog => catalog;
        public IBotCounterScorer Counters => counters;
        public event Action<Exception> ThinkFailed;
        public BotBrain(IBotMagicCatalog catalog, IBotCounterScorer counters, BotPersona persona, JavaRandom random, BotSkill skill = null, Func<IReadOnlyList<long>> deckCards = null, Fix64? opponentProgress = null) {
            this.catalog = catalog; this.counters = counters; this.persona = persona; this.random = random;
            Skill = skill ?? BotSkill.ForTier(persona.Tier); this.deckCards = deckCards ?? (() => Array.Empty<long>());
            if (persona.Tier == BotTier.Hospitality) hospitality = new HospitalityDirector(opponentProgress ?? HospitalityDirector.DefaultBoardShare);
        }
        public InputDecision Think(BotEye eye, IDictionary<string, Fix64> parameters, Master side, bool overdue = false) => ThinkSeeded(eye, parameters, side, random, overdue);
        public InputDecision ThinkSeeded(BotEye eye, IDictionary<string, Fix64> parameters, Master side, JavaRandom generator, bool overdue = false) {
            try { return Decide(eye, parameters, side, generator, overdue); }
            catch (Exception error) { ThinkFailed?.Invoke(error); return null; }
        }
        private InputDecision Decide(BotEye eye, IDictionary<string, Fix64> parameters, Master side, JavaRandom generator, bool overdue) {
            var velocities = Skill.Has(BotCapability.LeadTargeting) ? motion.Observe(eye) : new Dictionary<int, Vector3>();
            if (eye.Cards.Count == 0) return null;
            var position = new Vector3(side == Master.LeftPlayer ? 1 : 17, 0, 5);
            var enemySide = side == Master.LeftPlayer ? Master.RightPlayer : Master.LeftPlayer;
            var enemies = eye.Objects.Where(o => o.Master == enemySide).ToArray();
            var stats = new BotSpellStats(parameters);
            var units = Skill.Has(BotCapability.SummonStrength) || Skill.Has(BotCapability.ThreatStrength) || Skill.Has(BotCapability.RolePlacement) ? new UnitCatalogue(catalog, stats, parameters) : null;
            var threats = ThreatAssessment.Observe(eye.Objects, enemySide, position, eye.EnemyPlayerHp, o => units != null && Skill.Has(BotCapability.ThreatStrength) ? units.RelativeThreat(o) : Fix64.One, Skill.Has(BotCapability.AllyCover));
            var aimed = threats.LedBy(velocities, (Fix64)persona.NormalizedThinkingTimeMs / 1000 + (Fix64)0.25m);
            int Count(Master owner) => eye.Objects.Count(o => o.Master == owner && o.Prefab != "Player" && o.Targetable);
            var aggression = hospitality == null ? persona.NormalizedCounterAggression : hospitality.Aggression(Count(enemySide), Count(side), threats.Pressure);
            var budget = int.MaxValue;
            if (hospitality != null) {
                var board = new BoardValue(catalog, stats, parameters);
                budget = hospitality.SummonAllowance(board.ManaOnField(eye.Objects, enemySide), board.ManaOnField(eye.Objects, side));
            }
            var seen = new HashSet<long>(); var hand = new List<BotMagic>();
            foreach (var id in eye.Cards) if (seen.Add(id)) { var magic = catalog.GetMagic(id); if (magic != null) hand.Add(magic); }
            if (hand.Count == 0) return null;
            var c = new ThinkContext(hand, eye.Mana, stats, eye.Objects, position, side, enemySide, threats, Skill, generator);
            if (hospitality == null) { var priority = BotPriorityRules.Find(c); if (priority != null) return Decision(priority); }
            var maxMana = parameters.TryGetValue("player.max_mana", out var maximum) ? (int)(maximum.RawValue / (1L << 32)) : 0;
            if (hospitality == null && Skill.Has(BotCapability.GamePlan) && !overdue && threats.Pressure < (Fix64)0.35m) {
                if (!deck.HasValue) deck = DeckProfile.Of(deckCards(), catalog, stats);
                var plan = GamePlanner.Plan(c, maxMana, parameters, deck.Value, magic => Placement(magic, c, units));
                if (plan.Saving) return null;
                if (plan.Play != null) return Decision(plan.Play);
            }
            var plays = new List<ScoredPlay>(); var saving = new List<ScoredPlay>();
            BotMagic cheapest = null; var cheapestCost = int.MaxValue;
            foreach (var magic in hand) {
                if (hospitality != null && !magic.Summoning.HasValue) continue;
                var cost = stats.ManaCost(magic); if (cost >= BotSpellStats.UnknownManaCost) continue;
                if (cost > eye.Mana) {
                    if (Skill.Has(BotCapability.ManaDiscipline) && cost <= maxMana) { var future = BuildPlay(magic, cost, aggression, c, aimed, units, enemies); if (future != null) saving.Add(future); }
                    continue;
                }
                if (Skill.Has(BotCapability.SummonRelevance) && magic.Summoning.HasValue && !SummonRelevance.Relevant(magic, c, parameters) && !HandFull(eye) && !(maxMana > 0 && eye.Mana >= maxMana - GamePlanner.CapMargin)) continue;
                if (cost >= budget) continue;
                if (cost < cheapestCost) { cheapestCost = cost; cheapest = magic; }
                var play = BuildPlay(magic, cost, aggression, c, aimed, units, enemies); if (play != null) plays.Add(play);
            }
            var chosen = ScoredPlay.Best(plays, true);
            if (chosen != null) {
                if (!HandFull(eye) && !overdue && threats.Pressure < (Fix64)0.35m && chosen.Score != Fix64.MaxValue && saving.Any(p => p.Score > chosen.Score * (Fix64)1.25m)) return null;
                return Decision(chosen);
            }
            return overdue && cheapest != null ? new InputDecision(cheapest.Id, Placement(cheapest, c, units), CycleRule, "No card scored a target; cycling the cheapest affordable card " + cheapest.Name + ".") : null;
        }
        private static bool HandFull(BotEye eye) => eye.Cards.Count >= PlayerData.MaxCardCount;
        private static InputDecision Decision(ScoredPlay play) => new InputDecision(play.MagicId, play.Target, play.RuleId, play.Reason);
        private Vector3 Placement(BotMagic magic, ThinkContext c, UnitCatalogue units) => PlacementPlanner.Plan(c.PlayerPosition, c.SpellStats.CastRange(magic), c.BotSide, c.Threats, c.Random, new PlacementOptions(Skill.Has(BotCapability.StrongestLane), Skill.Has(BotCapability.RolePlacement) && units != null && units.SummonsRanged(magic)));
        private ScoredPlay BuildPlay(BotMagic magic, int cost, Fix64 aggression, ThinkContext c, ThreatAssessment aimed, UnitCatalogue units, IReadOnlyList<BotVisibleObject> enemies) {
            var counter = aggression == Fix64.Zero ? Fix64.Zero : (aggression > Fix64.Zero ? counters.Evaluate(magic, enemies) : counters.EvaluateVulnerability(magic, enemies)) * Fix64.Abs(aggression) * 3;
            if (!magic.Summoning.HasValue) {
                var impact = BlastTargetSelector.BestImpact(aimed.Threats, c.PlayerPosition, c.SpellStats.CastRange(magic), c.SpellStats.BlastRadius(magic), c.SpellStats.DamagePerTarget(magic));
                if (!impact.HasValue) return null;
                var hit = impact.Value;
                return c.Scored(magic, hit.Center, cost, PerCost(hit.ExpectedDamage + 8 * hit.LethalCount + counter, cost), ValueRule, "Best offensive value covers " + hit.CoveredCount + " target(s) with expected damage " + Display(hit.ExpectedDamage) + ".");
            }
            var summon = (Fix64)6;
            if (units != null && Skill.Has(BotCapability.SummonStrength) && units.AverageSummonCost > Fix64.Zero) summon *= (Fix64)cost / units.AverageSummonCost * units.RelativeEfficiency(magic, cost);
            var value = summon * (Fix64.One + (Fix64)1.5m * c.Threats.Pressure) + counter;
            return c.Scored(magic, Placement(magic, c, units), cost, PerCost(value, cost), ValueRule, "Best placement value for current defensive pressure " + Display(c.Threats.Pressure) + ".");
        }
        private static Fix64 PerCost(Fix64 value, int cost) => cost == 0 ? (value < Fix64.Zero ? Fix64.MinValue : value > Fix64.Zero ? Fix64.MaxValue : Fix64.Zero) : value / cost;
        public static string Display(Fix64 value) {
            decimal number = (decimal)value;
            decimal scale = 1;
            // Q32.32로 재변환했을 때 같은 raw 값이 되는 최소 소수 자릿수를 찾는다.
            // double을 거치면 플랫폼별 변환과 불필요한 이진 소수 잔여값이 표시될 수 있다.
            for (int digits = 0; digits <= 10; digits++, scale *= 10) {
                decimal rounded = decimal.Round(number, digits, MidpointRounding.ToEven);
                if (decimal.Truncate(rounded * 4294967296m) == value.RawValue) { number = rounded; break; }
                decimal outward = rounded + Math.Sign(number) / scale;
                if (decimal.Truncate(outward * 4294967296m) == value.RawValue) { number = outward; break; }
            }
            decimal magnitude = Math.Abs(number);
            int exponent = 0;
            if (magnitude != 0 && (magnitude < 0.001m || magnitude >= 10000000m)) {
                while (Math.Abs(number) >= 10) { number /= 10; exponent++; }
                while (Math.Abs(number) < 1) { number *= 10; exponent--; }
                return number.ToString("0.0############################", CultureInfo.InvariantCulture)
                    + "E" + exponent.ToString(CultureInfo.InvariantCulture);
            }
            return number.ToString("0.0############################", CultureInfo.InvariantCulture);
        }
    }
}
