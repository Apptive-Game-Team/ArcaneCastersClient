using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    // 원본의 피격·사거리 질의 형태. 물리엔진 body와 별도로 여러 hitbox를 가질 수 있다.
    public abstract class SimCollider : ISimCollider {
        public SimObject Owner { get; }
        public bool IsTrigger { get; }
        public virtual Fix64? CircleRadius => null;
        public Vector3 Position => Owner.Position;
        public Fix64 InverseMass => Owner.GetComponent<RigidBody>()?.InverseMass ?? Fix64.Zero;
        public Vector3 Velocity => Owner.GetComponent<RigidBody>()?.Velocity ?? Vector3.Zero;
        protected SimCollider(SimObject owner, bool trigger) { Owner = owner; IsTrigger = trigger; }
        public abstract bool IsColliding(SimCollider other);
        public abstract Vector3? Displacement(SimCollider other);
    }
    public sealed class CircleCollider : SimCollider {
        public Fix64 Radius { get; }
        public override Fix64? CircleRadius => Radius;
        public CircleCollider(SimObject owner, Fix64 radius, bool trigger = false) : base(owner, trigger) { Radius = radius; }
        public override bool IsColliding(SimCollider other) {
            if (other is CircleCollider circle) return Vector3.Distance(Position, circle.Position) < Radius + circle.Radius;
            return other is EdgeCollider && other.IsColliding(this);
        }
        public override Vector3? Displacement(SimCollider other) {
            if (other is CircleCollider) return Position - other.Position;
            if (other is EdgeCollider edge) { var value = edge.Displacement(this); return value.HasValue ? -value.Value : (Vector3?)null; }
            return null;
        }
    }
    public sealed class EdgeCollider : SimCollider {
        private readonly Vector3 relative1, relative2;
        private Vector3? cachedPosition;
        private Vector3 point1, point2;
        public Vector3 Point1 { get { Refresh(); return point1; } }
        public Vector3 Point2 { get { Refresh(); return point2; } }
        internal Vector3? CachedPosition => cachedPosition;
        public EdgeCollider(SimObject owner, Vector3 point1, Vector3 point2, bool trigger = false) : base(owner, trigger) { relative1 = point1; relative2 = point2; }
        private void Refresh() {
            if (cachedPosition.HasValue && cachedPosition.Value == Position) return;
            cachedPosition = Position; var ground = GroundVector.Of(Position); point1 = relative1 + ground; point2 = relative2 + ground;
        }
        private static bool Ccw(Vector3 a, Vector3 b, Vector3 c) => (c.Z - a.Z) * (b.X - a.X) > (b.Z - a.Z) * (c.X - a.X);
        private Vector3 Projection(Vector3 center) {
            var a = Point1; var ab = Point2 - a; Fix64 lengthSquared = ab.LengthSquared();
            if (lengthSquared == Fix64.Zero) return a;
            Fix64 t = MathHelper.Clamp(Vector3.Dot(center - a, ab) / lengthSquared, Fix64.Zero, Fix64.One);
            return a + ab * t;
        }
        public bool OverlapsSphere(Vector3 center, Fix64 radius) => (center - Projection(center)).LengthSquared() <= radius * radius;
        public override bool IsColliding(SimCollider other) {
            if (other is EdgeCollider edge) return Ccw(Point1, edge.Point1, edge.Point2) != Ccw(Point2, edge.Point1, edge.Point2)
                && Ccw(Point1, Point2, edge.Point1) != Ccw(Point1, Point2, edge.Point2);
            if (other is CircleCollider circle) return (circle.Position - Projection(circle.Position)).LengthSquared() <= circle.Radius * circle.Radius;
            return false;
        }
        public override Vector3? Displacement(SimCollider other) => other is CircleCollider ? Projection(other.Position) - other.Position : (Vector3?)null;
    }
    public static class CollisionChecker {
        public static bool IsColliding(SimObject first, SimObject second) {
            foreach (var a in first.Colliders.OfType<SimCollider>()) foreach (var b in second.Colliders.OfType<SimCollider>()) if (a.IsColliding(b)) return true;
            return false;
        }
    }
}
