using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class BeanParityTests {
        private const string ProjectileSource = "game/domain/object/component/magic/bean_projectile.rs";
        private const string ShooterSource = "game/domain/object/component/mob/statemachine/attacker/bean_shooter_mob.rs";
        private sealed class Fixture {
            public readonly SimulationContext C = new SimulationContext { DeltaTime = (Fix64)0.5m };
            public readonly SimObject Bean;
            public readonly BeanProjectile Projectile;
            public Fixture() {
                C.InitializationFailed += (o, e) => throw e; C.RegisterPrefab("Object", _ => { }); BeanPrefabs.Register(C);
                C.GameParameters["bean.radius"] = (Fix64)0.2m; C.GameParameters["bean.speed"] = 4;
                Bean = C.Spawn(Master.LeftPlayer, "Bean", Vector3.Zero); Projectile = Bean.GetComponent<BeanProjectile>();
            }
            public DummyMob Target(Master side, Vector3 position) { var o = C.Spawn(side, "Object", position); var mob = new DummyMob(o, 100); o.AddComponentImmediately(mob); return mob; }
        }
        [PortedTest(ProjectileSource)] public static void damages_ground_enemy_once_and_destroys_itself() {
            var f = new Fixture(); var enemy = f.Target(Master.RightPlayer, new Vector3(3, 0, 0)); f.Projectile.Launch(enemy.Owner.Position, 30, 9);
            f.Projectile.OnCollisionWithEnemy(enemy.Owner); f.Projectile.OnCollisionWithEnemy(enemy.Owner); Assert.Equal(70, enemy.Hp); Assert.True(f.Bean.IsDestroyed);
        }
        [PortedTest(ProjectileSource)] public static void passes_through_air_enemy_and_ally() {
            var f = new Fixture(); var air = f.Target(Master.RightPlayer, new Vector3(1, 5, 0)); var ally = f.Target(Master.LeftPlayer, new Vector3(1, 0, 1)); f.Projectile.Launch(new Vector3(3, 0, 0), 30, 9);
            f.Projectile.OnCollisionWithEnemy(air.Owner); f.Projectile.OnCollisionWithEnemy(ally.Owner); Assert.Equal(100, air.Hp); Assert.Equal(100, ally.Hp); Assert.True(!f.Bean.IsDestroyed);
        }
        [PortedTest(ProjectileSource)] public static void flies_toward_target_at_its_speed() {
            var f = new Fixture(); f.Projectile.Launch(new Vector3(10, 0, 0), 30, 9); f.Projectile.Update(); Assert.Equal((Fix64)2, f.Bean.Position.X);
        }
        [PortedTest(ProjectileSource)] public static void destroys_itself_after_max_distance() {
            var f = new Fixture(); f.Projectile.Launch(new Vector3(10, 0, 0), 30, 3); f.Projectile.Update(); Assert.True(!f.Bean.IsDestroyed); f.Projectile.Update(); Assert.True(f.Bean.IsDestroyed);
        }
        [PortedTest(ShooterSource)] public static void targets_ground_only() {
            var f = new Fixture(); var shooter = f.C.Spawn(Master.LeftPlayer, "Object", Vector3.Zero); var mob = new BeanShooterMob(shooter, 260, (Fix64)0.6m, 130, (Fix64)2.2m, (Fix64)6.5m); Assert.Equal(1, mob.TargetMask);
        }
        [PortedTest(ShooterSource)] public static void fires_bean_at_grounded_target_with_shooter_damage_and_travel_limit() {
            var f = new Fixture(); f.C.FlushObjects(); f.C.DeltaTime = 1; f.C.GameParameters["bean.speed"] = 7;
            var shooter = f.C.Spawn(Master.LeftPlayer, "Object", Vector3.Zero); var target = f.Target(Master.RightPlayer, new Vector3(4, 0, 3));
            var mob = new BeanShooterMob(shooter, 260, (Fix64)0.6m, 130, (Fix64)2.2m, 2); Assert.True(mob.Behavior(target.Owner));
            var bean = f.C.PendingObjects.Single(o => o.Prefab == "Bean"); Assert.Equal(ObjectStatus.Attack, shooter.Status); bean.GetComponent<BeanProjectile>().Update();
            Assert.True(bean.Position.X > 0 && bean.Position.Z > 0); Assert.True(bean.IsDestroyed);
        }
        public static void CheckActualShooter() {
            var f = new Fixture(); var c = f.C; c.FlushObjects();
            c.GameParameters["bean_shooter.hp"] = 260; c.GameParameters["bean_shooter.mass"] = 1; c.GameParameters["bean_shooter.radius"] = (Fix64)0.35m;
            c.GameParameters["bean_shooter.speed"] = (Fix64)0.6m; c.GameParameters["bean_shooter.damage"] = 30; c.GameParameters["bean_shooter.attack_interval"] = (Fix64)2.2m; c.GameParameters["bean_shooter.attack_range"] = 4;
            var registry = new MagicRegistry(); SpawnMagics.Register(registry); registry.GetBean("bean_shooter").Bind(1, "bean_shooter", Element.Nature).Run(c, Master.RightPlayer, null, new Vector3(10, 4, 5));
            var shooter = c.PendingObjects.Single(o => o.Prefab == "BeanShooter"); var mob = shooter.GetComponent<BeanShooterMob>();
            Assert.Equal(new Vector3(10, 0, 5), shooter.Position); Assert.True(shooter.HasComponent<RigidBody>() && shooter.HasComponent<ZPhysics>() && shooter.HasComponent<CommonEffectReceiver>() && shooter.Elements.Has(Element.Nature));
            var enemy = f.Target(Master.LeftPlayer, new Vector3(6, 0, 5)); var secondReceiver = new DummyMob(enemy.Owner, 100); enemy.Owner.AddComponentImmediately(secondReceiver);
            Assert.True(mob.Behavior(enemy.Owner)); var bean = c.PendingObjects.Single(o => o.Prefab == "Bean"); var projectile = bean.GetComponent<BeanProjectile>(); Assert.Equal(Master.RightPlayer, bean.Master); Assert.Equal(100, enemy.Hp);
            shooter.Destroy(); projectile.Update(); Assert.Equal(new Vector3(8, 0, 5), bean.Position); Assert.True(!bean.IsDestroyed);
            var inactive = f.Target(Master.LeftPlayer, new Vector3(8, 0, 5)); inactive.Owner.Destroy(); projectile.OnCollisionWithEnemy(inactive.Owner); Assert.Equal(100, inactive.Hp); Assert.True(!bean.IsDestroyed);
            projectile.OnCollisionWithEnemy(enemy.Owner); Assert.Equal(70, enemy.Hp); Assert.Equal(70, secondReceiver.Hp); Assert.True(bean.IsDestroyed);
            var other = f.Target(Master.LeftPlayer, new Vector3(7, 0, 5)); projectile.OnCollisionWithEnemy(other.Owner); Assert.Equal(100, other.Hp);
            var lastPosition = bean.Position; projectile.Update(); Assert.Equal(lastPosition, bean.Position); Assert.True(!mob.Behavior(inactive.Owner));
            var miss = c.Spawn(Master.RightPlayer, "Bean", new Vector3(10, 0, 5)); var flight = miss.GetComponent<BeanProjectile>(); flight.Update(); Assert.Equal(new Vector3(10, 0, 5), miss.Position);
            flight.Launch(new Vector3(0, 9, 5), 30, 6); flight.Update(); flight.Update(); Assert.True(!miss.IsDestroyed); flight.Update(); Assert.True(miss.IsDestroyed); Assert.Equal(new Vector3(4, 0, 5), miss.Position);
        }
    }
}
