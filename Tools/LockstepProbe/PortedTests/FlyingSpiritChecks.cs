using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class FlyingSpiritChecks {
        public static void Run() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; c.RegisterPrefab("Object", _ => { }); FlyingSpiritPrefabs.Register(c);
            foreach (var key in new[] { "bubble_spirit", "thunder_spirit" }) {
                c.GameParameters[key + ".hp"] = 100; c.GameParameters[key + ".mass"] = 1; c.GameParameters[key + ".radius"] = (Fix64)0.25m; c.GameParameters[key + ".speed"] = 2;
                c.GameParameters[key + ".damage"] = 10; c.GameParameters[key + ".attack_interval"] = 1; c.GameParameters[key + ".attack_range"] = 6;
            }
            c.GameParameters["bubble_spirit.projectile_speed"] = 4;
            var registry = new MagicRegistry(); SpawnMagics.Register(registry);
            registry.GetBean("bubble_spirit").Bind(1, "bubble_spirit", Element.Water).Run(c, Master.LeftPlayer, null, new Vector3(2, 0, 5));
            registry.GetBean("thunder_spirit").Bind(2, "thunder_spirit", Element.Lightning).Run(c, Master.LeftPlayer, null, new Vector3(2, 0, 1));
            var bubble = c.PendingObjects.Single(o => o.Prefab == "BubbleSpirit"); var thunder = c.PendingObjects.Single(o => o.Prefab == "ThunderSpirit");
            foreach (var owner in new[] { bubble, thunder }) { Assert.Equal((Fix64)3, owner.Position.Y); Assert.True(owner.HasComponent<RigidBody>() && owner.HasComponent<CommonEffectReceiver>()); Assert.True(owner.GetComponent<ZPhysics>().IsAerial && owner.GetComponent<ZPhysics>().CanHover); }
            Assert.True(bubble.Elements.Has(Element.Water) && bubble.Elements.Has(Element.Wind)); Assert.True(thunder.Elements.Has(Element.Lightning) && thunder.Elements.Has(Element.Wind) && thunder.HasComponent<ElectricDeathEnergy>());
            DummyMob Target(Master side, Vector3 p) { var o = c.Spawn(side, "Object", p); o.AddCollider(new CircleCollider(o, (Fix64)0.25m)); var mob = new DummyMob(o, 100); o.AddComponentImmediately(mob); return mob; }
            var air = Target(Master.RightPlayer, new Vector3(6, 3, 5)); var splash = Target(Master.RightPlayer, new Vector3(6, 2, 5)); var ground = Target(Master.RightPlayer, new Vector3(6, (Fix64)1.9m, 5));
            var ally = Target(Master.LeftPlayer, new Vector3(6, 3, 5)); var far = Target(Master.RightPlayer, new Vector3(10, 3, 5)); var extra = new DummyMob(air.Owner, 100); air.Owner.AddComponentImmediately(extra);
            c.FlushObjects(); var attacker = bubble.GetComponent<BubbleSpiritMob>(); Assert.Equal(2, attacker.TargetMask); Assert.True(attacker.Behavior(air.Owner)); Assert.Equal(ObjectStatus.Attack, bubble.Status);
            var ev = c.DrainEvents().Single(e => e.Type == "BubbleShot"); Assert.Equal((Fix64)1, ev.Duration); Assert.Equal(bubble.Id, ev.SourceId); Assert.Equal(air.Owner.Id, ev.TargetId);
            c.DeltaTime = (Fix64)0.5m; air.Update(); splash.Update(); Assert.Equal(100, air.Hp); Assert.Equal(100, splash.Hp);
            // 원본은 발사 순간 대상 목록을 확정하고, 피해 시점에 거리나 진영을 다시 검사하지 않는다.
            air.Owner.SetPosition(new Vector3(15, 3, 5)); air.Update(); splash.Update(); ground.Update(); ally.Update(); far.Update(); extra.Update();
            Assert.Equal(90, air.Hp); Assert.Equal(90, splash.Hp); Assert.Equal(100, ground.Hp); Assert.Equal(100, ally.Hp); Assert.Equal(100, far.Hp); Assert.Equal(100, extra.Hp);
            var groundTarget = Target(Master.RightPlayer, new Vector3(4, 0, 1)); var second = new DummyMob(groundTarget.Owner, 100); groundTarget.Owner.AddComponentImmediately(second); c.FlushObjects();
            var lightning = thunder.GetComponent<ProjectileRangeAttackMob>(); Assert.Equal(1, lightning.TargetMask); Assert.Equal(groundTarget.Owner, lightning.Detector.Detect(thunder)); Assert.True(lightning.Behavior(groundTarget.Owner)); Assert.Equal(90, groundTarget.Hp); Assert.Equal(100, second.Hp);
            ev = c.DrainEvents().Single(e => e.Type == "ElectricShot"); Assert.Equal((Fix64)0.5m, ev.Duration); Assert.Equal(thunder.Id, ev.SourceId);
            bubble.SetPosition(new Vector3(2, 2, 5)); c.DeltaTime = (Fix64)0.5m; bubble.GetComponent<ZPhysics>().ApplyHover(); Assert.Equal((Fix64)2.5m, bubble.Position.Y); bubble.GetComponent<ZPhysics>().ApplyHover(); Assert.Equal((Fix64)3, bubble.Position.Y);
        }
    }
}
