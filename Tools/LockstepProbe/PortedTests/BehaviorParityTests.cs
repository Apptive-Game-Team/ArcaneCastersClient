using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class BehaviorParityTests {
        private const string Source = "tests/behavior_mob.rs";
        private const string Rally = "game/domain/object/component/mob/directive/rally_move_directive.rs";
        private sealed class Directive : SimComponent, IMovementDirective {
            public int Priority { get; }
            public Vector3? Target { get; }
            public Fix64 ArrivalDistance => (Fix64)0.5m;
            public bool SuppressCombat => true;
            public Directive(SimObject owner, int priority, Vector3? target) : base(owner) { Priority = priority; Target = target; }
            public Vector3? GetMoveTarget(SimObject source) => Target;
            public bool AllowsCombatTarget(SimObject source, SimObject target) => false;
        }
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext(0);
            public readonly SimObject Owner;
            public readonly BehaviorMob Mob;
            public bool Attacked;
            public Fixture(int mask = 3) {
                Context.RegisterPrefab("RockSlime", _ => { }); Context.RegisterPrefab("Player", _ => { }); Context.RegisterPrefab("WindSpirit", _ => { });
                Owner = Object(Master.LeftPlayer, "RockSlime", Vector3.Zero, (Fix64)0.5m); Owner.AddComponentImmediately(new RigidBody(Owner, 1));
                Mob = new BehaviorMob(Owner, 10, 1, mask, 0, 1, _ => true); Owner.AddComponentImmediately(Mob);
            }
            public SimObject Object(Master master, string prefab, Vector3 position, Fix64 radius) {
                var owner = Context.Spawn(master, prefab, position); owner.AddCollider(new CircleCollider(owner, radius)); Context.FlushObjects(); return owner;
            }
            public Directive Add(int priority, int? x) { var directive = new Directive(Owner, priority, x.HasValue ? new Vector3(x.Value, 0, 0) : (Vector3?)null); Owner.AddComponentImmediately(directive); return directive; }
            public IMovementDirective Reference() => Owner.GetComponents<IMovementDirective>().OrderByDescending(d => d.Priority).FirstOrDefault(d => d.GetMoveTarget(Owner).HasValue);
            public void Selects(Directive expected) { Assert.True(ReferenceEquals(expected, Mob.ResolveHighestPriorityDirective())); Assert.True(ReferenceEquals(expected, Reference())); }
            public BehaviorMob Aerial(Vector3 position, Fix64 radius, bool ignoreHeight, out SimObject flyer) {
                var target = Object(Master.RightPlayer, "Player", Vector3.Zero, 1); target.AddComponentImmediately(new DummyMob(target, 100));
                flyer = Object(Master.LeftPlayer, "WindSpirit", position, radius); flyer.AddComponentImmediately(new RigidBody(flyer, 1));
                var mob = new BehaviorMob(flyer, 10, 1, 3, 0, (Fix64)1.5m, _ => { Attacked = true; return true; }, ignoreHeight);
                mob.Target = target; mob.TargetRadius = 1; flyer.AddComponentImmediately(mob); return mob;
            }
            public void Frames(BehaviorMob mob, int count) { for (int i = 0; i < count && !Attacked; i++) mob.Update(); }
        }
        [PortedTest(Source)] public static void air_only_mob_releases_target_after_it_lands() {
            var f = new Fixture(2); var target = f.Object(Master.RightPlayer, "RockSlime", new Vector3(0, 3, 0), 0);
            Assert.True(f.Mob.IsValidTarget(target)); target.SetPosition(Vector3.Zero); Assert.Equal(false, f.Mob.IsValidTarget(target));
        }
        [PortedTest(Source)] public static void ground_only_mob_releases_target_after_it_becomes_airborne() {
            var f = new Fixture(1); var target = f.Object(Master.RightPlayer, "RockSlime", Vector3.Zero, 0);
            Assert.True(f.Mob.IsValidTarget(target)); target.SetPosition(new Vector3(0, 2, 0)); Assert.Equal(false, f.Mob.IsValidTarget(target));
        }
        [PortedTest(Source)] public static void no_directives_means_no_directive() { var f = new Fixture(); Assert.True(f.Mob.ResolveHighestPriorityDirective() == null); Assert.True(f.Reference() == null); }
        [PortedTest(Source)] public static void the_only_directive_with_a_target_wins() { var f = new Fixture(); f.Selects(f.Add(10, 1)); }
        [PortedTest(Source)] public static void a_tied_priority_goes_to_the_directive_added_first() { var f = new Fixture(); var first = f.Add(100, 1); f.Add(100, 2); f.Selects(first); }
        [PortedTest(Source)] public static void a_higher_priority_added_later_still_wins() { var f = new Fixture(); f.Add(10, 1); var stronger = f.Add(100, 2); f.Add(50, 3); f.Selects(stronger); }
        [PortedTest(Source)] public static void a_highest_priority_directive_with_no_target_is_skipped() { var f = new Fixture(); f.Add(100, null); f.Selects(f.Add(50, 1)); }
        [PortedTest(Source)] public static void a_tie_is_broken_by_the_first_directive_that_actually_has_a_target() { var f = new Fixture(); f.Add(100, null); var usable = f.Add(100, 1); f.Add(100, 2); f.Selects(usable); }
        [PortedTest(Source)] public static void no_directive_with_a_target_means_no_directive() { var f = new Fixture(); f.Add(100, null); f.Add(50, null); Assert.True(f.Mob.ResolveHighestPriorityDirective() == null); Assert.True(f.Reference() == null); }
        [PortedTest(Source)] public static void selection_only_reads_the_directives_and_leaves_them_unchanged() {
            var f = new Fixture(); var stale = f.Add(100, null); var usable = f.Add(50, 1); f.Mob.ResolveHighestPriorityDirective(); f.Mob.ResolveHighestPriorityDirective();
            Assert.True(stale.Target == null); Assert.Equal(new Vector3(1, 0, 0), usable.Target.Value); Assert.Equal(4, f.Owner.Components.Length);
        }
        [PortedTest(Source)] public static void hovering_mob_attacks_a_ground_target_it_is_standing_over() {
            var f = new Fixture(); var mob = f.Aerial(new Vector3(0, 3, 0), (Fix64)0.3m, true, out _); mob.Start(); f.Frames(mob, 40); Assert.True(f.Attacked);
        }
        [PortedTest(Source)] public static void hovering_mob_still_waits_while_the_horizontal_gap_exceeds_range() {
            var f = new Fixture(); var mob = f.Aerial(new Vector3((Fix64)6.5m, 3, 0), (Fix64)0.3m, true, out _); mob.Start(); f.Frames(mob, 3); Assert.Equal(false, f.Attacked);
        }
        [PortedTest(Source)] public static void ground_mob_keeps_the_vertical_term_of_the_range_check() {
            var f = new Fixture(); var mob = f.Aerial(new Vector3(0, 3, 0), (Fix64)0.5m, false, out _); mob.Start(); f.Frames(mob, 40); Assert.Equal(false, f.Attacked);
        }
        [PortedTest(Source)] public static void ground_mob_keeps_its_existing_engagement_distance() {
            var f = new Fixture(); var mob = f.Aerial(new Vector3((Fix64)3.5m, 0, 0), (Fix64)0.5m, false, out _); mob.Start(); f.Frames(mob, 3); Assert.Equal(false, f.Attacked);
        }
        [PortedTest(Source)] public static void dive_progress_completes_instead_of_producing_nan_at_equal_altitude() {
            var f = new Fixture(); var mob = f.Aerial(new Vector3(0, 3, 0), (Fix64)0.3m, true, out _);
            Assert.Equal(Fix64.One, mob.DiveProgress(new Vector3(0, 3, 0), 3)); Assert.Equal((Fix64)0.5m, mob.DiveProgress(new Vector3(0, 6, 0), 0));
        }
        private static SimObject RallyMob(Fix64 range) {
            var owner = new SimObject(new SimulationContext(0), Master.LeftPlayer, "RockSlime", Vector3.Zero);
            var mob = new MeleeAttackMob(owner, 100, 1, 1, 1, 1) { AttackRange = range }; owner.AddComponentImmediately(mob); return owner;
        }
        private static SimObject RallyTarget(Fix64 x, Fix64 radius = default) {
            var owner = new SimObject(new SimulationContext(0), Master.RightPlayer, "RockSlime", new Vector3(x, 0, 0)); if (radius > Fix64.Zero) owner.AddCollider(new CircleCollider(owner, radius)); return owner;
        }
        private static SimObject Totem() { var owner = RallyTarget(0); owner.SetStatus(ObjectStatus.Idle); return owner; }
        [PortedTest(Rally)] public static void arrival_distance_is_the_zone_radius() { var mob = RallyMob((Fix64)0.5m); Assert.Equal((Fix64)3, new RallyMoveDirective(mob, Totem(), 3).ArrivalDistance); }
        [PortedTest(Rally)] public static void melee_mob_fights_only_targets_it_reaches_from_the_zone_edge() {
            var mob = RallyMob((Fix64)0.5m); var directive = new RallyMoveDirective(mob, Totem(), 3);
            Assert.True(directive.AllowsCombatTarget(mob, RallyTarget((Fix64)3.5m))); Assert.Equal(false, directive.AllowsCombatTarget(mob, RallyTarget((Fix64)3.51m)));
        }
        [PortedTest(Rally)] public static void ranged_mob_fights_the_same_target_that_a_melee_mob_is_refused() {
            var melee = RallyMob((Fix64)0.5m); var ranged = RallyMob(5); var target = RallyTarget((Fix64)4.5m); var directive = new RallyMoveDirective(melee, Totem(), 3);
            Assert.Equal(false, directive.AllowsCombatTarget(melee, target)); Assert.True(directive.AllowsCombatTarget(ranged, target));
        }
        [PortedTest(Rally)] public static void target_radius_extends_what_the_mob_can_hit_from_the_zone() {
            var mob = RallyMob((Fix64)0.5m); Assert.True(new RallyMoveDirective(mob, Totem(), 3).AllowsCombatTarget(mob, RallyTarget((Fix64)4.3m, 1)));
        }
        [PortedTest(Rally)] public static void disallows_combat_when_the_rally_totem_is_destroyed() {
            var mob = RallyMob(5); var totem = Totem(); totem.SetStatus(ObjectStatus.Destroyed); Assert.Equal(false, new RallyMoveDirective(mob, totem, 3).AllowsCombatTarget(mob, RallyTarget(0)));
        }
    }
}
