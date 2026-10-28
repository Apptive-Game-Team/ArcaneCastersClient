using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class RallyingTotem : SimComponent, IGaugeComponent {
        private readonly Fix64 speed, duration, detectionRange, buffDuration;
        private Fix64 elapsed;
        private bool landed;
        public Fix64 ZoneRadius { get; }
        public RallyingTotem(SimObject owner, Fix64 speed, Fix64 duration, Fix64 radius, Fix64 detectionRange, Fix64 buffDuration) : base(owner) {
            this.speed = speed; this.duration = duration; ZoneRadius = radius; this.detectionRange = detectionRange; this.buffDuration = buffDuration;
        }
        public Gauge GetGauge() => new Gauge(MathHelper.Max(0, duration - elapsed), duration, GaugeCategory.Ttl);
        public override void Update() {
            if (!landed) {
                Owner.SetPosition(Owner.Position + new Vector3(0, -speed * Context.DeltaTime, 0));
                if (Owner.Position.Y < 0) { Owner.SetPosition(GroundVector.Of(Owner.Position)); landed = true; }
                return;
            }
            elapsed += Context.DeltaTime; if (elapsed >= duration) { Owner.Destroy(); return; }
            Owner.ApplyUpdate();
            foreach (var target in Context.Objects) {
                if (ReferenceEquals(target, Owner) || Owner.Master == Master.None || target.Master != Owner.Master || !target.HasComponent<IStunnableBehavior>()) continue;
                if (Vector3.Distance(GroundVector.Of(Owner.Position), GroundVector.Of(target.Position)) > detectionRange) continue;
                if (!target.HasComponent<RallyMoveDirective>() && target.GetPendingComponents<RallyMoveDirective>().Length == 0) target.AddComponent(new RallyMoveDirective(target, Owner, ZoneRadius));
            }
            foreach (var target in SimulationQueries.OverlapSphereAll(Context, Owner.Position, ZoneRadius)) {
                if (ReferenceEquals(target, Owner) || Owner.Master == Master.None || target.Master != Owner.Master) continue;
                target.GetComponent<IEffectReceiver>()?.Receive(VisualEffect.Inspired, buffDuration);
            }
        }
        public static void RegisterPrefab(SimulationContext context) {
            context.RegisterPrefab("RallyingTotem", owner => {
                owner.AddCollider(new CircleCollider(owner, context.GameParameters["rallying_totem.radius"], true)); owner.Elements.AddNative(Element.Fire);
                owner.AddComponent(new RallyingTotem(owner, context.GameParameters["rallying_totem.speed"], context.GameParameters["rallying_totem.duration"], context.GameParameters["rallying_totem.effect_radius"], context.GameParameters["rallying_totem.detection_range"], context.GameParameters["rallying_totem.buff_duration"]));
            });
        }
    }
}
