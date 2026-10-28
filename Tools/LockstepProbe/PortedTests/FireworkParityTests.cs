using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class FireworkParityTests {
        private const string LauncherSource = "game/domain/object/component/magic/firework_launcher.rs";
        private const string ShellSource = "game/domain/object/prefab/implement/subprefab/firework_shell_prefab_initializer.rs";
        private static SimulationContext Context() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; c.RegisterPrefab("Object", _ => { }); FireworkPrefabs.Register(c);
            c.GameParameters["firework_shell.radius"] = (Fix64)2.5m; c.GameParameters["firework_shell.damage"] = 40; c.GameParameters["firework_shell.duration"] = (Fix64)1.5m;
            c.GameParameters["firework_tower.mass"] = 100; c.GameParameters["firework_tower.radius"] = (Fix64)0.5m; c.GameParameters["firework_tower.hp"] = 100;
            c.GameParameters["firework_tower.attack_interval"] = 2; c.GameParameters["firework_tower.attack_offset"] = 3; c.GameParameters["firework_tower.duration"] = 60; return c;
        }
        private static FireworkLauncher Launcher(SimulationContext c, Master master) {
            var o = c.Spawn(master, "Object", new Vector3(1, 0, 2)); c.FlushObjects(); return new FireworkLauncher(o, 2, 3);
        }
        private static void Launch(Master master, Vector3 impact) {
            var c = Context(); var launcher = Launcher(c, master); c.DeltaTime = 2; launcher.Update();
            var events = c.DrainEvents(); Assert.Equal(1, events.Length); Assert.Equal(new Vector3(1, 0, 2), events[0].StartPosition.Value); Assert.Equal(impact, events[0].EndPosition.Value);
            Assert.Equal("FireworkShell", events[0].Type); Assert.Equal((Fix64)0.5m, events[0].Duration); Assert.Equal(0, c.PendingObjects.Length);
        }
        private static void Land(Master master, Vector3 impact) {
            var c = Context(); var launcher = Launcher(c, master); c.DeltaTime = 2; launcher.Update(); Assert.Equal(0, c.PendingObjects.Length);
            c.DeltaTime = (Fix64)0.25m; launcher.Update(); Assert.Equal(0, c.PendingObjects.Length); launcher.Update();
            Assert.Equal(1, c.PendingObjects.Length); var shell = c.PendingObjects[0]; Assert.Equal("FireworkShell", shell.Prefab); Assert.Equal(master, shell.Master); Assert.Equal(impact, shell.Position);
        }
        [PortedTest(LauncherSource)] public static void launch_tick_sends_a_projectile_but_no_shell_yet_for_left_player() { Launch(Master.LeftPlayer, new Vector3(4, 0, 2)); }
        [PortedTest(LauncherSource)] public static void launch_tick_sends_a_projectile_but_no_shell_yet_for_right_player() { Launch(Master.RightPlayer, new Vector3(-2, 0, 2)); }
        [PortedTest(LauncherSource)] public static void shell_appears_at_impact_point_only_after_flight_duration_for_left_player() { Land(Master.LeftPlayer, new Vector3(4, 0, 2)); }
        [PortedTest(LauncherSource)] public static void shell_appears_at_impact_point_only_after_flight_duration_for_right_player() { Land(Master.RightPlayer, new Vector3(-2, 0, 2)); }
        [PortedTest(LauncherSource)] public static void master_none_produces_neither_a_projectile_nor_a_shell() {
            var c = Context(); var l = Launcher(c, Master.None); c.DeltaTime = 2; l.Update(); Assert.Equal(0, c.DrainEvents().Length); Assert.Equal(0, c.PendingObjects.Length);
        }
        [PortedTest(LauncherSource)] public static void launches_exactly_one_shot_per_attack_interval() {
            var c = Context(); var l = Launcher(c, Master.LeftPlayer); c.DeltaTime = (Fix64)0.5m;
            for (var i = 0; i < 3; i++) l.Update(); Assert.Equal(0, c.DrainEvents().Length); l.Update(); Assert.Equal(1, c.DrainEvents().Length);
            for (var i = 0; i < 3; i++) l.Update(); Assert.Equal(0, c.DrainEvents().Length); l.Update(); Assert.Equal(1, c.DrainEvents().Length);
        }
        [PortedTest(ShellSource)] public static void advertises_firework_shell_prefab_type() { Assert.Equal("FireworkShell", Context().Spawn(Master.LeftPlayer, "FireworkShell", Vector3.Zero).Prefab); }
        [PortedTest(ShellSource)] public static void wires_radius_duration_and_element_from_parameters() {
            var o = Context().Spawn(Master.LeftPlayer, "FireworkShell", Vector3.Zero); Assert.Equal((Fix64)2.5m, o.FirstCircleRadius(true).Value); Assert.Equal((Fix64)1.5m, o.GetComponent<TimedSelfDestroyer>().GetGauge().MaxValue);
            Assert.True(o.Elements.Has(Element.Fire)); Assert.Equal(1, o.GetComponents<OnStartAttacker>().Length);
        }
        [PortedTest(ShellSource)] public static void on_start_attacker_uses_the_same_radius_as_the_collider_to_damage_enemies_in_range() {
            var c = Context(); c.GameParameters["firework_shell.radius"] = 2;
            var target = c.Spawn(Master.RightPlayer, "Object", new Vector3(1, 0, 0)); var mob = new DummyMob(target, 100); target.AddComponentImmediately(mob); c.FlushObjects();
            c.Spawn(Master.LeftPlayer, "FireworkShell", Vector3.Zero); Assert.Equal(60, mob.Hp);
        }
        public static void CheckTower() {
            var c = Context(); var registry = new MagicRegistry(); BuildMagics.Register(registry); registry.GetBean("firework_tower").Bind(1, "firework_tower", Element.Fire).Run(c, Master.LeftPlayer, null, new Vector3(5, 0, 5));
            var tower = c.PendingObjects[0]; var target = c.Spawn(Master.RightPlayer, "Object", new Vector3(8, 0, 5)); var mob = new DummyMob(target, 100); target.AddComponentImmediately(mob); c.FlushObjects();
            c.DeltaTime = 2; tower.Update(); Assert.Equal(100, mob.Hp); Assert.Equal(1, c.DrainEvents().Length);
            c.DeltaTime = (Fix64)0.5m; tower.Update(); Assert.Equal(60, mob.Hp); Assert.Equal(1, c.PendingObjects.Length); c.FlushObjects(); c.DrainEvents();
            c.DeltaTime = (Fix64)1.5m; tower.Update(); Assert.Equal(1, c.DrainEvents().Length); tower.Destroy(); c.FlushObjects();
            c.DeltaTime = 1; foreach (var o in c.Objects) o.Update(); Assert.Equal(60, mob.Hp); Assert.Equal(0, c.PendingObjects.Length);
        }
    }
}
