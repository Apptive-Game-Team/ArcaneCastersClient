using System;
using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class FireSpiritChecks {
        private sealed class Receiver : SimComponent, IEffectReceiver {
            public readonly List<EffectApplication> Applications = new List<EffectApplication>();
            public Action Check;
            public Receiver(SimObject owner) : base(owner) { }
            public void Receive(EffectApplication application) { Check?.Invoke(); Applications.Add(application); }
            public void Receive(VisualEffect effect) { throw new Exception("Source required"); }
            public void Receive(VisualEffect effect, Fix64 duration) { throw new Exception("Source required"); }
            public void ReceiveDirectional(VisualEffect effect, Vector3 direction, Fix64 proximity) { throw new Exception("Unexpected direction"); }
        }
        public static void Run() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; c.RegisterPrefab("Object", _ => { }); FireSpiritPrefab.Register(c);
            c.GameParameters["fire_spirit.hp"] = 100; c.GameParameters["fire_spirit.mass"] = 1; c.GameParameters["fire_spirit.radius"] = (Fix64)0.25m; c.GameParameters["fire_spirit.speed"] = 2;
            c.GameParameters["fire_spirit.damage"] = 10; c.GameParameters["fire_spirit.attack_interval"] = 1; c.GameParameters["fire_spirit.attack_range"] = 4; c.GameParameters["fire_spirit.sub_attack_range"] = 3;
            var registry = new MagicRegistry(); SpawnMagics.Register(registry); registry.GetBean("fire_spirit").Bind(1, "fire_spirit", Element.Fire).Run(c, Master.LeftPlayer, null, new Vector3(2, 4, 5));
            var owner = c.PendingObjects[0]; var sprayer = owner.GetComponent<SprayingAttacker>();
            DummyMob Target(Master side, Fix64 x, Fix64 z) { var o = c.Spawn(side, "Object", new Vector3(x, 0, z)); o.AddCollider(new CircleCollider(o, (Fix64)0.25m)); var mob = new DummyMob(o, 100); o.AddComponentImmediately(mob); o.AddComponentImmediately(new Receiver(o)); return mob; }
            var first = Target(Master.RightPlayer, 4, 5); var overlap = Target(Master.RightPlayer, 6, 5); var ally = Target(Master.LeftPlayer, 8, 5); var far = Target(Master.RightPlayer, 13, 5);
            var secondaryMob = new DummyMob(overlap.Owner, 100); overlap.Owner.AddComponentImmediately(secondaryMob); var secondaryReceiver = new Receiver(overlap.Owner); overlap.Owner.AddComponentImmediately(secondaryReceiver);
            c.FlushObjects(); first.Owner.GetComponent<Receiver>().Check = () => { Assert.Equal(90, first.Hp); Assert.Equal(90, overlap.Hp); Assert.Equal(90, ally.Hp); };
            Assert.True(sprayer.Behavior(first.Owner)); Assert.Equal(90, sprayer.Hp); Assert.Equal(100, secondaryMob.Hp); Assert.Equal(100, far.Hp); Assert.Equal(0, secondaryReceiver.Applications.Count);
            foreach (var mob in new[] { first, overlap, ally }) { var received = mob.Owner.GetComponent<Receiver>().Applications; Assert.Equal(1, received.Count); Assert.Equal(owner, received[0].Source); Assert.Equal(VisualEffect.Burn, received[0].Effect); }
            var projection = c.DrainEvents().Single(e => e.Type == "SprayedFlame"); Assert.Equal(new Vector3(6, 0, 5), projection.StartPosition.Value); Assert.Equal(new Vector3(7, 0, 5), projection.EndPosition.Value); Assert.Equal((Fix64)0.5m, projection.Duration);
            Assert.Equal(1, sprayer.TargetMask); Assert.True(owner.HasComponent<RigidBody>() && owner.HasComponent<ZPhysics>() && owner.HasComponent<CommonEffectReceiver>()); Assert.True(owner.Elements.Has(Element.Fire) && owner.Elements.Has(Element.Wind));
            first.Owner.GetComponent<Receiver>().Check = null;
            var aura = owner.GetComponent<AreaEffectProvider>(); c.DeltaTime = (Fix64)0.75m; aura.Update(); Assert.Equal(1, first.Owner.GetComponent<Receiver>().Applications.Count);
            c.DeltaTime = (Fix64)0.5m; aura.Update(); Assert.Equal(2, first.Owner.GetComponent<Receiver>().Applications.Count); Assert.Equal(1, overlap.Owner.GetComponent<Receiver>().Applications.Count);
            c.DeltaTime = (Fix64)0.75m; aura.Update(); Assert.Equal(3, first.Owner.GetComponent<Receiver>().Applications.Count);
            // 분사 거리는 실행 시점 값을 사용하고, Mob가 없어도 표시 이벤트를 먼저 생성한다.
            var detachedOwner = c.Spawn(Master.LeftPlayer, "Object", new Vector3(2, 0, 1)); var aim = c.Spawn(Master.RightPlayer, "Object", new Vector3(10, 0, 1));
            var dynamicSprayer = new SprayingAttacker(detachedOwner, 100, 1, 1, 1, 1, 10, VisualEffect.Burn, "SprayedFlame"); dynamicSprayer.AttackRange = 2;
            c.DrainEvents(); Assert.True(!dynamicSprayer.Behavior(aim)); projection = c.DrainEvents().Single(); Assert.Equal(new Vector3(4, 0, 1), projection.StartPosition.Value);
            Assert.True(!dynamicSprayer.Behavior(detachedOwner)); projection = c.DrainEvents().Single(); Assert.Equal(detachedOwner.Position, projection.StartPosition.Value); Assert.Equal(detachedOwner.Position, projection.EndPosition.Value);
        }
    }
}
