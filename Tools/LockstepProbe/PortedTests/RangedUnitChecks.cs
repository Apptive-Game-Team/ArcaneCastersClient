using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class RangedUnitChecks {
        public static void Run() {
            foreach (var name in new[] { "aqua_archer", "rock_mage" }) {
                var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; c.RegisterPrefab("Object", _ => { }); RangedUnitPrefabs.Register(c);
                c.GameParameters[name + ".hp"] = 50; c.GameParameters[name + ".mass"] = 1; c.GameParameters[name + ".radius"] = (Fix64)0.5m; c.GameParameters[name + ".damage"] = 10;
                c.GameParameters[name + ".speed"] = 2; c.GameParameters[name + ".attack_interval"] = 1; c.GameParameters[name + ".attack_range"] = 3;
                var registry = new MagicRegistry(); SpawnMagics.Register(registry); registry.GetBean(name).Bind(1, name, Element.None).Run(c, Master.LeftPlayer, null, new Vector3(5, 4, 5)); var owner = c.PendingObjects[0]; var mob = owner.GetComponent<BehaviorMob>();
                DummyMob Target(Master side, Fix64 x, Fix64 z, Fix64 y = default) { var o = c.Spawn(side, "Object", new Vector3(x, y, z)); o.AddCollider(new CircleCollider(o, (Fix64)0.25m)); var m = new DummyMob(o, 100); o.AddComponentImmediately(m); return m; }
                var first = Target(Master.RightPlayer, 7, 5); c.FlushObjects(); Assert.True(mob.Behavior(first.Owner));
                bool twice = name == "rock_mage"; Assert.Equal(twice ? 80 : 90, first.Hp); var events = c.DrainEvents().Where(e => e.Type == "WaterShot" || e.Type == "RockShot").ToArray(); Assert.Equal(twice ? 2 : 1, events.Length);
                foreach (var ev in events) { Assert.Equal(twice ? "RockShot" : "WaterShot", ev.Type); Assert.Equal((Fix64)0.5m, ev.Duration); Assert.Equal(first.Owner.Id, ev.TargetId); }
                var second = Target(Master.RightPlayer, 7, 6); var tie = Target(Master.RightPlayer, 7, 4); var ally = Target(Master.LeftPlayer, 6, 5); var far = Target(Master.RightPlayer, 15, 5); c.FlushObjects();
                Assert.True(mob.Behavior(first.Owner)); Assert.Equal(twice ? 70 : 80, first.Hp); Assert.Equal(twice ? 90 : 100, second.Hp); Assert.Equal(100, tie.Hp); Assert.Equal(100, ally.Hp); Assert.Equal(100, far.Hp); c.DrainEvents();
                first.Owner.SetPosition(new Vector3(8, 0, 5)); var air = Target(Master.RightPlayer, 5, 5, 2); c.FlushObjects(); Assert.Equal(air.Owner, mob.Detector.Detect(owner)); Assert.Equal(3, mob.TargetMask);
                Assert.True(owner.HasComponent<RigidBody>() && owner.HasComponent<ZPhysics>() && owner.HasComponent<CommonEffectReceiver>()); Assert.True(owner.Elements.Has(twice ? Element.Rock : Element.Water));
                if (twice) {
                    var detector = (MultiEnemyDetector)mob.Detector; Assert.Equal(0, detector.DetectInRange(owner, 0, 3).Length); Assert.Equal(second.Owner, detector.Detect(owner, candidate => ReferenceEquals(candidate, second.Owner)));
                    var initial = Target(Master.RightPlayer, 5, 5); initial.Owner.SetStatus(ObjectStatus.Initializing); c.FlushObjects(); Assert.Equal(initial.Owner, detector.Detect(owner));
                    initial.Owner.Destroy(); Assert.Equal(air.Owner, detector.Detect(owner));
                }
            }
        }
    }
}
