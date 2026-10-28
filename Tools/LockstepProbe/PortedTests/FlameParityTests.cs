using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class FlameParityTests {
        private const string LauncherSource = "game/domain/object/component/magic/flame_launcher.rs";
        private const string PrefabSource = "game/domain/object/prefab/implement/fire/dragon_flame_prefab_initializer.rs";
        private static SimulationContext Context() {
            var c = new SimulationContext { DeltaTime = (Fix64)1.5m }; c.InitializationFailed += (o, e) => throw e; c.RegisterPrefab("Object", _ => { }); DragonTowerPrefabs.Register(c);
            c.GameParameters["dragon_flame.radius"] = (Fix64)0.3m; c.GameParameters["dragon_flame.damage"] = 30; c.GameParameters["dragon_flame.speed"] = 8;
            c.GameParameters["dragon_tower.mass"] = 100; c.GameParameters["dragon_tower.radius"] = 1; c.GameParameters["dragon_tower.hp"] = 100;
            c.GameParameters["dragon_tower.attack_interval"] = (Fix64)1.5m; c.GameParameters["dragon_tower.attack_range"] = 8; c.GameParameters["dragon_tower.duration"] = 30; return c;
        }
        private static FlameLauncher Launcher(SimulationContext c, Master master = Master.LeftPlayer) {
            var tower = c.Spawn(master, "Object", new Vector3(master == Master.RightPlayer ? 14 : 4, 0, 5)); var launcher = new FlameLauncher(tower, (Fix64)1.5m, 8); tower.AddComponentImmediately(launcher); c.FlushObjects(); return launcher;
        }
        [PortedTest(LauncherSource)] public static void launches_a_flame_with_nothing_in_front_of_it() {
            var c = Context(); Launcher(c).Update(); Assert.Equal(1, c.PendingObjects.Length); var flame = c.PendingObjects[0];
            Assert.Equal("DragonFlame", flame.Prefab); Assert.Equal(Master.LeftPlayer, flame.Master); Assert.Equal(new Vector3(4, 0, 5), flame.Position); Assert.Equal(Vector3.Right, flame.GetComponent<IDirectionalShot>().Direction.Value);
        }
        [PortedTest(LauncherSource)] public static void gives_the_flame_the_towers_range_to_fly() {
            var c = Context(); Launcher(c).Update(); var flame = c.PendingObjects[0]; var destroyers = flame.GetPendingComponents<DistanceSelfDestroyer>(); Assert.Equal(1, destroyers.Length);
            flame.SetPosition(new Vector3((Fix64)11.99m, 0, 5)); destroyers[0].Update(); Assert.True(!flame.IsDestroyed); flame.SetPosition(new Vector3(12, 0, 5)); destroyers[0].Update(); Assert.True(flame.IsDestroyed);
        }
        [PortedTest(LauncherSource)] public static void aims_the_other_way_for_the_right_player() {
            var c = Context(); Launcher(c, Master.RightPlayer).Update(); Assert.Equal(1, c.PendingObjects.Length); Assert.Equal(Vector3.Left, c.PendingObjects[0].GetComponent<IDirectionalShot>().Direction.Value);
        }
        [PortedTest(LauncherSource)] public static void holds_its_fire_until_the_attack_interval_has_passed() {
            var c = Context(); var l = Launcher(c); c.DeltaTime = 1; l.Update(); Assert.Equal(0, c.PendingObjects.Length); l.Update(); Assert.Equal(1, c.PendingObjects.Length);
        }
        [PortedTest(LauncherSource)] public static void launches_nothing_without_an_owner() { var c = Context(); Launcher(c, Master.None).Update(); Assert.Equal(0, c.PendingObjects.Length); }
        [PortedTest(LauncherSource)] public static void an_attack_speed_effect_reaches_the_launcher_through_interval_attacker() {
            var c = Context(); var l = Launcher(c); l.Owner.GetComponent<IIntervalAttacker>().AttackInterval.ModifierPercent = (Fix64)(-0.5m); c.DeltaTime = (Fix64)0.75m; l.Update(); Assert.Equal(1, c.PendingObjects.Length);
        }
        [PortedTest(PrefabSource)] public static void advertises_dragon_flame_prefab_type() { Assert.Equal("DragonFlame", Context().Spawn(Master.LeftPlayer, "DragonFlame", Vector3.Zero).Prefab); }
        [PortedTest(PrefabSource)] public static void builds_a_burning_fire_shot_that_carries_the_flames_own_damage_and_speed() {
            var c = Context(); var flame = c.Spawn(Master.LeftPlayer, "DragonFlame", new Vector3(4, 0, 5)); Assert.True(flame.Elements.Has(Element.Fire)); Assert.Equal((Fix64)0.3m, flame.FirstCircleRadius(true).Value);
            Assert.Equal(VisualEffect.Burn, flame.GetComponent<EffectProvider>().Effect);
            var shot = flame.GetComponent<Shot>(); shot.SetTarget(new Vector3(18, 0, 5)); c.DeltaTime = (Fix64)0.5m; shot.Update(); Assert.Equal(new Vector3(8, 0, 5), flame.Position);
            var enemy = c.Spawn(Master.RightPlayer, "Object", flame.Position); enemy.AddCollider(new CircleCollider(enemy, (Fix64)0.5m)); var mob = new DummyMob(enemy, 100); enemy.AddComponentImmediately(mob); c.FlushObjects();
            shot.OnCollisionWithEnemy(enemy); Assert.Equal(70, mob.Hp); Assert.True(flame.IsDestroyed);
        }
        [PortedTest(PrefabSource)] public static void is_destroyed_when_it_flies_off_the_field() {
            var c = Context(); var flame = c.Spawn(Master.LeftPlayer, "DragonFlame", new Vector3(4, 0, 5)); var shot = flame.GetComponent<Shot>(); shot.SetTarget(new Vector3(18, 0, 5)); c.DeltaTime = 1;
            shot.Update(); Assert.Equal((Fix64)12, flame.Position.X); Assert.True(!flame.IsDestroyed); shot.Update(); Assert.True(flame.IsDestroyed);
        }
        public static void CheckMagic() {
            var c = Context(); var registry = new MagicRegistry(); BuildMagics.Register(registry); registry.GetBean("dragon_tower").Bind(1, "dragon_tower", Element.Fire).Run(c, Master.LeftPlayer, null, new Vector3(4, 6, 5));
            var tower = c.PendingObjects[0]; Assert.Equal(Fix64.Zero, tower.Position.Y); Assert.True(tower.HasComponent<CommonEffectReceiver>()); c.FlushObjects(); tower.Update(); var flame = c.PendingObjects[0]; c.FlushObjects();
            c.DeltaTime = (Fix64)0.5m; flame.Update(); Assert.Equal((Fix64)8, flame.Position.X); Assert.True(!flame.IsDestroyed); c.FlushObjects(); flame.Update(); Assert.Equal((Fix64)12, flame.Position.X); Assert.True(flame.IsDestroyed);
        }
    }
}
