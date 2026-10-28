using System.Collections.Generic;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class PanicStatusEffect : BaseStatusEffect {
        public PanicStatusEffect(SimObject owner, Fix64 duration, StatusEffectKey key) : base(owner, duration, key, VisualEffect.Panic) { }
    }
    public class CowardMob : AttackMob {
        private static Vector3 NormalizeGround(Vector3 value) { value.Y = Fix64.Zero; return value.LengthSquared() == Fix64.Zero ? Vector3.Zero : Vector3.Normalize(value); }
        private readonly Fix64 detectionRange, panicDuration, panicSpeed;
        public PriorityEnemyDetector ObjectiveDetector { get; private set; }
        public PriorityEnemyDetector ThreatDetector { get; private set; }
        public Fix64 PanicCooldownRemaining { get; set; }
        public CowardMob(SimObject owner, int hp, Fix64 speed, int mask, int damage, Fix64 interval, Fix64 detectionRange, Fix64 panicDuration, Fix64? panicSpeed = null)
            : base(owner, hp, speed, mask, damage, interval, (Fix64)0.5m) {
            this.detectionRange = detectionRange; this.panicDuration = panicDuration; this.panicSpeed = panicSpeed ?? (Fix64)2;
        }
        public override void Start() {
            base.Start();
            ObjectiveDetector = new PriorityEnemyDetector(Context, TargetMask, Fix64.MaxValue, TargetCategory.Building, TargetCategory.Player);
            ThreatDetector = new PriorityEnemyDetector(Context, TargetMask, detectionRange, TargetCategory.Unit);
            Detector = ObjectiveDetector; SetCowardIdle();
        }
        public override void Update() { if (PanicCooldownRemaining > Fix64.Zero) PanicCooldownRemaining = MathHelper.Max(Fix64.Zero, PanicCooldownRemaining - Context.DeltaTime); base.Update(); }
        public void SetCowardIdle() => SetState(new CowardIdleState(this));
        public void SetCowardMove() => SetState(new CowardMoveState(this));
        public void SetCowardAttack() => SetState(new CowardAttackState(this));
        public void SetPanic(SimObject threat) => SetState(new PanicState(this, threat));
        private SimObject DetectThreat() => PanicCooldownRemaining > Fix64.Zero ? null : ThreatDetector.Detect(Owner);
        private void MoveOrAttack() {
            if (!IsValidTarget(Target)) { ResetTarget(); SetCowardIdle(); return; }
            TargetRadius = Target.FirstCircleRadius(false) ?? Fix64.Zero;
            if (WithinAttackRange(AttackRange)) SetCowardAttack(); else SetCowardMove();
        }
        private List<Vector3> Path() => PathFinder.FindPath(GroundVector.Of(Owner.Position), GroundVector.Of(Target.Position));
        public sealed class CowardIdleState : IMobState {
            private readonly CowardMob mob; private Fix64 timer;
            public CowardIdleState(CowardMob mob) { this.mob = mob; }
            public void OnEnter() { timer = 2; }
            public void OnExit() { }
            public void OnUpdate() {
                timer += mob.Context.DeltaTime; if (timer <= Fix64.One) return;
                var threat = mob.DetectThreat(); if (threat != null) { mob.SetPanic(threat); return; }
                mob.Target = mob.ObjectiveDetector.Detect(mob.Owner);
                if (mob.Target != null) { mob.MoveOrAttack(); return; } timer = Fix64.Zero;
            }
        }
        public sealed class CowardMoveState : IMobState {
            private readonly CowardMob mob; private Fix64 timer; private List<Vector3> path;
            public CowardMoveState(CowardMob mob) { this.mob = mob; }
            public void OnEnter() { if (!mob.IsValidTarget(mob.Target)) { mob.ResetTarget(); mob.SetCowardIdle(); return; } path = mob.Path(); }
            public void OnExit() { }
            public void OnUpdate() {
                var threat = mob.DetectThreat(); if (threat != null) { mob.SetPanic(threat); return; }
                if (!mob.IsValidTarget(mob.Target) || path == null || path.Count == 0) { mob.ResetTarget(); mob.SetCowardIdle(); return; }
                var position = GroundVector.Of(mob.Owner.Position);
                if (Vector3.Distance(position, path[0]) < (Fix64)0.5m) { path.RemoveAt(0); if (path.Count == 0) { mob.SetCowardIdle(); return; } }
                if (mob.WithinAttackRange(MathHelper.Max(Fix64.Zero, mob.AttackRange - (Fix64)0.1m))) { mob.SetCowardAttack(); return; }
                timer += mob.Context.DeltaTime;
                if (timer > Fix64.One) {
                    var target = mob.ObjectiveDetector.Detect(mob.Owner);
                    if (target != null && !ReferenceEquals(target, mob.Target)) {
                        mob.TargetRadius = target.FirstCircleRadius(false) ?? Fix64.Zero; mob.Target = target; path = mob.Path(); if (path.Count == 0) return;
                    }
                    timer = Fix64.Zero;
                }
                if (mob.Body == null) { mob.SetCowardIdle(); return; }
                mob.Body.AddVelocity(NormalizeGround(path[0] - position) * mob.Speed.Total);
            }
        }
        public sealed class CowardAttackState : IMobState {
            private readonly CowardMob mob; private Fix64 timer;
            public CowardAttackState(CowardMob mob) { this.mob = mob; }
            public void OnEnter() { }
            public void OnExit() { }
            public void OnUpdate() {
                var threat = mob.DetectThreat(); if (threat != null) { mob.SetPanic(threat); return; }
                if (!mob.IsValidTarget(mob.Target)) { mob.ResetTarget(); mob.SetCowardIdle(); return; }
                timer += mob.Context.DeltaTime;
                if (!mob.WithinAttackRange(mob.AttackRange)) mob.SetCowardMove();
                else if (timer > mob.AttackInterval.Total) {
                    timer = IntervalTimer.CarryOver(timer, mob.AttackInterval.Total, mob.Context.DeltaTime);
                    if (!mob.Behavior(mob.Target)) mob.SetCowardIdle();
                }
            }
        }
        public sealed class PanicState : IMobState {
            private readonly CowardMob mob; private readonly SimObject threat; private Vector3 direction; private Fix64 timer;
            public PanicState(CowardMob mob, SimObject threat) { this.mob = mob; this.threat = threat; }
            public void OnEnter() {
                timer = Fix64.Zero; direction = NormalizeGround(mob.Owner.Position - threat.Position);
                if (direction == Vector3.Zero) { Fix64 angle = mob.Context.Random.NextFraction() * Fix64.Pi * 2; direction = new Vector3(Fix64.Cos(angle), 0, Fix64.Sin(angle)); }
                var effect = mob.Owner.GetComponent<PanicStatusEffect>() ?? mob.Owner.GetPendingComponents<PanicStatusEffect>().FirstOrDefault();
                if (effect != null) effect.Refresh(mob.panicDuration); else mob.Owner.AddComponent(new PanicStatusEffect(mob.Owner, mob.panicDuration, StatusEffectKey.PanicReceive));
            }
            public void OnExit() { var effect = mob.Owner.GetComponent<PanicStatusEffect>(); if (effect != null) mob.Owner.RemoveComponent(effect); }
            public void OnUpdate() {
                timer += mob.Context.DeltaTime; mob.Owner.GetComponent<RigidBody>()?.AddVelocity(direction * mob.Speed.Total * mob.panicSpeed);
                if (timer < mob.panicDuration) return;
                mob.PanicCooldownRemaining = 10; mob.Target = mob.ObjectiveDetector.Detect(mob.Owner);
                if (mob.Target == null) { mob.ResetTarget(); mob.SetCowardIdle(); return; } mob.MoveOrAttack();
            }
        }
    }
    public sealed class NonAttackingCowardMob : CowardMob {
        private readonly Fix64 preferredRange;
        public NonAttackingCowardMob(SimObject owner, int hp, Fix64 speed, int mask, Fix64 interval, Fix64 detectionRange, Fix64 panicDuration, Fix64 preferredRange)
            : base(owner, hp, speed, mask, 0, interval, detectionRange, panicDuration) { this.preferredRange = preferredRange; Behavior = _ => true; }
        public override void Start() { base.Start(); AttackRange = preferredRange; }
    }
}
