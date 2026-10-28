using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class BuildMagicParityTests {
        private sealed class Catalog : IMagicCatalog {
            public System.Collections.Generic.IEnumerable<MagicInfo> GetAllMagic() => new[] { new MagicInfo(42, "seed_nest", Element.Nature) };
            public bool ExistUserMagic(long userId, long magicId) => userId == 7 && magicId == 42;
        }
        private const string Source = "game/domain/magic/implement/build/tests.rs";
        private const string AbstractSource = "game/domain/magic/implement/build/abstract_summon_magic.rs";
        private static readonly string[,] Summons = {
            { "cannon", "GroundCannon" }, { "fire_slime_nest", "FireSummon" }, { "healing_totem", "HealingTotem" },
            { "lightning_slime_nest", "ElectricSummon" }, { "mana_well", "ManaWell" }, { "nature_slime_nest", "PveNatureSlimeNest" },
            { "pve_water_slime_nest", "PveWaterSlimeNest" }, { "repair_totem", "RepairTotem" }, { "rock_slime_nest", "RockSummon" },
            { "seed_nest", "SeedNest" }, { "titan_remnant", "TitanRemnant" }, { "tower", "GroundTower" }, { "towerback", "Towerback" },
            { "bubble_generator", "BubbleGenerator" }, { "crater", "Crater" }, { "dragon_tower", "DragonTower" },
            { "electric_tower", "ElectricTower" }, { "firework_tower", "FireworkTower" }, { "grass_generator", "GrassGenerator" },
            { "life_tree", "LifeTree" }, { "rock_turret", "RockTurret" }, { "shock_trap", "ShockTrap" }, { "wind_totem", "WindTotem" },
            { "vine_colony", "VineColony" }, { "wind_slime_nest", "WindSummon" }
        };
        private static MagicRegistry Registry() { var r = new MagicRegistry(); BuildMagics.Register(r); return r; }
        private static bool Grounded(string name) => name == "cannon" || name == "tower" || name == "dragon_tower";
        private static SimObject Cast(MagicImplementation magic, string prefab, Master master, Vector3 position) {
            var c = new SimulationContext(); c.RegisterPrefab(prefab, _ => { }); magic.Bind(1, "test", Element.None).Run(c, master, null, position);
            Assert.Equal(1, c.PendingObjects.Length); return c.PendingObjects[0];
        }
        [PortedTest(Source)] public static void every_build_magic_is_registered_and_declares_the_prefab_it_summons() {
            var r = Registry(); for (int i = 0; i < Summons.GetLength(0); i++) { var magic = r.GetBean(Summons[i, 0]); Assert.True(magic != null); Assert.Equal(Summons[i, 1], magic.SummonedPrefab); }
        }
        [PortedTest(Source)] public static void a_cast_summons_the_prefab_for_the_caster_at_the_cast_position() {
            var r = Registry(); for (int i = 0; i < Summons.GetLength(0); i++) {
                var o = Cast(r.GetBean(Summons[i, 0]), Summons[i, 1], Master.RightPlayer, new Vector3(3, 0, 4));
                Assert.Equal(Summons[i, 1], o.Prefab); Assert.Equal(Master.RightPlayer, o.Master); Assert.Equal(new Vector3(3, 0, 4), o.Position);
            }
        }
        [PortedTest(Source)] public static void cannon_tower_and_dragon_tower_land_on_the_floor_whatever_height_the_cast_has() {
            var r = Registry(); for (int i = 0; i < Summons.GetLength(0); i++) if (Grounded(Summons[i, 0])) Assert.Equal(new Vector3(3, 0, 4), Cast(r.GetBean(Summons[i, 0]), Summons[i, 1], Master.LeftPlayer, new Vector3(3, (Fix64)2.5m, 4)).Position);
        }
        [PortedTest(Source)] public static void the_other_summons_keep_the_height_of_the_cast() {
            var r = Registry(); var position = new Vector3(3, (Fix64)2.5m, 4);
            for (int i = 0; i < Summons.GetLength(0); i++) if (!Grounded(Summons[i, 0])) Assert.Equal(position, Cast(r.GetBean(Summons[i, 0]), Summons[i, 1], Master.LeftPlayer, position).Position);
        }
        [PortedTest(AbstractSource)] public static void summons_its_prefab_at_the_cast_position() {
            var magic = BuildMagics.Create("GroundTower", "ground_tower"); var o = Cast(magic, "GroundTower", Master.LeftPlayer, new Vector3(3, 0, 4));
            Assert.Equal("GroundTower", o.Prefab); Assert.Equal(Master.LeftPlayer, o.Master); Assert.Equal(new Vector3(3, 0, 4), o.Position); Assert.Equal("GroundTower", magic.SummonedPrefab);
        }
        public static void CheckCatalogInputPlacement() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e;
            var parser = new DatabaseMagicParser(new Catalog(), Registry()); c.InputHandler = new MagicInputHandler(parser);
            var player = new PlayerData(new ManaCharger(100)) { Mana = 50 }; player.AddCard(42);
            c.Players.Add(Master.LeftPlayer, player); c.UserIds.Add(Master.LeftPlayer, 7);
            c.GameParameters["player.hp"] = 100; c.GameParameters["seed_nest.mana_cost"] = 20; c.GameParameters["seed_nest.range"] = 18;
            c.GameParameters["seed_nest.radius"] = 1; c.GameParameters["seed_nest.hp"] = 20; c.GameParameters["seed_nest.attack_interval"] = 2; c.GameParameters["seed_nest.duration"] = 10;
            NatureUnitPrefabs.Register(c); PlayerHealthComponent.Register(c);
            var caster = c.Spawn(Master.LeftPlayer, "Player", new Vector3(5, 0, 5)); c.FlushObjects();
            var bound = parser.GetMagic(42); Assert.Equal("SeedNest", bound.SummonedPrefab); Assert.Equal("seed_nest", bound.SummonedParameterKey); Assert.True(!bound.Airborne);
            var result = c.InputHandler.HandleInput(c, 7, 42, caster.Position, 1); Assert.Equal(InputResultCode.Success, result.ResultCode);
            Assert.Equal(30, player.Mana); Assert.Equal(0, player.Cards.Count); Assert.Equal(1, c.PendingObjects.Length);
            var nest = c.PendingObjects[0]; Assert.True(nest.GetComponent<Spawner>() != null); Assert.Equal(Master.LeftPlayer, nest.Master);
            Assert.True(Vector3.Distance(nest.Position, caster.Position) >= (Fix64)1.2m); Assert.True(!nest.IsDestroyed);
        }
    }
}
