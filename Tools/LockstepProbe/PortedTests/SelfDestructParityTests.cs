using System;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class SelfDestructParityTests {
        private const string Source = "game/domain/object/component/mob/statemachine/attacker/self_destruct_mob.rs";
        private sealed class NoDetector : IDetector { public SimObject Detect(SimObject source, Func<SimObject, bool> filter = null) => null; }
        private static SimulationContext Context() { var c = new SimulationContext(); c.RegisterPrefab("Object", _ => { }); c.InitializationFailed += (o, e) => throw e; return c; }
        private static SimObject Object(SimulationContext c, Master master, Vector3 p) { var o = c.Spawn(master, "Object", p); o.AddCollider(new CircleCollider(o, (Fix64)0.5m)); return o; }
        private static DummyMob Dummy(SimObject o) { var m = new DummyMob(o, 100); o.AddComponentImmediately(m); return m; }
        [PortedTest(Source)] public static void explosion_only_damages_active_enemies() {
            var c = Context(); var owner = Object(c, Master.LeftPlayer, Vector3.Zero); var ownMob = Dummy(owner); var ally = Dummy(Object(c, Master.LeftPlayer, Vector3.Zero)); var enemy = Dummy(Object(c, Master.RightPlayer, Vector3.Zero)); var dead = Dummy(Object(c, Master.RightPlayer, Vector3.Zero));
            var mob = new SelfDestructMob(owner, 10, 1, 3, 7, 1, 2, true); owner.AddComponentImmediately(mob); c.FlushObjects(); dead.Owner.Destroy(); mob.OnDeath();
            Assert.Equal(93, enemy.Hp); Assert.Equal(100, ally.Hp); Assert.Equal(100, dead.Hp); Assert.Equal(100, ownMob.Hp);
        }
        [PortedTest(Source)] public static void same_altitude_target_is_intercepted_instead_of_producing_nan_position() {
            var c = Context(); var owner = Object(c, Master.LeftPlayer, new Vector3(4, 3, 4)); var target = Object(c, Master.RightPlayer, new Vector3(6, 3, 4));
            var enemy = new SelfDestructMob(target, 10, 1, 2, 0, 1, 1, true); target.AddComponentImmediately(enemy);
            var mob = new SelfDestructMob(owner, 10, 1, 2, 7, 1, 2, true); owner.AddComponentImmediately(mob); c.FlushObjects(); mob.Start(); mob.SetState(new SelfDestructMob.AttackingState(mob, enemy)); mob.Update();
            Assert.Equal((Fix64)6, owner.Position.X); Assert.True(enemy.Hp < 10); Assert.True(owner.IsDestroyed);
        }
        [PortedTest(Source)] public static void explodes_when_altitude_drops_to_configured_threshold() {
            var c = Context(); var owner = Object(c, Master.LeftPlayer, new Vector3(0, (Fix64)1.1m, 0)); var enemy = Dummy(Object(c, Master.RightPlayer, Vector3.Zero));
            var mob = new SelfDestructMob(owner, 10, 1, 2, 7, 1, 2, true, 1); owner.AddComponentImmediately(mob); c.FlushObjects(); mob.Start(); mob.Update(); Assert.True(!owner.IsDestroyed); Assert.Equal(100, enemy.Hp);
            owner.SetPosition(new Vector3(0, 1, 0)); mob.Update(); Assert.True(owner.IsDestroyed); Assert.Equal(93, enemy.Hp);
        }
        public static void CheckActualWindSpirit() {
            var c = Context(); SelfDestructMob.RegisterWindSpirit(c);
            c.GameParameters["wind_spirit.hp"] = 100; c.GameParameters["wind_spirit.mass"] = 1; c.GameParameters["wind_spirit.radius"] = (Fix64)0.5m; c.GameParameters["wind_spirit.speed"] = 2;
            c.GameParameters["wind_spirit.damage"] = 7; c.GameParameters["wind_spirit.attack_interval"] = 99; c.GameParameters["wind_spirit.attack_range"] = 2;
            var registry = new MagicRegistry(); SpawnMagics.Register(registry);
            SimObject Spawn() { registry.GetBean("wind_spirit").Bind(1, "wind_spirit", Element.Wind).Run(c, Master.LeftPlayer, null, new Vector3(4, 0, 4)); return c.PendingObjects[c.PendingObjects.Length - 1]; }
            var owner = Spawn(); var mob = owner.GetComponent<SelfDestructMob>(); var physics = owner.GetComponent<ZPhysics>(); var enemy = Dummy(Object(c, Master.RightPlayer, new Vector3(6, 3, 4))); c.FlushObjects();
            Assert.Equal((Fix64)3, owner.Position.Y); Assert.Equal((Fix64)0.8m, mob.AttackRange); Assert.Equal(Fix64.Zero, mob.AttackInterval.Total); Assert.Equal(2, mob.TargetMask); Assert.True(mob.VerticalRangeIgnored && !mob.FallsOnDeath); Assert.True(physics.CanHover);
            Assert.True(mob.Behavior(enemy.Owner)); Assert.True(!physics.CanHover); mob.CurrentState.OnUpdate(); Assert.True(owner.IsDestroyed); Assert.True(physics.CanHover); Assert.Equal(93, enemy.Hp); mob.OnCollisionWithEnemy(enemy.Owner); mob.OnDeath(); Assert.Equal(93, enemy.Hp);
            var cancelled = Spawn(); var cancelMob = cancelled.GetComponent<SelfDestructMob>(); var cancelPhysics = cancelled.GetComponent<ZPhysics>(); var disappearing = Dummy(Object(c, Master.RightPlayer, new Vector3(5, 3, 4))); c.FlushObjects();
            cancelMob.Behavior(disappearing.Owner); disappearing.Owner.Destroy(); cancelMob.CurrentState.OnUpdate(); Assert.True(cancelMob.CurrentState is BehaviorMob.IdleState); Assert.True(cancelPhysics.CanHover); Assert.True(!cancelled.IsDestroyed);
            // 다른 높이로 공격할 때 수평 위치만 dive progress로 바꾸고 높이는 물리 시스템에 맡긴다.
            var lower = Dummy(Object(c, Master.RightPlayer, new Vector3(8, 0, 4))); c.FlushObjects(); cancelMob.Detector = new NoDetector(); cancelMob.Behavior(lower.Owner); cancelled.SetPosition(new Vector3(4, (Fix64)1.5m, 4)); cancelMob.CurrentState.OnUpdate(); Assert.Equal(new Vector3(6, (Fix64)1.5m, 4), cancelled.Position); Assert.True(!cancelled.IsDestroyed);
            cancelled.SetPosition(new Vector3(6, 4, 4)); cancelMob.CurrentState.OnUpdate(); Assert.Equal(new Vector3(4, 4, 4), cancelled.Position);
            cancelMob.SetIdle(); Assert.True(cancelPhysics.CanHover);
            var dying = Spawn(); var deathMob = dying.GetComponent<SelfDestructMob>(); var nearby = Dummy(Object(c, Master.RightPlayer, new Vector3(4, 3, 5))); c.FlushObjects(); deathMob.OnDamaged(new Hit(1000, 0, Element.None));
            Assert.True(dying.IsDestroyed); Assert.Equal((Fix64)3, dying.Position.Y); Assert.True(!dying.HasComponent<AerialDeathFall>()); Assert.Equal(93, nearby.Hp);
        }
    }
}
