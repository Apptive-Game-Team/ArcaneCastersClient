using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public static class RemnantAndVineMagics {
        public static void Register(MagicRegistry registry) {
            registry.Register("rock_blast", new MagicImplementation((context, master, origin, position) => {
                var remnants = SimulationQueries.OverlapSphereAll(context, position, context.GameParameters["rock_blast.radius"])
                    .Where(o => o.IsActive && RockRemnantSizes.FromPrefab(o.Prefab).HasValue).ToArray();
                foreach (var remnant in remnants) {
                    var size = RockRemnantSizes.FromPrefab(remnant.Prefab).Value; var location = remnant.Position; var side = remnant.Master;
                    remnant.Destroy(); context.Spawn(master, "RockExplode", location);
                    var remainder = size.BlastRemainderPrefab(); if (remainder != null) context.Spawn(side, remainder, location);
                }
            }));
            registry.Register("vine_world", new MagicImplementation((context, master, origin, position) => {
                var tracker = new VineHitTracker(); var giant = VineSpawnContext.RunWithTracker(context, tracker, () => context.Spawn(master, "GiantVine", position));
                giant.AddComponent(new VineWorldGrowth(giant, tracker));
            }));
        }
        public static void RegisterGiantVine(SimulationContext context) {
            context.RegisterPrefab("GiantVine", owner => {
                var p = context.GameParameters; var radius = p["giant_vine.radius"]; owner.AddCollider(new CircleCollider(owner, radius, true)); owner.Elements.AddNative(Element.Nature);
                owner.AddComponent(new EffectProvider(owner, VisualEffect.Snared)); owner.AddComponent(new TimedSelfDestroyer(owner, p["giant_vine.duration"]));
                owner.AddComponent(new OnStartSeedSpiritEvolver(owner, radius, "VineSpirit"));
                owner.AddComponent(new OnStartAttacker(owner, radius, (int)(p["giant_vine.damage"].RawValue / (1L << 32)), VineSpawnContext.CurrentTracker(context)));
            });
        }
    }
    public sealed class VineWorldGrowth : SimComponent {
        private readonly VineHitTracker giantTracker, smallTracker = new VineHitTracker();
        private Fix64 elapsed;
        private long positiveSteps;
        private bool grown;
        public VineWorldGrowth(SimObject owner, VineHitTracker giantTracker) : base(owner) { this.giantTracker = giantTracker; }
        private void SpawnRing(Fix64 radius, int count, Fix64 offset) {
            for (int i = 0; i < count; i++) {
                var angle = offset + Fix64.Pi * 2 * i / count; var position = Owner.Position + new Vector3(Fix64.Cos(angle) * radius, 0, Fix64.Sin(angle) * radius);
                VineSpawnContext.RunWithTracker(Context, smallTracker, () => Context.Spawn(Owner.Master, "Vine", position));
            }
        }
        public override void Update() {
            if (grown) return; elapsed += Context.DeltaTime; if (Context.DeltaTime > 0) positiveSteps++;
            var due = (Fix64)0.75m;
            if (elapsed < due && due.RawValue - elapsed.RawValue >= positiveSteps) return;
            SpawnRing((Fix64)2.25m, 10, 0); SpawnRing(4, 16, Fix64.Pi / 16);
            int count = giantTracker.HitCount * 2 + smallTracker.HitCount;
            for (int i = 0; i < count; i++) {
                var radius = (Fix64)0.65m * Fix64.Sqrt(i + 1); var angle = i * (Fix64)2.3999632m;
                // 원본의 씨앗 소환 나선은 X/Y 평면을 사용한다.
                Context.Spawn(Owner.Master, "SeedSpirit", Owner.Position + new Vector3(Fix64.Cos(angle) * radius, Fix64.Sin(angle) * radius, 0));
            }
            grown = true; Owner.RemoveComponent(this);
        }
    }
}
