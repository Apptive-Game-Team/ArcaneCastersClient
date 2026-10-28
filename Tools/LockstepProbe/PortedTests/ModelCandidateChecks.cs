using System;
using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    // 원본 llm_candidate_builder.rs / llm_candidates.rs / unit_guide.rs / llm_json.rs / magic_descriptions.rs 및
    // stance_decision_source.rs의 Turn 판단 테스트를 고정 소수점으로 이관한다. 기존 simulation 대응표와 별도로 센다.
    public static class ModelCandidateChecks {
        private const long FireShotId = 3, RockGolemId = 7, SlimeSummonId = 9;
        private const string Source = "game-rust/crates/server/src/bot/llm";

        private sealed class Neutral : IBotCounterScorer {
            public Fix64 Evaluate(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => Fix64.Zero;
            public Fix64 EvaluateVulnerability(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => Fix64.Zero;
        }
        private sealed class FixedCounters : IBotCounterScorer {
            private readonly Dictionary<long, Fix64> evaluate = new Dictionary<long, Fix64>(), vulnerability = new Dictionary<long, Fix64>();
            public FixedCounters Evaluate(long magicId, Fix64 score) { evaluate[magicId] = score; return this; }
            public FixedCounters Vulnerability(long magicId, Fix64 score) { vulnerability[magicId] = score; return this; }
            public Fix64 Evaluate(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => evaluate.TryGetValue(magic.Id, out var score) ? score : Fix64.Zero;
            public Fix64 EvaluateVulnerability(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => vulnerability.TryGetValue(magic.Id, out var score) ? score : Fix64.Zero;
        }
        private sealed class RecordingCounters : IBotCounterScorer {
            public readonly List<IReadOnlyList<BotVisibleObject>> Seen = new List<IReadOnlyList<BotVisibleObject>>();
            public Fix64 Evaluate(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) { Seen.Add(enemies); return Fix64.Zero; }
            public Fix64 EvaluateVulnerability(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => Fix64.Zero;
        }

        private static TestBotCatalog Catalog() => new TestBotCatalog(
            new BotMagic(FireShotId, "fire_shot"),
            new BotMagic(RockGolemId, "rock_golem_summon", summoning: new SummonedBodies("RockGolem")),
            new BotMagic(SlimeSummonId, "wind_slime_summon", summoning: new SummonedBodies("WindSlime", 2, true)));
        private static long[] FullHand() => new[] { FireShotId, RockGolemId, SlimeSummonId };
        // fire_shot은 3 마나로 사거리 10 안의 반지름 1.5에 20 피해, rock_golem_summon은 8, wind_slime_summon은 4로 둘 다 사거리 6이다.
        private static Dictionary<string, Fix64> Parameters() => new Dictionary<string, Fix64> {
            ["fire_shot.mana_cost"] = 3, ["fire_shot.damage"] = 20, ["fire_shot.radius"] = (Fix64)1.5m, ["fire_shot.range"] = 10,
            ["rock_golem_summon.mana_cost"] = 8, ["rock_golem_summon.range"] = 6,
            ["wind_slime_summon.mana_cost"] = 4, ["wind_slime_summon.range"] = 6
        };
        private static BotVisibleObject Visible(int id, Master master, string prefab, decimal x, decimal z, int hp, bool mob) =>
            new BotVisibleObject(id, master, prefab, new Vector3((Fix64)x, 0, (Fix64)z), hp, mob, true);
        // 봇은 5 마나로 좌측을 잡는다. 적 슬라임 셋과 투사체가 (1,5)의 기지로 밀고 들어오고 (3.5,4)의 하나가 가장 앞섰다.
        private static BotEye Eye() => EyeWith(new[] { FireShotId, RockGolemId }, 5);
        private static BotEye EyeWith(IEnumerable<long> cards, int mana) => new BotEye(new[] {
            Visible(1, Master.RightPlayer, "Player", 17, 5, 88, true),
            Visible(2, Master.LeftPlayer, "Player", 1, 5, 90, true),
            Visible(10, Master.RightPlayer, "FireSlime", 12, 4, 30, true),
            Visible(11, Master.RightPlayer, "FireSlime", 6, 6, 25, true),
            Visible(13, Master.RightPlayer, "FireSlime", (decimal)6.5, (decimal)5.5, 15, true),
            Visible(14, Master.RightPlayer, "FireSlime", (decimal)3.5, 4, 20, true),
            Visible(12, Master.RightPlayer, "FireShot", 5, 5, 10, false),
            Visible(20, Master.LeftPlayer, "FireSlime", 4, 5, 40, true)
        }, cards, mana, 88);
        // 적 슬라임 하나만 길 끝에 있어 급한 일이 없는 판이다.
        private static BotEye QuietEye(IEnumerable<long> cards, int mana) => new BotEye(new[] {
            Visible(1, Master.RightPlayer, "Player", 17, 5, 88, true),
            Visible(2, Master.LeftPlayer, "Player", 1, 5, 90, true),
            Visible(10, Master.RightPlayer, "FireSlime", 14, 4, 30, true)
        }, cards, mana, 88);
        private static Func<JavaRandom> Seeded(long seed) => () => new JavaRandom(seed);
        private static ModelCandidateBuilder Builder() => BuilderWith(new Neutral());
        private static ModelCandidateBuilder BuilderWith(IBotCounterScorer counters) =>
            new ModelCandidateBuilder(Catalog(), counters, Seeded(7));
        private static ModelCandidateBuilder CardBuilder(params KeyValuePair<string, string>[] texts) {
            var byName = new Dictionary<string, string>();
            foreach (var text in texts) byName[text.Key] = text.Value;
            return ModelCandidateBuilder.Cards(Catalog(), new Neutral(), Seeded(7), new MagicDescriptions(byName));
        }
        private static ModelCandidateBuilder CardBuilderWithText() =>
            CardBuilder(new KeyValuePair<string, string>("fire_shot", "Fires a test bolt."));
        private static ModelCandidateMenu Candidates() => Builder().Build(Eye(), Parameters(), Master.LeftPlayer, null);
        private static InputDecision Decision(long magicId, decimal x, decimal z, string ruleId, string reason) =>
            new InputDecision(magicId, new Vector3((Fix64)x, 0, (Fix64)z), ruleId, reason);
        private static ModelCandidatePlay Play(ModelCandidateMenu menu, long magicId) {
            foreach (var play in menu.Plays) if (play.MagicId == magicId) return play;
            throw new Exception("No play of magic " + magicId);
        }
        private static string[] Keys(ModelCandidateMenu menu) => menu.Plays.Select(play => play.Key).ToArray();
        private static void Contains(string text, string fragment) {
            if (!text.Contains(fragment)) throw new Exception("Expected \"" + fragment + "\" in \"" + text + "\"");
        }
        private static void StartsWith(string text, string prefix) {
            if (!text.StartsWith(prefix, StringComparison.Ordinal)) throw new Exception("Expected prefix \"" + prefix + "\" in \"" + text + "\"");
        }
        private static void EndsWith(string text, string suffix) {
            if (!text.EndsWith(suffix, StringComparison.Ordinal)) throw new Exception("Expected suffix \"" + suffix + "\" in \"" + text + "\"");
        }

        // ---- llm_candidate_builder.rs ----

        private static void OffersCompletePlaysOfAffordableMagicsOnly() {
            var candidates = Candidates();
            // 골렘은 5 마나에 8이라 빠지고, fire_shot은 최선의 폭발과 가장 앞선 슬라임을 얻는다.
            Assert.Sequence(new[] { "p0", "p1" }, Keys(candidates));
            var best = candidates.Play("p0");
            Assert.Equal(FireShotId, best.MagicId);
            Assert.Equal(3, best.ManaCost);
            Assert.Equal(new Vector3(6, 0, 6), best.Target);
            Assert.Equal("fire_shot (damage spell, cost 3, 20 damage in radius 1.5) at (6, 6), 5.1 from our base: "
                + "hits 2 enemy units (FireSlime, FireSlime) for 35 total damage (11.7 per mana), kills 1. "
                + "Neutral against the enemy units on the field (counter score 0).", best.Description);
            var advanced = candidates.Play("p1");
            Assert.Equal(new Vector3((Fix64)3.5m, 0, 4), advanced.Target);
            Contains(advanced.Description, "at (3.5, 4), 2.7 from our base: hits 1 enemy unit (FireSlime)");
            Contains(advanced.Description, "kills 1");
        }

        private static void SummonPlaysSayWhatTheyPutDownAndWhatTheyBlock() {
            var candidates = Builder().Build(EyeWith(FullHand(), 9), Parameters(), Master.LeftPlayer, null);
            StartsWith(Play(candidates, SlimeSummonId).Description,
                "wind_slime_summon (summon, cost 4) at (2.6, 5), 1.6 from our base: puts 2 WindSlime (flying) in lane z=5; "
                + "blocks the nearest enemy FireSlime (2.7 from our base); pressure on our base is medium (0.69).");
            Contains(Play(candidates, RockGolemId).Description, "puts 1 RockGolem in lane");
        }

        private static void SaysPlainlyHowEachMagicMatchesTheEnemyUnits() {
            var counters = new FixedCounters().Evaluate(FireShotId, 3).Vulnerability(SlimeSummonId, (Fix64)1.5m);
            var candidates = BuilderWith(counters).Build(EyeWith(FullHand(), 9), Parameters(), Master.LeftPlayer, null);
            EndsWith(Play(candidates, FireShotId).Description, "Strong against the enemy units on the field (counter score +3).");
            EndsWith(Play(candidates, SlimeSummonId).Description, "Weak against the enemy units on the field (counter score -1.5).");
            EndsWith(Play(candidates, RockGolemId).Description, "Neutral against the enemy units on the field (counter score 0).");
        }

        private static void CounterIsScoredOnEnemyUnitsOnly() {
            var counters = new RecordingCounters();
            BuilderWith(counters).Build(Eye(), Parameters(), Master.LeftPlayer, null);
            Assert.True(counters.Seen.Count > 0);
            foreach (var item in counters.Seen[0]) {
                Assert.Equal("FireSlime", item.Prefab);
                Assert.Equal(Master.RightPlayer, item.Master);
            }
        }

        private static void OffersWaitOnlyWhenNothingIsUrgentAndADearerMagicIsInHand() {
            var saving = Builder().Build(QuietEye(FullHand(), 5), Parameters(), Master.LeftPlayer, null);
            Assert.True(saving.OffersWait);
            Assert.Equal("Cast nothing now and save mana for rock_golem_summon (cost 8), mana is 5 now; "
                + "nothing is urgent: the nearest enemy unit is 13 from our base.", saving.Criteria().GetText(ModelProtocol.WaitPlayKey));
            Assert.Equal(ModelProtocol.WaitPlayKey, saving.Criteria().Keys[saving.Criteria().Count - 1]);

            var rich = Builder().Build(QuietEye(FullHand(), 9), Parameters(), Master.LeftPlayer, null);
            Assert.True(!rich.OffersWait);

            var pushed = Builder().Build(EyeWith(FullHand(), 5), Parameters(), Master.LeftPlayer, null);
            Assert.True(!pushed.OffersWait);
            Assert.True(!pushed.Criteria().ContainsKey(ModelProtocol.WaitPlayKey));
        }

        private static void TheRuleBrainsPlayComesFirstMarkedAsTheStandardTactic() {
            var rule = Decision(FireShotId, 5, 5, "combo.mob-cluster", "AbstractExplosionMagic targets a cluster of 3 enemy mobs.");
            var candidates = Builder().Build(Eye(), Parameters(), Master.LeftPlayer, rule);
            Assert.Equal(3, candidates.Plays.Count);
            var standard = candidates.Play("p0");
            Assert.Equal(new Vector3(5, 0, 5), standard.Target);
            StartsWith(standard.Description,
                "STANDARD TACTIC (what the rule bot would do: AbstractExplosionMagic targets a cluster of 3 enemy mobs): "
                + "fire_shot (damage spell, cost 3, 20 damage in radius 1.5) at (5, 5), 4 from our base: hits 1 enemy unit (FireSlime)");
            Assert.True(standard.Standard);
        }

        private static void TheRuleBrainsPlayReplacesTheSamePlayInsteadOfRepeatingIt() {
            var rule = Decision(FireShotId, 6, (decimal)6.5, "score.best-value", "Best offensive value covers 2 target(s).");
            var candidates = Builder().Build(Eye(), Parameters(), Master.LeftPlayer, rule);
            Assert.Sequence(new[] { "p0", "p1" }, Keys(candidates));
            StartsWith(candidates.Play("p0").Description, "STANDARD TACTIC");
            Assert.Equal(new Vector3(6, 0, (Fix64)6.5m), candidates.Play("p0").Target);
            Assert.Equal(new Vector3((Fix64)3.5m, 0, 4), candidates.Play("p1").Target);
        }

        private static void AnUnknownOrUnaffordableRuleMagicIsLeftOut() {
            foreach (var rule in new[] { Decision(999, 5, 5, "x", "y"), Decision(RockGolemId, 5, 5, "x", "y") }) {
                var candidates = Builder().Build(Eye(), Parameters(), Master.LeftPlayer, rule);
                foreach (var entry in candidates.Criteria().Entries)
                    Assert.True(!(entry.Value.AsText ?? "").StartsWith(ModelCandidateBuilder.StandardTactic, StringComparison.Ordinal));
            }
        }

        private static void AnEnemyBaseInRangeIsAimedAtAndTheMostAdvancedUnitStaysAnOption() {
            var eye = new BotEye(new[] {
                Visible(1, Master.LeftPlayer, "Player", 1, 5, 60, true),
                Visible(2, Master.RightPlayer, "Player", 17, 5, 88, true),
                Visible(11, Master.LeftPlayer, "FireSlime", 12, 5, 25, true)
            }, new[] { FireShotId }, 5, 60);
            var parameters = Parameters(); parameters["fire_shot.range"] = 17;
            var candidates = Builder().Build(eye, parameters, Master.RightPlayer, null);
            Assert.Equal(2, candidates.Plays.Count);
            Contains(candidates.Play("p0").Description, "at (1, 5), 16 from our base: hits the enemy base for 20 total damage");
            Contains(candidates.Play("p1").Description,
                "at (12, 5), 5 from our base: hits 1 enemy unit (FireSlime) for 20 total damage (6.7 per mana), kills 0");
            Assert.Equal("right", candidates.State.GetText("side"));
            Assert.Equal(88, candidates.State.Get("ourHp").AsNumberInt);
            Assert.Equal(60, candidates.State.Get("enemyHp").AsNumberInt);
        }

        private static void CapsThePlays() {
            var magics = new List<BotMagic>();
            var hand = new List<long>();
            var parameters = new Dictionary<string, Fix64>();
            for (long id = 100; id < 120; id++) {
                string name = "bolt_" + id;
                magics.Add(new BotMagic(id, name)); hand.Add(id);
                parameters[name + ".mana_cost"] = 1; parameters[name + ".range"] = 10; parameters[name + ".radius"] = 1;
            }
            var candidates = new ModelCandidateBuilder(new TestBotCatalog(magics.ToArray()), null, Seeded(1))
                .Build(EyeWith(hand, 20), parameters, Master.LeftPlayer, null);
            Assert.Equal(ModelCandidateBuilder.MaxPlays, candidates.Plays.Count);
            Assert.True(candidates.Play("p15") != null);
        }

        private static void TheStateIsCompactAndCarriesBothHpAndThePressure() {
            var state = Candidates().State;
            Assert.Sequence(new[] { "side", "ourHp", "enemyHp", "mana", "pressure", "enemyNearOurBase", "hand",
                "handSlotsUsed", "enemyUnits", "ourUnits", "unitGuide" }, state.Keys);
            Assert.Equal(90, state.Get("ourHp").AsNumberInt);
            Assert.Equal(88, state.Get("enemyHp").AsNumberInt);
            Assert.Equal(5, state.Get("mana").AsNumberInt);
            Assert.Equal("0.69", state.Get("pressure").Write());
            Assert.Equal(true, state.Get("enemyNearOurBase").AsBool);
            var enemies = state.Get("enemyUnits").AsList;
            // 투사체와 적 플레이어 본체는 유닛이 아니다.
            Assert.Equal(4, enemies.Count);
            var first = enemies[0].AsMap;
            Assert.Sequence(new[] { "type", "hp", "toOurBase", "z" }, first.Keys);
            Assert.Equal("FireSlime", first.GetText("type"));
            Assert.Equal(20, first.Get("hp").AsNumberInt);
            Assert.Equal("2.7", first.Get("toOurBase").Write());
            Assert.Equal("4.0", first.Get("z").Write());
            Assert.Equal("13.0", state.Get("ourUnits").AsList[0].AsMap.Get("toEnemyBase").Write());
            Assert.Equal("{\"type\":\"FireSlime\",\"hp\":20,\"toOurBase\":2.7,\"z\":4.0}", first.Write());
        }

        private static void NothingAffordableMeansNothingToAsk() {
            Assert.True(!Builder().Build(EyeWith(FullHand(), 2), Parameters(), Master.LeftPlayer, null).HasPlays);
        }

        // ---- llm_candidate_builder.rs, LlmCardMenuTest ----

        private static void OffersOneOptionPerAffordableCardKeyedByItsNameAndWaiting() {
            var cards = CardBuilderWithText().Build(EyeWith(new[] { FireShotId, SlimeSummonId, RockGolemId }, 5),
                Parameters(), Master.LeftPlayer, null);
            var keys = cards.Criteria().Keys.ToList(); keys.Sort(StringComparer.Ordinal);
            Assert.Sequence(new[] { "fire_shot", "wait", "wind_slime_summon" }, keys);
        }

        private static void ASpellSaysWhatTheCardDoesAndWhatItWouldHitNow() {
            var cards = CardBuilderWithText().Build(Eye(), Parameters(), Master.LeftPlayer, null);
            string fireShot = cards.Describe("fire_shot");
            StartsWith(fireShot, "fire_shot (spell, 3 mana, range 10, radius 1.5, 20 damage to each target in the radius): Fires a test bolt.");
            Contains(fireShot, "Right now its best target hits");
        }

        private static void AUnitThatNeverMovesSaysSoAndCountsTheEnemiesItWouldReach() {
            var parameters = Parameters();
            parameters["wind_slime.hp"] = 100; parameters["wind_slime.push_force"] = 3; parameters["wind_slime.push_range_x"] = 6;
            var cards = CardBuilderWithText().Build(EyeWith(new[] { SlimeSummonId }, 5), parameters, Master.LeftPlayer, null);
            string summon = cards.Describe("wind_slime_summon");
            Contains(summon, "stays where it is placed and never moves");
            Contains(summon, "blows enemy units back");
            Contains(summon, "within its reach of 6 now");
        }

        private static void ASpellWithNothingToHitIsLeftOut() {
            var cards = CardBuilderWithText().Build(QuietEye(new[] { FireShotId, SlimeSummonId }, 5), Parameters(), Master.LeftPlayer, null);
            Assert.Sequence(new[] { "wind_slime_summon", "wait" }, cards.Criteria().Keys);
        }

        private static void TheStateCountsCopiesAndHandSlotsAndWaitingSaysAFullHandDrawsNothing() {
            var fullHand = new[] { FireShotId, FireShotId, RockGolemId, RockGolemId, RockGolemId, SlimeSummonId };
            var cards = CardBuilderWithText().Build(EyeWith(fullHand, 5), Parameters(), Master.LeftPlayer, null);
            Assert.Equal("6 of 6", cards.State.GetText("handSlotsUsed"));
            string hand = cards.State.Get("hand").Write();
            Contains(hand, "{\"magic\":\"fire_shot\",\"cost\":3,\"copies\":2}");
            Contains(hand, "{\"magic\":\"rock_golem_summon\",\"cost\":8,\"copies\":3}");
            Contains(cards.WaitDescription, "rock_golem_summon (8 mana)");
            Contains(cards.WaitDescription, "The hand is full, so no new card is drawn");
        }

        private static void NoOptionIsMarkedAsTheRuleBotsPlay() {
            var rule = Decision(FireShotId, 6, (decimal)6.5, "rule.test", "rule");
            var cards = CardBuilderWithText().Build(Eye(), Parameters(), Master.LeftPlayer, rule);
            foreach (var entry in cards.Criteria().Entries)
                Assert.True(!(entry.Value.AsText ?? "").Contains(ModelCandidateBuilder.StandardTactic));
        }

        private static void ASpellIsAimedByCodeAndAsksNoAimQuestion() {
            var cards = CardBuilderWithText().Build(Eye(), Parameters(), Master.LeftPlayer, null);
            Assert.Equal(0, cards.Play("fire_shot").Aims.Count);
        }

        private static void ASummonOffersLanesAtTwoDepthsAndBlocking() {
            var cards = CardBuilderWithText().Build(QuietEye(new[] { SlimeSummonId }, 5), Parameters(), Master.LeftPlayer, null);
            var slime = cards.Play("wind_slime_summon");
            foreach (string key in new[] { "block", "near_low", "near_high" }) Assert.True(slime.Aim(key) != null);
            var nearLow = slime.Aim("near_low");
            Assert.Equal((Fix64)2, nearLow.Target.Z);
            StartsWith(nearLow.Description, "Near our base in the low lane (z=2)");
            foreach (var aim in slime.Aims)
                Assert.True(Vector3.Distance(aim.Target, new Vector3(1, 0, 5)) <= (Fix64)6.01m);
        }

        private static void TheChosenAimNamesThePointTheSummonIsPut() {
            var cards = CardBuilderWithText().Build(QuietEye(new[] { SlimeSummonId }, 5), Parameters(), Master.LeftPlayer, null);
            var slime = cards.Play("wind_slime_summon");
            Assert.Equal(slime.Aim("near_high").Target, slime.TargetFor("near_high"));
            // 모르는 조준은 카드가 가진 지점으로 돌아간다.
            Assert.Equal(slime.Target, slime.TargetFor("nowhere"));
            Assert.Equal(slime.Target, slime.TargetFor(null));
        }

        // 원본 card_builder_bundled: 묶인 카드 설명문을 쓰는 실제 기본 경로다.
        private static ModelCandidateBuilder CardBuilderBundled() =>
            ModelCandidateBuilder.Cards(Catalog(), new Neutral(), Seeded(7));

        private static void TheBundledCardTextIsUsedByTheCardBuilder() {
            var cards = CardBuilderBundled().Build(EyeWith(new[] { SlimeSummonId }, 5), Parameters(), Master.LeftPlayer, null);
            Assert.True(cards.Play("wind_slime_summon") != null);
        }

        private static void TheBundledCardTextCoversTheMagicBook() {
            var bundled = MagicDescriptions.Bundled;
            StartsWith(bundled.Describe("wind_totem"), "Places a wind totem that repeatedly pushes enemy ground units");
            Assert.True(bundled.Describe("no_such_magic") == null);
            Assert.True(bundled.Describe("aqua_archer") != null);
        }

        // ---- stance_decision_source.rs, Turn ----

        private static InputDecision StanceRule() => Decision(FireShotId, 6, (decimal)6.5, "rule.test", "rule");
        private static ModelCandidateMenu StanceCandidates(BotEye eye, Dictionary<string, Fix64> parameters) =>
            Builder().Build(eye, parameters, Master.LeftPlayer, null);

        private static void AttackHitsTheEnemyBaseWhenASpellReachesIt() {
            var parameters = Parameters(); parameters["fire_shot.range"] = 20;
            var eye = QuietEye(new[] { FireShotId, SlimeSummonId }, 5);
            var menu = StanceCandidates(eye, parameters);
            var decision = new ModelStanceTurn(eye, menu, StanceRule()).Play(ModelStance.Attack);
            Assert.True(decision != null);
            Assert.Equal(FireShotId, decision.MagicId);
            Assert.Equal(new Vector3(17, 0, 5), decision.Target);
            Assert.Equal("llm.jev.attack", decision.RuleId);
        }

        private static void AttackSummonsToPushWhenNothingReachesTheBase() {
            var eye = QuietEye(new[] { FireShotId, SlimeSummonId }, 5);
            var menu = StanceCandidates(eye, Parameters());
            var decision = new ModelStanceTurn(eye, menu, StanceRule()).Play(ModelStance.Attack);
            Assert.True(decision != null);
            Assert.Equal(SlimeSummonId, decision.MagicId);
        }

        private static void SaveHoldsUntilTheDearestCardIsAffordable() {
            var cards = new[] { FireShotId, RockGolemId };
            var poor = QuietEye(cards, 5);
            var rich = QuietEye(cards, 9);
            Assert.True(new ModelStanceTurn(poor, StanceCandidates(poor, Parameters()), StanceRule()).Play(ModelStance.Save) == null);
            var decision = new ModelStanceTurn(rich, StanceCandidates(rich, Parameters()), StanceRule()).Play(ModelStance.Save);
            Assert.True(decision != null);
            Assert.Equal(RockGolemId, decision.MagicId);
        }

        private static void AnEnemyCloseToTheBaseOverridesAttackAndSave() {
            var eye = Eye();
            var turn = new ModelStanceTurn(eye, StanceCandidates(eye, Parameters()), StanceRule());
            Assert.True(turn.Urgent);
            Assert.Equal("rule.test", turn.Play(ModelStance.Attack).RuleId);
            Assert.Equal("rule.test", turn.Play(ModelStance.Save).RuleId);
        }

        private static void CriteriaOfferEveryStance() {
            var eye = QuietEye(new[] { FireShotId, RockGolemId }, 5);
            var criteria = new ModelStanceTurn(eye, StanceCandidates(eye, Parameters()), StanceRule()).Criteria();
            Assert.Sequence(new[] { "attack", "defend", "save" }, criteria.Keys);
            string save = criteria.GetText("save");
            Contains(save, "cost 8");
            Contains(save, "mana is 5");
        }

        // ---- unit_guide.rs ----

        private static ModelUnitGuide Guide() => new ModelUnitGuide(new Dictionary<string, Fix64> {
            ["wind_spirit.hp"] = 50, ["wind_spirit.damage"] = 300, ["wind_spirit.attack_interval"] = 3,
            ["wind_spirit.attack_range"] = (Fix64)1.5m, ["wind_spirit.speed"] = (Fix64)1.5m, ["wind_spirit.spawn_height"] = 3
        });
        private static void ReadsTheGameObjectNamedLikeThePrefabWithoutItsSuffix() {
            Assert.Equal("wind_spirit", ModelUnitGuide.ObjectName("WindSpirit"));
        }
        private static void DescribesAUnitFromItsParameters() {
            Assert.Equal("flying, 50 hp, hits for 300 every 3s (100 dps) at range 1.5, speed 1.5", Guide().Describe("WindSpirit"));
        }
        private static void TheRoleSaysHowTheUnitMovesAndHowHardItHits() {
            Assert.Equal("flies at speed 1.5; 50 hp; hits for 300 every 3s (100 dps) at range 1.5", Guide().Role("WindSpirit"));
        }
        private static void ATypeWithNoRowsIsUnknownRatherThanGuessed() {
            Assert.Equal("no stats known", Guide().Describe("FireSlime"));
        }
        private static void SecondsToReachSubtractTheAttackRange() {
            var stats = Guide().Stats("WindSpirit");
            Assert.Equal((Fix64)4, stats.SecondsToReach((Fix64)7.5m).Value);
            Assert.Equal(Fix64.Zero, stats.SecondsToReach(Fix64.One).Value);
        }
        private static void RoundsAndPrintsNumbersAsJavaDoes() {
            Assert.Equal(0L, ModelText.JavaRound(Fix64.FromRaw(2147483647)));
            Assert.Equal(-2L, ModelText.JavaRound((Fix64)(-2.5m)));
            Assert.Equal(3L, ModelText.JavaRound((Fix64)2.5m));
            Assert.Equal("5.1", ModelText.Number(Fix64.Sqrt((Fix64)26)));
            Assert.Equal("13", ModelText.Number((Fix64)13));
            Assert.Equal("-1.5", ModelText.Number((Fix64)(-1.5m)));
            Assert.Equal("0", ModelText.Number((Fix64)(-0.04m)));
            Assert.Equal("0.69", ModelText.Decimal(ModelText.Round2Hundredths((Fix64)0.6851851851851851m), 2));
        }

        // ---- llm_answer.rs ----

        private static void CollapsesWhitespaceAndCutsLongText() {
            Assert.Equal("a b", ModelText.Truncate("a \n\t b"));
            Assert.Equal(ModelText.MaxReasonLength, ModelText.Truncate(new string('x', 300)).Length);
        }

        // ---- llm_json.rs ----

        private static void WritesKeysInInsertionOrderAndNumbersWithTheirJavaType() {
            var map = new ModelJsonMap();
            map.Put("z", 1L); map.Put("a", ModelJson.Decimal(130, 1)); map.Put("m", ModelJson.Null); map.Put("z", 2L);
            Assert.Equal("{\"z\":2,\"a\":13.0,\"m\":null}", map.Write());
        }

        // ---- magic_descriptions.rs ----

        private static void ReadsPropertiesAsJavaDoes() {
            string text = "# comment\n! also a comment\n\n  key1 = value one\nkey2:value two\nkey3 value three\n"
                + "long = first \\\n      second\\\\\nescaped\\ key=tab\\there \\u00e9\\uD83D\\uDE00\r\n"
                + "key1=replaced\nempty\n";
            var entries = MagicDescriptions.ParseProperties(text);
            Assert.Sequence(new[] { "key1", "key2", "key3", "long", "escaped key", "empty" }, entries.Select(entry => entry.Key));
            Assert.Sequence(new[] { "replaced", "value two", "value three", "first second\\", "tab\there é😀", "" },
                entries.Select(entry => entry.Value));
        }
        private static void AMalformedUnicodeEscapeFailsTheLoad() {
            bool failed = false;
            try { MagicDescriptions.ParseProperties("key=\\u12"); } catch (System.IO.InvalidDataException) { failed = true; }
            Assert.True(failed);
        }

        public static int Run() {
            var checks = new (string, Action)[] {
                (Source + "/llm_candidate_builder.rs::offers_complete_plays_of_affordable_magics_only", OffersCompletePlaysOfAffordableMagicsOnly),
                (Source + "/llm_candidate_builder.rs::summon_plays_say_what_they_put_down_and_what_they_block", SummonPlaysSayWhatTheyPutDownAndWhatTheyBlock),
                (Source + "/llm_candidate_builder.rs::says_plainly_how_each_magic_matches_the_enemy_units", SaysPlainlyHowEachMagicMatchesTheEnemyUnits),
                (Source + "/llm_candidate_builder.rs::counter_is_scored_on_enemy_units_only", CounterIsScoredOnEnemyUnitsOnly),
                (Source + "/llm_candidate_builder.rs::offers_wait_only_when_nothing_is_urgent_and_a_dearer_magic_is_in_hand", OffersWaitOnlyWhenNothingIsUrgentAndADearerMagicIsInHand),
                (Source + "/llm_candidate_builder.rs::the_rule_brains_play_comes_first_marked_as_the_standard_tactic", TheRuleBrainsPlayComesFirstMarkedAsTheStandardTactic),
                (Source + "/llm_candidate_builder.rs::the_rule_brains_play_replaces_the_same_play_instead_of_repeating_it", TheRuleBrainsPlayReplacesTheSamePlayInsteadOfRepeatingIt),
                (Source + "/llm_candidate_builder.rs::an_unknown_or_unaffordable_rule_magic_is_left_out", AnUnknownOrUnaffordableRuleMagicIsLeftOut),
                (Source + "/llm_candidate_builder.rs::an_enemy_base_in_range_is_aimed_at_and_the_most_advanced_unit_stays_an_option", AnEnemyBaseInRangeIsAimedAtAndTheMostAdvancedUnitStaysAnOption),
                (Source + "/llm_candidate_builder.rs::caps_the_plays", CapsThePlays),
                (Source + "/llm_candidate_builder.rs::the_state_is_compact_and_carries_both_hp_and_the_pressure", TheStateIsCompactAndCarriesBothHpAndThePressure),
                (Source + "/llm_candidate_builder.rs::nothing_affordable_means_nothing_to_ask", NothingAffordableMeansNothingToAsk),
                (Source + "/llm_candidate_builder.rs::offers_one_option_per_affordable_card_keyed_by_its_name_and_waiting", OffersOneOptionPerAffordableCardKeyedByItsNameAndWaiting),
                (Source + "/llm_candidate_builder.rs::a_spell_says_what_the_card_does_and_what_it_would_hit_now", ASpellSaysWhatTheCardDoesAndWhatItWouldHitNow),
                (Source + "/llm_candidate_builder.rs::a_unit_that_never_moves_says_so_and_counts_the_enemies_it_would_reach", AUnitThatNeverMovesSaysSoAndCountsTheEnemiesItWouldReach),
                (Source + "/llm_candidate_builder.rs::a_spell_with_nothing_to_hit_is_left_out", ASpellWithNothingToHitIsLeftOut),
                (Source + "/llm_candidate_builder.rs::the_state_counts_copies_and_hand_slots_and_waiting_says_a_full_hand_draws_nothing", TheStateCountsCopiesAndHandSlotsAndWaitingSaysAFullHandDrawsNothing),
                (Source + "/llm_candidate_builder.rs::no_option_is_marked_as_the_rule_bots_play", NoOptionIsMarkedAsTheRuleBotsPlay),
                (Source + "/llm_candidate_builder.rs::a_spell_is_aimed_by_code_and_asks_no_aim_question", ASpellIsAimedByCodeAndAsksNoAimQuestion),
                (Source + "/llm_candidate_builder.rs::a_summon_offers_lanes_at_two_depths_and_blocking", ASummonOffersLanesAtTwoDepthsAndBlocking),
                (Source + "/llm_candidate_builder.rs::the_chosen_aim_names_the_point_the_summon_is_put", TheChosenAimNamesThePointTheSummonIsPut),
                (Source + "/llm_candidate_builder.rs::the_bundled_card_text_is_used_by_the_card_builder", TheBundledCardTextIsUsedByTheCardBuilder),
                (Source + "/magic_descriptions.rs::the_bundled_card_text_covers_the_magic_book", TheBundledCardTextCoversTheMagicBook),
                (Source + "/stance_decision_source.rs::attack_hits_the_enemy_base_when_a_spell_reaches_it", AttackHitsTheEnemyBaseWhenASpellReachesIt),
                (Source + "/stance_decision_source.rs::attack_summons_to_push_when_nothing_reaches_the_base", AttackSummonsToPushWhenNothingReachesTheBase),
                (Source + "/stance_decision_source.rs::save_holds_until_the_dearest_card_is_affordable", SaveHoldsUntilTheDearestCardIsAffordable),
                (Source + "/stance_decision_source.rs::an_enemy_close_to_the_base_overrides_attack_and_save", AnEnemyCloseToTheBaseOverridesAttackAndSave),
                (Source + "/stance_decision_source.rs::criteria_offer_every_stance", CriteriaOfferEveryStance),
                (Source + "/unit_guide.rs::reads_the_game_object_named_like_the_prefab_without_its_suffix", ReadsTheGameObjectNamedLikeThePrefabWithoutItsSuffix),
                (Source + "/unit_guide.rs::describes_a_unit_from_its_parameters", DescribesAUnitFromItsParameters),
                (Source + "/unit_guide.rs::the_role_says_how_the_unit_moves_and_how_hard_it_hits", TheRoleSaysHowTheUnitMovesAndHowHardItHits),
                (Source + "/unit_guide.rs::a_type_with_no_rows_is_unknown_rather_than_guessed", ATypeWithNoRowsIsUnknownRatherThanGuessed),
                (Source + "/unit_guide.rs::seconds_to_reach_subtract_the_attack_range", SecondsToReachSubtractTheAttackRange),
                (Source + "/unit_guide.rs::rounds_and_prints_numbers_as_java_does", RoundsAndPrintsNumbersAsJavaDoes),
                (Source + "/llm_answer.rs::collapses_whitespace_and_cuts_long_text", CollapsesWhitespaceAndCutsLongText),
                (Source + "/llm_json.rs::writes_keys_in_insertion_order_and_numbers_with_their_java_type", WritesKeysInInsertionOrderAndNumbersWithTheirJavaType),
                (Source + "/magic_descriptions.rs::reads_properties_as_java_does", ReadsPropertiesAsJavaDoes),
                (Source + "/magic_descriptions.rs::a_malformed_unicode_escape_fails_the_load", AMalformedUnicodeEscapeFailsTheLoad)
            };
            var failures = new List<string>();
            int passed = 0;
            foreach (var check in checks) {
                try { check.Item2(); passed++; Console.WriteLine("MODEL PASS " + check.Item1); }
                catch (Exception exception) { failures.Add(check.Item1 + ": " + exception); }
            }
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("\n", failures));
            Console.WriteLine("PASS model-original tests=" + passed);
            return passed;
        }
    }
}
