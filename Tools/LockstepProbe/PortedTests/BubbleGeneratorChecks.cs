using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class BubbleGeneratorChecks {
        public static void Run() {
            var c = new SimulationContext { DeltaTime = (Fix64)0.5m }; c.InitializationFailed += (o, e) => throw e;
            BubbleGeneratorMob.RegisterPrefab(c); c.RegisterPrefab("Object", _ => { });
            foreach (var key in new[] { "mass", "radius", "attack_interval" }) c.GameParameters["bubble_generator." + key] = 1;
            c.GameParameters["bubble_generator.hp"] = 30; c.GameParameters["bubble_generator.attack_range"] = 3;
            c.GameParameters["bubble_generator.projectile_speed"] = 4; c.GameParameters["bubble_generator.duration"] = 10;
            var registry = new MagicRegistry(); BuildMagics.Register(registry);
            registry.GetBean("bubble_generator").Bind(1, "bubble_generator", Element.Water).Run(c, Master.LeftPlayer, null, new Vector3(5, 0, 5));
            var generator = c.PendingObjects[0]; var mob = generator.GetComponent<BubbleGeneratorMob>(); c.FlushObjects();
            mob.Update(); mob.Update(); mob.Update(); Assert.Equal(0, c.DrainEvents().Length);
            SimObject Target(Master master, Fix64 x, bool receiver = true, bool damageable = true) {
                var o = c.Spawn(master, "Object", new Vector3(x, 0, 5)); o.AddCollider(new CircleCollider(o, (Fix64)0.5m));
                if (damageable) o.AddComponentImmediately(new DummyMob(o, 20)); if (receiver) o.AddComponentImmediately(new CommonEffectReceiver(o)); return o;
            }
            Target(Master.RightPlayer, 5); Target(Master.LeftPlayer, 5, receiver: false); Target(Master.LeftPlayer, 5, damageable: false);
            var inactive = Target(Master.LeftPlayer, 5); inactive.Destroy(); var far = Target(Master.LeftPlayer, 15);
            var first = Target(Master.LeftPlayer, 7); var equal = Target(Master.LeftPlayer, 3); c.FlushObjects();
            c.DeltaTime = (Fix64)0.125m; mob.Update(); var events = c.DrainEvents(); Assert.Equal(1, events.Length);
            Assert.Equal("WaterShot", events[0].Type); Assert.Equal(generator.Id, events[0].SourceId); Assert.Equal(first.Id, events[0].TargetId); Assert.Equal((Fix64)0.5m, events[0].Duration);
            Assert.Equal(1, first.GetPendingComponents<BubbleStatusEffect>().Length); Assert.Equal(0, equal.GetPendingComponents<BubbleStatusEffect>().Length); Assert.Equal(0, far.GetPendingComponents<BubbleStatusEffect>().Length);
            c.FlushObjects(); mob.Update(); Assert.Equal(0, c.DrainEvents().Length);
            c.DeltaTime = 1; mob.Update(); events = c.DrainEvents(); Assert.Equal(1, events.Length); Assert.Equal(equal.Id, events[0].TargetId); c.FlushObjects();
            first.GetComponent<DummyMob>().OnDamaged(new Hit(4, 0, Element.None)); Assert.Equal(20, first.GetComponent<DummyMob>().Hp);
            Assert.True(generator.HasComponent<BuildingEffectReceiver>()); Assert.Equal((Fix64)10, generator.GetComponent<TimedSelfDestroyer>().GetGauge().MaxValue);
            // 원본은 대기 효과를 제외 조건에 넣지 않는다. 같은 틱의 두 생성기도 첫 대상을 고른다.
            var pending = Target(Master.LeftPlayer, 6); c.FlushObjects(); pending.AddComponent(new BubbleStatusEffect(pending, 8, StatusEffectKey.BubbleReceive));
            mob.Update(); Assert.Equal(pending.Id, c.DrainEvents()[0].TargetId);
        }
    }
}
