using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class BuildEffectPrefabParityTests {
        private const string Source = "game/domain/object/prefab/implement/build/tests.rs";
        [PortedTest(Source)] public static void wires_hp_radius_and_duration_from_parameters_onto_their_components() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; NatureUnitPrefabs.Register(c);
            c.GameParameters["repair_totem.hp"] = 150; c.GameParameters["repair_totem.radius"] = 4; c.GameParameters["repair_totem.effect_radius"] = 6; c.GameParameters["repair_totem.duration"] = 20; c.GameParameters["repair_totem.mass"] = 1000000;
            var o = c.Spawn(Master.LeftPlayer, "RepairTotem", Vector3.Zero); Assert.Equal((Fix64)150, o.GetComponent<DummyMob>().GetGauge().MaxValue); Assert.Equal((Fix64)4, o.Colliders.Single().CircleRadius.Value);
            Assert.True(o.HasComponent<RepairAura>() && o.HasComponent<BuildingEffectReceiver>()); Assert.Equal((Fix64)20, o.GetComponent<TimedSelfDestroyer>().GetGauge().MaxValue); Assert.True(o.Elements.Total.Contains(Element.Nature));
        }
        [PortedTest(Source)] public static void wind_totem_lives_for_hp_times_attack_interval_over_damage_seconds_truncated_to_an_int() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; WindBuildingPrefabs.Register(c);
            string[] keys = { "mass", "radius", "hp", "attack_interval", "damage", "push_force", "push_range_x", "push_range_y", "push_range_z" }; decimal[] values = { 1, 0.5m, 100, 1.5m, 4, 3, 2, 1, 2 };
            for (int i = 0; i < keys.Length; i++) c.GameParameters["wind_totem." + keys[i]] = (Fix64)values[i]; var o = c.Spawn(Master.LeftPlayer, "WindTotem", Vector3.Zero);
            Assert.Equal((Fix64)37, o.GetComponent<TimedSelfDestroyer>().GetGauge().MaxValue); Assert.True(o.Elements.NativeHas(Element.Wind));
        }
        private static SimObject Trap() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; ShockTrapDetector.Register(c);
            string[] keys = { "mass", "radius", "effect_radius", "hp", "trigger_delay", "stun_duration" }; decimal[] values = { 5, 2.5m, 3.5m, 30, 1.2m, 2.4m };
            for (int i = 0; i < keys.Length; i++) c.GameParameters["shock_trap." + keys[i]] = (Fix64)values[i]; return c.Spawn(Master.LeftPlayer, "ShockTrap", Vector3.Zero);
        }
        [PortedTest(Source)] public static void wires_shock_trap_components_from_parameters() { var o = Trap(); Assert.True(o.Elements.NativeHas(Element.Lightning) && o.HasComponent<RigidBody>() && o.HasComponent<DummyMob>() && o.HasComponent<BuildingEffectReceiver>() && o.HasComponent<ShockTrapDetector>()); Assert.True(!o.HasComponent<TimedSelfDestroyer>()); }
        [PortedTest(Source)] public static void gives_the_collider_the_body_radius() => Assert.Equal((Fix64)2.5m, Trap().Colliders.Single().CircleRadius.Value);
        [PortedTest(Source)] public static void life_tree_heals_within_effect_radius() {
            var c = new SimulationContext { DeltaTime = 1 }; c.InitializationFailed += (o, e) => throw e; NatureUnitPrefabs.Register(c); c.RegisterPrefab("Ally", _ => { });
            string[] keys = { "mass", "hp", "damage", "radius", "range", "effect_radius", "attack_interval", "duration" }; decimal[] values = { 1, 100, -3, 0.5m, 6, 1.5m, 1, 30 };
            for (int i = 0; i < keys.Length; i++) c.GameParameters["life_tree." + keys[i]] = (Fix64)values[i]; var tree = c.Spawn(Master.LeftPlayer, "LifeTree", Vector3.Zero);
            DummyMob Ally(int x) { var o = c.Spawn(Master.LeftPlayer, "Ally", new Vector3(x, 0, 0)); var m = new DummyMob(o, 20); o.AddComponentImmediately(m); m.ApplyDamage(new Hit(10, 0, Element.None)); return m; }
            var near = Ally(1); var far = Ally(3); c.FlushObjects(); tree.GetComponent<Totem>().Update(); Assert.Equal(13, near.Hp); Assert.Equal(10, far.Hp);
        }
    }
}
