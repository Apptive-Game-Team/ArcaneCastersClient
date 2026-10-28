using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class BotBrainCapabilityParityTests {
        private const string Source = "game/domain/bot/bot_brain.rs";
        private sealed class NoCounters : IBotCounterScorer {
            public Fix64 Evaluate(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => Fix64.Zero;
            public Fix64 EvaluateVulnerability(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => Fix64.Zero;
        }
        private sealed class Fixture {
            public readonly Dictionary<string, Fix64> Values = new Dictionary<string, Fix64>();
            public readonly List<BotMagic> Magics = new List<BotMagic>();
            public void Summon(long id, string name, string prefab, int cost) { Magics.Add(new BotMagic(id, name, summoning: new SummonedBodies(prefab))); Values[name + ".mana_cost"] = cost; Values[name + ".range"] = 10; }
            public void Offensive(long id, string name, int cost, int damage) { Magics.Add(new BotMagic(id, name)); Values[name + ".mana_cost"] = cost; Values[name + ".range"] = 20; Values[name + ".radius"] = 1; if (damage > 0) Values[name + ".damage"] = damage; }
            public BotBrain Brain(BotCapability? capability = null, long[] deck = null) {
                var brain = new BotBrain(new TestBotCatalog(Magics.ToArray()), new NoCounters(), new BotPersona { Tier = BotTier.Elite, ThinkingTimeMs = 0, CounterAggression = 0 }, new JavaRandom(1), new BotSkill(0, capability.HasValue ? new[] { capability.Value } : new BotCapability[0]), () => deck ?? new long[0]);
                brain.ThinkFailed += error => throw error; return brain;
            }
            public InputDecision Think(BotEye eye, BotCapability? capability = null, bool overdue = false, long[] deck = null) => Brain(capability, deck).Think(eye, Values, Master.LeftPlayer, overdue);
        }
        private static BotVisibleObject Enemy(decimal x, int hp = 40, string prefab = "FireSpirit", decimal z = 5) => new BotVisibleObject(7, Master.RightPlayer, prefab, new Vector3((Fix64)x, 0, (Fix64)z), hp, prefab != "Player", true);
        private static BotEye Eye(int mana, long[] cards, params BotVisibleObject[] objects) => new BotEye(objects, cards, mana, 1000);
        [PortedTest(Source)] public static void summon_strength_picks_the_summon_that_buys_more_fight_per_mana() {
            var f = new Fixture(); f.Summon(1, "cheap", "FireSpirit", 3); f.Summon(2, "big", "WaterSlime", 9);
            f.Values["fire_spirit.hp"] = 10; f.Values["fire_spirit.damage"] = 10; f.Values["water_slime.hp"] = 160; f.Values["water_slime.damage"] = 10;
            var eye = Eye(20, new long[] { 1, 2 }); Assert.Equal(1L, f.Think(eye).MagicId); Assert.Equal(2L, f.Think(eye, BotCapability.SummonStrength).MagicId);
        }
        [PortedTest(Source)] public static void lethal_finishes_the_enemy_player_before_anything_else() {
            var f = new Fixture(); f.Offensive(1, "finisher", 4, 6); f.Summon(2, "cheap_summon", "FireSpirit", 1);
            var eye = new BotEye(new[] { Enemy(17, 6, "Player") }, new long[] { 1, 2 }, 20, 6);
            Assert.Equal(2L, f.Think(eye).MagicId); var decision = f.Think(eye, BotCapability.Lethal);
            Assert.Equal("finish.lethal", decision.RuleId); Assert.Equal(1L, decision.MagicId); Assert.Equal(new Vector3(17, 0, 5), decision.Target); Assert.Equal("finisher finishes the enemy player at 6 hp.", decision.Reason);
        }
        [PortedTest(Source)] public static void lethal_needs_a_damage_the_parameters_actually_state() {
            var f = new Fixture(); f.Offensive(1, "guessed", 4, 0); var eye = new BotEye(new[] { Enemy(17, 6, "Player") }, new long[] { 1 }, 20, 1);
            Assert.True(f.Think(eye, BotCapability.Lethal).RuleId != "finish.lethal");
        }
        private static Fixture Saving(int max) { var f = new Fixture(); f.Values["player.max_mana"] = max; f.Summon(1, "small", "FireSpirit", 4); f.Offensive(2, "blast", 8, 40); return f; }
        [PortedTest(Source)] public static void mana_discipline_saves_for_a_clearly_better_card_while_calm() {
            var f = Saving(10); var eye = Eye(4, new long[] { 1, 2 }, Enemy(15)); Assert.Equal(1L, f.Think(eye).MagicId); Assert.True(f.Think(eye, BotCapability.ManaDiscipline) == null);
        }
        [PortedTest(Source)] public static void mana_discipline_never_waits_for_a_card_the_bot_can_never_afford() => Assert.Equal(1L, Saving(6).Think(Eye(4, new long[] { 1, 2 }, Enemy(15)), BotCapability.ManaDiscipline).MagicId);
        [PortedTest(Source)] public static void mana_discipline_does_not_hold_out_when_overdue() => Assert.Equal(1L, Saving(10).Think(Eye(4, new long[] { 1, 2 }, Enemy(15)), BotCapability.ManaDiscipline, true).MagicId);
        [PortedTest(Source)] public static void lead_targeting_aims_where_a_moving_enemy_will_be() {
            var f = new Fixture(); f.Offensive(1, "shot", 1, 5); var leading = f.Brain(BotCapability.LeadTargeting); var plain = f.Brain();
            var before = new BotEye(new[] { Enemy(10, 10) }, new long[] { 1 }, 20, 100, 1); var after = new BotEye(new[] { Enemy(9, 10) }, new long[] { 1 }, 20, 100, (Fix64)1.5m);
            leading.Think(before, f.Values, Master.LeftPlayer); plain.Think(before, f.Values, Master.LeftPlayer);
            Assert.Equal(new Vector3((Fix64)8.5m, 0, 5), leading.Think(after, f.Values, Master.LeftPlayer).Target); Assert.Equal(new Vector3(9, 0, 5), plain.Think(after, f.Values, Master.LeftPlayer).Target);
        }
        private static Fixture Plan() { var f = new Fixture(); f.Values["player.max_mana"] = 120; return f; }
        [PortedTest(Source)] public static void an_anti_air_unit_waits_for_an_enemy_flyer() {
            var f = Plan(); f.Summon(1, "wind_spirit", "WindSpirit", 15); f.Values["thunder_bird.spawn_height"] = 3;
            var empty = Eye(20, new long[] { 1 }); Assert.Equal(1L, f.Think(empty).MagicId); Assert.True(f.Think(empty, BotCapability.SummonRelevance) == null);
            Assert.Equal(1L, f.Think(Eye(20, new long[] { 1 }, Enemy(14, 50, "ThunderBird")), BotCapability.SummonRelevance).MagicId);
        }
        [PortedTest(Source)] public static void a_pusher_waits_for_an_enemy_on_our_half_and_mana_near_the_cap_still_goes_to_it() {
            var f = Plan(); f.Summon(1, "wind_totem", "WindTotem", 15); var brain = f.Brain(BotCapability.SummonRelevance);
            Assert.True(brain.Think(Eye(20, new long[] { 1 }, Enemy(15, 50)), f.Values, Master.LeftPlayer) == null);
            Assert.Equal(1L, brain.Think(Eye(20, new long[] { 1 }, Enemy(6, 50)), f.Values, Master.LeftPlayer).MagicId);
            Assert.Equal(1L, brain.Think(Eye(115, new long[] { 1 }), f.Values, Master.LeftPlayer).MagicId);
        }
        [PortedTest(Source)] public static void a_full_hand_casts_a_useless_summon_to_free_its_slot() {
            var f = Plan(); f.Summon(1, "wind_spirit", "WindSpirit", 15); Assert.Equal(1L, f.Think(Eye(20, new long[] { 1, 1, 1, 1, 1, 1 }), BotCapability.SummonRelevance).MagicId);
        }
        private static Fixture Wave() { var f = Plan(); f.Summon(1, "evil_ent", "EvilEnt", 80); f.Summon(2, "ember_spirit_swarm", "FireSlime", 15); return f; }
        [PortedTest(Source)] public static void a_dear_fighter_in_hand_makes_the_bot_save_for_the_whole_wave() {
            var f = Wave(); var brain = f.Brain(BotCapability.GamePlan); var eye = Eye(20, new long[] { 1, 2 }); Assert.Equal(2L, f.Think(eye).MagicId); Assert.True(brain.Think(eye, f.Values, Master.LeftPlayer) == null);
            var first = brain.Think(Eye(95, new long[] { 1, 2 }), f.Values, Master.LeftPlayer); Assert.Equal("plan.wave", first.RuleId); Assert.Equal(1L, first.MagicId); Assert.Equal("Releases a wave at mana 95: evil_ent.", first.Reason);
        }
        [PortedTest(Source)] public static void a_fighter_below_the_wave_cost_is_not_saved_for() {
            var f = Plan(); f.Summon(1, "thunder_spirit", "ThunderSpirit", 35); Assert.True(f.Think(Eye(20, new long[] { 1 }), BotCapability.GamePlan) == null); Assert.True(GamePlanner.WaveCardCost > 35);
        }
        [PortedTest(Source)] public static void a_fighter_still_near_our_base_is_a_wave_the_next_fighter_joins() {
            var f = Wave(); f.Values["evil_ent.speed"] = 1; var staged = new Vector3(4, 0, 6);
            var joined = f.Think(Eye(15, new long[] { 1, 2 }, new BotVisibleObject(30, Master.LeftPlayer, "EvilEnt", staged, 900, true, true)), BotCapability.GamePlan);
            Assert.Equal("plan.wave", joined.RuleId); Assert.Equal(2L, joined.MagicId); Assert.Equal(staged, joined.Target); Assert.Equal("Joins the wave forming near our base: ember_spirit_swarm.", joined.Reason);
        }
        [PortedTest(Source)] public static void a_deck_without_enough_dear_fighters_never_saves() {
            var f = Wave(); var eye = Eye(20, new long[] { 1, 2 }); Assert.Equal(2L, f.Think(eye, BotCapability.GamePlan, deck: new long[] { 1, 2, 2, 2 }).MagicId); Assert.True(f.Think(eye, BotCapability.GamePlan, deck: new long[] { 1, 1, 1, 2 }) == null);
        }
        [PortedTest(Source)] public static void every_fighter_row_is_a_known_role() {
            Assert.Equal((CardRole?)CardRole.AntiAir, CardRoles.OfName("wind_spirit")); Assert.Equal((CardRole?)CardRole.Fighter, CardRoles.OfName("thunder_bird_swarm")); Assert.Equal((CardRole?)null, CardRoles.OfName("no_such_magic")); Assert.Equal(44, CardRoles.Table.Count);
        }
    }
}
