using System;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class SummonMob : TimedBehaviorMob {
        private IDetector detector;
        private readonly Fix64 range;
        private readonly string prefab;
        public SummonMob(SimObject owner, int hp, Fix64 speed, Fix64 interval, Fix64 range, string prefab) : base(owner, hp, speed, interval) { this.range = range; this.prefab = prefab; Behavior = Summon; }
        public override void Start() { detector = new ClosestEnemyDetector(Context, 1); }
        public override void OnDeath() { Owner.Destroy(); }
        private bool Summon() {
            if (detector == null) throw new InvalidOperationException("summon mob acted before start");
            var target = detector.Detect(Owner, other => CombatRange.Contains(Owner.Position, Owner.FirstCircleRadius(false) ?? Fix64.Zero, other.Position, other.FirstCircleRadius(false) ?? Fix64.Zero, range));
            if (target == null) return false;
            Context.Spawn(Owner.Master, prefab, target.Position); return true;
        }
        public static void RegisterPrefabs(SimulationContext context) {
            context.RegisterPrefab("VineColony", owner => {
                Fix64 Value(string key) => context.GameParameters["vine_colony." + key]; int Integer(string key) => (int)(Value(key).RawValue / (1L << 32));
                owner.AddComponentImmediately(new RigidBody(owner, Integer("mass"))); owner.AddCollider(new CircleCollider(owner, Value("radius")));
                owner.AddComponentImmediately(new SummonMob(owner, Integer("hp"), 0, Integer("attack_interval"), Integer("attack_range"), "Vine"));
                owner.AddComponent(new TimedSelfDestroyer(owner, Value("duration"))); owner.Elements.AddNative(Element.Nature); owner.AddComponentImmediately(new CommonEffectReceiver(owner));
            });
        }
    }
}
