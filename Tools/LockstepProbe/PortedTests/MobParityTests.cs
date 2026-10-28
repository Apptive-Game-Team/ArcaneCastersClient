using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class MobParityTests {
        private const string Source = "tests/mob.rs";
        private sealed class TestMob : MobComponent {
            private readonly bool falls;
            public TestMob(SimObject owner, int hp, bool falls = true) : base(owner, hp, Fix64.One) { this.falls = falls; }
            public override bool FallsOnDeath => falls;
            public override void OnDeath() { Owner.Destroy(); }
        }
        private sealed class Listener : SimComponent, ICombatDeathListener {
            public int Notifications;
            public Listener(SimObject owner) : base(owner) { }
            public void OnCombatDeath() { Notifications++; }
        }
        private sealed class Counting : SimComponent {
            public int Updates;
            public Counting(SimObject owner) : base(owner) { }
            public override void Update() { Updates++; }
        }
        private static SimulationContext Context() {
            var context = new SimulationContext(0);
            foreach (string prefab in new[] { "FireField", "WaterField", "LeafField", "ElectricField" }) context.RegisterPrefab(prefab, _ => { });
            return context;
        }
        private static SimObject Object(SimulationContext context, Master master = Master.LeftPlayer, Vector3? position = null) {
            var owner = new SimObject(context, master, "ZapMouse", position ?? Vector3.Zero); owner.SetStatus(ObjectStatus.Idle); return owner;
        }
        private static Listener Listen(SimObject owner) { var listener = new Listener(owner); owner.AddComponent(listener); owner.FlushComponents(); return listener; }
        private static TestMob Add(SimObject owner, int hp = 10, bool falls = true) {
            var mob = new TestMob(owner, hp, falls); owner.AddComponent(mob); owner.FlushComponents(); return mob;
        }
        private static SimObject Aerial(SimulationContext context, Fix64 height) {
            var owner = Object(context, position: new Vector3(9, height, 5)); owner.Elements.AddNative(Element.Lightning);
            owner.AddComponent(new ZPhysics(owner, height)); owner.FlushComponents(); return owner;
        }
        private static void Tick(SimObject owner) {
            owner.Update(); if (!owner.IsActive) return;
            var physics = owner.GetComponent<ZPhysics>(); if (physics.CanHover) physics.ApplyHover(); else physics.ApplyZForce();
            owner.FlushComponents();
        }
        [PortedTest(Source)]
        public static void notifies_combat_death_listener_only_when_damage_is_lethal() {
            var owner = Object(Context()); owner.Elements.AddNative(Element.Lightning); var listener = Listen(owner); var mob = new TestMob(owner, 10);
            mob.ApplyDamage(new Hit(9, 0)); Assert.Equal(0, listener.Notifications);
            mob.ApplyDamage(new Hit(1, 0)); Assert.Equal(1, listener.Notifications);
        }
        [PortedTest(Source)]
        public static void direct_destroy_does_not_notify_combat_death_listener() {
            var owner = Object(Context()); owner.Elements.AddNative(Element.Lightning); var listener = Listen(owner);
            owner.Destroy(); Assert.Equal(0, listener.Notifications);
        }
        [PortedTest(Source)]
        public static void overrides_both_max_hp_and_current_hp_and_tells_the_client() {
            var context = Context(); var updates = new List<int>(); context.Updated += owner => updates.Add(owner.Id);
            var target = Object(context, Master.RightPlayer); var mob = Add(target, 100); updates.Clear();
            mob.OverrideMaxHp(500); Assert.Equal(500, mob.MaxHp); Assert.Equal(500, mob.Hp); Assert.Sequence(new[] { target.Id }, updates);
        }
        [PortedTest(Source)]
        public static void a_non_positive_override_is_ignored() {
            var mob = Add(Object(Context(), Master.RightPlayer), 100); mob.OverrideMaxHp(0); mob.OverrideMaxHp(-10);
            Assert.Equal(100, mob.MaxHp); Assert.Equal(100, mob.Hp);
        }
        [PortedTest(Source)]
        public static void publishes_hit_event_when_damage_lands() {
            var context = Context(); Object(context); var attacker = Object(context); var target = Object(context, Master.RightPlayer);
            new TestMob(target, 10).ApplyDamage(new Hit(3, attacker.Id)); var events = context.DrainEvents();
            Assert.Equal(1, events.Length); Assert.Equal("hit", events[0].Type); Assert.Equal(attacker.Id, events[0].SourceId); Assert.Equal(target.Id, events[0].TargetId);
        }
        [PortedTest(Source)]
        public static void publishes_nothing_when_damage_has_no_attacker() {
            var context = Context(); new TestMob(Object(context, Master.RightPlayer), 10).ApplyDamage(new Hit(3, 0)); Assert.Equal(0, context.DrainEvents().Length);
        }
        [PortedTest(Source)]
        public static void publishes_nothing_for_heals() {
            var context = Context(); Object(context); var healer = Object(context); var target = Object(context);
            new TestMob(target, 10).ApplyDamage(new Hit(-3, healer.Id)); Assert.Equal(0, context.DrainEvents().Length);
        }
        [PortedTest(Source)]
        public static void combat_death_lays_the_element_field_on_the_ground_for_the_owner() {
            var context = Context(); var owner = Object(context, position: new Vector3(2, 3, 4)); owner.Elements.AddNative(Element.Fire);
            new TestMob(owner, 5).ApplyDamage(new Hit(5, 0)); var field = context.PendingObjects.Last();
            Assert.Equal("FireField", field.Prefab); Assert.Equal(Master.LeftPlayer, field.Master); Assert.Equal(new Vector3(2, 0, 4), field.Position);
        }
        [PortedTest(Source)]
        public static void picks_one_of_the_field_elements_when_the_body_has_several() {
            for (int seed = 0; seed < 20; seed++) {
                var context = Context(); context.Random = new JavaRandom(seed); var owner = Object(context);
                foreach (var element in new[] { Element.Water, Element.Wind, Element.Lightning }) owner.Elements.AddNative(element);
                new TestMob(owner, 1).ApplyDamage(new Hit(1, 0)); Assert.Equal(1, context.PendingObjects.Length);
                Assert.True(new[] { "WaterField", "ElectricField" }.Contains(context.PendingObjects[0].Prefab));
            }
        }
        [PortedTest(Source)]
        public static void elements_without_a_field_leave_nothing() {
            var context = Context(); var owner = Object(context); owner.Elements.AddNative(Element.Rock); owner.Elements.AddNative(Element.Wind);
            new TestMob(owner, 1).ApplyDamage(new Hit(1, 0)); Assert.Equal(0, context.PendingObjects.Length);
        }
        [PortedTest(Source)]
        public static void direct_destroy_leaves_nothing() {
            var context = Context(); var owner = Object(context); owner.Elements.AddNative(Element.Fire); new TestMob(owner, 1);
            owner.Destroy(); Assert.Equal(0, context.PendingObjects.Length);
        }
        [PortedTest(Source)]
        public static void aerial_mob_keeps_falling_before_it_is_removed() {
            var owner = Aerial(Context(), (Fix64)3); var mob = Add(owner); var listener = Listen(owner);
            mob.ApplyDamage(new Hit(10, 0)); Assert.Equal(ObjectStatus.Dying, owner.Status); Assert.True(!owner.IsDestroyed); Assert.Equal(0, listener.Notifications);
            int ticks = 0; while (!owner.IsDestroyed && ticks < 200) { Tick(owner); ticks++; }
            Assert.True(owner.IsDestroyed); Assert.Near(owner.GetComponent<ZPhysics>().GroundY, owner.Position.Y, (Fix64)0.01m);
            Assert.Equal(1, listener.Notifications); Assert.True(ticks > 10);
        }
        [PortedTest(Source)]
        public static void dying_aerial_mob_stops_acting_and_cannot_be_hit_or_targeted() {
            var context = Context(); var owner = Aerial(context, (Fix64)3); var mob = Add(owner); var behavior = new Counting(owner);
            owner.AddComponent(behavior); owner.FlushComponents(); mob.ApplyDamage(new Hit(10, 0)); int hp = mob.Hp;
            Tick(owner); Tick(owner); mob.OnDamaged(new Hit(5, 0)); Assert.Equal(hp, mob.Hp); Assert.Equal(ObjectStatus.Dying, owner.Status); Assert.Equal(0, behavior.Updates);
            var enemy = Object(context, Master.RightPlayer, new Vector3(9, 0, 5)); Assert.True(!TargetRelation.CanAttack(enemy, owner));
        }
        [PortedTest(Source)]
        public static void dying_status_is_kept_until_the_mob_is_removed() {
            var owner = Aerial(Context(), (Fix64)3); var mob = Add(owner); mob.ApplyDamage(new Hit(10, 0));
            owner.SetStatus(ObjectStatus.Idle); owner.SetStatus(ObjectStatus.Damaged); Assert.Equal(ObjectStatus.Dying, owner.Status);
            owner.Destroy(); Assert.Equal(ObjectStatus.Destroyed, owner.Status);
        }
        [PortedTest(Source)]
        public static void ground_mob_is_removed_immediately() {
            var owner = Object(Context()); owner.Elements.AddNative(Element.Lightning); owner.AddComponent(new ZPhysics(owner)); owner.FlushComponents();
            var mob = Add(owner); var listener = Listen(owner); mob.ApplyDamage(new Hit(10, 0));
            Assert.Equal(ObjectStatus.Destroyed, owner.Status); Assert.Equal(1, listener.Notifications);
        }
        [PortedTest(Source)]
        public static void aerial_mob_already_on_the_ground_is_removed_immediately() {
            var owner = Aerial(Context(), Fix64.Zero); var mob = Add(owner); var listener = Listen(owner); mob.ApplyDamage(new Hit(10, 0));
            Assert.Equal(ObjectStatus.Destroyed, owner.Status); Assert.Equal(1, listener.Notifications);
        }
        [PortedTest(Source)]
        public static void aerial_mob_that_dies_at_a_point_is_removed_where_it_was_hit() {
            var owner = Aerial(Context(), (Fix64)3); var height = owner.Position.Y; var mob = Add(owner, 10, false); var listener = Listen(owner);
            mob.ApplyDamage(new Hit(10, 0)); Assert.Equal(ObjectStatus.Destroyed, owner.Status); Assert.Equal(1, listener.Notifications);
            Assert.Near(height, owner.Position.Y, (Fix64)0.01m);
        }
    }
}
