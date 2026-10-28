using System;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class Cannon : TimedBehaviorMob, IFrenzyImmune, IMindControlImmune {
        private readonly Hit hit;
        private readonly int mask;
        private readonly Fix64 duration, range;
        private IDetector detector;
        public Cannon(SimObject owner, int hp, int damage, int mask, Fix64 interval, Fix64 range, Fix64? duration = null) : base(owner, hp, 0, interval) {
            hit = new Hit(damage, owner.Id, Element.Rock); this.mask = mask; this.range = range; this.duration = duration ?? (Fix64)0.5m; Behavior = Attack;
        }
        public override void Start() { detector = new ClosestEnemyDetector(Context, mask); }
        public override void OnDeath() { Owner.Destroy(); }
        private bool Attack() {
            if (detector == null) throw new InvalidOperationException("cannon attacked before start");
            var target = detector.Detect(Owner, candidate => CombatRange.Contains(Owner.Position, Owner.FirstCircleRadius(false) ?? Fix64.Zero, candidate.Position, candidate.FirstCircleRadius(false) ?? Fix64.Zero, range));
            var receiver = target?.GetComponent<IDamageable>(); if (receiver == null) return false;
            Context.AddEvent(new BattleEvent("RockShot", Owner.Id, target.Id, duration));
            receiver.OnDamagedWithDelay(hit, duration); Owner.SetStatus(ObjectStatus.Attack); return true;
        }
        public static void RegisterPrefab(SimulationContext context) {
            context.RegisterPrefab("GroundCannon", owner => {
                var p = context.GameParameters;
                owner.AddComponentImmediately(new RigidBody(owner, (int)(p["ground_cannon.mass"].RawValue / (1L << 32)))); owner.AddCollider(new CircleCollider(owner, p["ground_cannon.radius"]));
                owner.AddComponentImmediately(new Cannon(owner, (int)(p["ground_cannon.hp"].RawValue / (1L << 32)), (int)(p["ground_cannon.damage"].RawValue / (1L << 32)), 1, p["ground_cannon.attack_interval"], p["ground_cannon.attack_range"]));
                owner.AddComponent(new TimedSelfDestroyer(owner, 60)); owner.AddComponent(new RockDeathRemnant(owner));
                owner.Elements.AddNative(Element.Rock); owner.AddComponentImmediately(new CommonEffectReceiver(owner));
            });
        }
    }
}
