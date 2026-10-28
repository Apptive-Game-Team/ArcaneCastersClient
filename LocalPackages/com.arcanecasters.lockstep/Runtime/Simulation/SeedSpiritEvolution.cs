using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public static class SeedSpiritEvolver {
        public static int EvolveAlliedSeedSpirits(SimObject source, Fix64 radius, string evolvedPrefab) {
            int count = 0;
            foreach (var target in SimulationQueries.OverlapSphereAll(source.Context, source.Position, radius)) {
                if (ReferenceEquals(target, source) || target.IsDestroyed || source.Master != Master.None && target.Master != source.Master || target.Prefab != "SeedSpirit") continue;
                var position = target.Position; var master = target.Master; target.Destroy(); source.Context.Spawn(master, evolvedPrefab, position); count++;
            }
            return count;
        }
    }
    public sealed class OnStartSeedSpiritEvolver : SimComponent {
        private readonly Fix64 radius;
        private readonly string prefab;
        public OnStartSeedSpiritEvolver(SimObject owner, Fix64 radius, string prefab) : base(owner) { this.radius = radius; this.prefab = prefab; }
        public override void Start() { SeedSpiritEvolver.EvolveAlliedSeedSpirits(Owner, radius, prefab); }
    }
    public sealed class SeedSpiritVineSummoner : SimComponent {
        public Fix64 Radius { get; }
        public SeedSpiritVineSummoner(SimObject owner, Fix64 radius) : base(owner) { Radius = MathHelper.Max(Fix64.Zero, radius); }
        public override void Update() {
            if (Owner.IsDestroyed) return;
            foreach (var target in SimulationQueries.OverlapSphereAll(Context, Owner.Position, Radius)) {
                if (ReferenceEquals(target, Owner) || target.IsDestroyed || target.Prefab != "SeedSpirit" || target.Master != Owner.Master) continue;
                Context.Spawn(Owner.Master, "Vine", target.Position);
            }
        }
    }
    public static class VinePrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("Vine", owner => {
                Fix64 radius = context.GameParameters["vine.radius"];
                owner.AddCollider(new CircleCollider(owner, radius, true)); owner.Elements.AddNative(Element.Nature);
                owner.AddComponent(new EffectProvider(owner, VisualEffect.Snared));
                owner.AddComponent(new TimedSelfDestroyer(owner, context.GameParameters["vine.duration"]));
                owner.AddComponent(new OnStartSeedSpiritEvolver(owner, radius, "VineSpirit"));
                owner.AddComponent(new OnStartAttacker(owner, radius, (int)(context.GameParameters["vine.damage"].RawValue / (1L << 32)), VineSpawnContext.CurrentTracker(context)));
            });
        }
    }
}
