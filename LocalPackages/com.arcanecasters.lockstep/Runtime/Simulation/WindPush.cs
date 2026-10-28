using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class WindPushComponent : SimComponent {
        private readonly Fix64 force;
        private Fix64 timer;
        public Vector3 BoxSize { get; }
        public Vector3 Direction => Owner.Master == Master.LeftPlayer ? Vector3.Right : Vector3.Left;
        public Vector3 BoxCenter => GroundVector.Of(Owner.Position) + new Vector3(Direction.X * BoxSize.X / 2, BoxSize.Y / 2, 0);
        public WindPushComponent(SimObject owner, Fix64 force, Vector3 size) : base(owner) { this.force = force; BoxSize = size; }
        public override void Update() {
            timer -= Context.DeltaTime; if (timer > Fix64.Zero) return; timer = (Fix64)0.5m;
            if (Owner.Master == Master.None) return;
            foreach (var target in SimulationQueries.OverlapBoxAll(Context, BoxCenter, BoxSize)) {
                if (ReferenceEquals(target, Owner) || target.IsDestroyed || target.Master == Owner.Master || !target.HasComponent<MobComponent>()) continue;
                TimedMassPush.Apply(target, Owner, Direction, force, (Fix64)0.5m + Context.DeltaTime);
            }
        }
    }
    public static class WindBuildingPrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("WindTotem", owner => {
                Fix64 Value(string name) => context.GameParameters["wind_totem." + name];
                int Integer(string name) => (int)(Value(name).RawValue / (1L << 32));
                owner.AddComponent(new RigidBody(owner, Integer("mass"))); owner.AddCollider(new CircleCollider(owner, Value("radius")));
                owner.AddComponent(new DummyMob(owner, Integer("hp"))); owner.AddComponent(new WindPushComponent(owner, Value("push_force"), new Vector3(Value("push_range_x"), Value("push_range_y"), Value("push_range_z"))));
                var duration = Value("hp") * Value("attack_interval") / Value("damage");
                owner.AddComponent(new TimedSelfDestroyer(owner, (int)(duration.RawValue / (1L << 32))));
                owner.Elements.AddNative(Element.Wind); owner.AddComponent(new CommonEffectReceiver(owner));
            });
        }
    }
}
