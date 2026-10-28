using System;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    // 행동이 요청한 한 틱의 속도를 모은다. PhysicsWorld에 전달한 뒤 비운다.
    public sealed class RigidBody : SimComponent {
        public int Mass { get; }
        public Vector3 Velocity { get; private set; }
        public Fix64 InverseMass => Mass == -1 ? Fix64.Zero : Fix64.One / (Fix64)Mass;
        public RigidBody(SimObject owner, int mass) : base(owner) { Mass = mass; }
        public void AddVelocity(Vector3 velocity) { Velocity += velocity; }
        public Vector3 ConsumeVelocity() { var value = Velocity; Velocity = Vector3.Zero; return value; }
        public void ApplyVelocity() { Owner.SetPosition(Owner.Position + ConsumeVelocity() * Context.DeltaTime); }
    }
    public static class ForcedMovement {
        public static Fix64 MassMultiplier(int mass) => mass < 0 ? Fix64.Zero : Fix64.One / Fix64.Sqrt((Fix64)Math.Max(1, mass));
        public static Fix64 MassMultiplier(SimObject owner) { var body = owner.GetComponent<RigidBody>(); return body == null ? Fix64.One : MassMultiplier(body.Mass); }
    }
    public sealed class KnockbackStatusEffect : BaseStatusEffect {
        private readonly Vector3 direction;
        private readonly Fix64 proximity, multiplier;
        private Fix64 moved;
        public KnockbackStatusEffect(SimObject owner, Vector3 direction, Fix64 proximity, StatusEffectKey key) : base(owner, (Fix64)0.5m, key, VisualEffect.Knockback) {
            this.direction = direction; this.proximity = MathHelper.Max(proximity, (Fix64)0.2m); multiplier = ForcedMovement.MassMultiplier(owner);
        }
        public override void Start() { Owner.GetComponent<ZPhysics>()?.AddImpulseZ(10 * proximity * multiplier); Owner.GetComponent<IStunnableBehavior>()?.SetStun((Fix64)0.5m); }
        public override void Expire() { Owner.GetComponent<ZPhysics>()?.AddImpulseZ(-10 * proximity * multiplier); base.Expire(); }
        public override void Update() {
            Fix64 step = MathHelper.Min(4 * Context.DeltaTime, (Fix64)2 - moved);
            // 0.1초의 고정 소수점 절삭 잔량 때문에 만료가 한 틱 늦어지지 않도록 마지막 이동에 합친다.
            if ((Fix64)2 - moved - step < (Fix64)0.000001m) step = (Fix64)2 - moved;
            if (step <= Fix64.Zero) { Expire(); return; }
            Owner.GetComponent<RigidBody>()?.AddVelocity(direction * (step / Context.DeltaTime * proximity * multiplier)); moved += step;
        }
    }
}
