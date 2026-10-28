using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class ColliderParityTests {
        private sealed class Counting : SimComponent {
            public int Updates;
            private readonly bool fails;
            public Counting(SimObject owner, bool fails) : base(owner) { this.fails = fails; }
            public override void Update() { Updates++; if (fails) throw new System.InvalidOperationException("broken component"); }
        }
        [PortedTest("game/service/system/component_update_system.rs")]
        public static void an_object_that_panics_is_skipped_and_the_others_still_update() {
            var context = new SimulationContext(0); context.RegisterPrefab("ZapMouse", _ => { });
            var broken = context.Spawn(Master.RightPlayer, "ZapMouse", Vector3.Zero); var healthy = context.Spawn(Master.LeftPlayer, "ZapMouse", Vector3.Zero);
            var brokenComponent = new Counting(broken, true); var healthyComponent = new Counting(healthy, false);
            broken.AddComponentImmediately(brokenComponent); healthy.AddComponentImmediately(healthyComponent); context.FlushObjects();
            var system = new ComponentUpdateSystem(); system.Update(context); system.Update(context);
            Assert.Equal(2, brokenComponent.Updates); Assert.Equal(2, healthyComponent.Updates);
        }
        private const string Edge = "game/domain/object/component/physic/edge_collider.rs";
        private const string Body = "game/domain/object/component/physic/rigid_body.rs";
        [PortedTest(Body)] public static void apply_velocity_moves_by_delta_time_and_clears() {
            var owner = Object(new Vector3(5,0,5)); owner.Context.DeltaTime = (Fix64)0.1m;
            var body = new RigidBody(owner,1); body.AddVelocity(new Vector3(2,0,0)); body.ApplyVelocity();
            // 고정 소수점에는 NaN이 없다. 이동량의 허용 오차는 원본 float보다 작게 제한한다.
            Assert.Near((Fix64)5.2m, owner.Position.X, Fix64.FromRaw(1));
            Assert.Equal(Fix64.Zero, owner.Position.Y); Assert.Equal((Fix64)5, owner.Position.Z);
            Assert.Equal(Vector3.Zero,body.Velocity);
            var moved = owner.Position; body.ApplyVelocity(); Assert.Equal(moved,owner.Position);
        }
        private static SimObject Object(Vector3 position) => new SimObject(new SimulationContext(0), Master.None, "Wall", position);
        private static EdgeCollider LeftEdge(SimObject owner) { var edge = new EdgeCollider(owner, Vector3.Zero, new Vector3(0, 0, 10)); owner.AddCollider(edge); return edge; }
        [PortedTest(Edge)] public static void cached_endpoints_are_reused_while_the_owner_stays_put() {
            var edge = LeftEdge(Object(Vector3.Zero)); var first = edge.Point1; var second = edge.Point2; var cached = edge.CachedPosition;
            Assert.True(cached.HasValue); Assert.Equal(first, edge.Point1); Assert.Equal(second, edge.Point2); Assert.Equal(cached.Value, edge.CachedPosition.Value);
        }
        [PortedTest(Edge)] public static void cached_endpoints_are_rebuilt_when_the_owner_moves() {
            var owner = Object(Vector3.Zero); var edge = LeftEdge(owner); Assert.Equal(Vector3.Zero, edge.Point1); Assert.Equal(new Vector3(0, 0, 10), edge.Point2);
            owner.SetPosition(new Vector3(3, 0, 2)); Assert.Equal(false, owner.IsDestroyed); Assert.Equal(new Vector3(3, 0, 2), edge.Point1); Assert.Equal(new Vector3(3, 0, 12), edge.Point2);
        }
        [PortedTest(Edge)] public static void cached_endpoints_ignore_the_owner_height() {
            var owner = Object(Vector3.Zero); var edge = LeftEdge(owner); owner.SetPosition(new Vector3(1, 4, 0)); Assert.Equal(Fix64.Zero, edge.Point1.Y); Assert.Equal(Fix64.Zero, edge.Point2.Y);
        }
        [PortedTest(Edge)] public static void airborne_object_does_not_collide_with_the_wall_it_would_hit_on_the_ground() {
            var edge = LeftEdge(Object(Vector3.Zero)); var ground = new CircleCollider(Object(new Vector3(0, 0, 5)), (Fix64)0.5m); var air = new CircleCollider(Object(new Vector3(0, 2, 5)), (Fix64)0.5m);
            Assert.True(edge.IsColliding(ground)); Assert.Equal(false, edge.IsColliding(air)); Assert.True(ground.IsColliding(edge)); Assert.Equal(false, air.IsColliding(edge));
        }
        [PortedTest(Edge)] public static void airborne_object_within_radius_of_the_edge_plane_still_collides() {
            var wall = Object(Vector3.Zero); LeftEdge(wall); var flyer = Object(new Vector3(0, (Fix64)0.25m, 5)); flyer.AddCollider(new CircleCollider(flyer, (Fix64)0.5m)); Assert.True(CollisionChecker.IsColliding(flyer, wall));
        }
        [PortedTest(Body)] public static void resolves_rigid_body_registered_through_deferred_add_component_queue() {
            var owner = Object(Vector3.Zero); var collider = new CircleCollider(owner, (Fix64)0.5m); owner.AddCollider(collider); owner.AddComponent(new RigidBody(owner, 4)); Assert.Equal(Fix64.Zero, collider.InverseMass);
            owner.FlushComponents(); Assert.Equal((Fix64)0.25m, collider.InverseMass); var body = owner.GetComponent<RigidBody>(); body.AddVelocity(new Vector3(1, 0, 2)); Assert.Equal(body.Velocity, collider.Velocity);
        }
        [PortedTest(Body)] public static void reports_zero_inv_mass_when_no_rigid_body_exists() {
            var collider = new CircleCollider(Object(Vector3.Zero), (Fix64)0.5m); Assert.Equal(Fix64.Zero, collider.InverseMass); Assert.Equal(Vector3.Zero, collider.Velocity);
        }
    }
}
