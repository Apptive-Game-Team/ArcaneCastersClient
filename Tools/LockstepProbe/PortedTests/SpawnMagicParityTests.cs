using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class SpawnMagicParityTests {
        private const string SpawnSource = "game/domain/magic/implement/spawn/abstract_spawn_magic.rs";
        private const string BossSource = "game/domain/magic/implement/spawn/spawn_boss_magic.rs";
        private static SimulationContext Context() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e;
            foreach (var name in new[] { "AquaArcher", "EarthCall", "RockRemnant", "MediumRockRemnant", "RockSlime", "MiniRock", "RockGolem" }) c.RegisterPrefab(name, _ => { }); return c;
        }
        [PortedTest(SpawnSource)] public static void one_body_lands_on_the_aim_at_the_spawn_height() {
            var c = Context(); var magic = SpawnMagics.Create("AquaArcher", "aqua_archer", 3).Bind(1, "aqua_archer", Element.None); var state = c.Random.State; magic.Run(c, Master.LeftPlayer, null, new Vector3(4, 0, 5));
            Assert.Equal(1, c.PendingObjects.Length); Assert.Equal(new Vector3(4, 3, 5), c.PendingObjects[0].Position); Assert.True(magic.Airborne); Assert.Equal(1, SpawnMagics.Quantity(c, magic.SummonedParameterKey)); Assert.Equal(state, c.Random.State);
        }
        [PortedTest(SpawnSource)] public static void a_swarm_scatters_within_one_unit() {
            var c = Context(); c.GameParameters["aqua_archer.quantity"] = 3; var magic = SpawnMagics.Create("AquaArcher", "aqua_archer").Bind(1, "aqua_archer", Element.None); magic.Run(c, Master.LeftPlayer, null, new Vector3(4, 0, 5));
            Assert.Equal(3, c.PendingObjects.Length); foreach (var o in c.PendingObjects) { Assert.True(Fix64.Abs(o.Position.X - 4) <= 1 && Fix64.Abs(o.Position.Z - 5) <= 1); Assert.Equal(Fix64.Zero, o.Position.Y); }
            Assert.Equal(3, SpawnMagics.Quantity(c, magic.SummonedParameterKey)); Assert.True(!magic.Airborne);
        }
        [PortedTest(BossSource)] public static void only_spawn_magics_report_is_spawn() {
            var registry = new MagicRegistry(); SpawnMagics.Register(registry); var spawn = registry.GetBean("zap_mouse").Bind(7, "zap_mouse", Element.None); var explosion = registry.GetBean("tornado_strike").Bind(8, "tornado_strike", Element.None);
            Assert.Equal(7L, spawn.Id); Assert.True(spawn.IsSpawnMagic); Assert.True(!explosion.IsSpawnMagic);
        }
        [PortedTest(BossSource)] public static void registers_the_thirty_java_beans() {
            var registry = new MagicRegistry(); SpawnMagics.Register(registry); Assert.Equal(30, registry.BeanNames().Length);
            foreach (var name in new[] { "aqua_archer", "thunder_bird_swarm", "earth_call", "tornado_strike" }) Assert.True(registry.ContainsBean(name));
            foreach (var name in registry.BeanNames()) { var magic = registry.GetBean(name).Bind(1, name, Element.None); var summon = name != "earth_call" && name != "tornado_strike"; Assert.Equal(summon, magic.SummonedParameterKey != null); Assert.Equal(summon, magic.IsSpawnMagic); }
        }
        [PortedTest("game/domain/magic/implement/spawn/earth_call_magic.rs")] public static void converts_small_and_medium_remnants_into_matching_owned_summons() {
            var c = Context(); c.GameParameters["earth_call.radius"] = 3;
            SimObject Remnant(string prefab, Master side, Vector3 position) { var o = c.Spawn(side, prefab, position); o.AddCollider(new CircleCollider(o, (Fix64)0.3m)); return o; }
            var small = Remnant("RockRemnant", Master.RightPlayer, new Vector3(2, 0, 3)); var medium = Remnant("MediumRockRemnant", Master.LeftPlayer, new Vector3(4, 0, 5)); var unrelated = Remnant("RockSlime", Master.RightPlayer, new Vector3(3, 0, 4)); c.FlushObjects();
            var registry = new MagicRegistry(); SpawnMagics.Register(registry); registry.GetBean("earth_call").Bind(1, "earth_call", Element.Rock).Run(c, Master.LeftPlayer, null, new Vector3(3, 0, 4));
            Assert.Sequence(new[] { "EarthCall", "MiniRock", "RockGolem" }, c.PendingObjects.Select(o => o.Prefab)); Assert.True(c.PendingObjects.Skip(1).All(o => o.Master == Master.LeftPlayer)); Assert.Equal(small.Position, c.PendingObjects[1].Position); Assert.Equal(medium.Position, c.PendingObjects[2].Position); Assert.True(small.IsDestroyed && medium.IsDestroyed && !unrelated.IsDestroyed);
        }
        public static void CheckRealSwarm() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; RockUnitPrefabs.Register(c);
            c.GameParameters["mini_rock.quantity"] = 3; c.GameParameters["mini_rock.mass"] = 1; c.GameParameters["mini_rock.radius"] = (Fix64)0.2m; c.GameParameters["mini_rock.hp"] = 10; c.GameParameters["mini_rock.damage"] = 2; c.GameParameters["mini_rock.speed"] = 1; c.GameParameters["mini_rock.attack_interval"] = 1;
            var registry = new MagicRegistry(); SpawnMagics.Register(registry); var magic = registry.GetBean("mini_rock_swarm").Bind(1, "mini_rock_swarm", Element.Rock); magic.Run(c, Master.RightPlayer, null, new Vector3(5, 9, 5));
            Assert.Equal(3, c.PendingObjects.Length); var rng = new JavaRandom(1);
            foreach (var o in c.PendingObjects) { Assert.True(o.HasComponent<Slime>() && o.HasComponent<RigidBody>()); Assert.Equal(Master.RightPlayer, o.Master); Assert.Equal(new Vector3(5 + rng.NextFloatFraction() * 2 - 1, 0, 5 + rng.NextFloatFraction() * 2 - 1), o.Position); }
            c.FlushObjects(); c.GameParameters["mini_rock.quantity"] = 0; var state = c.Random.State; magic.Run(c, Master.LeftPlayer, null, Vector3.Zero); Assert.Equal(0, c.PendingObjects.Length); Assert.Equal(state, c.Random.State);
        }
    }
}
