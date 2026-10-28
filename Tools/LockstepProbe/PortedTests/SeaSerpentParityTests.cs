using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class SeaSerpentParityTests {
        private sealed class Recorder : SimComponent, IDamageable {
            public readonly List<Hit> Hits = new List<Hit>(); public readonly List<Fix64> Delays = new List<Fix64>();
            public Recorder(SimObject owner) : base(owner) { }
            public void OnDamaged(Hit hit) { Hits.Add(hit); Delays.Add(0); }
            public void OnDamagedWithDelay(Hit hit, Fix64 delay) { Hits.Add(hit); Delays.Add(delay); }
        }
        [PortedTest("game/domain/object/component/mob/statemachine/attacker/sea_serpent_mob.rs")] public static void hydro_pump_damages_every_enemy_inside_the_line_including_air_targets() {
            var c = new SimulationContext(); c.RegisterPrefab("Object", _ => { }); var owner = c.Spawn(Master.LeftPlayer, "Object", Vector3.Zero); owner.Elements.AddNative(Element.Water); owner.AddCollider(new CircleCollider(owner, (Fix64)1.2m));
            Recorder Target(Master master, int x, int y, int z) { var o = c.Spawn(master, "Object", new Vector3(x, y, z)); o.AddCollider(new CircleCollider(o, (Fix64)0.25m)); var r = new Recorder(o); o.AddComponentImmediately(r); return r; }
            var ground = Target(Master.RightPlayer, 4, 0, 0); var air = Target(Master.RightPlayer, 4, 2, 0); var outside = Target(Master.RightPlayer, 4, 0, 2); var ally = Target(Master.LeftPlayer, 3, 0, 0); c.FlushObjects();
            Assert.True(SeaSerpentMob.FireHydroPump(owner, ground.Owner, 16, 7, 1));
            foreach (var r in new[] { ground, air }) { Assert.Equal(1, r.Hits.Count); Assert.Equal(16, r.Hits.Single().Damage); Assert.Equal((Fix64)0.35m, r.Delays.Single()); }
            Assert.Equal(0, outside.Hits.Count); Assert.Equal(0, ally.Hits.Count); var e = c.DrainEvents().Single(); Assert.Equal("SeaSerpentHydroPump", e.Type); Assert.Equal(new Vector3(0, (Fix64)1.6m, 0), e.StartPosition.Value); Assert.Equal(new Vector3((Fix64)8.2m, 0, 0), e.EndPosition.Value); Assert.Equal((Fix64)0.35m, e.Duration); Assert.Equal(Fix64.Zero, e.Width); Assert.Equal(ObjectStatus.Attack, owner.Status);
        }
        public static void CheckActualSerpent() {
            var c = new SimulationContext { DeltaTime = (Fix64)0.35m }; c.InitializationFailed += (o, e) => throw e; SeaSerpentMob.RegisterPrefabs(c); c.RegisterPrefab("Target", _ => { }); c.RegisterPrefab("WaterField", _ => { });
            string[] keys = { "mass", "radius", "hp", "speed", "damage", "attack_interval", "attack_range", "beam_width" }; decimal[] values = { 10, 1.2m, 180, 0.45m, 16, 2, 7, 1 };
            for (int i = 0; i < keys.Length; i++) c.GameParameters["sea_serpent." + keys[i]] = (Fix64)values[i];
            var owner = c.Spawn(Master.LeftPlayer, "SeaSerpent", new Vector3(2, 0, 5)); var target = c.Spawn(Master.RightPlayer, "Target", new Vector3(6, 3, 5)); var victim = new DummyMob(target, 100); target.AddComponentImmediately(victim); target.AddCollider(new CircleCollider(target, (Fix64)0.25m)); c.FlushObjects();
            var mob = owner.GetComponent<SeaSerpentMob>(); Assert.Equal(3, mob.TargetMask); Assert.True(owner.HasComponent<RigidBody>() && owner.HasComponent<ZPhysics>() && owner.HasComponent<CommonEffectReceiver>()); Assert.True(mob.Behavior(target)); Assert.Equal(100, victim.Hp); victim.Update(); Assert.Equal(84, victim.Hp);
            owner.SetPosition(new Vector3(3, 0, 5)); owner.GetComponent<PathSpawner>().Update(); Assert.Equal("WaterField", c.PendingObjects.Single().Prefab); Assert.Equal(Master.None, c.PendingObjects.Single().Master);
            c.DrainEvents(); target.SetPosition(owner.Position); Assert.True(!mob.Behavior(target)); Assert.Equal(0, c.DrainEvents().Length);
            Assert.Equal((Fix64)2, SeaSerpentMob.DistanceToSegment(new Vector3(3, 0, 0), Vector3.Zero, new Vector3(1, 0, 0)));
            Assert.Equal((Fix64)3, SeaSerpentMob.DistanceToSegment(new Vector3(3, 0, 0), Vector3.Zero, Vector3.Zero));
            target.SetPositionInPlace(new Vector3(6, 20, 5)); mob.Target = target;
            Assert.True(mob.VerticalRangeIgnored && mob.WithinAttackRange(7));
            mob.SetAttack(); c.DeltaTime = 3; mob.Update(); Assert.True(c.DrainEvents().Any(e => e.Type == "SeaSerpentHydroPump"));
        }
    }
}
