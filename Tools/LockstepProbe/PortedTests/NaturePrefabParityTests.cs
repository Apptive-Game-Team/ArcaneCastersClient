using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class NaturePrefabParityTests {
        private const string Source = "game/domain/object/prefab/implement/nature/mod.rs";
        private static SimulationContext All() {
            var c = new SimulationContext(); c.InitializationFailed += (_, error) => throw error; StandardPrefabs.Register(c); return c;
        }
        [PortedTest(Source)] public static void giant_vine_snares_evolves_seeds_and_hits_on_start() {
            var c = All(); c.GameParameters["giant_vine.radius"] = 2; c.GameParameters["giant_vine.duration"] = 6; c.GameParameters["giant_vine.damage"] = 5;
            var o = c.Spawn(Master.LeftPlayer, "GiantVine", Vector3.Zero);
            Assert.Equal(1, o.GetComponents<EffectProvider>().Length); Assert.Equal(1, o.GetComponents<TimedSelfDestroyer>().Length);
            Assert.Equal(1, o.GetComponents<OnStartSeedSpiritEvolver>().Length); Assert.Equal(1, o.GetComponents<OnStartAttacker>().Length);
            Assert.True(o.Colliders[0].IsTrigger); Assert.True(o.Elements.Has(Element.Nature));
        }
        [PortedTest(Source)] public static void leaf_field_snares_and_heals() {
            var c = All(); c.GameParameters["leaf_field.radius"] = (Fix64)2.5m; c.GameParameters["leaf_field.duration"] = 8;
            var o = c.Spawn(Master.LeftPlayer, "LeafField", new Vector3(1,3,2)); Assert.Equal(new Vector3(1,0,2), o.Position);
            Assert.Equal(2, o.GetComponents<EffectProvider>().Length); Assert.True(o.HasComponent<LeafFieldEffectReceiver>()); Assert.True(o.HasComponent<TimedSelfDestroyer>());
            Assert.True(o.Colliders[0].IsTrigger); Assert.True(o.Elements.Has(Element.Nature));
        }
        [PortedTest(Source)] public static void leaf_slime_and_seed_spirit_read_their_own_parameters() {
            var c = All();
            foreach (var name in new[] { "leaf_slime", "seed_spirit" }) foreach (var key in new[] { "mass", "radius", "hp", "speed", "damage", "attack_interval" }) c.GameParameters[name + "." + key] = 1;
            foreach (var name in new[] { "LeafSlime", "SeedSpirit" }) {
                var o = c.Spawn(Master.LeftPlayer, name, Vector3.Zero); Assert.Equal(1, o.GetComponent<Slime>().MaxHp); Assert.Equal(Fix64.One, o.Colliders[0].CircleRadius.Value);
                Assert.True(o.HasComponent<PathSpawner>()); Assert.True(o.HasComponent<CommonEffectReceiver>()); Assert.True(o.Elements.Has(Element.Nature));
            }
            CheckNestAndSlimeParameters();
        }
        [PortedTest(Source)] public static void vine_toss_carries_only_its_shot() {
            var o = All().Spawn(Master.LeftPlayer, "VineToss", Vector3.Zero); Assert.True(o.HasComponent<VineToss>()); Assert.True(o.Elements.Has(Element.Nature));
        }
        [PortedTest(Source)] public static void registers_the_six_java_beans() {
            var c = All(); foreach (var name in new[] { "GiantVine", "LeafField", "LeafSlime", "SeedNest", "SeedSpirit", "VineToss" }) Assert.True(c.HasPrefab(name));
        }
        private static SimulationContext Context() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; NatureUnitPrefabs.Register(c);
            c.GameParameters["seed_nest.radius"] = 1; c.GameParameters["seed_nest.hp"] = 30; c.GameParameters["seed_nest.attack_interval"] = 3; c.GameParameters["seed_nest.duration"] = 20;
            foreach (var key in new[] { "leaf_slime", "seed_spirit" }) {
                c.GameParameters[key + ".mass"] = 1; c.GameParameters[key + ".radius"] = key == "leaf_slime" ? (Fix64)0.5m : (Fix64)0.25m;
                c.GameParameters[key + ".hp"] = key == "leaf_slime" ? 40 : 20; c.GameParameters[key + ".speed"] = 1; c.GameParameters[key + ".damage"] = 3; c.GameParameters[key + ".attack_interval"] = 1;
            }
            return c;
        }
        [PortedTest("game/domain/object/prefab/implement/nature/mod.rs")] public static void seed_nest_spawns_seed_spirits_until_it_expires() {
            var c = Context(); var nest = c.Spawn(Master.LeftPlayer, "SeedNest", new Vector3(5, 0, 5)); Assert.Equal(30, nest.GetComponent<Spawner>().MaxHp); Assert.Equal(1, nest.GetComponents<TimedSelfDestroyer>().Count()); Assert.Equal(1, nest.GetComponents<CommonEffectReceiver>().Count()); Assert.True(!nest.Colliders.Single().IsTrigger);
        }
        public static void CheckNestAndSlimeParameters() {
            var c = Context(); var leaf = c.Spawn(Master.LeftPlayer, "LeafSlime", new Vector3(5, 0, 5)); var seed = c.Spawn(Master.LeftPlayer, "SeedSpirit", new Vector3(6, 0, 5));
            Assert.Equal(40, leaf.GetComponent<Slime>().MaxHp); Assert.Equal(20, seed.GetComponent<Slime>().MaxHp); Assert.Equal((Fix64)0.5m, leaf.Colliders.Single().CircleRadius.Value); Assert.Equal((Fix64)0.25m, seed.Colliders.Single().CircleRadius.Value);
            foreach (var o in new[] { leaf, seed }) Assert.True(o.HasComponent<PathSpawner>() && o.HasComponent<CommonEffectReceiver>() && o.Elements.NativeHas(Element.Nature));
            var nest = c.Spawn(Master.LeftPlayer, "SeedNest", new Vector3(8, 0, 5)); c.FlushObjects(); c.DeltaTime = 3; nest.GetComponent<Spawner>().Update(); var summoned = c.PendingObjects.Single(); Assert.Equal("SeedSpirit", summoned.Prefab); Assert.Equal(Master.LeftPlayer, summoned.Master); Assert.Equal(20, summoned.GetComponent<Slime>().Hp);
            c.DeltaTime = 20; nest.GetComponent<TimedSelfDestroyer>().Update(); Assert.True(nest.IsDestroyed);
        }
    }
}
