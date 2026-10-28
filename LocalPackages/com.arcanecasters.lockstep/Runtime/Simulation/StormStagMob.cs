using System;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class StormStagMob : StateMachineMob, ICollidable {
        private readonly int mask, damage;
        private readonly Fix64 acceleration, detectionRange, panicDuration;
        private RigidBody body;
        private MovementSpeedTracker tracker;
        public PriorityEnemyDetector ObjectiveDetector { get; private set; }
        public PriorityEnemyDetector ThreatDetector { get; private set; }
        public SimObject Target { get; set; }
        public Fix64 PanicCooldownRemaining { get; set; }
        public StormStagMob(SimObject owner, int hp, Fix64 speed, int mask, int damage, Fix64 acceleration, Fix64 detectionRange, Fix64 panicDuration) : base(owner, hp, speed) {
            this.mask = mask; this.damage = damage; this.acceleration = acceleration; this.detectionRange = detectionRange; this.panicDuration = panicDuration;
        }
        public override void Start() {
            body = Owner.GetComponent<RigidBody>(); tracker = Owner.GetComponent<MovementSpeedTracker>() ?? throw new InvalidOperationException("StormStagMob has no MovementSpeedTracker");
            ObjectiveDetector = new PriorityEnemyDetector(Context, mask, Fix64.MaxValue, TargetCategory.Unit, TargetCategory.Building, TargetCategory.Player);
            ThreatDetector = new PriorityEnemyDetector(Context, mask, detectionRange, TargetCategory.Unit, TargetCategory.Player);
            SetState(new TargetSearchState(this));
        }
        public override void Update() { if (PanicCooldownRemaining > Fix64.Zero) PanicCooldownRemaining = MathHelper.Max(Fix64.Zero, PanicCooldownRemaining - Context.DeltaTime); base.Update(); }
        public override void OnDeath() { Owner.Destroy(); }
        private bool Valid(SimObject target) => target != null && !target.IsDestroyed && TargetRelation.CanAttack(Owner, target);
        private SimObject Threat() => PanicCooldownRemaining > Fix64.Zero ? null : ThreatDetector.Detect(Owner);
        public static int JavaRound(Fix64 value) => (int)(Fix64.Floor(value + (Fix64)0.5m).RawValue / (1L << 32));
        private static Vector3 Direction(Vector3 value) { value.Y = Fix64.Zero; return value.LengthSquared() == Fix64.Zero ? Vector3.Zero : Vector3.Normalize(value); }
        private void Impact(SimObject target) {
            var damageable = target?.GetComponent<IDamageable>();
            if (!Valid(target) || damageable == null) { SetState(new TargetSearchState(this)); return; }
            int tier = tracker.Tier;
            Fix64 multiplier = tier == 4 ? (Fix64)2 : tier == 3 ? (Fix64)1.5m : tier == 2 ? (Fix64)1.25m : Fix64.One;
            if (tier >= 3) Context.AddEvent(new BattleEvent("ElectricShot", Owner.Id, (Target ?? throw new InvalidOperationException("StormStagMob projection without a target")).Id, (Fix64)0.2m));
            damageable.OnDamaged(new Hit(JavaRound(damage * multiplier), Owner.Id, Owner.Elements.Total));
            Owner.SetStatus(ObjectStatus.Attack); Target = target; SetState(new PanicState(this, target));
        }
        public void OnCollision(SimObject other) {
            if (other.Prefab != "Wall" || !(CurrentState is PanicState)) return;
            PanicCooldownRemaining = 10; SetState(Valid(Target) ? (IMobState)new ChargeState(this) : new TargetSearchState(this));
        }
        public void OnCollisionWithEnemy(SimObject other) { if (CurrentState is ChargeState && Valid(other)) Impact(other); }
        public sealed class TargetSearchState : IMobState {
            private readonly StormStagMob mob; private Fix64 timer;
            public TargetSearchState(StormStagMob mob) { this.mob = mob; }
            public void OnEnter() { mob.Target = null; mob.tracker.Reset(); mob.Owner.SetStatus(ObjectStatus.Idle); timer = 1; }
            public void OnExit() { }
            public void OnUpdate() {
                timer += mob.Context.DeltaTime; if (timer < Fix64.One) return;
                var threat = mob.Threat(); if (threat != null) { mob.SetState(new PanicState(mob, threat)); return; }
                mob.Target = mob.ObjectiveDetector.Detect(mob.Owner);
                if (mob.Target != null) { mob.SetState(new ChargeState(mob)); return; } timer = Fix64.Zero;
            }
        }
        public sealed class ChargeState : IMobState {
            private readonly StormStagMob mob; private Fix64 speed, threatTimer;
            public ChargeState(StormStagMob mob) { this.mob = mob; }
            public void OnEnter() { speed = Fix64.Zero; threatTimer = 1; mob.tracker.Begin(); mob.Owner.SetStatus(ObjectStatus.Move); }
            public void OnExit() { mob.tracker.Reset(); }
            public void OnUpdate() {
                if (!mob.Valid(mob.Target)) { mob.SetState(new TargetSearchState(mob)); return; }
                threatTimer += mob.Context.DeltaTime;
                if (threatTimer >= Fix64.One) {
                    var threat = mob.Threat(); if (threat != null) { mob.SetState(new PanicState(mob, threat)); return; } threatTimer = Fix64.Zero;
                }
                if (CombatRange.Contains(mob.Owner.Position, mob.Owner.FirstCircleRadius(false) ?? Fix64.Zero, mob.Target.Position, mob.Target.FirstCircleRadius(false) ?? Fix64.Zero, (Fix64)0.05m)) { mob.Impact(mob.Target); return; }
                if (mob.body == null) { mob.SetState(new TargetSearchState(mob)); return; }
                speed = MathHelper.Min(mob.Speed.Total, speed + mob.acceleration * mob.Context.DeltaTime);
                mob.body.AddVelocity(Direction(mob.Target.Position - mob.Owner.Position) * speed);
            }
        }
        public sealed class PanicState : IMobState {
            private readonly StormStagMob mob; private readonly SimObject threat; private Vector3 direction; private Fix64 timer;
            public PanicState(StormStagMob mob, SimObject threat) { this.mob = mob; this.threat = threat; }
            public void OnEnter() {
                mob.tracker.Reset(); timer = Fix64.Zero; direction = Direction(mob.Owner.Position - threat.Position);
                if (direction == Vector3.Zero) { Fix64 angle = mob.Context.Random.NextFraction() * Fix64.Pi * 2; direction = new Vector3(Fix64.Cos(angle), 0, Fix64.Sin(angle)); }
                var effect = mob.Owner.GetComponent<PanicStatusEffect>() ?? mob.Owner.GetPendingComponents<PanicStatusEffect>().FirstOrDefault();
                if (effect != null) effect.Refresh(mob.panicDuration); else mob.Owner.AddComponent(new PanicStatusEffect(mob.Owner, mob.panicDuration, StatusEffectKey.PanicReceive));
                mob.Owner.SetStatus(ObjectStatus.Hindered);
            }
            public void OnExit() {
                var effect = mob.Owner.GetComponent<PanicStatusEffect>(); if (effect != null) mob.Owner.RemoveComponent(effect);
                foreach (var pending in mob.Owner.GetPendingComponents<PanicStatusEffect>()) mob.Owner.RemoveComponent(pending);
                mob.Owner.RemoveEffect(VisualEffect.Panic);
            }
            public void OnUpdate() {
                timer += mob.Context.DeltaTime; mob.body?.AddVelocity(direction * mob.Speed.Total * (Fix64)1.25m);
                if (timer < mob.panicDuration) return;
                mob.PanicCooldownRemaining = 10; mob.SetState(new TargetSearchState(mob));
            }
        }
    }
}
