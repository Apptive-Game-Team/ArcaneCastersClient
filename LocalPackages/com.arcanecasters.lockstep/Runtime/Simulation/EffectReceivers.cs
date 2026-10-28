using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public interface IEffectReceiver {
        void Receive(EffectApplication application);
        void Receive(VisualEffect effect);
        void Receive(VisualEffect effect, Fix64 duration);
        void ReceiveDirectional(VisualEffect effect, Vector3 direction, Fix64 proximity);
    }
    public partial class CommonEffectReceiver : IEffectReceiver, ICollidable {
        public virtual void Receive(EffectApplication application) { Receive(application.Effect); }
        public void OnCollision(SimObject other) { }
        public void OnCollisionWithEnemy(SimObject other) { }
        protected Fix64 Parameter(string key) => Context.GameParameters["game." + key];
        protected int IntParameter(string key) => (int)(Parameter(key).RawValue / (1L << 32));
        protected void Refresh(StatusEffectKey key, System.Func<BaseStatusEffect> factory, Fix64 duration) => ApplyEffect(key, factory, EffectApplyPolicy.RefreshDuration, duration);
        public virtual void Receive(VisualEffect effect) {
            if (effect == VisualEffect.Burn && Owner.Elements.Has(Element.Fire) || effect == VisualEffect.Wet && Owner.Elements.Has(Element.Water)
                || effect == VisualEffect.Shock && Owner.Elements.Has(Element.Lightning) || effect == VisualEffect.Snared && Owner.Elements.Has(Element.Nature)) return;
            Fix64 duration;
            switch (effect) {
                case VisualEffect.Wet:
                    duration = Parameter("wet_duration");
                    Refresh(StatusEffectKey.WetReceive, () => new WetStatusEffect(Owner, duration, StatusEffectKey.WetReceive), duration);
                    if (Owner.Elements.Has(Element.Nature)) Refresh(StatusEffectKey.DotHealNatureWithWaterField,
                        () => new DotStatusEffect(Owner, duration, -IntParameter("wet_nature_heal"), Element.None, StatusEffectKey.DotHealNatureWithWaterField), duration);
                    break;
                case VisualEffect.Burn:
                    duration = Parameter("burn_duration");
                    Refresh(StatusEffectKey.BurnReceive, () => new BurnStatusEffect(Owner, duration, StatusEffectKey.BurnReceive), duration);
                    Refresh(StatusEffectKey.DotDealBurn, () => new DotStatusEffect(Owner, duration, IntParameter("burn_total_damage"), Element.Fire, StatusEffectKey.DotDealBurn), duration);
                    break;
                case VisualEffect.Shock:
                    duration = Parameter("shock_stun_duration");
                    Refresh(StatusEffectKey.ShockReceive, () => new ShockStatusEffect(Owner, duration, StatusEffectKey.ShockReceive), Parameter("shock_refresh_duration"));
                    break;
                case VisualEffect.Snared:
                    duration = Parameter("snare_duration");
                    Refresh(StatusEffectKey.SnaredReceive, () => new SnaredStatusEffect(Owner, duration, IntParameter("snare_fire_damage"), Parameter("snare_slow_percent"), StatusEffectKey.SnaredReceive), duration);
                    break;
                case VisualEffect.LeafFieldHeal:
                    duration = Parameter("leaf_field_heal_duration");
                    Refresh(StatusEffectKey.DotHealNatureField, () => new DotStatusEffect(Owner, duration, -IntParameter("leaf_field_heal_amount"), Element.None, StatusEffectKey.DotHealNatureField), duration);
                    break;
                case VisualEffect.Sandstorm:
                    duration = Parameter("sandstorm_effect_duration");
                    Refresh(StatusEffectKey.DotSandStorm, () => new DotStatusEffect(Owner, duration, IntParameter("sandstorm_effect_damage"), Element.None, StatusEffectKey.DotSandStorm), duration);
                    break;
                case VisualEffect.Frenzy:
                    Refresh(StatusEffectKey.FrenzyReceive, () => new FrenzyStatusEffect(Owner, 10, StatusEffectKey.FrenzyReceive), 10); break;
                case VisualEffect.Bubble:
                    Refresh(StatusEffectKey.BubbleReceive, () => new BubbleStatusEffect(Owner, 8, StatusEffectKey.BubbleReceive), 8); break;
                case VisualEffect.Inspired:
                    Refresh(StatusEffectKey.InspiredReceive, () => new InspiredStatusEffect(Owner, 10, StatusEffectKey.InspiredReceive), 10); break;
            }
        }
        public virtual void Receive(VisualEffect effect, Fix64 duration) {
            if (effect == VisualEffect.Frenzy) Refresh(StatusEffectKey.FrenzyReceive, () => new FrenzyStatusEffect(Owner, duration, StatusEffectKey.FrenzyReceive), duration);
            else if (effect == VisualEffect.Inspired) Refresh(StatusEffectKey.InspiredReceive, () => new InspiredStatusEffect(Owner, duration, StatusEffectKey.InspiredReceive), duration);
            else Receive(effect);
        }
        public void ReceiveDirectional(VisualEffect effect, Vector3 direction, Fix64 proximity) {
            ApplyEffect(StatusEffectKey.KnockbackReceive, () => new KnockbackStatusEffect(Owner, direction, proximity, StatusEffectKey.KnockbackReceive), EffectApplyPolicy.Ignore, 3);
        }
    }
    public sealed class BuildingEffectReceiver : CommonEffectReceiver {
        public BuildingEffectReceiver(SimObject owner) : base(owner) { }
        public override void Receive(VisualEffect effect) {
            Fix64 duration;
            switch (effect) {
                case VisualEffect.Wet:
                    duration = Parameter("wet_duration"); Refresh(StatusEffectKey.WetReceive, () => new WetStatusEffect(Owner, duration, StatusEffectKey.WetReceive), duration); break;
                case VisualEffect.Burn:
                    duration = Parameter("burn_duration"); Refresh(StatusEffectKey.BurnReceive, () => new BurnStatusEffect(Owner, duration, StatusEffectKey.BurnReceive), duration); break;
                case VisualEffect.Snared:
                    duration = Parameter("snare_duration"); Refresh(StatusEffectKey.SnaredReceive, () => new DotStatusEffect(Owner, duration, -IntParameter("building_snare_heal"), Element.None, StatusEffectKey.SnaredReceive), duration); break;
                case VisualEffect.Inspired:
                    Refresh(StatusEffectKey.InspiredReceive, () => new InspiredStatusEffect(Owner, 10, StatusEffectKey.InspiredReceive), 10); break;
            }
        }
        public override void Receive(VisualEffect effect, Fix64 duration) {
            if (effect == VisualEffect.Inspired) base.Receive(effect, duration); else Receive(effect);
        }
    }
}
