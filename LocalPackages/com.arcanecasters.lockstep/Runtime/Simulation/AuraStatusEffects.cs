using System;
using System.Linq;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class AttackAuraStatusEffect : BaseStatusEffect {
        public Element Element { get; }
        internal static VisualEffect Visual(Element element, bool attack) => element == Element.None ? VisualEffect.None : (VisualEffect)Enum.Parse(typeof(VisualEffect), element + (attack ? "AttackAura" : "IdleAura"));
        public AttackAuraStatusEffect(SimObject owner, Element element, Fix64 duration) : base(owner, duration, StatusEffectKey.Default, Visual(element, true)) { Element = element; }
    }
    public sealed class IdleAuraStatusEffect : BaseStatusEffect {
        private FlowSubscription subscription;
        public Element Element { get; }
        public IdleAuraStatusEffect(SimObject owner, Element element) : base(owner, Fix64.MaxValue, StatusEffectKey.Default, AttackAuraStatusEffect.Visual(element, false)) {
            Element = element; subscription = owner.SubscribeStatusChange(change => {
                if (change.Current == ObjectStatus.Attack) {
                    if (!owner.GetComponents<AttackAuraStatusEffect>().Concat(owner.GetPendingComponents<AttackAuraStatusEffect>()).Any(effect => effect.Element == element)) owner.AddComponent(new AttackAuraStatusEffect(owner, element, (Fix64)0.3m));
                    Expire();
                } else if (change.Current == ObjectStatus.Hindered) Expire();
            });
        }
        public override void Update() { }
        public void Cancel() => Expire();
        private void StopListening() { subscription?.Cancel(); subscription = null; }
        public override void Expire() { StopListening(); base.Expire(); }
        public override void OnDestroy() { StopListening(); base.OnDestroy(); }
    }
}
