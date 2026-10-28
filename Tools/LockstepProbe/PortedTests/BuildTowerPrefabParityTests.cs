using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class BuildTowerPrefabParityTests {
        private const string Source = "game/domain/object/prefab/implement/build/tests.rs";
        private static SimObject Build(bool firework) {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e;
            if (firework) FireworkPrefabs.Register(c); else DragonTowerPrefabs.Register(c);
            string key = firework ? "firework_tower" : "dragon_tower";
            string[] names = { "mass", "radius", "hp", "attack_interval", firework ? "attack_offset" : "attack_range", "duration" };
            decimal[] values = firework ? new decimal[] { -1, 1.2m, 500, 4, 3, 60 } : new decimal[] { 1, 0.8m, 120, 1.5m, 8, 20 };
            for (int i = 0; i < names.Length; i++) c.GameParameters[key + "." + names[i]] = (Fix64)values[i];
            c.GameParameters["firework_shell.radius"] = 1; c.GameParameters["firework_shell.duration"] = 1; c.GameParameters["firework_shell.damage"] = 10;
            return c.Spawn(Master.LeftPlayer, firework ? "FireworkTower" : "DragonTower", new Vector3(5, 0, 5));
        }
        [PortedTest(Source)] public static void advertises_dragon_tower_prefab_type() => Assert.Equal("DragonTower", Build(false).Prefab);
        [PortedTest(Source)] public static void builds_a_launcher_tower_that_expires_after_duration() {
            var o = Build(false); Assert.True(o.Elements.Has(Element.Fire)); Assert.Equal((Fix64)0.8m, o.Colliders.Single().CircleRadius.Value); Assert.True(!o.Colliders.Single().IsTrigger);
            Assert.Equal(120, o.GetComponent<DummyMob>().Hp); Assert.Equal(120, o.GetComponent<DummyMob>().MaxHp); Assert.True(o.HasComponent<FlameLauncher>() && o.HasComponent<CommonEffectReceiver>()); Assert.Equal((Fix64)20, o.GetComponent<TimedSelfDestroyer>().GetGauge().MaxValue);
        }
        [PortedTest(Source)] public static void does_not_attach_the_targeting_tower_component() => Assert.True(!Build(false).HasComponent<Tower>());
        [PortedTest(Source)] public static void advertises_firework_tower_prefab_type() => Assert.Equal("FireworkTower", Build(true).Prefab);
        [PortedTest(Source)] public static void wires_hp_collider_and_duration_from_parameters() {
            var o = Build(true); Assert.Equal((Fix64)1.2m, o.Colliders.Single().CircleRadius.Value); Assert.True(!o.Colliders.Single().IsTrigger); Assert.Equal(1, o.GetComponents<RigidBody>().Count()); Assert.Equal(1, o.GetComponents<BuildingEffectReceiver>().Count());
            Assert.Equal(500, o.GetComponent<DummyMob>().Hp); Assert.Equal(500, o.GetComponent<DummyMob>().MaxHp); Assert.Equal((Fix64)60, o.GetComponent<TimedSelfDestroyer>().GetGauge().MaxValue); Assert.True(o.Elements.Has(Element.Fire));
        }
        [PortedTest(Source)] public static void firework_launcher_uses_attack_interval_and_attack_offset_from_parameters() {
            var o = Build(true); var c = o.Context; c.FlushObjects(); var launcher = o.GetComponent<FireworkLauncher>(); c.DeltaTime = 4; launcher.Update(); Assert.True(!c.PendingObjects.Any(p => p.Prefab == "FireworkShell"));
            c.DeltaTime = (Fix64)0.5m; launcher.Update(); var shell = c.PendingObjects.Single(p => p.Prefab == "FireworkShell"); Assert.Equal(new Vector3(8, 0, 5), shell.Position);
        }
    }
}
