using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class BotBrainParityTests {
        private const string Source = "game/domain/bot/bot_brain.rs";
        [PortedTest(Source)] public static void renders_doubles_the_way_java_does() {
            Assert.Equal("20.0", BotBrain.Display(20));
            Assert.Equal("0.25", BotBrain.Display((Fix64)0.25m));
            Assert.Equal("0.0", BotBrain.Display(0));
            Assert.Equal("1.0E7", BotBrain.Display(10000000));
            Assert.Equal("1.5E-4", BotBrain.Display((Fix64)0.00015m));
            Assert.Equal("123456.789", BotBrain.Display((Fix64)123456.789m));
            var saved = System.Globalization.CultureInfo.CurrentCulture;
            try {
                System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
                Assert.Equal("-1.5E-4", BotBrain.Display((Fix64)(-0.00015m)));
                Assert.Equal("0.001", BotBrain.Display((Fix64)0.001m));
                foreach (var value in new[] { Fix64.MinValue, Fix64.MaxValue, Fix64.FromRaw(1), Fix64.FromRaw(-1), (Fix64)0.1m, (Fix64)(-123456.789m) }) {
                    var parsed = decimal.Parse(BotBrain.Display(value), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
                    Assert.Equal(value.RawValue, ((Fix64)parsed).RawValue);
                }
            } finally { System.Globalization.CultureInfo.CurrentCulture = saved; }
        }
        private sealed class Catalog : IBotMagicCatalog {
            public readonly List<BotMagic> Known = new List<BotMagic> { Summon(1, "favourable"), Summon(2, "losing"), new BotMagic(3, "shot"), Summon(4, "enemy_board") };
            public BotMagic Price = Summon(4, "enemy_board");
            public BotMagic GetMagic(long id) => Known.FirstOrDefault(m => m.Id == id);
            public IEnumerable<BotMagic> GetAllMagics() => new[] { Price };
        }
        private sealed class Scorer : IBotCounterScorer {
            public int Asked;
            public Fix64 Evaluate(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) { Asked++; return magic.Id == 1 ? 10 : 0; }
            public Fix64 EvaluateVulnerability(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) { Asked++; return magic.Id == 2 ? 10 : 0; }
        }
        private static BotMagic Summon(long id, string name, int count = 1) => new BotMagic(id, name, summoning: new SummonedBodies("FireSpirit", count));
        private static Dictionary<string, Fix64> Costs() => new Dictionary<string, Fix64> { ["favourable.mana_cost"] = 5, ["losing.mana_cost"] = 5, ["shot.mana_cost"] = 5, ["enemy_board.mana_cost"] = 25 };
        private static BotEye Eye(params long[] cards) => new BotEye(new[] { new BotVisibleObject(1, Master.RightPlayer, "FireSpirit", new Vector3(), 10, true, true) }, cards, 100, 100);
        private static BotBrain Brain(int aggression = 1, bool hospitality = false, Catalog catalog = null, Scorer scorer = null) {
            var brain = new BotBrain(catalog ?? new Catalog(), scorer ?? new Scorer(), new BotPersona { Tier = hospitality ? BotTier.Hospitality : BotTier.Elite, CounterAggression = aggression, ThinkingTimeMs = 0, ReactionIntervalFrames = 1, Hospitality = hospitality }, new JavaRandom(1));
            brain.ThinkFailed += error => throw error; return brain;
        }
        [PortedTest(Source)] public static void positive_aggression_picks_the_play_that_beats_the_field() => Assert.Equal(1L, Brain().Think(Eye(1, 2), Costs(), Master.LeftPlayer).MagicId);
        [PortedTest(Source)] public static void negative_aggression_picks_the_play_the_field_answers_best() => Assert.Equal(2L, Brain(-1).Think(Eye(1, 2), Costs(), Master.LeftPlayer).MagicId);
        [PortedTest(Source)] public static void negative_aggression_still_casts() => Assert.True(Brain(-1).Think(Eye(1, 2), Costs(), Master.LeftPlayer) != null);
        [PortedTest(Source)] public static void neutral_aggression_asks_for_neither_direction() {
            var scorer = new Scorer(); Brain(0, scorer: scorer).Think(Eye(1, 2), Costs(), Master.LeftPlayer); Assert.Equal(0, scorer.Asked);
        }
        [PortedTest(Source)] public static void hospitality_never_casts_a_non_summon() => Assert.Equal(2L, Brain(-1, true).Think(Eye(3, 2), Costs(), Master.LeftPlayer).MagicId);
        [PortedTest(Source)] public static void hospitality_prefers_the_play_the_field_answers_even_with_a_positive_persona_value() => Assert.Equal(2L, Brain(1, true).Think(Eye(1, 2), Costs(), Master.LeftPlayer).MagicId);
        [PortedTest(Source)] public static void hospitality_only_casts_summons_cheaper_than_the_enemy_board() {
            var catalog = new Catalog(); catalog.Known.Add(Summon(5, "too_big")); var costs = Costs(); costs["too_big.mana_cost"] = 25;
            Assert.Equal(2L, Brain(-1, true, catalog).Think(Eye(5, 2), costs, Master.LeftPlayer).MagicId);
        }
        [PortedTest(Source)] public static void hospitality_refuses_to_summon_when_nothing_stays_under_the_board() => Assert.True(Brain(-1, true, new Catalog { Price = Summon(2, "losing") }).Think(Eye(1, 2), Costs(), Master.LeftPlayer, true) == null);
        [PortedTest(Source)] public static void ordinary_tiers_ignore_the_enemy_board_cap() => Assert.Equal(1L, Brain(1, false, new Catalog { Price = Summon(2, "losing") }).Think(Eye(1, 2), Costs(), Master.LeftPlayer).MagicId);
        [PortedTest(Source)] public static void a_swarm_body_is_worth_its_share_of_the_cast() {
            var catalog = new Catalog { Price = Summon(6, "swarm", 5) }; catalog.Known.Add(catalog.Price); var costs = Costs(); costs["swarm.mana_cost"] = 25;
            Assert.True(Brain(-1, true, catalog).Think(Eye(1, 2), costs, Master.LeftPlayer, true) == null);
        }
        [PortedTest(Source)] public static void holds_for_mana_when_nothing_in_hand_is_affordable() {
            var costs = Costs(); costs["favourable.mana_cost"] = 200; costs["losing.mana_cost"] = 200; Assert.True(Brain(-1, true).Think(Eye(1, 2), costs, Master.LeftPlayer, true) == null);
        }
        [PortedTest(Source)] public static void spends_a_card_when_overdue_and_nothing_scored_a_target() {
            var decision = Brain(0).Think(new BotEye(new BotVisibleObject[0], new long[] { 3 }, 100, 100), Costs(), Master.LeftPlayer, true); Assert.Equal(BotBrain.CycleRule, decision.RuleId); Assert.Equal(3L, decision.MagicId);
        }
        [PortedTest(Source)] public static void waits_when_not_overdue_and_nothing_scored_a_target() => Assert.True(Brain(0).Think(new BotEye(new BotVisibleObject[0], new long[] { 3 }, 100, 100), Costs(), Master.LeftPlayer) == null);
        [PortedTest(Source)] public static void an_empty_hand_or_unknown_cards_decide_nothing() {
            var brain = Brain(); Assert.True(brain.Think(Eye(), Costs(), Master.LeftPlayer) == null); Assert.True(brain.Think(Eye(99), Costs(), Master.LeftPlayer) == null);
        }
        private static Dictionary<string, Fix64> ComboParameters() {
            var values = new Dictionary<string, Fix64>();
            foreach (var name in new[] { "vine_toss", "water_explosion", "wind_explosion" }) { values[name + ".mana_cost"] = 1; values[name + ".range"] = 20; values[name + ".radius"] = 1; values[name + ".damage"] = 10; }
            return values;
        }
        private static BotBrain ComboBrain(BotTier tier, params BotMagic[] magics) {
            var brain = new BotBrain(new TestBotCatalog(magics), new Scorer(), new BotPersona { Tier = tier, ThinkingTimeMs = 0, CounterAggression = 0 }, new JavaRandom(1)); brain.ThinkFailed += error => throw error; return brain;
        }
        private static BotMagic Water() => new BotMagic(2, "water_explosion", kind: BotMagicKind.Explosion, className: "WaterExplosionMagic");
        private static BotVisibleObject Mob(int id, Master side, string prefab, int x, int z) => new BotVisibleObject(id, side, prefab, new Vector3(x, 0, z), 10, true, true);
        [PortedTest(Source)] public static void seed_spirit_combo_precedes_the_existing_value_decision() {
            var brain = ComboBrain(BotTier.Elite, new BotMagic(1, "vine_toss", kind: BotMagicKind.VineToss, className: "VineTossMagic"), Water());
            var eye = new BotEye(new[] { Mob(1, Master.LeftPlayer, "SeedSpirit", 4, 5), Mob(2, Master.RightPlayer, "WaterSlime", 6, 4), Mob(3, Master.RightPlayer, "WaterSlime", 6, 5), Mob(4, Master.RightPlayer, "WaterSlime", 6, 6) }, new long[] { 1, 2 }, 20, 100);
            var decision = brain.ThinkSeeded(eye, ComboParameters(), Master.LeftPlayer, new JavaRandom(1)); Assert.Equal("combo.seed-spirit", decision.RuleId); Assert.Equal(1L, decision.MagicId); Assert.Equal(new Vector3(4, 0, 5), decision.Target);
        }
        [PortedTest(Source)] public static void explosion_combo_targets_the_average_of_three_nearby_enemy_mobs() {
            var eye = new BotEye(new[] { Mob(2, Master.RightPlayer, "WaterSlime", 5, 4), Mob(3, Master.RightPlayer, "WaterSlime", 5, 5), Mob(4, Master.RightPlayer, "WaterSlime", 5, 6) }, new long[] { 2 }, 20, 100);
            var decision = ComboBrain(BotTier.Elite, Water()).ThinkSeeded(eye, ComboParameters(), Master.LeftPlayer, new JavaRandom(1)); Assert.Equal("combo.mob-cluster", decision.RuleId); Assert.Equal(new Vector3(5, 0, 5), decision.Target); Assert.True(decision.Reason.Contains("3 enemy mobs"));
        }
        [PortedTest(Source)] public static void applies_tier_noise_exactly_once_per_scored_candidate() {
            var brain = ComboBrain(BotTier.Beginner, Water(), new BotMagic(3, "wind_explosion", kind: BotMagicKind.Explosion, className: "WindExplosionMagic"));
            var eye = new BotEye(new[] { Mob(2, Master.RightPlayer, "WaterSlime", 5, 5) }, new long[] { 2, 3 }, 20, 100);
            var random = new JavaRandom(1); var expected = new JavaRandom(1); expected.NextFraction(); expected.NextFraction();
            Assert.True(brain.ThinkSeeded(eye, ComboParameters(), Master.LeftPlayer, random) != null); Assert.Equal(expected.State, random.State);
        }
        [PortedTest(Source)] public static void a_seeded_generator_makes_the_noisy_choice_repeatable() {
            BotBrain Create() => new BotBrain(new Catalog(), new Scorer(), new BotPersona { Tier = BotTier.Intro, CounterAggression = 1, ThinkingTimeMs = 0 }, new JavaRandom(77));
            var first = Create().Think(Eye(1, 2, 3), Costs(), Master.LeftPlayer); var second = Create().Think(Eye(1, 2, 3), Costs(), Master.LeftPlayer);
            Assert.True(first != null); Assert.Equal(first.MagicId, second.MagicId); Assert.Equal(first.Target, second.Target); Assert.Equal(first.RuleId, second.RuleId); Assert.Equal(first.Reason, second.Reason);
        }
    }
}
