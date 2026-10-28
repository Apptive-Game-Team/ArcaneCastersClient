using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class WindPushParityTests {
        private const string Source = "game/domain/object/component/build/wind_push_component.rs";
        private static SimulationContext Context() { var c = new SimulationContext(1) { DeltaTime = (Fix64)0.25m }; c.RegisterPrefab("Body", _ => { }); return c; }
        private static SimObject Mob(SimulationContext c, Master master, Vector3 position, int mass = 1) { var o = c.Spawn(master, "Body", position); o.AddComponentImmediately(new DummyMob(o, 10)); o.AddComponentImmediately(new RigidBody(o, mass)); return o; }
        [PortedTest(Source)] public static void applies_push_every_half_second_to_enemies_only() {
            var c = Context(); var owner = c.Spawn(Master.LeftPlayer, "Body", Vector3.Zero); var enemy = Mob(c, Master.RightPlayer, Vector3.Zero); var ally = Mob(c, Master.LeftPlayer, Vector3.Zero); c.FlushObjects();
            var wind = new WindPushComponent(owner, 10, new Vector3(6, 1, 3)); wind.Update(); wind.Update(); wind.Update();
            Assert.Equal(1, enemy.GetPendingComponents<TimedMassPush>().Length); Assert.Equal(0, ally.GetPendingComponents<TimedMassPush>().Length);
        }
        [PortedTest(Source)] public static void push_box_covers_ground_range_on_x_and_z_without_swapping_axes() {
            var c = Context(); var owner = c.Spawn(Master.LeftPlayer, "Body", new Vector3(4, 0, 5));
            var wind = new WindPushComponent(owner, 10, new Vector3(6, 1, 3)); wind.Update();
            Assert.Equal(new Vector3(6, 1, 3), wind.BoxSize); Assert.Equal(new Vector3(7, (Fix64)0.5m, 5), wind.BoxCenter);
        }
        public static void CheckPushBoundaryAndRefresh() {
            foreach (var master in new[] { Master.LeftPlayer, Master.RightPlayer }) {
                var c = Context(); var other = master == Master.LeftPlayer ? Master.RightPlayer : Master.LeftPlayer; int direction = master == Master.LeftPlayer ? 1 : -1;
                WindBuildingPrefabs.Register(c);
                foreach (string key in new[] { "mass", "radius", "attack_interval", "push_range_y" }) c.GameParameters["wind_totem." + key] = 1;
                c.GameParameters["wind_totem.hp"] = 20; c.GameParameters["wind_totem.damage"] = 3; c.GameParameters["wind_totem.push_force"] = 10; c.GameParameters["wind_totem.push_range_x"] = 6; c.GameParameters["wind_totem.push_range_z"] = 3;
                var owner = c.Spawn(master, "WindTotem", new Vector3(7, 4, 5)); var wind = owner.GetComponent<WindPushComponent>(); Assert.True(wind != null && !owner.IsDestroyed);
                Assert.Equal((Fix64)6, owner.GetComponent<TimedSelfDestroyer>().GetGauge().MaxValue);
                var ground = Mob(c, other, new Vector3(7 + direction * 3, 0, 5), 4); var aerial = Mob(c, other, new Vector3(7 + direction * 3, 3, 5));
                var outside = Mob(c, other, new Vector3(7 + direction * 3, 0, 7)); c.FlushObjects(); wind.Update(); ground.FlushComponents();
                var push = ground.GetComponent<TimedMassPush>(); Assert.True(push != null); push.Update(); Assert.Equal(new Vector3(direction * 5, 0, 0), ground.GetComponent<RigidBody>().ConsumeVelocity());
                Assert.Equal(0, aerial.GetPendingComponents<TimedMassPush>().Length); Assert.Equal(0, outside.GetPendingComponents<TimedMassPush>().Length);
                var late = Mob(c, other, new Vector3(7 + direction * 2, 0, 5)); c.FlushObjects(); wind.Update(); Assert.Equal(0, late.GetPendingComponents<TimedMassPush>().Length);
                wind.Update(); Assert.Equal(1, late.GetPendingComponents<TimedMassPush>().Length); Assert.Equal(0, ground.GetPendingComponents<TimedMassPush>().Length);
            }
        }
    }
}
