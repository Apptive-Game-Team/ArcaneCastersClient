using System;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class Tower : SimComponent, IIntervalAttacker {
        public Stat AttackInterval { get; }
        private readonly Hit hit;
        private readonly int mask;
        private readonly Fix64 duration, range;
        private IDetector detector;
        private Fix64 timer;
        public Tower(SimObject owner, int damage, int mask, Fix64 interval, Fix64 range, Fix64? duration = null) : base(owner) {
            hit = new Hit(damage, owner.Id, Element.Rock); this.mask = mask; AttackInterval = new Stat(interval); this.range = range; this.duration = duration ?? (Fix64)0.2m;
        }
        public override void Start() { detector = new ClosestEnemyDetector(Context, mask); }
        public override void Update() {
            timer += Context.DeltaTime; var interval = AttackInterval.Total; if (timer < interval) return;
            if (detector == null) throw new InvalidOperationException("tower attacked before start");
            var target = detector.Detect(Owner, candidate => CombatRange.Contains(Owner.Position, Owner.FirstCircleRadius(false) ?? Fix64.Zero, candidate.Position, candidate.FirstCircleRadius(false) ?? Fix64.Zero, range));
            if (target == null || !target.HasComponent<IDamageable>()) return;
            foreach (var candidate in SimulationQueries.OverlapSphereAll(Context, target.Position, 1)) {
                if (TargetRelation.CanAttack(Owner, candidate)) candidate.GetComponent<IDamageable>()?.OnDamagedWithDelay(hit, duration);
            }
            Context.AddEvent(new BattleEvent("RockShot", Owner.Id, target.Id, duration)); Owner.SetStatus(ObjectStatus.Attack);
            timer = IntervalTimer.CarryOver(timer, interval, Context.DeltaTime);
        }
    }
    public sealed class Turret : TimedBehaviorMob, IFrenzyImmune, IMindControlImmune {
        private readonly Hit hit;
        private readonly int mask;
        private readonly Fix64 duration, range;
        private IDetector detector;
        public Turret(SimObject owner, int hp, int damage, int mask, Fix64 interval, Fix64 range, Fix64? duration = null) : base(owner, hp, 0, interval) {
            hit = new Hit(damage, owner.Id, Element.Rock); this.mask = mask; this.range = range; this.duration = duration ?? (Fix64)0.5m; Behavior = Attack;
        }
        public override void Start() { detector = new ClosestEnemyDetector(Context, mask); }
        public override void OnDeath() { Owner.Destroy(); }
        private bool Attack() {
            if (detector == null) throw new InvalidOperationException("turret attacked before start");
            var target = detector.Detect(Owner, candidate => CombatRange.Contains(Owner.Position, Owner.FirstCircleRadius(false) ?? Fix64.Zero, candidate.Position, candidate.FirstCircleRadius(false) ?? Fix64.Zero, range));
            var receiver = target?.GetComponent<IDamageable>(); if (receiver == null) return false;
            receiver.OnDamagedWithDelay(hit, duration); Context.AddEvent(new BattleEvent("RockShot", Owner.Id, target.Id, duration)); Owner.SetStatus(ObjectStatus.Attack); return true;
        }
        public static void RegisterPrefab(SimulationContext context) {
            context.RegisterPrefab("RockTurret", owner => {
                var p = context.GameParameters;
                owner.AddComponentImmediately(new RigidBody(owner, (int)(p["rock_turret.mass"].RawValue / (1L << 32)))); owner.AddCollider(new CircleCollider(owner, p["rock_turret.radius"]));
                owner.AddComponentImmediately(new Turret(owner, (int)(p["rock_turret.hp"].RawValue / (1L << 32)), (int)(p["rock_turret.damage"].RawValue / (1L << 32)), 1, p["rock_turret.attack_interval"], p["rock_turret.attack_range"], (Fix64)0.2m));
                owner.AddComponent(new TimedSelfDestroyer(owner, p["rock_turret.duration"])); owner.Elements.AddNative(Element.Rock); owner.AddComponentImmediately(new CommonEffectReceiver(owner));
            });
        }
    }
}
