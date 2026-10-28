using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class SeaSerpentMob : BehaviorMob {
        public static readonly Fix64 HydroPumpDuration = (Fix64)0.35m;
        public static readonly Fix64 MuzzleHeight = (Fix64)1.6m;
        public SeaSerpentMob(SimObject owner, int hp, Fix64 speed, int mask, int damage, Fix64 interval, Fix64 range, Fix64 beamWidth)
            : base(owner, hp, speed, mask, interval, range, null, true) { Behavior = target => FireHydroPump(owner, target, damage, range, beamWidth); }
        public static Fix64 DistanceToSegment(Vector3 point, Vector3 start, Vector3 end) {
            var segment = end - start; var squared = Vector3.Dot(segment, segment);
            if (squared == 0) return (point - start).Length();
            var progress = MathHelper.Clamp(Vector3.Dot(point - start, segment) / squared, Fix64.Zero, Fix64.One);
            return (point - (start + segment * progress)).Length();
        }
        public static bool FireHydroPump(SimObject owner, SimObject target, int damage, Fix64 range, Fix64 width) {
            var origin = new Vector3(owner.Position.X, 0, owner.Position.Z); var direction = new Vector3(target.Position.X, 0, target.Position.Z) - origin;
            var length = direction.Length(); if (length == 0) return false;
            direction = new Vector3(direction.X / length, 0, direction.Z / length);
            var end = origin + direction * (range + (owner.FirstCircleRadius(false) ?? Fix64.Zero));
            var hit = new Hit(damage, owner.Id, owner.Elements.Total);
            foreach (var candidate in owner.Context.Objects) {
                if (!candidate.IsActive || !TargetRelation.CanAttack(owner, candidate)) continue;
                var damageable = candidate.GetComponent<IDamageable>(); if (damageable == null) continue;
                var radius = candidate.Colliders.FirstOrDefault(c => c.CircleRadius.HasValue)?.CircleRadius ?? Fix64.Zero;
                if (DistanceToSegment(new Vector3(candidate.Position.X, 0, candidate.Position.Z), origin, end) > width + radius) continue;
                damageable.OnDamagedWithDelay(hit, HydroPumpDuration);
            }
            owner.Context.AddEvent(new BattleEvent("SeaSerpentHydroPump", owner.Position + new Vector3(0, MuzzleHeight, 0), new Vector3(end.X, target.Position.Y, end.Z), HydroPumpDuration));
            owner.SetStatus(ObjectStatus.Attack); return true;
        }
        public static void RegisterPrefabs(SimulationContext context) {
            context.RegisterPrefab("SeaSerpent", owner => {
                var p = context.GameParameters; Fix64 Value(string key) => p["sea_serpent." + key]; int Integer(string key) => (int)(Value(key).RawValue / (1L << 32));
                owner.AddComponent(new RigidBody(owner, Integer("mass"))); owner.AddComponent(new ZPhysics(owner)); owner.AddCollider(new CircleCollider(owner, Value("radius")));
                owner.AddComponent(new SeaSerpentMob(owner, Integer("hp"), Value("speed"), 3, Integer("damage"), Value("attack_interval"), Value("attack_range"), Value("beam_width")));
                owner.AddComponent(new PathSpawner(owner, "WaterField", 1)); owner.Elements.AddNative(Element.Water); owner.AddComponent(new CommonEffectReceiver(owner));
            });
        }
    }
}
