using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class WaterSlimeParityTests {
        private const string Source = "game/domain/object/component/mob/statemachine/attacker/water_slime_range_attack_mob.rs";
        private sealed class Recorder : SimComponent, IDamageable {
            public readonly List<Hit> Hits = new List<Hit>();
            public Recorder(SimObject owner) : base(owner) { }
            public void OnDamaged(Hit hit) => Hits.Add(hit);
            public void OnDamagedWithDelay(Hit hit, Fix64 delay) => Hits.Add(hit);
        }
        private static SimulationContext Context() { var c = new SimulationContext(); foreach (var name in new[] { "WaterSlime", "Target", "WaterField" }) c.RegisterPrefab(name, _ => { }); return c; }
        private static SimObject Slime(SimulationContext c, Vector3? position = null) { var o = c.Spawn(Master.LeftPlayer, "WaterSlime", position ?? Vector3.Zero); o.Elements.AddNative(Element.Water); return o; }
        private static Recorder Target(SimulationContext c) { var o = c.Spawn(Master.RightPlayer, "Target", new Vector3(3, 0, 0)); var r = new Recorder(o); o.AddComponentImmediately(r); return r; }
        private static WaterSlimeRangeAttackMob Attacker(SimObject owner, SimObject target) { var m = new WaterSlimeRangeAttackMob(owner, 10, 1, 1, 4, 1, 6); m.Target = target; m.SetAttack(); return m; }
        private static void Shot(SimulationContext c, SimObject owner, SimObject target) { var e = c.DrainEvents().Single(); Assert.Equal("WaterShot", e.Type); Assert.Equal(owner.Id, e.SourceId); Assert.Equal(target.Id, e.TargetId); Assert.Equal((Fix64)0.5m, e.Duration); Assert.Equal(Fix64.Zero, e.Width); }
        [PortedTest(Source)] public static void attack_state_fires_one_water_shot_and_applies_slime_damage_at_its_own_cadence() {
            var c = Context(); var owner = Slime(c); var target = Target(c); var mob = Attacker(owner, target.Owner); c.DeltaTime = (Fix64)0.9m; mob.Update(); Assert.Equal(0, c.DrainEvents().Length); Assert.Equal(0, target.Hits.Count);
            c.DeltaTime = (Fix64)0.2m; mob.Update(); Shot(c, owner, target.Owner); Assert.Equal(4, target.Hits.Single().Damage); Assert.True(target.Hits.Single().Elements.Contains(Element.Water)); Assert.Equal(ObjectStatus.Attack, owner.Status);
        }
        [PortedTest(Source)] public static void separate_water_slimes_fire_independently_without_group_multiplier() {
            var c = Context(); var first = Slime(c); var second = Slime(c); var target = Target(c); var a = Attacker(first, target.Owner); var b = Attacker(second, target.Owner); c.DeltaTime = (Fix64)1.1m;
            a.Update(); Shot(c, first, target.Owner); b.Update(); Shot(c, second, target.Owner); Assert.Equal(2, target.Hits.Count); Assert.True(target.Hits.All(h => h.Damage == 4));
        }
        [PortedTest(Source)] public static void lethal_damage_creates_exactly_one_owned_water_field_at_death_position() {
            var c = Context(); var owner = Slime(c, new Vector3(2, 0, 3)); c.FlushObjects(); var mob = new WaterSlimeRangeAttackMob(owner, 5, 1, 1, 4, 1, 6); mob.OnDamaged(new Hit(5, 0, Element.Water)); mob.OnDamaged(new Hit(5, 0, Element.Water));
            var field = c.PendingObjects.Single(); Assert.Equal("WaterField", field.Prefab); Assert.Equal(Master.LeftPlayer, field.Master); Assert.Equal(owner.Position, field.Position); Assert.True(owner.IsDestroyed);
        }
        [PortedTest(Source)] public static void direct_destroy_does_not_create_death_field() {
            var c = Context(); var owner = Slime(c); owner.AddComponentImmediately(new WaterSlimeRangeAttackMob(owner, 5, 1, 1, 4, 1, 6)); c.FlushObjects(); owner.Destroy(); Assert.True(owner.IsDestroyed); Assert.Equal(0, c.PendingObjects.Length);
        }
        public static void CheckActualSlime() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; WaterSlimeRangeAttackMob.RegisterPrefabs(c); c.RegisterPrefab("WaterField", _ => { });
            string[] keys = { "mass", "radius", "hp", "speed", "damage", "attack_interval" }; decimal[] values = { 1, 0.3m, 10, 1, 4, 1 };
            for (int i = 0; i < keys.Length; i++) c.GameParameters["water_slime." + keys[i]] = (Fix64)values[i]; c.GameParameters["aqua_archer.attack_range"] = 6;
            var owner = c.Spawn(Master.LeftPlayer, "WaterSlime", new Vector3(2, 0, 5)); var mob = owner.GetComponent<WaterSlimeRangeAttackMob>(); Assert.Equal((Fix64)6, mob.AttackRange); Assert.Equal(1, mob.TargetMask); Assert.True(owner.HasComponent<RigidBody>() && owner.HasComponent<ZPhysics>() && owner.HasComponent<CommonEffectReceiver>()); Assert.True(!owner.Colliders.Single().IsTrigger);
            c.FlushObjects(); owner.SetPosition(new Vector3(3, 0, 5)); owner.GetComponent<PathSpawner>().Update(); Assert.Equal("WaterField", c.PendingObjects.Single().Prefab); Assert.Equal(Master.None, c.PendingObjects.Single().Master);
        }
    }
}
