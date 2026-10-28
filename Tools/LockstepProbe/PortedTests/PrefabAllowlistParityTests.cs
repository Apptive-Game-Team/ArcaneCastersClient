using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;

namespace ArcaneCasters.Parity {
    public static class PrefabAllowlistParityTests {
        private const string Source = "game/domain/object/prefab/allowlist_tests.rs";
        [PortedTest("game/domain/object/prefab/implement/fire/mod.rs")]
        public static void ember_spirit_prefab_is_a_fire_slime_built_from_the_ember_spirit_rows() {
            var initializer = ElementalNests.EmberSpiritInitializer();
            Assert.Equal("FireSlime", initializer.Prefab);
            var context = PrefabParameterFixture.Create();
            context.GameParameters["ember_spirit.hp"] = 17; context.GameParameters["fire_slime.hp"] = 91;
            var owner = new SimObject(context, Master.LeftPlayer, initializer.Prefab, Vector3.Zero);
            initializer.Initialize(owner);
            Assert.Equal(17, owner.GetComponent<Slime>().MaxHp);
            Assert.True(owner.HasComponent<PathSpawner>() && owner.HasComponent<CommonEffectReceiver>());
            Assert.True(owner.Elements.NativeHas(Element.Fire));
            // 원본 registry의 EmberSpirit 별칭은 별도 initializer 대신 FireSlime을 사용한다.
            var alias = context.Spawn(Master.LeftPlayer, "EmberSpirit", Vector3.Zero);
            Assert.Equal(91, alias.GetComponent<Slime>().MaxHp);
        }
        private static void Registered(params string[] names) {
            var context = PrefabParameterFixture.Create(); context.InputHandler = new MagicInputHandler(null);
            foreach (var prefix in new[] { "pve_nature_slime_nest","pve_water_slime_nest","pve_vine_witch" })
                foreach (var key in new[] { "hp","mass","radius" }) context.GameParameters[prefix + "." + key] = 1;
            context.GameParameters["crater_ember.attack_range"] = 2;
            foreach (var name in names) {
                Assert.True(context.HasPrefab(name));
                var bean = System.Text.RegularExpressions.Regex.Replace(name,"(?<!^)([A-Z])","_$1").ToLowerInvariant() + "_prefab";
                Assert.Equal(bean,PrefabCatalog.BeanName(name));
                var owner = context.Spawn(Master.LeftPlayer,name,Vector3.Zero);
                Assert.Equal(name,owner.Prefab); Assert.True(owner.IsInitialized);
            }
        }
        [PortedTest("game/domain/object/prefab/implement/build/tests.rs")] public static void every_build_initializer_is_registered_under_its_bean_name() {
            Registered("BubbleGenerator","Crater","DragonTower","ElectricTower","FireworkTower","GrassGenerator","LifeTree","RepairTotem","RockTurret","ShockTrap","TitanRemnant","WindTotem");
        }
        [PortedTest("game/domain/object/prefab/implement/fire/mod.rs")] public static void registers_every_fire_prefab_under_its_java_bean_name() {
            Registered("FireSlime","FireShot","FireSummon","DragonFlame","FireField");
        }
        [PortedTest("game/domain/object/prefab/implement/lightning/mod.rs")] public static void registers_every_lightning_prefab_under_its_java_bean_name() {
            Registered("ZapMouse","ElectricSlime","ElectricShot","StormStag","LightningCloud","ElectricField","ElectricSummon");
        }
        [PortedTest("game/domain/object/prefab/implement/rock/mod.rs")] public static void registers_every_rock_prefab_under_its_java_bean_name() {
            Registered("RockRemnant","RockSummon","RockSlime","MediumRockRemnant","RockRolling","MiniRock","EarthCall","BoulderStrike");
        }
        [PortedTest("game/domain/object/prefab/implement/pve/tests.rs")] public static void every_pve_initializer_is_registered_under_its_bean_name() {
            Registered("PveBrickGolem","PveCorruptedVineSpirit","PveCorruptedWindSpirit","PveDimensionToad","PveEvilEnt","PveFireTadpole","PveLightningTadpole","PveNatureSlimeNest","PveVineColony","PveVineWitch","PveWaterSlimeNest");
        }
        public static class Drop {
            [PortedTest("game/domain/object/prefab/implement/drop/mod.rs")] public static void every_prefab_resolves_to_its_own_initializer_by_bean_name() {
                Registered("FireDrop","NatureDrop","RockDrop","WindDrop","FrenzyTotem","Leafair","RallyingTotem");
            }
        }
        public static class Explode {
            [PortedTest("game/domain/object/prefab/implement/explode/mod.rs")] public static void every_prefab_resolves_to_its_own_initializer_by_bean_name() {
                Registered("ElectricExplode","FireExplode","LeafExplode","RockExplode","WaterExplode","MagmaExplosion","Overgrowth","RazorGale","ShockOverload","WaterExplosion","WindExplode");
            }
        }
        public static class Subprefab {
            [PortedTest("game/domain/object/prefab/implement/subprefab/mod.rs")] public static void every_prefab_resolves_to_its_own_initializer_by_bean_name() {
                Registered("CraterEmber","CraterExplosion","CraterShell","FireworkShell","MagmaFist","MeteorDrop","TitanFist","Vine");
            }
        }
        [PortedTest(Source)] public static void every_misc_prefab_type_has_its_initializer_registered_under_the_java_bean_name() {
            var c = PrefabParameterFixture.Create(); c.InputHandler = new MagicInputHandler(null);
            foreach (var name in new[] { "AquaArcher", "ArcaneInfantry", "CraterCarrier", "BubbleSpirit", "ChainLightning", "ChickenCommando", "DimensionToad", "FireChildSpirit", "FireLordSpirit", "FireSpirit", "FireTadpole", "GroundCannon", "GroundTower", "HealingTotem", "LightningTadpole", "MagmaSpirit", "ManaWell", "MeteorShower", "Player", "RockGolem", "RockMage", "SandStorm", "StormRider", "ThunderSpirit", "TideCall", "TornadoStrike", "Towerback", "VineColony", "Wall", "WallGolem", "WillOWisp", "CloudDragon", "EvilEnt", "ThunderBird", "TreeGolem", "VineSpirit" }) {
                Assert.True(c.HasPrefab(name)); Assert.Equal(name, c.Spawn(Master.LeftPlayer, name, Vector3.Zero).Prefab);
            }
        }
        [PortedTest(Source)] public static void electric_death_energy_attaches_only_to_the_lightning_creature_allowlist() {
            var c = PrefabParameterFixture.Create();
            foreach (var name in new[] { "ElectricSlime", "ZapMouse", "LightningTadpole", "PveLightningTadpole", "ThunderSpirit", "ThunderBird", "StormRider" })
                Assert.True(c.Spawn(Master.LeftPlayer, name, Vector3.Zero).HasComponent<ElectricDeathEnergy>());
            foreach (var name in new[] { "ElectricTower", "ElectricSummon", "ManaWell", "DimensionToad", "PveDimensionToad" })
                Assert.True(!c.Spawn(Master.LeftPlayer, name, Vector3.Zero).HasComponent<ElectricDeathEnergy>());
        }
        private static void Remnant(string name, string expected) {
            var c = PrefabParameterFixture.Create(); var o = c.Spawn(Master.LeftPlayer, name, Vector3.Zero); int before = c.PendingObjects.Count();
            o.GetComponent<RockDeathRemnant>().OnCombatDeath(); Assert.Equal(before + 1, c.PendingObjects.Count()); Assert.Equal(expected, c.PendingObjects.Last().Prefab);
        }
        [PortedTest(Source)] public static void rock_death_remnant_attaches_configured_sizes_only_to_eligible_rock_summons_and_buildings() {
            foreach (var name in new[] { "WallGolem", "MagmaSpirit", "TitanRemnant" }) Remnant(name, "MediumRockRemnant");
            foreach (var name in new[] { "CraterCarrier", "RockGolem", "GroundCannon", "GroundTower" }) Remnant(name, "RockRemnant");
            var c = PrefabParameterFixture.Create();
            foreach (var name in new[] { "RockMage", "RockSlime", "MiniRock", "RockDrop", "RockTurret", "DragonTower", "ElectricTower", "Towerback", "RockRemnant", "RockSummon", "RockRolling", "RockExplode", "RockRune" })
                Assert.True(!c.Spawn(Master.LeftPlayer, name, Vector3.Zero).HasComponent<RockDeathRemnant>());
        }
    }
}
