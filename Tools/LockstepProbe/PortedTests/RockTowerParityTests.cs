using System.Collections.Generic;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class RockTowerParityTests {
        private sealed class Receiver : SimComponent, IDamageable {
            public readonly List<Hit> Hits = new List<Hit>(); public readonly List<Fix64> Delays = new List<Fix64>();
            public Receiver(SimObject owner) : base(owner) { }
            public void OnDamaged(Hit hit) { Hits.Add(hit); }
            public void OnDamagedWithDelay(Hit hit, Fix64 delay) { Hits.Add(hit); Delays.Add(delay); }
        }
        private static SimulationContext Context() {
            var c = new SimulationContext { DeltaTime = (Fix64)0.5m }; c.InitializationFailed += (o, e) => throw e; c.RegisterPrefab("Object", _ => { }); Turret.RegisterPrefab(c);
            c.GameParameters["rock_turret.mass"] = 99999; c.GameParameters["rock_turret.radius"] = (Fix64)0.5m;
            c.GameParameters["rock_turret.hp"] = 40; c.GameParameters["rock_turret.damage"] = 2;
            c.GameParameters["rock_turret.attack_interval"] = (Fix64)0.5m; c.GameParameters["rock_turret.attack_range"] = 3; c.GameParameters["rock_turret.duration"] = 7; return c;
        }
        [PortedTest("game/domain/object/prefab/implement/build/tests.rs")] public static void uses_configured_rock_turret_duration_for_lifetime() {
            var c = Context(); var o = c.Spawn(Master.LeftPlayer, "RockTurret", Vector3.Zero); var gauge = o.GetComponent<TimedSelfDestroyer>().GetGauge(); Assert.Equal((Fix64)7, gauge.MaxValue); Assert.Equal(GaugeCategory.Ttl, gauge.Category);
        }
        public static void CheckAttacks() {
            var c = Context();
            Receiver Target(Master side, Fix64 x, Fix64 y = default) {
                var o = c.Spawn(side, "Object", new Vector3(x, y, 5)); o.AddCollider(new CircleCollider(o, (Fix64)0.25m)); var r = new Receiver(o); o.AddComponentImmediately(r); return r;
            }
            var registry = new MagicRegistry(); BuildMagics.Register(registry); registry.GetBean("rock_turret").Bind(1, "rock_turret", Element.Rock).Run(c, Master.LeftPlayer, null, new Vector3(5, 0, 5));
            var owner = c.PendingObjects[0]; var turret = owner.GetComponent<Turret>(); c.FlushObjects(); turret.Update(); Assert.Equal(0, c.DrainEvents().Length);
            var air = Target(Master.RightPlayer, 5, 2); var ground = Target(Master.RightPlayer, 7); var splash = Target(Master.RightPlayer, (Fix64)7.5m); var ally = Target(Master.LeftPlayer, (Fix64)7.5m); var far = Target(Master.RightPlayer, 15); c.FlushObjects();
            c.DeltaTime = (Fix64)0.125m; turret.Update(); Assert.Equal(1, ground.Hits.Count); Assert.Equal(0, splash.Hits.Count); Assert.Equal(0, air.Hits.Count);
            Assert.Equal((Fix64)0.2m, ground.Delays[0]); Assert.Equal(2, ground.Hits[0].Damage); Assert.Sequence(new[] { Element.Rock }, ground.Hits[0].Elements);
            var events = c.DrainEvents(); Assert.Equal(1, events.Length); Assert.Equal(ground.Owner.Id, events[0].TargetId); Assert.Equal("RockShot", events[0].Type);
            turret.Update(); Assert.Equal(0, c.DrainEvents().Length); Assert.True(owner.HasComponent<IFrenzyImmune>() && owner.HasComponent<IMindControlImmune>());
            var towerOwner = c.Spawn(Master.LeftPlayer, "Object", new Vector3(5, 0, 5)); var tower = new Tower(towerOwner, 4, 1, 1, 3); towerOwner.AddComponentImmediately(tower); tower.Start(); c.FlushObjects();
            Assert.True(!towerOwner.HasComponent<IDamageable>()); c.DeltaTime = 1; tower.Update();
            Assert.Equal(2, ground.Hits.Count); Assert.Equal(1, splash.Hits.Count); Assert.Equal(0, ally.Hits.Count); Assert.Equal(0, far.Hits.Count); Assert.Equal(0, air.Hits.Count);
            Assert.Equal((Fix64)0.2m, splash.Delays[0]); Assert.Equal(4, splash.Hits[0].Damage);
            // 단일 공격은 피해를 예약하고, 실제 Mob의 update가 지연 후 적용한다.
            var victim = c.Spawn(Master.RightPlayer, "Object", new Vector3(6, 0, 5)); victim.AddCollider(new CircleCollider(victim, (Fix64)0.25m)); victim.AddComponentImmediately(new DummyMob(victim, 20)); c.FlushObjects();
            turret.Update(); Assert.Equal(20, victim.GetComponent<DummyMob>().Hp); c.DeltaTime = (Fix64)0.25m; victim.GetComponent<DummyMob>().Update(); Assert.Equal(18, victim.GetComponent<DummyMob>().Hp);
        }
    }
}
