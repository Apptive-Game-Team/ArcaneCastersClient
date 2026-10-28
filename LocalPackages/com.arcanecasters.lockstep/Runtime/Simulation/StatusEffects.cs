using System;
using System.Linq;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public enum StatusEffectKey {
        Default, BurnReceive, WetReceive, ShockReceive, ShockOverloadReceive, SnaredReceive, KnockbackReceive,
        DotHealNatureWithWaterField, DotHealNatureField, DotDealBurn, DotSandStorm, FrenzyReceive, InspiredReceive,
        PanicReceive, BubbleReceive, OverchargeReceive, TrapStunReceive
    }
    public enum EffectApplyPolicy { Ignore, RefreshDuration, ExtendDuration }
    public interface IIntervalAttacker { Stat AttackInterval { get; } }
    public interface IStunnableBehavior : IIntervalAttacker { void SetStun(Fix64 duration); void SetIdle(); }
    public interface IFrenzyImmune { }

    public abstract class BaseStatusEffect : SimComponent, IElementReaction {
        private Fix64 initialDuration;
        private readonly VisualEffect visual;
        public Fix64 Remaining { get; protected set; }
        public StatusEffectKey Key { get; }
        protected BaseStatusEffect(SimObject owner, Fix64 duration, StatusEffectKey key, VisualEffect visual = VisualEffect.None) : base(owner) {
            initialDuration = Remaining = duration; Key = key; this.visual = visual; owner.AddEffect(visual);
        }
        public virtual void OnAttacked(Element element) { }
        public void ResetDuration() { Remaining = initialDuration; }
        public void Refresh(Fix64 duration) { initialDuration = Remaining = duration; Start(); }
        public void Extend(Fix64 duration) { Remaining += duration; }
        public virtual void Expire() { Owner.RemoveEffect(visual); Owner.RemoveComponent(this); }
        public override void Update() { Remaining -= Context.DeltaTime; if (Remaining <= Fix64.Zero) Expire(); }
        public override void OnDestroy() { Owner.RemoveEffect(visual); }
    }

    public sealed class BurnStatusEffect : BaseStatusEffect {
        public BurnStatusEffect(SimObject owner, Fix64 duration, StatusEffectKey key) : base(owner, duration, key, VisualEffect.Burn) { }
        public override void Start() { var wet = Owner.GetComponent<WetStatusEffect>(); if (wet != null) { wet.Expire(); Expire(); } }
    }
    public sealed class WetStatusEffect : BaseStatusEffect {
        public WetStatusEffect(SimObject owner, Fix64 duration, StatusEffectKey key) : base(owner, duration, key, VisualEffect.Wet) { }
        public override void Start() {
            Owner.Elements.AddBonus(Element.Water, SourceId);
            var burn = Owner.GetComponent<BurnStatusEffect>(); if (burn != null) { burn.Expire(); Expire(); }
        }
        public override void Expire() { Owner.Elements.RemoveBonus(Element.Water, SourceId); base.Expire(); }
    }
    public sealed class DotStatusEffect : BaseStatusEffect {
        private Fix64 elapsed;
        private readonly Fix64 interval;
        private readonly int unit;
        private readonly Element element;
        private readonly bool empty;
        public DotStatusEffect(SimObject owner, Fix64 duration, int totalAmount, Element element, StatusEffectKey key) : base(owner, duration, key) {
            empty = totalAmount == 0;
            interval = empty ? Fix64.Zero : duration / (Fix64)Math.Abs((long)totalAmount);
            unit = totalAmount >= 0 ? 1 : -1; this.element = element;
        }
        public override void Update() {
            base.Update(); elapsed += Context.DeltaTime;
            // 원본의 총량 0은 무한 간격이므로 피해가 발생하지 않는다.
            if (empty) return;
            int ticks = interval == Fix64.Zero ? int.MaxValue : (int)Fix64.Floor(elapsed / interval);
            if (ticks <= 0) return;
            elapsed -= (Fix64)ticks * interval;
            Owner.GetComponent<MobComponent>()?.ApplyDamage(new Hit(unchecked(unit * ticks), 0, element));
        }
    }
    public sealed class SlowStatusEffect : BaseStatusEffect {
        private readonly Fix64 amount;
        public SlowStatusEffect(SimObject owner, Fix64 duration, Fix64 amount, StatusEffectKey key) : base(owner, duration, key) { this.amount = amount; }
        public override void Start() { var mob = Owner.GetComponent<ISpeedHolder>(); if (mob != null) mob.Speed.ModifierPercent = -amount; }
        public override void Expire() { var mob = Owner.GetComponent<ISpeedHolder>(); if (mob != null) mob.Speed.ModifierPercent = Fix64.Zero; base.Expire(); }
    }
    public class StunStatusEffect : BaseStatusEffect {
        private readonly Fix64 duration, gravityMultiplier;
        private ZPhysics physics;
        private Fix64 originalGravity;
        private bool gravityApplied;
        public StunStatusEffect(SimObject owner, Fix64 duration, StatusEffectKey key, VisualEffect visual, Fix64? gravityMultiplier = null) : base(owner, duration, key, visual) {
            this.duration = duration; this.gravityMultiplier = gravityMultiplier ?? Fix64.One;
        }
        public override void Start() {
            physics = Owner.GetComponent<ZPhysics>();
            if (physics != null) {
                physics.LockHover(SourceId);
                if (!gravityApplied && gravityMultiplier != Fix64.One) {
                    originalGravity = physics.Gravity; physics.Gravity = originalGravity * gravityMultiplier; gravityApplied = true;
                }
            }
            Owner.GetComponent<IStunnableBehavior>()?.SetStun(duration);
        }
        public override void Expire() {
            if (physics != null) {
                if (gravityApplied) { physics.Gravity = originalGravity; gravityApplied = false; }
                physics.UnlockHover(SourceId);
            }
            Owner.GetComponent<IStunnableBehavior>()?.SetIdle(); base.Expire();
        }
    }
    public sealed class ShockStatusEffect : StunStatusEffect {
        public ShockStatusEffect(SimObject owner, Fix64 duration, StatusEffectKey key) : base(owner, duration, key, VisualEffect.Shock) { }
    }
    public sealed class SnaredStatusEffect : BaseStatusEffect {
        private readonly int removalDamage;
        private readonly Fix64 slow;
        public SnaredStatusEffect(SimObject owner, Fix64 duration, int removalDamage, Fix64 slow, StatusEffectKey key) : base(owner, duration, key, VisualEffect.Snared) { this.removalDamage = removalDamage; this.slow = slow; }
        public override void Start() {
            var mob = Owner.GetComponent<ISpeedHolder>(); if (mob != null) mob.Speed.ModifierPercent = -slow;
            Owner.GetComponent<ZPhysics>()?.LockHover(SourceId);
            var behavior = Owner.GetComponent<IStunnableBehavior>(); if (behavior != null) behavior.AttackInterval.ModifierPercent = slow;
        }
        public override void OnAttacked(Element element) {
            if (element != Element.Fire) return;
            Owner.GetComponent<MobComponent>()?.ApplyDamage(new Hit(removalDamage, 0, Element.Fire)); Expire();
        }
        public override void Expire() {
            var mob = Owner.GetComponent<ISpeedHolder>(); if (mob != null) mob.Speed.ModifierPercent = Fix64.Zero;
            Owner.GetComponent<ZPhysics>()?.UnlockHover(SourceId);
            var behavior = Owner.GetComponent<IStunnableBehavior>(); if (behavior != null) behavior.AttackInterval.ModifierPercent = Fix64.Zero;
            base.Expire();
        }
    }
    public sealed class FrenzyStatusEffect : BaseStatusEffect, IFrenzyState {
        private Master? originalMaster;
        public bool IsFrenzied => originalMaster.HasValue && Owner.Master == Master.None;
        public FrenzyStatusEffect(SimObject owner, Fix64 duration, StatusEffectKey key) : base(owner, duration, key, VisualEffect.Frenzy) { }
        private void SetAttackModifier(Fix64 modifier) { foreach (var attacker in Owner.GetComponents<IIntervalAttacker>()) attacker.AttackInterval.ModifierPercent = modifier; }
        public override void Start() {
            if (!originalMaster.HasValue) {
                if (Owner.Master == Master.None || !Owner.HasComponent<MobComponent>() || Owner.HasComponent<IPlayerHealth>() || Owner.HasComponent<IFrenzyImmune>()) { Owner.RemoveComponent(this); return; }
                originalMaster = Owner.Master; Owner.SetMaster(Master.None);
            }
            SetAttackModifier(-(Fix64)0.5m / (Fix64)1.5m);
        }
        public override void Expire() {
            if (originalMaster.HasValue && Owner.Master == Master.None) Owner.SetMaster(originalMaster.Value);
            SetAttackModifier(Fix64.Zero); base.Expire();
        }
    }

    public partial class CommonEffectReceiver : SimComponent {
        public CommonEffectReceiver(SimObject owner) : base(owner) { }
        public BaseStatusEffect GetEffectByKey(StatusEffectKey key) => Owner.Components.Concat(Owner.PendingComponents).OfType<BaseStatusEffect>().FirstOrDefault(effect => effect.Key == key);
        public void ApplyEffect(StatusEffectKey key, Func<BaseStatusEffect> factory, EffectApplyPolicy policy, Fix64 duration) {
            var existing = GetEffectByKey(key);
            if (existing != null) {
                if (policy == EffectApplyPolicy.RefreshDuration) existing.Refresh(duration);
                else if (policy == EffectApplyPolicy.ExtendDuration) existing.Extend(duration);
                return;
            }
            Owner.AddComponent(factory());
        }
    }
    public sealed class InspiredStatusEffect : BaseStatusEffect {
        public InspiredStatusEffect(SimObject owner, Fix64 duration, StatusEffectKey key) : base(owner, duration, key, VisualEffect.Inspired) { }
        private void SetModifier(Fix64 value) { foreach (var attacker in Owner.GetComponents<IIntervalAttacker>()) attacker.AttackInterval.ModifierPercent = value; }
        public override void Start() { SetModifier(-(Fix64)0.5m / (Fix64)1.5m); }
        public override void Expire() { SetModifier(Fix64.Zero); base.Expire(); }
    }
    public sealed class BubbleStatusEffect : BaseStatusEffect, IDamageInterceptor {
        public BubbleStatusEffect(SimObject owner, Fix64 duration, StatusEffectKey key) : base(owner, duration, key, VisualEffect.Bubble) { }
        public bool BeforeDamage(Hit hit) { if (hit.Damage <= 0) return false; Expire(); return true; }
    }
}
