using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class CardSelectedStatusEffect : BaseStatusEffect {
        private int selectedCount = 1;
        private FlowSubscription subscription;
        public bool IsExpired { get; private set; }
        public CardSelectedStatusEffect(SimObject owner) : base(owner, Fix64.MaxValue, StatusEffectKey.Default, VisualEffect.CardSelected) {
            subscription = owner.SubscribeStatusChange(change => { if (change.Current == ObjectStatus.Attack || change.Current == ObjectStatus.Hindered) Expire(); });
        }
        public void Select() { selectedCount++; }
        public void Unselect() { selectedCount--; if (selectedCount <= 0) Expire(); }
        public void Cancel() => Expire();
        public override void Update() { }
        private void StopListening() { subscription?.Cancel(); subscription = null; }
        public override void Expire() { if (IsExpired) return; IsExpired = true; selectedCount = 0; StopListening(); base.Expire(); }
        public override void OnDestroy() { StopListening(); base.OnDestroy(); }
    }
}
