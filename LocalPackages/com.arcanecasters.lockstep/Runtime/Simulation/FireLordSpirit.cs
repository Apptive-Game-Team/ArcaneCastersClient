using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class KeepDistanceMob : MobComponent, IGaugeComponent {
        private readonly IDetector detector;
        private readonly Fix64 preferredRange;
        private RigidBody body;
        private SimObject target;
        private Fix64 detectTimer;
        public KeepDistanceMob(SimObject owner, int hp, Fix64 speed, int mask, Fix64 preferredRange) : base(owner, hp, speed) {
            detector = new ClosestEnemyDetector(Context, mask); this.preferredRange = preferredRange;
        }
        public override void Start() { body = Owner.GetComponent<RigidBody>(); target = detector.Detect(Owner); }
        public override void Update() {
            base.Update(); detectTimer += Context.DeltaTime;
            if (detectTimer > Fix64.One) { target = detector.Detect(Owner); detectTimer = Fix64.Zero; }
            if (target == null || target.IsDestroyed || !TargetRelation.CanAttack(Owner, target)) return;
            var distance = CombatRange.HorizontalEdgeDistance(Owner.Position, Owner.FirstCircleRadius(false) ?? Fix64.Zero, target.Position, target.FirstCircleRadius(false) ?? Fix64.Zero);
            if (distance <= preferredRange + (Fix64)0.25m || body == null) return;
            var direction = VineMagics.Normalize(GroundVector.Of(target.Position - Owner.Position));
            if (direction != Vector3.Zero) body.AddVelocity(direction * Speed.Total);
        }
        public override void OnDeath() { Owner.Destroy(); }
        public Gauge GetGauge() => new Gauge(Hp, MaxHp, GaugeCategory.Hp);
    }
    public static class FireLordSpiritPrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("FireLordSpirit", owner => Initialize(owner, true));
            context.RegisterPrefab("FireChildSpirit", owner => Initialize(owner, false));
        }
        private static void Initialize(SimObject owner, bool lord) {
            string key = lord ? "fire_lord_spirit" : "fire_child_spirit";
            var p = owner.Context.GameParameters;
            int Integer(string name) => (int)(p[key + "." + name].RawValue / (1L << 32));
            owner.AddComponent(new RigidBody(owner, Integer("mass")));
            owner.AddComponent(new ZPhysics(owner, 3));
            owner.AddCollider(new CircleCollider(owner, p[key + ".radius"], false));
            if (lord) {
                owner.AddComponent(new KeepDistanceMob(owner, Integer("hp"), p[key + ".speed"], 3, 4));
                owner.AddComponent(new LimitedSequenceSpawner(owner, 5, 5, new[] { "FireChildSpirit" }));
            } else owner.AddComponent(new ProjectileRangeAttackMob(owner, Integer("hp"), p[key + ".speed"], 3, Integer("damage"), p[key + ".attack_interval"], p[key + ".attack_range"], "FireShot", (Fix64)0.4m));
            owner.Elements.AddNative(Element.Fire);
            if (lord) owner.Elements.AddNative(Element.Wind);
            owner.AddComponent(new CommonEffectReceiver(owner));
        }
    }
}
