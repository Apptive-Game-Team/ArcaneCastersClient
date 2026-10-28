using System.Collections.Generic;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class LightningTowerChecks {
        private sealed class Receiver : SimComponent, IDamageable {
            public readonly List<Hit> Hits = new List<Hit>();
            public Receiver(SimObject owner) : base(owner) { }
            public void OnDamaged(Hit hit) { Hits.Add(hit); }
            public void OnDamagedWithDelay(Hit hit, Fix64 delay) { Assert.Equal((Fix64)0.2m, delay); Hits.Add(hit); }
        }
        public static void Run() {
            var c = new SimulationContext { DeltaTime = 1 }; c.InitializationFailed += (o, e) => throw e; c.RegisterPrefab("Object", _ => { }); LightningTowerMob.RegisterPrefab(c);
            FieldPrefabs.RegisterElectricField(c); c.GameParameters["electric_field.radius"] = 4; c.GameParameters["electric_field.duration"] = 5;
            c.GameParameters["electric_tower.mass"] = 100; c.GameParameters["electric_tower.radius"] = (Fix64)0.5m; c.GameParameters["electric_tower.hp"] = 40;
            c.GameParameters["electric_tower.damage"] = 10; c.GameParameters["electric_tower.chain_damage"] = 3; c.GameParameters["electric_tower.chain_count"] = 2;
            c.GameParameters["electric_tower.attack_interval"] = 1; c.GameParameters["electric_tower.attack_range"] = 2; c.GameParameters["electric_tower.chain_radius"] = (Fix64)1.5m; c.GameParameters["electric_tower.duration"] = 30;
            var registry = new MagicRegistry(); BuildMagics.Register(registry); registry.GetBean("electric_tower").Bind(1, "electric_tower", Element.Lightning).Run(c, Master.LeftPlayer, null, new Vector3(4, 0, 5));
            var tower = c.PendingObjects[0]; var attack = tower.GetComponent<LightningTowerMob>(); c.FlushObjects(); attack.Update(); Assert.Equal(0, c.DrainEvents().Length);
            Receiver Target(Master side, Fix64 x, Fix64 z, Fix64 y = default) {
                var o = c.Spawn(side, "Object", new Vector3(x, y, z)); o.AddCollider(new CircleCollider(o, (Fix64)0.25m)); var r = new Receiver(o); o.AddComponentImmediately(r); return r;
            }
            var first = Target(Master.RightPlayer, 6, 5); var upper = Target(Master.RightPlayer, 7, 6); var lower = Target(Master.RightPlayer, 7, 4); var last = Target(Master.RightPlayer, 8, 6);
            var ally = Target(Master.LeftPlayer, 6, 6); var far = Target(Master.RightPlayer, 15, 5); var secondReceiver = new Receiver(first.Owner); first.Owner.AddComponentImmediately(secondReceiver); c.FlushObjects();
            c.DeltaTime = (Fix64)0.125m; attack.Update(); var events = c.DrainEvents(); Assert.Equal(3, events.Length);
            Assert.Equal(first.Owner.Id, events[0].TargetId); Assert.Equal(tower.Id, events[0].SourceId); Assert.Equal(upper.Owner.Id, events[1].TargetId); Assert.Equal(first.Owner.Id, events[1].SourceId); Assert.Equal(last.Owner.Id, events[2].TargetId); Assert.Equal(upper.Owner.Id, events[2].SourceId);
            Assert.Equal(10, first.Hits[0].Damage); Assert.Equal(10, secondReceiver.Hits[0].Damage); Assert.Equal(3, upper.Hits[0].Damage); Assert.Equal(3, last.Hits[0].Damage);
            Assert.Equal(tower.Id, last.Hits[0].AttackerId); Assert.Sequence(new[] { Element.Lightning }, last.Hits[0].Elements);
            Assert.Equal(0, lower.Hits.Count); Assert.Equal(0, ally.Hits.Count); Assert.Equal(0, far.Hits.Count); attack.Update(); Assert.Equal(0, c.DrainEvents().Length);
            // 지상/공중을 모두 탐색하고 실제 Mob의 지연 피해를 적용한다.
            var air = c.Spawn(Master.RightPlayer, "Object", new Vector3(4, 1, 5)); air.AddCollider(new CircleCollider(air, (Fix64)0.25m)); var mob = new DummyMob(air, 100); air.AddComponentImmediately(mob); c.FlushObjects();
            c.DeltaTime = 1; attack.Update(); events = c.DrainEvents(); Assert.Equal(air.Id, events[0].TargetId); Assert.Equal(100, mob.Hp);
            c.DeltaTime = (Fix64)0.1m; mob.Update(); Assert.Equal(100, mob.Hp); c.DeltaTime = (Fix64)0.2m - c.DeltaTime; mob.Update(); Assert.Equal(90, mob.Hp);
            Assert.True(tower.HasComponent<BuildingEffectReceiver>()); Assert.Equal((Fix64)30, tower.GetComponent<TimedSelfDestroyer>().GetGauge().MaxValue);
            attack.ApplyDamage(new Hit(1000, 0, Element.None)); Assert.True(tower.IsDestroyed);
            Assert.Equal(1, c.PendingObjects.Length); var field = c.PendingObjects[0]; Assert.Equal("ElectricField", field.Prefab); Assert.Equal(Master.LeftPlayer, field.Master); Assert.Equal(VisualEffect.Shock, field.GetComponent<EffectProvider>().Effect);
            c.DeltaTime = 5; field.GetComponent<TimedSelfDestroyer>().Update(); Assert.True(field.IsDestroyed);
        }
    }
}
