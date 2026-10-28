using System;
using System.Collections.Generic;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class GamePlannerChecks {
        public static void Run() {
            var parameters = new Dictionary<string, Fix64> { ["evil_ent.mana_cost"] = 80, ["cloud_dragon.mana_cost"] = 80, ["fire_spirit.mana_cost"] = 10, ["fire_spirit.speed"] = 1, ["cloud_dragon.spawn_height"] = 3 };
            BotMagic Summon(long id, string name, string prefab) => new BotMagic(id, name, summoning: new SummonedBodies(prefab));
            var ent = Summon(1, "evil_ent", "EvilEnt"); var dragon = Summon(2, "cloud_dragon", "CloudDragon"); var spirit = Summon(3, "fire_spirit", "FireSpirit");
            var catalog = new TestBotCatalog(ent, dragon, spirit); var stats = new BotSpellStats(parameters);
            var deck = DeckProfile.Of(new long[] { 1, 1, 2, 3, 99 }, catalog, stats); Assert.Equal(5, deck.Cards); Assert.Equal(3, deck.BigFighters); Assert.True(deck.WavesPayOff);
            Assert.True(!DeckProfile.Of(Array.Empty<long>(), catalog, stats).Known); Assert.True(!DeckProfile.Of(new long[] { 1, 2, 3 }, catalog, stats).WavesPayOff);
            var origin = new Vector3(1, 0, 5); var spot = new Vector3(6, 0, 5);
            ThinkContext Context(int mana, BotVisibleObject[] objects, params BotMagic[] hand) => new ThinkContext(hand, mana, stats, objects, origin, Master.LeftPlayer, Master.RightPlayer, ThreatAssessment.Observe(objects, Master.RightPlayer, origin, 100), BotSkill.ForTier(BotTier.Elite), new JavaRandom(1));
            var empty = Array.Empty<BotVisibleObject>(); var saving = Context(80, empty, ent, spirit);
            Assert.True(GamePlanner.Plan(saving, 200, parameters, deck, _ => spot).Saving);
            Assert.Equal(BotPlanKind.None, GamePlanner.Plan(saving, 200, parameters, new DeckProfile(5, 2), _ => spot).Kind);
            var released = GamePlanner.Plan(Context(90, empty, ent, spirit), 200, parameters, deck, _ => spot);
            Assert.Equal(BotPlanKind.Wave, released.Kind); Assert.Equal(1L, released.Play.MagicId); Assert.Equal(spot, released.Play.Target);
            var capped = GamePlanner.Plan(Context(90, empty, ent, dragon), 100, parameters, deck, _ => spot); Assert.Equal(BotPlanKind.Wave, capped.Kind); Assert.Equal(1L, capped.Play.MagicId);
            Assert.Equal(BotPlanKind.None, GamePlanner.Plan(Context(50, empty, spirit), 100, parameters, deck, _ => spot).Kind);
            BotVisibleObject Unit(int id, Master side, string prefab, Vector3 position, bool targetable = true, bool mob = true) => new BotVisibleObject(id, side, prefab, position, 10, mob, targetable);
            var forming = new[] { Unit(1, Master.LeftPlayer, "FireSpirit", new Vector3(4, 0, 5)) };
            var joined = GamePlanner.Plan(Context(10, forming, ent, spirit), 200, parameters, deck, _ => throw new Exception("Joining must not ask for placement"));
            Assert.Equal(BotPlanKind.Wave, joined.Kind); Assert.Equal(3L, joined.Play.MagicId); Assert.Equal(forming[0].Position, joined.Play.Target);
            var aa = Summon(4, "wind_spirit", "WindSpirit"); var defense = Summon(5, "wind_totem", "WindTotem"); var support = Summon(6, "healing_totem", "HealingTotem");
            Assert.True(!SummonRelevance.Relevant(aa, Context(100, empty), parameters));
            Assert.True(SummonRelevance.Relevant(aa, Context(100, new[] { Unit(1, Master.RightPlayer, "CloudDragon", new Vector3(16, 0, 5)) }), parameters));
            Assert.True(!SummonRelevance.Relevant(aa, Context(100, new[] { Unit(1, Master.RightPlayer, "CloudDragon", spot, false) }), parameters));
            Assert.True(SummonRelevance.Relevant(defense, Context(100, new[] { Unit(1, Master.RightPlayer, "FireSpirit", new Vector3(9, 0, 5)) }), parameters));
            Assert.True(!SummonRelevance.Relevant(defense, Context(100, new[] { Unit(1, Master.RightPlayer, "FireSpirit", new Vector3(10, 0, 5)) }), parameters));
            Assert.True(!SummonRelevance.Relevant(support, Context(100, forming), parameters));
            Assert.True(SummonRelevance.Relevant(support, Context(100, new[] { forming[0], Unit(2, Master.LeftPlayer, "Wall", spot, mob: false) }), parameters));
            Assert.True(SummonRelevance.Relevant(spirit, Context(100, empty), parameters)); Assert.Equal(44, CardRoles.Table.Count);
            int warnings = 0; Action<string> listener = _ => warnings++; CardRoles.UnknownSummon += listener;
            var unknown = Summon(100, "unknown_game_planner_check", "Unknown"); CardRoles.Of(unknown); CardRoles.Of(unknown); CardRoles.UnknownSummon -= listener;
            Assert.Equal(1, warnings); Assert.True(SummonRelevance.Relevant(unknown, Context(100, empty), parameters));
        }
    }
}
