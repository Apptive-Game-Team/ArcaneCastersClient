using System;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class RollingRock : Shot {
        private readonly Fix64 radius;
        private int bounces;
        private int? lockedTarget;
        private static readonly Fix64 Separation = (Fix64)0.05m;
        public RollingRock(SimObject owner, int damage, Fix64 speed, Fix64 radius) : base(owner, damage, speed) { this.radius = radius; }
        private static Vector3 Normalize(Vector3 value) { var length = value.Length(); return length == 0 ? Vector3.Zero : new Vector3(value.X / length, value.Y / length, value.Z / length); }
        public override void SetTarget(Vector3 target) { Direction = Normalize(GroundVector.Of(target) - GroundVector.Of(Owner.Position)); }
        private bool ConsumeBounce() { if (bounces >= 2) return false; bounces++; return true; }
        public override void Update() {
            if (!Direction.HasValue || Direction.Value == Vector3.Zero) return;
            if (lockedTarget.HasValue) {
                var target = Context.Objects.FirstOrDefault(o => o.Id == lockedTarget.Value);
                if (target == null || target.IsDestroyed || !CollisionChecker.IsColliding(Owner, target)) lockedTarget = null;
            }
            var direction = Direction.Value; var next = GroundVector.Of(Owner.Position + direction * (speed * Context.DeltaTime)); bool bounced = false;
            if (next.X <= radius || next.X >= 18 - radius) {
                direction.X = -direction.X; next.X = MathHelper.Clamp(next.X, radius, 18 - radius) + (direction.X > 0 ? Separation : -Separation); bounced = true;
            }
            if (next.Z <= radius || next.Z >= 10 - radius) {
                direction.Z = -direction.Z; next.Z = MathHelper.Clamp(next.Z, radius, 10 - radius) + (direction.Z > 0 ? Separation : -Separation); bounced = true;
            }
            if (bounced) {
                if (!ConsumeBounce()) { Owner.Destroy(); return; }
                Direction = Normalize(direction); next.X = MathHelper.Clamp(next.X, radius, 18 - radius); next.Z = MathHelper.Clamp(next.Z, radius, 10 - radius);
            }
            Owner.SetPosition(next);
        }
        public override void OnCollisionWithEnemy(SimObject other) {
            if (ReferenceEquals(other, Owner) || other.Master == Owner.Master || lockedTarget == other.Id) return;
            var receivers = other.GetComponents<IDamageable>(); if (receivers.Length == 0) return;
            var hit = new Hit(damage, Owner.Id, Owner.Elements.Total); other.SetStatus(ObjectStatus.Damaged); foreach (var receiver in receivers) receiver.OnDamaged(hit); lockedTarget = other.Id;
            if (!ConsumeBounce()) { Owner.Destroy(); return; }
            var incoming = Direction ?? throw new InvalidOperationException("NullPointerException: RollingRock direction is not set");
            Vector3? normal = Owner.Position - other.Position;
            if (normal.Value.X == 0 && normal.Value.Z == 0) {
                var ownCircle = Owner.Colliders.OfType<CircleCollider>().FirstOrDefault(); var otherCircle = other.Colliders.OfType<CircleCollider>().FirstOrDefault();
                if (ownCircle != null && otherCircle != null) normal = ownCircle.Displacement(otherCircle);
            }
            var n = normal.HasValue && (normal.Value.X != 0 || normal.Value.Z != 0) ? Normalize(GroundVector.Of(normal.Value)) : Normalize(-incoming);
            var reflected = Normalize(incoming - n * (2 * Vector3.Dot(incoming, n))); if (reflected == Vector3.Zero) reflected = -incoming;
            Direction = Normalize(GroundVector.Of(reflected)); Owner.SetStatus(ObjectStatus.Attack); Owner.SetPosition(Owner.Position + Direction.Value * Separation);
        }
    }
}
