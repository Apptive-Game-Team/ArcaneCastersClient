using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    internal sealed class TestBotCatalog : IBotMagicCatalog {
        private readonly BotMagic[] magics;
        public TestBotCatalog(params BotMagic[] magics) { this.magics = magics; }
        public BotMagic GetMagic(long id) => id <= 0 ? null : magics.FirstOrDefault(m => m.Id == id);
        public IEnumerable<BotMagic> GetAllMagics() => magics;
    }
    public static class UnitCatalogueParityTests {
        private const string Source = "game/domain/bot/unit_catalogue.rs";
        private static BotMagic Weak() => new BotMagic(1, "weak", summoning: new SummonedBodies("FireSpirit"));
        private static BotMagic Strong() => new BotMagic(2, "strong", summoning: new SummonedBodies("WaterSlime"));
        private static Dictionary<string, Fix64> Values() => new Dictionary<string, Fix64> { ["weak.mana_cost"] = 5, ["strong.mana_cost"] = 5, ["fire_spirit.hp"] = 10, ["fire_spirit.damage"] = 10, ["water_slime.hp"] = 40, ["water_slime.damage"] = 10 };
        private static UnitCatalogue Catalogue(Dictionary<string, Fix64> values = null) { values = values ?? Values(); return new UnitCatalogue(new TestBotCatalog(Weak(), Strong()), new BotSpellStats(values), values); }
        private static BotVisibleObject Visible(string prefab, int hp) => new BotVisibleObject(9, Master.RightPlayer, prefab, new Vector3(5, 0, 5), hp, true, true);
        [PortedTest(Source)] public static void ranks_summons_by_combat_power_per_mana() {
            var units = Catalogue(); Assert.True(units.RelativeEfficiency(Strong(), 5) > Fix64.One); Assert.True(units.RelativeEfficiency(Weak(), 5) < Fix64.One); Assert.Equal((Fix64)5, units.AverageSummonCost);
        }
        [PortedTest(Source)] public static void a_unit_the_parameters_cannot_judge_is_average() {
            var units = Catalogue(); var unknown = new BotMagic(3, "unknown", summoning: new SummonedBodies("SeedSpirit"));
            Assert.Equal(Fix64.One, units.RelativeEfficiency(unknown, 5)); Assert.Equal(Fix64.One, units.RelativeThreat(Visible("SeedSpirit", 10)));
        }
        [PortedTest(Source)] public static void a_wounded_enemy_threatens_less_than_a_fresh_one() { var units = Catalogue(); Assert.True(units.RelativeThreat(Visible("WaterSlime", 10)) < units.RelativeThreat(Visible("WaterSlime", 40))); }
        [PortedTest(Source)] public static void the_enemy_player_is_not_a_fighter() => Assert.Equal(Fix64.Zero, Catalogue().RelativeThreat(Visible("Player", 100)));
        [PortedTest(Source)] public static void reads_range_from_the_summoned_prefab() { var values = Values(); values["water_slime.attack_range"] = 4; var units = Catalogue(values); Assert.True(units.SummonsRanged(Strong())); Assert.True(!units.SummonsRanged(Weak())); }
        [PortedTest("game/service/bot/tag_catalog.rs")] public static void names_a_prefab_in_snake_case() {
            Assert.Equal("seed_spirit", UnitCatalogue.GameObjectName("SeedSpirit")); Assert.Equal("player", UnitCatalogue.GameObjectName("Player")); Assert.Equal("fire_slime", UnitCatalogue.GameObjectName("FireSlime"));
        }
        public static void CheckClampsAndQuantities() {
            var units = Catalogue(); Assert.Equal((Fix64)2, units.RelativeEfficiency(new BotMagic(3, "swarm", summoning: new SummonedBodies("WaterSlime", 5)), 5));
            Assert.Equal((Fix64)0.5m, units.RelativeEfficiency(Weak(), 100)); Assert.Equal((Fix64)3, units.RelativeThreat(Visible("WaterSlime", 10000)));
            Assert.Equal((Fix64)0.25m, units.RelativeThreat(Visible("FireSpirit", 1)));
            var empty = new Dictionary<string, Fix64>(); var unknown = new UnitCatalogue(new TestBotCatalog(Weak()), new BotSpellStats(empty), empty);
            Assert.Equal(Fix64.Zero, unknown.AverageSummonCost); Assert.Equal(Fix64.One, unknown.RelativeThreat(Visible("FireSpirit", 1)));
        }
    }
}
