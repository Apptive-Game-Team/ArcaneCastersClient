using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class CloudDragonParityTests {
        private const string Source = "game/domain/object/component/mob/statemachine/attacker/cloud_dragon_mob.rs";
        private sealed class Recorder : CloudDragonMob {
            public readonly List<SimObject> Shots = new List<SimObject>();
            public Recorder(SimObject owner) : base(owner, 30, (Fix64)0.6m, 3, 5, 1, 5, 15) { }
            protected override void FireChainLightning(SimObject target) { Shots.Add(target); }
        }
        private sealed class PassiveState : IMobState { public void OnEnter() { } public void OnExit() { } public void OnUpdate() { } }
        private static SimulationContext Context() { var c = new SimulationContext(); c.RegisterPrefab("Object", _ => { }); c.InitializationFailed += (o, e) => throw e; return c; }
        [PortedTest(Source)] public static void fires_chain_lightning_once_per_cooldown_at_current_target() {
            var c = Context(); var owner = c.Spawn(Master.LeftPlayer, "Object", Vector3.Zero); var target = c.Spawn(Master.RightPlayer, "Object", new Vector3(3, 0, 0)); var mob = new Recorder(owner); mob.Target = target;
            c.DeltaTime = (Fix64)14.9m; mob.Update(); Assert.Equal(0, mob.Shots.Count); c.DeltaTime = (Fix64)0.1m; mob.Update(); Assert.Equal(1, mob.Shots.Count); Assert.Equal(target, mob.Shots[0]);
            c.DeltaTime = (Fix64)14.9m; mob.Update(); Assert.Equal(1, mob.Shots.Count); c.DeltaTime = (Fix64)0.1m; mob.Update(); Assert.Equal(2, mob.Shots.Count);
        }
        public static void CheckActualDragon() {
            var c = Context(); CloudDragonMob.RegisterPrefabs(c);
            c.GameParameters["cloud_dragon.hp"] = 100; c.GameParameters["cloud_dragon.mass"] = 2; c.GameParameters["cloud_dragon.radius"] = (Fix64)0.5m; c.GameParameters["cloud_dragon.speed"] = 1;
            c.GameParameters["cloud_dragon.damage"] = 5; c.GameParameters["cloud_dragon.attack_interval"] = 1; c.GameParameters["cloud_dragon.attack_range"] = 6; c.GameParameters["cloud_dragon.chain_lightning_cooldown"] = 2;
            c.GameParameters["chain_lightning.radius"] = (Fix64)0.25m; c.GameParameters["chain_lightning.speed"] = (Fix64)8.9m; c.GameParameters["chain_lightning.damage"] = 18; c.GameParameters["chain_lightning.attack_range"] = 10; c.GameParameters["chain_lightning.min_damage"] = 2;
            var registry = new MagicRegistry(); SpawnMagics.Register(registry); registry.GetBean("cloud_dragon").Bind(1, "cloud_dragon", Element.Water).Run(c, Master.LeftPlayer, null, new Vector3(2, 0, 5)); var owner = c.PendingObjects[0]; var mob = owner.GetComponent<CloudDragonMob>();
            DummyMob Target(int x) { var o = c.Spawn(Master.RightPlayer, "Object", new Vector3(x, 3, 5)); o.AddCollider(new CircleCollider(o, (Fix64)0.25m)); var m = new DummyMob(o, 100); o.AddComponentImmediately(m); return m; }
            var first = Target(6); var second = Target(7); c.FlushObjects(); mob.SetState(new PassiveState()); Assert.Equal((Fix64)3, owner.Position.Y); Assert.Equal(3, mob.TargetMask); Assert.True(owner.HasComponent<AreaEffectProvider>() && owner.HasComponent<CommonEffectReceiver>() && owner.GetComponent<ZPhysics>().CanHover); Assert.Sequence(new[] { Element.Water, Element.Wind, Element.Lightning }.OrderBy(e => e), owner.Elements.Total);
            c.DeltaTime = 10; mob.Update(); Assert.Equal(0, c.PendingObjects.Length); mob.Target = first.Owner; c.DeltaTime = (Fix64)0.5m; mob.Update(); var shot = c.PendingObjects.Single(); Assert.Equal("ChainLightning", shot.Prefab); Assert.Equal(owner.Master, shot.Master); Assert.Equal(owner.Position, shot.Position); Assert.Equal(VisualEffect.Shock, shot.GetComponent<EffectProvider>().Effect); Assert.True(shot.Colliders.Single().IsTrigger);
            var chain = shot.GetComponent<ChainShot>(); chain.Update(); Assert.Equal(new Vector3(6, 3, 5), shot.Position); chain.OnCollisionWithEnemy(first.Owner); Assert.Equal(82, first.Hp); chain.OnCollisionWithEnemy(second.Owner); Assert.Equal(100, second.Hp);
            c.DeltaTime = (Fix64)0.125m; chain.Update(); chain.Update(); chain.OnCollisionWithEnemy(second.Owner); Assert.Equal(91, second.Hp); Assert.True(shot.IsDestroyed);
            Assert.True(mob.Behavior(first.Owner)); Assert.Equal(77, first.Hp); Assert.Equal((Fix64)0.5m, c.DrainEvents().Single(e => e.Type == "WaterShot").Duration);
            // 쿨다운의 raw 변환 오차 상한 밖에서는 발사하지 않고, 0초 update는 그 상한을 늘리지 않는다.
            var boundaryOwner = c.Spawn(Master.LeftPlayer, "Object", Vector3.Zero); var boundary = new Recorder(boundaryOwner); boundary.Target = first.Owner;
            c.DeltaTime = (Fix64)15 - Fix64.FromRaw(2); boundary.Update(); Assert.Equal(0, boundary.Shots.Count); c.DeltaTime = 0; for (int i = 0; i < 100; i++) boundary.Update(); Assert.Equal(0, boundary.Shots.Count); c.DeltaTime = Fix64.FromRaw(2); boundary.Update(); Assert.Equal(1, boundary.Shots.Count);
        }
    }
}
