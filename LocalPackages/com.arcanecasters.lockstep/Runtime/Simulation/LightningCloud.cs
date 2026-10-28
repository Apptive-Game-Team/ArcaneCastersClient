using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class LightningCloud : SimComponent {
        private readonly Fix64 interval;
        private readonly int count, damage;
        private Fix64 timer, visualTimer;
        private int strikes;
        public Vector3 BoxSize { get; }
        public Vector3 BoxCenter => new Vector3(Owner.Position.X, BoxSize.Y / 2, Owner.Position.Z);
        public LightningCloud(SimObject owner, Fix64 interval, int count, int damage, Fix64 radius) : base(owner) { this.interval = interval; this.count = count; this.damage = damage; BoxSize = new Vector3(radius * 2, 10, radius * 2); }
        private void Strike() {
            if (strikes >= count) return;
            Owner.SetStatus(ObjectStatus.Attack); var hit = new Hit(damage, Owner.Id, Owner.Elements.Total);
            foreach (var target in SimulationQueries.OverlapBoxAll(Context, BoxCenter, BoxSize)) {
                if (ReferenceEquals(target, Owner) || target.IsDestroyed) continue;
                if (Owner.Master != Master.None && target.Master == Owner.Master) { LightningSummonEffectReceiver.Overcharge(target); continue; }
                var receivers = target.GetComponents<IDamageable>(); if (receivers.Length == 0) continue;
                target.SetStatus(ObjectStatus.Damaged); foreach (var receiver in receivers) receiver.OnDamaged(hit);
            }
            Context.Spawn(Master.None, "ElectricField", GroundVector.Of(Owner.Position)); strikes++;
        }
        public override void Start() { if (count <= 0) Owner.Destroy(); else Strike(); }
        public override void Update() {
            if (strikes >= count) { visualTimer += Context.DeltaTime; if (visualTimer >= (Fix64)0.3m) Owner.Destroy(); return; }
            timer += Context.DeltaTime;
            // Q32.32 변환의 1 LSB 잔량 때문에 정확한 주기의 공격이 한 tick 밀리지 않게 한다.
            while (strikes < count && timer + Fix64.FromRaw(1) >= interval) { timer -= interval; Strike(); }
        }
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("LightningCloud", owner => {
                owner.Elements.AddNative(Element.Lightning);
                owner.AddComponent(new LightningCloud(owner, context.GameParameters["lightning_cloud.attack_interval"], (int)(context.GameParameters["lightning_cloud.quantity"].RawValue / (1L << 32)), (int)(context.GameParameters["lightning_drop.damage"].RawValue / (1L << 32)), context.GameParameters["lightning_drop.radius"]));
            });
        }
    }
}
