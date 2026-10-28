using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class MovementSpeedTracker : SimComponent {
        private readonly Fix64 maxSpeed;
        private Vector3 samplePosition;
        private Fix64 elapsed;
        private bool tracking;
        public int Tier { get; private set; } = 1;
        public MovementSpeedTracker(SimObject owner, Fix64 maxSpeed) : base(owner) { this.maxSpeed = maxSpeed; }
        private static bool ChargeEffect(VisualEffect effect) => effect == VisualEffect.StormStagCharge2 || effect == VisualEffect.StormStagCharge3 || effect == VisualEffect.StormStagCharge4;
        public void Begin() { Reset(); tracking = true; }
        public void Reset() {
            tracking = false; elapsed = Fix64.Zero; samplePosition = Owner.Position; Tier = 1;
            Owner.RemoveEffects(ChargeEffect);
        }
        public static int TierFor(Fix64 currentSpeed, Fix64 maxSpeed) {
            if (maxSpeed <= Fix64.Zero) return 1;
            Fix64 percentage = currentSpeed / maxSpeed * 100;
            return percentage >= 100 ? 4 : percentage >= 60 ? 3 : percentage >= 30 ? 2 : 1;
        }
        public override void Start() { samplePosition = Owner.Position; }
        public override void Update() {
            if (!tracking) return;
            elapsed += Context.DeltaTime;
            if (elapsed + (Fix64)0.0001m < (Fix64)0.15m) return;
            var displacement = Owner.Position - samplePosition; displacement.Y = Fix64.Zero;
            int next = TierFor(displacement.Length() / elapsed, maxSpeed);
            if (Tier != next) {
                Tier = next; Owner.RemoveEffects(ChargeEffect);
                if (next == 2) Owner.AddEffect(VisualEffect.StormStagCharge2);
                else if (next == 3) Owner.AddEffect(VisualEffect.StormStagCharge3);
                else if (next == 4) Owner.AddEffect(VisualEffect.StormStagCharge4);
            }
            samplePosition = Owner.Position; elapsed = Fix64.Zero;
        }
        public override void OnDestroy() { Owner.RemoveEffects(ChargeEffect); }
    }
}
