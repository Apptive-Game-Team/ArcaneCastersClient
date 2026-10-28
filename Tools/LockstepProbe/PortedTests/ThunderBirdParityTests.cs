using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class ThunderBirdParityTests {
        private const string Source = "game/domain/object/component/mob/statemachine/attacker/thunder_bird_mob.rs";
        private static SimulationContext Context() { var c = new SimulationContext { DeltaTime = (Fix64)0.05m }; c.InitializationFailed += (o, e) => throw e; c.RegisterPrefab("Object", _ => { }); return c; }
        private static DummyMob Target(SimulationContext c, Vector3 position) { var o = c.Spawn(Master.RightPlayer, "Object", position); o.AddCollider(new CircleCollider(o, (Fix64)0.5m)); var mob = new DummyMob(o, 100); o.AddComponentImmediately(mob); return mob; }
        [PortedTest(Source)] public static void stops_diving_at_an_already_destroyed_target() {
            var c = Context(); var target = Target(c, new Vector3(1, 0, 0)); var bird = c.Spawn(Master.LeftPlayer, "Object", new Vector3(0, 3, 0)); bird.AddCollider(new CircleCollider(bird, (Fix64)0.3m)); bird.AddComponentImmediately(new RigidBody(bird, 1));
            var mob = new ThunderBirdMob(bird, 10, (Fix64)1.5m, 1, 5, 3, 4); bird.AddComponentImmediately(mob); c.FlushObjects(); mob.Start(); mob.SetState(new ThunderBirdMob.AttackingState(mob, target)); var before = bird.Position; target.Owner.Destroy(); mob.Update(); Assert.Equal(100, target.Hp); Assert.Equal(before, bird.Position);
        }
        public static void CheckActualSwarm() {
            var c = Context(); ThunderBirdMob.RegisterPrefab(c); c.GameParameters["thunder_bird.hp"] = 100; c.GameParameters["thunder_bird.mass"] = 1; c.GameParameters["thunder_bird.radius"] = (Fix64)0.5m; c.GameParameters["thunder_bird.speed"] = 2;
            c.GameParameters["thunder_bird.damage"] = 10; c.GameParameters["thunder_bird.attack_interval"] = 3; c.GameParameters["thunder_bird.attack_range"] = 4; c.GameParameters["thunder_bird.quantity"] = 2;
            var registry = new MagicRegistry(); SpawnMagics.Register(registry); registry.GetBean("thunder_bird_swarm").Bind(1, "thunder_bird", Element.Lightning).Run(c, Master.LeftPlayer, null, new Vector3(4, 0, 4)); var birds = c.PendingObjects;
            Assert.Equal(2, birds.Length); foreach (var bird in birds) { Assert.Equal("ThunderBird", bird.Prefab); Assert.Equal((Fix64)3, bird.Position.Y); Assert.True(bird.HasComponent<ElectricDeathEnergy>() && bird.HasComponent<LightningSummonEffectReceiver>() && bird.Elements.NativeHas(Element.Lightning)); Assert.True(!bird.Elements.NativeHas(Element.Wind)); }
            var owner = birds[0]; owner.SetPosition(new Vector3(4, 3, 4)); var mob = owner.GetComponent<ThunderBirdMob>(); var physics = owner.GetComponent<ZPhysics>(); var target = Target(c, new Vector3(5, 0, 4)); var second = new DummyMob(target.Owner, 100); target.Owner.AddComponentImmediately(second); c.FlushObjects();
            Assert.Equal(1, mob.TargetMask); Assert.True(mob.VerticalRangeIgnored); Assert.True(mob.Behavior(target.Owner)); Assert.True(!physics.CanHover); Assert.True(mob.CurrentState is ThunderBirdMob.AttackingState);
            owner.SetPosition(new Vector3(4, (Fix64)1.5m, 4)); mob.CurrentState.OnUpdate(); Assert.Equal(new Vector3((Fix64)4.5m, (Fix64)1.5m, 4), owner.Position); Assert.Equal(100, target.Hp);
            owner.SetPosition(new Vector3(5, (Fix64)0.5m, 4)); mob.CurrentState.OnUpdate(); Assert.Equal(90, target.Hp); Assert.Equal(100, second.Hp); Assert.True(mob.CurrentState is ThunderBirdMob.FloatingState); Assert.True(physics.CanHover); Assert.True(!owner.IsDestroyed);
            mob.CurrentState.OnUpdate(); Assert.True(mob.CurrentState is ThunderBirdMob.FloatingState); c.DeltaTime = (Fix64)2.5m; physics.ApplyHover(); mob.CurrentState.OnUpdate(); Assert.True(mob.CurrentState is BehaviorMob.IdleState);
            owner.SetPosition(new Vector3(5, (Fix64)2.5m, 4)); Assert.True(mob.Behavior(target.Owner)); Assert.True(mob.CurrentState is ThunderBirdMob.FloatingState); Assert.Equal(90, target.Hp);
            owner.SetPosition(new Vector3(5, 3, 4)); mob.Behavior(target.Owner); owner.SetPosition(new Vector3(5, 1, 4)); mob.CurrentState.OnUpdate(); Assert.Equal(82, target.Hp);
            owner.SetPosition(new Vector3(4, 3, 4)); mob.Behavior(target.Owner); target.Owner.Destroy(); var before = owner.Position; mob.CurrentState.OnUpdate(); Assert.Equal(before, owner.Position); Assert.True(physics.CanHover); Assert.True(mob.CurrentState is ThunderBirdMob.FloatingState);
            var noMob = c.Spawn(Master.RightPlayer, "Object", new Vector3(5, 0, 4)); Assert.True(!mob.Behavior(noMob));
            var hit = c.DrainEvents().Last(e => e.Type == "hit"); Assert.Equal(owner.Id, hit.SourceId);
        }
    }
}
