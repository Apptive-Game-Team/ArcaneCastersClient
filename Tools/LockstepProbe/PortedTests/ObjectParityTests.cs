using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class ObjectParityTests {
        private static SimulationContext ExampleContext() {
            var context = new SimulationContext();
            context.GameParameters["fire_slime.mass"] = 1; context.GameParameters["fire_slime.radius"] = (Fix64)0.5m;
            context.RegisterPrefab("FireSlime", owner => {
                owner.AddComponentImmediately(new RigidBody(owner, (int)context.GameParameters["fire_slime.mass"]));
                owner.AddComponentImmediately(new ZPhysics(owner));
                owner.AddCollider(new CircleCollider(owner, context.GameParameters["fire_slime.radius"], false));
                owner.Elements.AddNative(Element.Fire);
                owner.AddComponentImmediately(new TimedSelfDestroyer(owner, 3));
            });
            // 원본 예제의 미등록 prefab은 빈 초기화로 재현한다. 실제 콘텐츠의 누락 거부는 유지한다.
            context.RegisterPrefab("RockSlime", _ => { }); context.RegisterPrefab("FireField", _ => { });
            return context;
        }
        private readonly struct ExampleHit {
            public readonly int Attacker, Target;
            public ExampleHit(int attacker, int target) { Attacker = attacker; Target = target; }
        }
        private sealed class ExampleSpiky : SimComponent, ICollidable, IDamageable {
            private readonly FlowSubscription subscription;
            public int Hits;
            public ExampleSpiky(SimObject owner) : base(owner) {
                subscription = owner.SubscribeStatusChange(change => { if (change.Current == ObjectStatus.Attack) Hits++; });
            }
            public void OnCollision(SimObject other) { }
            public void OnCollisionWithEnemy(SimObject other) {
                var body = other.GetComponent<RigidBody>();
                if (body != null) {
                    var angle = Context.Random.NextFraction() * Fix64.Pi * 2;
                    body.AddVelocity(new Vector3(Fix64.Cos(angle), 0, Fix64.Sin(angle)));
                }
                other.GetComponent<IDamageable>()?.OnDamaged(new Hit(1, Owner.Id, Element.Fire));
                Context.AddEvent(new ExampleHit(Owner.Id, other.Id));
            }
            public void OnDamaged(Hit hit) => Context.Spawn(Owner.Master, "FireField", Owner.Position);
            public void OnDamagedWithDelay(Hit hit, Fix64 delay) => OnDamaged(hit);
            public override void OnDestroy() => subscription.Cancel();
        }
        [PortedTest("tests/porting_examples.rs")] public static void the_prefab_initializer_builds_the_slime() {
            var context = ExampleContext(); var slime = context.Spawn(Master.LeftPlayer, "FireSlime", new Vector3(5, 0, 5));
            Assert.Equal(ObjectStatus.Idle, slime.Status); Assert.Equal(1, slime.GetComponent<RigidBody>().Mass);
            Assert.True(slime.HasComponent<ZPhysics>()); Assert.Equal<Fix64?>((Fix64)0.5m, slime.FirstCircleRadius(false));
            Assert.True(slime.Elements.Has(Element.Fire));
        }
        [PortedTest("tests/porting_examples.rs")] public static void the_component_destroys_its_object_after_its_lifetime() {
            var context = ExampleContext(); context.DeltaTime = 1;
            var slime = context.Spawn(Master.LeftPlayer, "FireSlime", new Vector3(5, 0, 5));
            var destroyer = slime.GetComponent<TimedSelfDestroyer>();
            destroyer.Freeze(); slime.Update(); slime.Update(); destroyer.RecoverBy((Fix64)0.5m);
            slime.Update(); slime.Update(); Assert.True(!slime.IsDestroyed);
            slime.Update(); Assert.True(slime.IsDestroyed);
        }
        [PortedTest("tests/porting_examples.rs")] public static void interfaces_lookups_spawning_status_events_and_randomness() {
            var context = ExampleContext();
            var owner = context.Spawn(Master.LeftPlayer, "RockSlime", new Vector3(5, 0, 5));
            var spiky = new ExampleSpiky(owner); owner.AddComponent(spiky); owner.FlushComponents();
            var target = context.Spawn(Master.RightPlayer, "FireSlime", new Vector3(6, 0, 5));
            target.AddComponent(new ExampleSpiky(target)); target.FlushComponents();
            var collidables = owner.GetComponents<ICollidable>(); Assert.Equal(1, collidables.Length);
            collidables[0].OnCollisionWithEnemy(target);
            Assert.True(target.GetComponent<RigidBody>().Velocity != Vector3.Zero);
            Assert.Sequence(new[] { new ExampleHit(owner.Id, target.Id) }, context.DrainEvents<ExampleHit>());
            Assert.Equal("FireField", context.PendingObjects.Last().Prefab);
            owner.SetStatus(ObjectStatus.Attack); Assert.Equal(1, spiky.Hits);
        }
        private const string Builder = "game/service/objects_info_dto_builder.rs";
        private sealed class OutputFixture {
            public readonly SimulationContext Context = new SimulationContext();
            public readonly BattlePresentationOutput Output;
            public BattlePresentationFrame Last;
            public OutputFixture() { Output = new BattlePresentationOutput(null, value => Last = value); Output.Initialize(Context, new BattleEconomySystem(), 180000); Context.RegisterPrefab("FireShot", _ => { }); }
            public SimObject Detached() => new SimObject(Context, Master.LeftPlayer, "FireShot", Vector3.Zero);
            public BattlePresentationFrame Take() { Output.BeginFrame(Context); Output.EndFrame(Context); return Last; }
        }
        [PortedTest(Builder)] public static void repeated_updates_in_one_frame_collapse_into_one_dto_carrying_the_last_state() {
            var f = new OutputFixture(); var owner = f.Detached(); owner.ApplyUpdate(); owner.SetPosition(new Vector3(1, 0, 2));
            owner.SetStatus(ObjectStatus.Move); owner.SetMaster(Master.RightPlayer); owner.AddEffect(VisualEffect.Burn); owner.SetPosition(new Vector3(3, 0, 4));
            var updates = f.Take().Updated; Assert.Equal(1, updates.Length); var dto = updates[0];
            Assert.Equal(owner.Id, dto.Id); Assert.Equal(new Vector3(3, 0, 4), dto.Position); Assert.Equal(ObjectStatus.Move, dto.Status);
            Assert.Equal(Master.RightPlayer, dto.Master); Assert.Sequence(new[] { VisualEffect.Burn }, dto.Effects);
        }
        [PortedTest(Builder)] public static void update_list_keeps_first_update_order_when_objects_are_updated_again() {
            var f = new OutputFixture(); var a = f.Detached(); var b = f.Detached(); var c = f.Detached();
            foreach (var owner in new[] { a, b, c, a, b }) owner.ApplyUpdate();
            Assert.Sequence(new[] { a.Id, b.Id, c.Id }, f.Take().Updated.Select(value => value.Id));
        }
        [PortedTest(Builder)] public static void get_objects_info_dto_resets_list_and_index_together_so_the_next_frame_starts_empty() {
            var f = new OutputFixture(); var owner = f.Detached(); owner.ApplyUpdate(); var first = f.Take();
            Assert.Equal(0, f.Take().Updated.Length); owner.SetPosition(new Vector3(5, 0, 6)); var third = f.Take();
            Assert.Equal(1, first.Updated.Length); Assert.Equal(1, third.Updated.Length); Assert.Equal(Vector3.Zero, first.Updated[0].Position); Assert.Equal(new Vector3(5, 0, 6), third.Updated[0].Position);
        }
        [PortedTest(Builder)] public static void object_created_and_updated_in_the_same_frame_appears_once_in_each_list() {
            var f = new OutputFixture(); var owner = f.Context.Spawn(Master.LeftPlayer, "FireShot", Vector3.Zero); owner.ApplyUpdate(); owner.SetStatus(ObjectStatus.Attack);
            var frame = f.Take(); Assert.Equal(1, f.Context.PendingObjects.Length); Assert.Equal(1, frame.Created.Length); Assert.Equal(1, frame.Updated.Length);
            Assert.Equal(owner.Id, frame.Created[0].Id); Assert.Equal(owner.Id, frame.Updated[0].Id); Assert.Equal(ObjectStatus.Attack, frame.Updated[0].Status);
        }
        [PortedTest(Builder)] public static void effects_and_gizmos_are_snapshotted() {
            var f = new OutputFixture(); f.Context.ObjectInitializer = owner => { owner.DrawCircle(Vector3.Zero, 1, GizmoCategory.Collider); owner.AddEffect(VisualEffect.Wet); };
            var obj = f.Context.Spawn(Master.LeftPlayer, "FireShot", Vector3.Zero); obj.ApplyUpdate(); var frame = f.Take(); obj.ClearEffects();
            Assert.Sequence(new[] { SimGizmo.Circle(Vector3.Zero, 1, GizmoCategory.Collider) }, frame.Created[0].Gizmos); Assert.Sequence(new[] { VisualEffect.Wet }, frame.Updated[0].Effects);
        }
        [PortedTest(Builder)] public static void projections_carry_their_targets_and_width() {
            var f = new OutputFixture(); f.Context.AddEvent(new BattleEvent("Beam", Vector3.Zero, new Vector3(1, 0, 2), (Fix64)0.25m));
            var frame = f.Take(); Assert.Equal(1, frame.Events.Length); Assert.Equal(Fix64.Zero, frame.Events[0].Width); Assert.Equal(0, f.Take().Events.Length);
        }
        private static SimObject SnapshotOwner() {
            var owner = new SimObject(new SimulationContext(0), Master.LeftPlayer, "FireShot", new Vector3(1, 2, 3)); owner.SetStatus(ObjectStatus.Move); return owner;
        }
        [PortedTest("game/util/snapshot_mapper.rs")] public static void maps_every_field_with_the_java_names() {
            var owner = SnapshotOwner(); var dto = new ObjectSnapshot(owner);
            Assert.Equal(owner.Id, dto.Id); Assert.Equal("FireShot", dto.Prefab); Assert.Equal(new Vector3(1, 2, 3), dto.Position);
            Assert.Equal(Master.LeftPlayer, dto.Master); Assert.Equal(ObjectStatus.Move, dto.Status); Assert.Equal(0, dto.Effects.Count); Assert.Equal(0, dto.Gizmos.Count); Assert.Equal(0, dto.Gauges.Count);
        }
        [PortedTest("game/util/snapshot_mapper.rs")] public static void non_empty_effects_and_gizmos_are_snapshotted() {
            var owner = SnapshotOwner(); owner.AddEffect(VisualEffect.Burn); owner.DrawCircle(Vector3.Zero, 1, GizmoCategory.Collider);
            var dto = new ObjectSnapshot(owner); owner.ClearEffects(); owner.DrawBox(Vector3.Zero, new Vector3(1, 1, 1), GizmoCategory.SpawnArea);
            Assert.Sequence(new[] { VisualEffect.Burn }, dto.Effects); Assert.Equal(1, dto.Gizmos.Count); Assert.Equal(SimGizmo.Circle(Vector3.Zero, 1, GizmoCategory.Collider), dto.Gizmos[0]);
        }
        private sealed class SnapshotBar : SimComponent, IGaugeComponent {
            private readonly Gauge gauge;
            public SnapshotBar(SimObject owner, Gauge gauge) : base(owner) { this.gauge = gauge; }
            public Gauge GetGauge() => gauge;
        }
        [PortedTest("game/util/snapshot_mapper.rs")] public static void a_later_gauge_replaces_its_category_and_a_negative_max_is_skipped() {
            var owner = SnapshotOwner();
            foreach (var gauge in new[] { new Gauge(1, 10, GaugeCategory.Hp), new Gauge(5, -1, GaugeCategory.Ttl), new Gauge(2, 10, GaugeCategory.Hp) }) owner.AddComponentImmediately(new SnapshotBar(owner, gauge));
            Assert.Sequence(new[] { new Gauge(2, 10, GaugeCategory.Hp) }, new ObjectSnapshot(owner).Gauges);
        }
        private const string Object = "game/domain/object/game_object.rs";
        private const string ElementSource = "game/domain/object/element.rs";
        private const string Flow = "game/util/synchronous_flow_publisher.rs";
        private const string Flush = "game/service/system/game_object_add_remote_system.rs";
        private sealed class Counter : SimComponent {
            public int Starts, Updates, Destroys;
            public Counter(SimObject owner) : base(owner) { }
            public override void Start() { Starts++; }
            public override void Update() { Updates++; }
            public override void OnDestroy() { Destroys++; }
        }
        private static SimulationContext Context() {
            var context = new SimulationContext();
            // 원본 GameContext::for_test의 빈 PrefabProvider에 해당한다.
            context.RegisterPrefab("FireSlime", _ => { }); return context;
        }
        private static SimObject NewObject(SimulationContext context) => context.Spawn(Master.LeftPlayer, "FireSlime", new Vector3(5, 0, 5));

        [PortedTest(Object)]
        public static void a_new_object_starts_and_queues_itself() {
            var context = Context(); var owner = NewObject(context);
            Assert.Equal(ObjectStatus.Idle, owner.Status); Assert.True(owner.IsActive); Assert.Equal(1, context.PendingObjects.Length);
        }
        [PortedTest(Object)]
        public static void components_are_added_and_removed_at_the_flush() {
            var owner = NewObject(Context()); var counter = new Counter(owner); owner.AddComponent(counter);
            Assert.True(!owner.HasComponent<Counter>()); owner.FlushComponents();
            Assert.True(owner.HasComponent<Counter>()); Assert.Equal(1, counter.Starts);
            owner.Update(); Assert.Equal(1, counter.Updates); owner.RemoveComponent(counter);
            Assert.True(owner.HasComponent<Counter>()); owner.FlushComponents();
            Assert.True(!owner.HasComponent<Counter>()); Assert.Equal(1, counter.Destroys);
        }
        [PortedTest(Object)]
        public static void status_rules_and_listeners() {
            var owner = NewObject(Context()); var seen = new List<StatusChange>(); owner.SubscribeStatusChange(seen.Add);
            owner.SetStatus(ObjectStatus.Idle); owner.SetStatus(ObjectStatus.Attack); owner.SetStatus(ObjectStatus.Attack);
            owner.SetStatus(ObjectStatus.Dying); owner.SetStatus(ObjectStatus.Idle); owner.SetStatus(ObjectStatus.Destroyed); owner.SetStatus(ObjectStatus.Idle);
            Assert.Sequence(new[] { ObjectStatus.Attack, ObjectStatus.Attack, ObjectStatus.Dying, ObjectStatus.Destroyed }, seen.Select(value => value.Current));
            Assert.Equal(ObjectStatus.Destroyed, owner.Status);
        }
        [PortedTest(Object)]
        public static void leaving_the_arena_destroys_the_object() {
            var owner = NewObject(Context()); var counter = new Counter(owner); owner.AddComponent(counter); owner.FlushComponents();
            owner.SetPosition(new Vector3((Fix64)18.5m, 0, 5)); Assert.True(owner.IsDestroyed); Assert.Equal(1, counter.Destroys);
        }
        [PortedTest(Object)]
        public static void effects_ignore_none_and_duplicates() {
            var owner = NewObject(Context()); owner.AddEffect(VisualEffect.None); owner.AddEffect(VisualEffect.Burn);
            owner.AddEffect(VisualEffect.Burn); owner.AddEffect(VisualEffect.Wet);
            Assert.Sequence(new[] { VisualEffect.Burn, VisualEffect.Wet }, owner.Effects);
            owner.RemoveEffects(effect => effect == VisualEffect.Burn); Assert.Sequence(new[] { VisualEffect.Wet }, owner.Effects);
            owner.ClearEffects(); Assert.Equal(0, owner.Effects.Length);
        }
        [PortedTest(Object)]
        public static void a_dying_object_only_updates_components_that_opt_in() {
            var owner = NewObject(Context()); var counter = new Counter(owner); owner.AddComponent(counter); owner.FlushComponents();
            owner.SetStatus(ObjectStatus.Dying); owner.Update(); Assert.Equal(0, counter.Updates);
        }
        [PortedTest(ElementSource)]
        public static void total_is_none_when_empty_and_merges_bonus_sources() {
            var elements = new ElementSet(); Assert.Sequence(new[] { Element.None }, elements.Total);
            elements.AddNative(Element.Water); elements.AddBonus(Element.Fire, 1); elements.AddBonus(Element.Fire, 2);
            Assert.Sequence(new[] { Element.Fire, Element.Water }, elements.Total);
            elements.RemoveBonus(Element.Fire, 1); Assert.True(elements.Has(Element.Fire));
            elements.ClearBonusFrom(2); Assert.True(!elements.Has(Element.Fire)); Assert.True(elements.NativeHas(Element.Water));
        }
        [PortedTest(Flow)]
        public static void publishes_to_every_subscriber_until_canceled() {
            var publisher = new FlowPublisher<int>(); var seen = new List<int>(); var subscription = publisher.Subscribe(seen.Add);
            publisher.Publish(1); subscription.Cancel(); publisher.Publish(2); Assert.Sequence(new[] { 1 }, seen);
        }
        [PortedTest(Flow)]
        public static void a_subscriber_may_cancel_itself_while_being_called() {
            var publisher = new FlowPublisher<int>(); var seen = new List<int>(); FlowSubscription subscription = null;
            subscription = publisher.Subscribe(value => { seen.Add(value); subscription.Cancel(); });
            publisher.Publish(1); publisher.Publish(2); Assert.Sequence(new[] { 1 }, seen);
        }
        [PortedTest(Flow)]
        public static void a_closed_publisher_accepts_and_sends_nothing() {
            var publisher = new FlowPublisher<int>(); int seen = 0; publisher.Subscribe(_ => seen++); publisher.Close();
            publisher.Subscribe(_ => seen++); publisher.Publish(1); Assert.Equal(0, seen); Assert.Equal(0, publisher.SubscriberCount);
        }
        [PortedTest(Flush)]
        public static void moves_created_objects_in_and_destroyed_objects_out() {
            var context = Context(); var first = NewObject(context); var second = NewObject(context);
            context.FlushObjects(); Assert.Equal(2, context.Objects.Length);
            first.Destroy(); context.FlushObjects(); Assert.Equal(1, context.Objects.Length); Assert.Equal(second.Id, context.Objects[0].Id);
        }
    }
}
