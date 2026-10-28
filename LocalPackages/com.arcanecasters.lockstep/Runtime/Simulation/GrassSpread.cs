using System;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class GrassSpread : SimComponent {
        private readonly Fix64 interval, radius, leafRadius;
        private readonly int quantity, rings;
        private readonly SimObject[] fields;
        private readonly TimedSelfDestroyer[] timers;
        private Fix64 elapsed;
        public GrassSpread(SimObject owner, Fix64 interval, Fix64 radius, int quantity, Fix64 leafRadius) : base(owner) {
            this.interval = MathHelper.Max((Fix64)0.05m, interval); this.radius = MathHelper.Max(0, radius); this.quantity = Math.Max(1, quantity); this.leafRadius = MathHelper.Max(0, leafRadius);
            rings = Math.Max(1, (int)Fix64.Ceiling(this.radius / (Fix64)1.75m)); fields = new SimObject[checked(rings * this.quantity)]; timers = new TimedSelfDestroyer[fields.Length];
        }
        private Vector3 Position(int index) {
            int ring = index / quantity, slot = index % quantity;
            Fix64 r = ring == rings - 1 ? radius : (Fix64)1.75m * (ring + 1);
            Fix64 angle = (ring % 2 == 0 ? Fix64.Zero : Fix64.Pi / quantity) + Fix64.Pi * 2 * slot / quantity;
            return Owner.Position + new Vector3(Fix64.Cos(angle) * r, 0, Fix64.Sin(angle) * r);
        }
        private void SpreadOne() {
            for (int i = 0; i < fields.Length; i++) {
                if (fields[i] != null) continue;
                var position = Position(i);
                if (SimulationQueries.OverlapSphereAll(Context, position, leafRadius).Any(o => o.GetComponents<EffectProvider>().Any(p => p.Effect == VisualEffect.Burn))) continue;
                fields[i] = Context.Spawn(Owner.Master, "LeafField", position); timers[i] = fields[i].GetComponent<TimedSelfDestroyer>(); return;
            }
        }
        public override void Update() {
            if (Owner.IsDestroyed) return;
            for (int i = 0; i < fields.Length; i++) {
                if (fields[i] != null && fields[i].IsDestroyed) { fields[i] = null; timers[i] = null; }
                timers[i]?.Freeze();
            }
            elapsed += Context.DeltaTime;
            while (elapsed >= interval) { elapsed -= interval; SpreadOne(); }
        }
        public static void RegisterPrefab(SimulationContext context) {
            context.RegisterPrefab("GrassGenerator", owner => {
                var p = context.GameParameters;
                owner.AddComponent(new RigidBody(owner, (int)(p["grass_generator.mass"].RawValue / (1L << 32))));
                owner.AddCollider(new CircleCollider(owner, p["grass_generator.radius"]));
                owner.AddComponent(new DummyMob(owner, (int)(p["grass_generator.hp"].RawValue / (1L << 32))));
                owner.AddComponent(new GrassSpread(owner, p["grass_generator.attack_interval"], p["grass_generator.effect_radius"], (int)(p["grass_generator.quantity"].RawValue / (1L << 32)), p["leaf_field.radius"]));
                owner.AddComponent(new SeedSpiritVineSummoner(owner, p["grass_generator.effect_radius"]));
                owner.AddComponent(new TimedSelfDestroyer(owner, p["grass_generator.duration"])); owner.Elements.AddNative(Element.Nature); owner.AddComponent(new BuildingEffectReceiver(owner));
            });
        }
    }
}
