using System.Linq;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class ShockTrapDetector : SimComponent {
        private readonly Fix64 radius, delay, duration;
        private bool counting, triggered;
        private Fix64 counter;
        public ShockTrapDetector(SimObject owner, Fix64 radius, Fix64 delay, Fix64 duration) : base(owner) { this.radius = radius; this.delay = delay; this.duration = duration; }
        private SimObject[] Detect() => SimulationQueries.OverlapSphereAll(Context, Owner.Position, radius).Where(target => TargetRelation.CanAttack(Owner, target) && target.HasComponent<IDamageable>()).ToArray();
        private void Stun(SimObject target) {
            const StatusEffectKey key = StatusEffectKey.TrapStunReceive;
            var receiver = target.GetComponent<CommonEffectReceiver>();
            if (receiver != null) { receiver.ApplyEffect(key, () => new StunStatusEffect(target, duration, key, VisualEffect.Shock), EffectApplyPolicy.RefreshDuration, duration); return; }
            var pending = target.GetPendingComponents<StunStatusEffect>().FirstOrDefault(effect => effect.Key == key);
            if (pending != null) pending.Refresh(duration); else target.AddComponent(new StunStatusEffect(target, duration, key, VisualEffect.Shock));
        }
        public override void Update() {
            if (triggered) return;
            if (!counting) {
                if (Detect().Length != 0) { counting = true; counter = Fix64.Zero; Owner.AddEffect(VisualEffect.ShockTrapArming); }
                return;
            }
            counter += Context.DeltaTime; if (counter < delay) return;
            foreach (var target in Detect()) Stun(target);
            Owner.RemoveEffect(VisualEffect.ShockTrapArming); Owner.SetStatus(ObjectStatus.Attack); Context.AddEvent(new BattleEvent("shock", Owner.Id, 0)); triggered = true; Owner.Destroy();
        }
        public override void OnDestroy() { Owner.RemoveEffect(VisualEffect.ShockTrapArming); }
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("ShockTrap", owner => {
                Fix64 Value(string key) => context.GameParameters["shock_trap." + key];
                int Integer(string key) => (int)(Value(key).RawValue / (1L << 32));
                owner.AddComponent(new RigidBody(owner, Integer("mass"))); owner.AddCollider(new CircleCollider(owner, Value("radius")));
                owner.AddComponent(new DummyMob(owner, Integer("hp"))); owner.AddComponent(new ShockTrapDetector(owner, Value("effect_radius"), Value("trigger_delay"), Value("stun_duration")));
                owner.AddComponent(new BuildingEffectReceiver(owner)); owner.Elements.AddNative(Element.Lightning);
            });
        }
    }
}
