using System.Collections.Generic;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class RallyParityTests {
        private const string Source = "game/domain/object/component/magic/rallying_totem.rs";
        private const string PrefabSource = "game/domain/object/prefab/implement/drop/rallying_totem_prefab_initializer.rs";
        private sealed class Receiver : CommonEffectReceiver {
            public readonly List<Fix64> Durations = new List<Fix64>();
            public Receiver(SimObject owner) : base(owner) { }
            public override void Receive(VisualEffect effect, Fix64 duration) { Assert.Equal(VisualEffect.Inspired, effect); Durations.Add(duration); }
        }
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext { DeltaTime = (Fix64)0.5m };
            public readonly SimObject Owner;
            public readonly RallyingTotem Totem;
            public Fixture() { Context.RegisterPrefab("Object", _ => { }); Owner = Object(Master.LeftPlayer, 0, 4); Totem = new RallyingTotem(Owner, 5, 10, 3, 8, 2); }
            public SimObject Object(Master master, Fix64 x, Fix64 y = default) => Context.Spawn(master, "Object", new Vector3(x, y, 0));
            public SimObject Mob(Fix64 x, Master master = Master.LeftPlayer, Fix64? range = null) {
                var o = Object(master, x); var mob = new MeleeAttackMob(o, 100, 1, 1, 1, 1); mob.AttackRange = range ?? (Fix64)0.5m; o.AddComponentImmediately(mob); Context.FlushObjects(); return o;
            }
            public Receiver Receiver() { var o = Object(Master.LeftPlayer, 1); o.AddCollider(new CircleCollider(o, (Fix64)0.1m)); var r = new Receiver(o); o.AddComponentImmediately(r); Context.FlushObjects(); return r; }
            public void Tick(int count) { for (int i = 0; i < count; i++) Totem.Update(); }
            public RallyMoveDirective Directive(SimObject mob) { Owner.SetPosition(Vector3.Zero); return new RallyMoveDirective(mob, Owner, 3); }
            public SimObject Target(Fix64 x, Fix64 radius = default) { var o = Object(Master.RightPlayer, x); if (radius > 0) o.AddCollider(new CircleCollider(o, radius)); return o; }
        }
        [PortedTest(Source)] public static void rallys_nobody_and_buffs_nobody_while_falling() {
            var f = new Fixture(); var mob = f.Mob(1); var receiver = f.Receiver(); f.Tick(1);
            Assert.Equal((Fix64)1.5m, f.Owner.Position.Y); Assert.Equal(0, mob.PendingComponents.Length); Assert.Equal(0, receiver.Durations.Count);
        }
        [PortedTest(Source)] public static void landing_snaps_to_ground_and_starts_rallying_on_the_next_tick() {
            var f = new Fixture(); var mob = f.Mob(1); var receiver = f.Receiver(); f.Tick(2);
            Assert.Equal(Fix64.Zero, f.Owner.Position.Y); Assert.Equal(0, mob.PendingComponents.Length); f.Tick(1);
            Assert.Equal(1, mob.GetPendingComponents<RallyMoveDirective>().Length); Assert.Sequence(new Fix64[] { 2 }, receiver.Durations);
        }
        [PortedTest(Source)] public static void duration_starts_counting_at_landing() { var f = new Fixture(); f.Tick(21); Assert.True(!f.Owner.IsDestroyed); f.Tick(1); Assert.True(f.Owner.IsDestroyed); }
        [PortedTest(Source)] public static void recruits_only_allied_mobs_within_detection_range() { var f = new Fixture(); var near = f.Mob(8); var far = f.Mob((Fix64)8.5m); f.Tick(3); Assert.Equal(1, near.GetPendingComponents<RallyMoveDirective>().Length); Assert.Equal(0, far.PendingComponents.Length); }
        [PortedTest(Source)] public static void does_not_rally_an_enemy_behavior_mob() { var f = new Fixture(); var enemy = f.Mob(1, Master.RightPlayer); f.Tick(3); Assert.Equal(0, enemy.PendingComponents.Length); }
        [PortedTest(Source)] public static void reports_the_time_left_after_landing_as_a_ttl_gauge() { var f = new Fixture(); f.Tick(2); Assert.Equal((Fix64)10, f.Totem.GetGauge().Value); f.Tick(1); var gauge = f.Totem.GetGauge(); Assert.Equal((Fix64)9.5m, gauge.Value); Assert.Equal((Fix64)10, gauge.MaxValue); Assert.Equal(GaugeCategory.Ttl, gauge.Category); }
        private static SimObject Prefab() {
            var c = new SimulationContext(); RallyingTotem.RegisterPrefab(c); c.InitializationFailed += (o, e) => throw e;
            c.GameParameters["rallying_totem.radius"] = (Fix64)0.6m; c.GameParameters["rallying_totem.speed"] = 5; c.GameParameters["rallying_totem.duration"] = 10;
            c.GameParameters["rallying_totem.effect_radius"] = 2; c.GameParameters["rallying_totem.detection_range"] = 8; c.GameParameters["rallying_totem.buff_duration"] = 10;
            return c.Spawn(Master.LeftPlayer, "RallyingTotem", Vector3.Zero);
        }
        [PortedTest(PrefabSource)] public static void prefab_has_no_item_component() { var o = Prefab(); Assert.True(o.GetComponent<RallyingTotem>() != null); foreach (var component in o.Components) Assert.True(component.GetType().Name != "Item"); }
        [PortedTest(PrefabSource)] public static void the_rally_zone_is_the_effect_radius() { Assert.Equal((Fix64)2, Prefab().GetComponent<RallyingTotem>().ZoneRadius); }
        public static void CheckDirectiveLifecycle() {
            var f = new Fixture(); var ally = f.Mob(6); var mob = ally.GetComponent<BehaviorMob>(); var body = new RigidBody(ally, 1); ally.AddComponentImmediately(body); mob.Body = body; mob.PathFinder = new SimplePathFinder(); f.Tick(4);
            Assert.Equal(1, ally.GetPendingComponents<RallyMoveDirective>().Length);
            f.Context.FlushObjects(); f.Tick(1); Assert.Equal(0, ally.GetPendingComponents<RallyMoveDirective>().Length);
            var directive = ally.GetComponent<RallyMoveDirective>(); Assert.True(ReferenceEquals(directive, mob.ResolveHighestPriorityDirective()));
            mob.Update(); Assert.True(mob.CurrentState is BehaviorMob.DirectiveMoveState);
            f.Owner.Destroy(); Assert.True(!directive.GetMoveTarget(ally).HasValue); directive.Update(); f.Context.FlushObjects();
            Assert.True(ally.GetComponent<RallyMoveDirective>() == null); mob.Update(); mob.Update(); Assert.True(!(mob.CurrentState is BehaviorMob.DirectiveMoveState));
            var neutral = new Fixture(); neutral.Owner.SetMaster(Master.None); var n = neutral.Mob(1, Master.None); neutral.Tick(3); Assert.Equal(0, n.GetPendingComponents<RallyMoveDirective>().Length);
        }
    }
}
