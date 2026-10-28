using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public class EvilEntMob : BehaviorMob, IGaugeComponent {
        public static readonly Fix64 PunchImpactFraction = (Fix64)0.55m, GrabImpactFraction = (Fix64)0.22m, FistImpactFraction = (Fix64)0.45m;
        private static readonly Fix64 PullDuration = (Fix64)0.8m;
        public int Damage { get; } public int SubDamage { get; }
        public Fix64 ProjectileSpeed { get; } public Fix64 SubAttackRange { get; } public Fix64 SubAttackInterval { get; } public Fix64 PullMassLimit { get; }
        public int PullCharges { get; set; }
        public Fix64 PullRechargeTimer { get; private set; }
        public bool IsInGrabSequence => CurrentState is GrabState || CurrentState is FistState;
        public EvilEntMob(SimObject owner, int hp, Fix64 speed, int mask, int damage, Fix64 interval, Fix64 range, Fix64 projectileSpeed, int subDamage, Fix64 subRange, Fix64 subInterval, Fix64 massLimit) : base(owner, hp, speed, mask, interval, range, null) {
            Damage = damage; SubDamage = subDamage; ProjectileSpeed = projectileSpeed; SubAttackRange = subRange; SubAttackInterval = subInterval; PullMassLimit = massLimit; Behavior = Punch;
        }
        public Gauge GetGauge() => new Gauge(Hp, MaxHp, GaugeCategory.Hp);
        private bool InRange(SimObject victim, Fix64 range) => CombatRange.Contains(Owner.Position, Owner.FirstCircleRadius(false) ?? Fix64.Zero, victim.Position, victim.FirstCircleRadius(false) ?? Fix64.Zero, range);
        private bool CanDrag(SimObject victim) { var body = victim.GetComponent<RigidBody>(); return body != null && ForcedMovement.MassMultiplier(body.Mass) != 0 && body.Mass <= PullMassLimit; }
        private Fix64 ReachTime(SimObject victim) => Vector3.Distance(victim.Position, Owner.Position) / ProjectileSpeed;
        private Hit Attack(int damage) => new Hit(damage, Owner.Id, Owner.Elements.Total);
        private void Project(string type, SimObject victim, Fix64 duration) { Context.AddEvent(new BattleEvent(type, Owner.Id, victim.Id, duration)); Owner.SetStatus(ObjectStatus.Attack); }
        private bool Punch(SimObject victim) { var reach = ReachTime(victim); Project("EvilEntPunchArm", victim, reach / PunchImpactFraction); victim.GetComponent<IDamageable>()?.OnDamagedWithDelay(Attack(Damage), reach); return true; }
        private Vector3 PullDestination(SimObject victim) {
            var direction = VineMagics.Normalize(GroundVector.Of(victim.Position - Owner.Position)); if (direction == Vector3.Zero) direction = Vector3.Right;
            return GroundVector.Of(Owner.Position) + direction * ((Owner.FirstCircleRadius(false) ?? Fix64.Zero) + (victim.FirstCircleRadius(false) ?? Fix64.Zero));
        }
        private void Abort() { ResetTarget(); SetIdle(); }
        private void Strike(SimObject victim) { victim.GetComponent<IDamageable>()?.OnDamaged(Attack(SubDamage)); victim.GetComponent<IEffectReceiver>()?.Receive(VisualEffect.Burn); }
        public override void Update() {
            base.Update();
            if (PullCharges == 3) PullRechargeTimer = 0;
            else {
                PullRechargeTimer += Context.DeltaTime;
                while (PullRechargeTimer >= SubAttackInterval && PullCharges < 3) { PullRechargeTimer -= SubAttackInterval; PullCharges++; }
                if (PullCharges == 3) PullRechargeTimer = 0;
            }
            if (IsInGrabSequence || PullCharges == 0 || !IsValidTarget(Target) || !InRange(Target, SubAttackRange) || !CanDrag(Target)) return;
            PullCharges--; SetState(new GrabState(this, Target));
        }
        public sealed class GrabState : IMobState {
            private readonly EvilEntMob mob; private readonly SimObject victim;
            private Fix64 timer, pullStartTime, pullEndTime;
            private Vector3 pullStart, pullTarget;
            private bool launched, pulled;
            public Fix64 ReachTime { get; private set; }
            public GrabState(EvilEntMob mob, SimObject victim) { this.mob = mob; this.victim = victim; }
            public void OnEnter() { } public void OnExit() { }
            public void OnUpdate() {
                if (!mob.IsValidTarget(victim)) { mob.Abort(); return; }
                if (!launched) {
                    launched = true; var duration = MathHelper.Max(mob.ReachTime(victim) / GrabImpactFraction, PullDuration / (1 - GrabImpactFraction)); ReachTime = duration * GrabImpactFraction; mob.Project("EvilEntGrabArm", victim, duration);
                }
                timer += mob.Context.DeltaTime;
                if (!pulled) {
                    if (timer < ReachTime && timer < 3) return;
                    pulled = true; if (!mob.CanDrag(victim)) { mob.Abort(); return; }
                    victim.GetComponent<BehaviorMob>()?.SetStun(PullDuration); pullStartTime = timer; pullEndTime = timer + PullDuration; pullStart = victim.Position; pullTarget = mob.PullDestination(victim);
                }
                var progress = MathHelper.Clamp((timer - pullStartTime) / PullDuration, 0, 1); victim.SetPosition(pullStart + (pullTarget - pullStart) * progress);
                if (mob.InRange(victim, (Fix64)0.2m) || timer >= pullEndTime || timer >= 3) mob.SetState(new FistState(mob, victim));
            }
        }
        public sealed class FistState : IMobState {
            private readonly EvilEntMob mob; private readonly SimObject victim;
            private Fix64 timer, impactTime; private bool launched, landed;
            public FistState(EvilEntMob mob, SimObject victim) { this.mob = mob; this.victim = victim; }
            public void OnEnter() { } public void OnExit() { }
            public void OnUpdate() {
                if (!landed && !mob.IsValidTarget(victim)) { mob.Abort(); return; }
                if (!launched) { launched = true; impactTime = mob.ReachTime(victim); mob.Project("EvilEntFireFist", victim, impactTime / FistImpactFraction); }
                timer += mob.Context.DeltaTime;
                if (!landed) { if (timer < impactTime && timer < 3) return; landed = true; mob.Strike(victim); }
                if (timer >= impactTime + (Fix64)0.4m || timer >= 3) mob.SetIdle();
            }
        }
    }
}
