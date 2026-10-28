using System;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class MultiEnemyDetector : IDetector {
        private readonly SimulationContext context;
        private readonly int mask;
        public MultiEnemyDetector(SimulationContext context, int mask) { this.context = context; this.mask = mask; }
        public SimObject[] DetectInRange(SimObject source, int maximum, Fix64 range, Func<SimObject, bool> filter = null) => context.Objects
            .Where(target => !ReferenceEquals(source, target) && !target.IsDestroyed && TargetRelation.CanAttack(source, target) && target.HasComponent<IDamageable>()
                && (CombatRange.TargetMask(target.Position) & mask) != 0 && (filter == null || filter(target))
                && CombatRange.Contains(source.Position, source.FirstCircleRadius(false) ?? Fix64.Zero, target.Position, target.FirstCircleRadius(false) ?? Fix64.Zero, range))
            .OrderBy(target => Vector3.Distance(source.Position, target.Position)).Take(maximum).ToArray();
        public SimObject Detect(SimObject source, Func<SimObject, bool> filter = null) => DetectInRange(source, 1, Fix64.MaxValue, filter).FirstOrDefault();
    }
    public sealed class DoubleTargetMob : BehaviorMob {
        public DoubleTargetMob(SimObject owner, int hp, Fix64 speed, int mask, int damage, Fix64 interval, Fix64 range, string projectile, Fix64 duration) : base(owner, hp, speed, mask, interval, range, null) {
            var detector = new MultiEnemyDetector(Context, mask); Detector = detector;
            var shoot = ProjectileRangeAttackMob.AttackBehavior(owner, damage, projectile, duration);
            Behavior = target => {
                if (!target.HasComponent<IDamageable>()) return false;
                shoot(target); var second = detector.DetectInRange(owner, 2, range).FirstOrDefault(candidate => !ReferenceEquals(candidate, target)); shoot(second ?? target); return true;
            };
        }
    }
    public static class RangedUnitPrefabs {
        public static void Register(SimulationContext context) {
            Register(context, "AquaArcher", "aqua_archer", Element.Water, false);
            Register(context, "RockMage", "rock_mage", Element.Rock, true);
        }
        private static void Register(SimulationContext context, string prefab, string key, Element element, bool doubleTarget) {
            context.RegisterPrefab(prefab, owner => {
                var p = context.GameParameters; int Integer(string name) => (int)(p[key + "." + name].RawValue / (1L << 32));
                owner.AddComponentImmediately(new RigidBody(owner, Integer("mass"))); owner.AddComponentImmediately(new ZPhysics(owner)); owner.AddCollider(new CircleCollider(owner, p[key + ".radius"]));
                if (doubleTarget) owner.AddComponentImmediately(new DoubleTargetMob(owner, Integer("hp"), p[key + ".speed"], 3, Integer("damage"), p[key + ".attack_interval"], p[key + ".attack_range"], "RockShot", (Fix64)0.5m));
                else owner.AddComponentImmediately(new ProjectileRangeAttackMob(owner, Integer("hp"), p[key + ".speed"], 3, Integer("damage"), p[key + ".attack_interval"], p[key + ".attack_range"], "WaterShot", (Fix64)0.5m));
                owner.Elements.AddNative(element); owner.AddComponentImmediately(new CommonEffectReceiver(owner));
            });
        }
    }
}
