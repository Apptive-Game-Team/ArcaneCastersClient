using System;
using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class BoardValueChecks {
        private sealed class Rows : IMagicCatalog {
            public readonly List<MagicInfo> Entries = new List<MagicInfo>();
            public IEnumerable<MagicInfo> GetAllMagic() => Entries;
            public bool ExistUserMagic(long userId, long magicId) => true;
        }
        private static BotVisibleObject Body(string prefab, int hp, Master side = Master.RightPlayer, bool targetable = true) => new BotVisibleObject(1, side, prefab, Vector3.Zero, hp, true, targetable);
        public static void Run() {
            var context = new SimulationContext(); var parameters = context.GameParameters;
            parameters["ember_spirit.quantity"] = 5; parameters["ember_spirit.hp"] = 10; parameters["ember_spirit_swarm.mana_cost"] = 20;
            var rows = new Rows(); rows.Entries.Add(new MagicInfo(1, "ember_spirit_swarm", Element.Fire)); rows.Entries.Add(new MagicInfo(2, "overgrowth", Element.Nature)); rows.Entries.Add(new MagicInfo(3, "vine_toss"));
            rows.Entries.Add(new MagicInfo(4, "bomb_sprite")); rows.Entries.Add(new MagicInfo(5, "no_implementation"));
            var parser = new DatabaseMagicParser(rows, MagicRegistry.Standard()); var catalog = new ParsedBotMagicCatalog(parser, context);
            var swarm = catalog.GetMagic(1); Assert.Equal(Element.Fire, swarm.Element); Assert.Equal("EmberSpiritSwarmMagic", swarm.ClassName);
            Assert.Equal("EmberSpirit", swarm.Summoning.Value.Prefab); Assert.Equal(5, swarm.Summoning.Value.Quantity); Assert.True(!swarm.Summoning.Value.Airborne);
            Assert.Equal(BotMagicKind.Overgrowth, catalog.GetMagic(2).Kind); Assert.True(catalog.GetMagic(2).IsExplosion);
            Assert.Equal(BotMagicKind.VineToss, catalog.GetMagic(3).Kind); Assert.True(catalog.GetMagic(4).Summoning.Value.Airborne); Assert.True(catalog.GetMagic(5) == null && catalog.GetMagic(0) == null);
            var board = new BoardValue(catalog, new BotSpellStats(parameters), parameters); Assert.Equal(4, board.ManaCostOf("EmberSpirit"));
            Assert.Equal(20, board.ManaOnField(Enumerable.Repeat(Body("EmberSpirit", 10), 5), Master.RightPlayer));
            Assert.Equal(2, board.ManaOnField(new[] { Body("EmberSpirit", 5) }, Master.RightPlayer));
            Assert.Equal(1, board.ManaOnField(new[] { Body("EmberSpirit", 2) }, Master.RightPlayer));
            Assert.Equal(4, board.ManaOnField(new[] { Body("EmberSpirit", 30) }, Master.RightPlayer));
            Assert.Equal(0, board.ManaOnField(new[] { Body("EmberSpirit", -1), Body("EmberSpirit", 10, targetable: false), Body("EmberSpirit", 10, Master.LeftPlayer), Body("Player", 100), Body("Unknown", 100) }, Master.RightPlayer));
            parameters.Remove("ember_spirit.hp"); Assert.Equal(4, board.ManaOnField(new[] { Body("EmberSpirit", 1) }, Master.RightPlayer));
            parameters["ember_spirit.quantity"] = 10; Assert.Equal(5, catalog.GetMagic(1).Summoning.Value.Quantity);
            catalog.InvalidateCache(); Assert.Equal(10, catalog.GetMagic(1).Summoning.Value.Quantity); Assert.Equal(5, swarm.Summoning.Value.Quantity);
            var samePrefab = new TestBotCatalog(new BotMagic(1, "expensive", summoning: new SummonedBodies("EmberSpirit", 2)), new BotMagic(2, "cheap", summoning: new SummonedBodies("EmberSpirit", 0)));
            parameters["expensive.mana_cost"] = 9; parameters["cheap.mana_cost"] = 3;
            Assert.Equal(3, new BoardValue(samePrefab, new BotSpellStats(parameters), parameters).ManaCostOf("EmberSpirit"));
            rows.Entries.Clear(); parser.InvalidateCache(); catalog.InvalidateCache(); Assert.Equal(0, catalog.GetAllMagics().Count());
            rows.Entries.Add(new MagicInfo(6, "water_slime_swarm")); Assert.True(catalog.GetMagic(6) != null);
        }
    }
}
