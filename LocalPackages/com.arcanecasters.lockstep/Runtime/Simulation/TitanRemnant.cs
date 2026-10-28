using System;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class TitanRemnantMob : TimedBehaviorMob {
        private readonly Fix64 range;
        private IDetector detector;
        public TitanRemnantMob(SimObject owner, int hp, Fix64 interval, Fix64 range) : base(owner, hp, 0, interval) { this.range = range; Behavior = Attack; }
        public override void Start() { detector = new ClosestEnemyDetector(Context, 3); }
        public override void OnDeath() { Owner.Destroy(); }
        private bool Attack() {
            if (detector == null) throw new InvalidOperationException("titan remnant attacked before start");
            var target = detector.Detect(Owner, candidate => CombatRange.Contains(Owner.Position, Owner.FirstCircleRadius(false) ?? Fix64.Zero, candidate.Position, candidate.FirstCircleRadius(false) ?? Fix64.Zero, range));
            if (target == null) return false;
            Context.Spawn(Owner.Master, "TitanFist", GroundVector.Of(target.Position)); Owner.SetStatus(ObjectStatus.Attack); return true;
        }
    }
    public static class TitanPrefabs {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("TitanRemnant", owner => {
                var p = context.GameParameters;
                owner.AddComponent(new RigidBody(owner, (int)(p["titan_remnant.mass"].RawValue / (1L << 32)))); owner.AddCollider(new CircleCollider(owner, p["titan_remnant.radius"]));
                owner.AddComponent(new TitanRemnantMob(owner, (int)(p["titan_remnant.hp"].RawValue / (1L << 32)), p["titan_remnant.attack_interval"], p["titan_remnant.attack_range"]));
                owner.AddComponent(new TimedSelfDestroyer(owner, p["titan_remnant.duration"])); owner.Elements.AddNative(Element.Rock);
                owner.AddComponent(new RockDeathRemnant(owner, RockRemnantSize.Medium)); owner.AddComponent(new BuildingEffectReceiver(owner));
            });
            context.RegisterPrefab("TitanFist", owner => {
                var p = context.GameParameters; var radius = p["titan_fist.radius"];
                owner.AddCollider(new CircleCollider(owner, radius, true)); owner.Elements.AddNative(Element.Rock);
                owner.AddComponent(new OnStartAttacker(owner, radius, (int)(p["titan_fist.damage"].RawValue / (1L << 32))));
                owner.AddComponent(new TimedSelfDestroyer(owner, p["titan_fist.duration"]));
            });
        }
    }
}
