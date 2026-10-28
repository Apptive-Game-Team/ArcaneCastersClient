using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public readonly struct EffectApplication {
        public readonly VisualEffect Effect;
        public readonly SimObject Source;
        public EffectApplication(VisualEffect effect, SimObject source = null) { Effect = effect; Source = source; }
        public bool IsFriendlyTo(SimObject target) => IsFriendly(Source, target);
        public static bool IsFriendly(SimObject source, SimObject target) => source == null || source.Master == Master.None || source.Master == target.Master;
    }
    public sealed class LightningSummonEffectReceiver : CommonEffectReceiver {
        public LightningSummonEffectReceiver(SimObject owner) : base(owner) { }
        public static void Overcharge(SimObject target) { target.GetComponent<LightningSummonEffectReceiver>()?.Receive(VisualEffect.Shock); }
        public override void Receive(EffectApplication application) {
            if (application.Effect == VisualEffect.Shock && !application.IsFriendlyTo(Owner)) base.Receive(VisualEffect.Shock);
            else Receive(application.Effect);
        }
        public override void Receive(VisualEffect effect) {
            if (effect == VisualEffect.Shock) OverchargeStatusEffect.Apply(Owner, 3); else base.Receive(effect);
        }
    }
    public sealed class OverchargeStatusEffect : BaseStatusEffect {
        private bool speedApplied;
        private Fix64 projectileTimer;
        public bool IsPermanent { get; }
        public OverchargeStatusEffect(SimObject owner, Fix64 duration, bool permanent = false) : base(owner, permanent ? Fix64.MaxValue : duration, StatusEffectKey.OverchargeReceive, VisualEffect.Overcharge) { IsPermanent = permanent; }
        public static void Apply(SimObject owner, Fix64 duration) {
            var effect = owner.GetComponent<OverchargeStatusEffect>();
            if (effect != null) {
                if (owner.CancelComponentRemoval(effect)) {
                    effect.Remaining = MathHelper.Max(effect.Remaining, Fix64.Zero); owner.AddEffect(VisualEffect.Overcharge); effect.Start();
                }
                if (!effect.IsPermanent) effect.Extend(duration); return;
            }
            effect = owner.GetPendingComponents<OverchargeStatusEffect>().FirstOrDefault();
            if (effect != null) { if (!effect.IsPermanent) effect.Extend(duration); } else owner.AddComponent(new OverchargeStatusEffect(owner, duration));
        }
        public override void Start() {
            var mob = Owner.GetComponent<ISpeedHolder>(); if (mob == null || speedApplied) return;
            mob.Speed.SetMultiplier(checked((int)SourceId), (Fix64)1.5m); speedApplied = true;
        }
        private void RemoveSpeed() {
            if (!speedApplied) return;
            var mob = Owner.GetComponent<ISpeedHolder>(); if (mob == null) return;
            mob.Speed.RemoveMultiplier(checked((int)SourceId)); speedApplied = false;
        }
        public override void Expire() { RemoveSpeed(); base.Expire(); }
        public override void OnDestroy() { RemoveSpeed(); base.OnDestroy(); }
        public override void Update() {
            Fix64 activeDelta = MathHelper.Min(Context.DeltaTime, MathHelper.Max(Remaining, Fix64.Zero));
            if (!IsPermanent) base.Update(); projectileTimer += activeDelta;
            while (projectileTimer >= Fix64.One) { projectileTimer -= Fix64.One; EmitProjectile(); }
        }
        private void EmitProjectile() {
            SimObject closest = null; Fix64 distance = Fix64.MaxValue;
            foreach (var target in Context.Objects) {
                if (!target.IsActive || target.Master == Owner.Master || !target.HasComponent<IDamageable>()) continue;
                if (!CombatRange.Contains(Owner.Position, Owner.FirstCircleRadius(false) ?? Fix64.Zero, target.Position, target.FirstCircleRadius(false) ?? Fix64.Zero, 2)) continue;
                Fix64 next = Vector3.Distance(Owner.Position, target.Position);
                if (closest == null || next < distance) { closest = target; distance = next; }
            }
            if (closest == null) return;
            Context.AddEvent(new BattleEvent("ElectricShot", Owner.Id, closest.Id, (Fix64)0.2m));
            int damage = (int)(Context.GameParameters["electric_shot.damage"].RawValue / (1L << 32));
            foreach (var target in closest.GetComponents<IDamageable>()) target.OnDamagedWithDelay(new Hit(damage, 0, Element.Lightning), (Fix64)0.2m);
        }
    }
}
