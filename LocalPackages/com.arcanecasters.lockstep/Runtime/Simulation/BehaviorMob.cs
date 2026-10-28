using System;
using System.Collections.Generic;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public interface IMobState { void OnEnter(); void OnExit(); void OnUpdate(); }
    public abstract class StateMachineMob : MobComponent {
        public IMobState CurrentState { get; private set; }
        private bool stateLocked;
        protected StateMachineMob(SimObject owner, int hp, Fix64 speed) : base(owner, hp, speed) { }
        public virtual void SetState(IMobState next) {
            if (stateLocked) return;
            CurrentState?.OnExit(); CurrentState = next; next.OnEnter();
        }
        public void LockState() { stateLocked = true; }
        public override void Update() { base.Update(); CurrentState?.OnUpdate(); }
        public override void OnDestroy() { CurrentState?.OnExit(); }
    }
    public interface IDetector { SimObject Detect(SimObject source, Func<SimObject, bool> filter = null); }
    public sealed class ClosestEnemyDetector : IDetector {
        private readonly SimulationContext context;
        private readonly int mask;
        public ClosestEnemyDetector(SimulationContext context, int mask) { this.context = context; this.mask = mask; }
        public SimObject Detect(SimObject source, Func<SimObject, bool> filter = null) {
            SimObject closest = null; Fix64 distance = Fix64.MaxValue;
            foreach (var target in context.Objects) {
                if (!target.IsActive || !TargetRelation.CanAttack(source, target) || !target.HasComponent<IDamageable>() || (CombatRange.TargetMask(target.Position) & mask) == 0 || filter != null && !filter(target)) continue;
                Fix64 candidate = Vector3.Distance(source.Position, target.Position);
                if (candidate < distance) { distance = candidate; closest = target; }
            }
            return closest;
        }
    }
    public interface IMovementDirective {
        int Priority { get; }
        Vector3? GetMoveTarget(SimObject source);
        Fix64 ArrivalDistance { get; }
        bool SuppressCombat { get; }
        bool AllowsCombatTarget(SimObject source, SimObject target);
    }
    public class BehaviorMob : StateMachineMob, IStunnableBehavior {
        public IPathFinder PathFinder { get; set; }
        public IDetector Detector { get; set; }
        public SimObject Target { get; set; }
        public Fix64 TargetRadius { get; set; }
        public RigidBody Body { get; set; }
        public Stat AttackInterval { get; }
        public Fix64 AttackRange { get; set; }
        public int TargetMask { get; }
        public bool VerticalRangeIgnored { get; }
        public Func<SimObject, bool> Behavior { get; set; }
        private Master? observedMaster;
        public BehaviorMob(SimObject owner, int hp, Fix64 speed, int mask, Fix64 interval, Fix64 range, Func<SimObject, bool> behavior, bool verticalRangeIgnored = false) : base(owner, hp, speed) {
            TargetMask = mask; AttackInterval = new Stat(interval); AttackRange = range; Behavior = behavior; VerticalRangeIgnored = verticalRangeIgnored;
            PathFinder = verticalRangeIgnored ? (IPathFinder)new SimplePathFinder() : Context.Navigation; Detector = new ClosestEnemyDetector(Context, mask);
        }
        public override void Start() { observedMaster = Owner.Master; SetIdle(); Body = Owner.GetComponent<RigidBody>(); }
        public override void OnDeath() { Owner.Destroy(); }
        public virtual void SetIdle() { SetState(new IdleState(this)); }
        public void SetMove() { SetState(new MoveState(this)); }
        public void SetAttack() { SetState(new AttackState(this)); }
        public virtual void SetStun(Fix64 duration) { SetState(new StunState(this, duration)); }
        public virtual Fix64 AttackRangeFor(SimObject target) => AttackRange;
        public virtual bool IsValidTarget(SimObject target) => target != null && !target.IsDestroyed && TargetRelation.CanAttack(Owner, target) && (CombatRange.TargetMask(target.Position) & TargetMask) != 0;
        public void ResetTarget() { Target = null; TargetRadius = Fix64.Zero; }
        public void TakeTarget(SimObject target) {
            TargetRadius = target.Colliders.FirstOrDefault(c => c.CircleRadius.HasValue)?.CircleRadius ?? throw new InvalidOperationException("Target has no circle collider"); Target = target;
        }
        public IMovementDirective ResolveHighestPriorityDirective() {
            IMovementDirective best = null;
            foreach (var candidate in Owner.GetComponents<IMovementDirective>()) {
                if (best != null && candidate.Priority <= best.Priority || !candidate.GetMoveTarget(Owner).HasValue) continue;
                best = candidate;
            }
            return best;
        }
        public override void Update() {
            if (!observedMaster.HasValue) observedMaster = Owner.Master;
            else if (observedMaster.Value != Owner.Master) { observedMaster = Owner.Master; ResetTarget(); SetIdle(); }
            var directive = ResolveHighestPriorityDirective();
            if (directive != null && directive.SuppressCombat) {
                var target = Detector.Detect(Owner, candidate => directive.AllowsCombatTarget(Owner, candidate));
                if (target != null) {
                    if (!ReferenceEquals(Target, target) || !(CurrentState is MoveState || CurrentState is AttackState)) { TakeTarget(target); SetMove(); }
                } else if (!(CurrentState is DirectiveMoveState)) { ResetTarget(); SetState(new DirectiveMoveState(this, directive)); }
            }
            base.Update();
        }
        public bool WithinAttackRange(Fix64 range) {
            if (Target == null) return false;
            Fix64 sourceRadius = Owner.FirstCircleRadius(false) ?? Fix64.Zero, targetRadius = Target.FirstCircleRadius(false) ?? Fix64.Zero;
            return VerticalRangeIgnored ? CombatRange.HorizontalEdgeDistance(Owner.Position, sourceRadius, Target.Position, targetRadius) <= range
                : CombatRange.Contains(Owner.Position, sourceRadius, Target.Position, targetRadius, range);
        }
        public Fix64 DiveProgress(Vector3 start, Fix64 targetY) {
            Fix64 gap = targetY - start.Y; return Fix64.Abs(gap) <= (Fix64)0.0001m ? Fix64.One : (Owner.Position.Y - start.Y) / gap;
        }
        private List<Vector3> FindPathTo(Vector3 end) => PathFinder.FindPath(GroundVector.Of(Owner.Position), GroundVector.Of(end));
        private Vector3? StepAlong(List<Vector3> path) {
            if (path == null || path.Count == 0) return null;
            if (Vector3.Distance(GroundVector.Of(Owner.Position), path[0]) < (Fix64)0.5m) { path.RemoveAt(0); if (path.Count == 0) return null; }
            return path[0];
        }
        private bool PushToward(Vector3 next) {
            if (Body == null) return false;
            var direction = GroundVector.Of(next - GroundVector.Of(Owner.Position));
            if (direction.LengthSquared() != Fix64.Zero) direction.Normalize();
            Body.AddVelocity(direction * Speed.Total); return true;
        }
        public abstract class State : IMobState {
            protected readonly BehaviorMob Mob;
            protected State(BehaviorMob mob) { Mob = mob; }
            public virtual void OnEnter() { }
            public virtual void OnExit() { }
            public abstract void OnUpdate();
        }
        public sealed class IdleState : State {
            private Fix64 timer;
            public IdleState(BehaviorMob mob) : base(mob) { }
            public override void OnEnter() { timer = 2; }
            public override void OnUpdate() {
                timer += Mob.Context.DeltaTime; if (timer <= Fix64.One) return;
                var target = Mob.Detector.Detect(Mob.Owner);
                if (target != null) { Mob.TakeTarget(target); Mob.SetMove(); return; }
                Mob.Target = null; timer = Fix64.Zero;
            }
        }
        public sealed class StunState : State {
            private readonly Fix64 duration;
            private Fix64 timer;
            public StunState(BehaviorMob mob, Fix64 duration) : base(mob) { this.duration = duration; }
            public override void OnUpdate() { timer += Mob.Context.DeltaTime; if (timer > duration) Mob.SetIdle(); }
        }
        public sealed class MoveState : State {
            private Fix64 timer;
            private List<Vector3> path;
            public MoveState(BehaviorMob mob) : base(mob) { }
            public override void OnEnter() {
                if (!Mob.IsValidTarget(Mob.Target)) { Mob.ResetTarget(); Mob.SetIdle(); return; }
                path = Mob.FindPathTo(Mob.Target.Position);
            }
            public override void OnUpdate() {
                if (!Mob.IsValidTarget(Mob.Target) || path == null || path.Count == 0) { Mob.ResetTarget(); Mob.SetIdle(); return; }
                if (Mob.WithinAttackRange(MathHelper.Max(Fix64.Zero, Mob.AttackRangeFor(Mob.Target) - (Fix64)0.1m))) { Mob.SetAttack(); return; }
                if (!Mob.StepAlong(path).HasValue) { Mob.SetIdle(); return; }
                timer += Mob.Context.DeltaTime;
                if (timer > Fix64.One) {
                    var target = Mob.Detector.Detect(Mob.Owner);
                    if (target != null && !ReferenceEquals(Mob.Target, target)) { Mob.TakeTarget(target); path = Mob.FindPathTo(target.Position); if (path.Count == 0) return; }
                    timer = Fix64.Zero;
                }
                if (path.Count > 0 && !Mob.PushToward(path[0])) Mob.SetIdle();
            }
        }
        public sealed class AttackState : State {
            private Fix64 timer;
            public AttackState(BehaviorMob mob) : base(mob) { }
            public override void OnUpdate() {
                if (!Mob.IsValidTarget(Mob.Target)) { Mob.ResetTarget(); Mob.SetIdle(); return; }
                timer += Mob.Context.DeltaTime; Fix64 interval = Mob.AttackInterval.Total;
                if (!Mob.WithinAttackRange(Mob.AttackRangeFor(Mob.Target))) Mob.SetMove();
                else if (timer > interval) {
                    timer = IntervalTimer.CarryOver(timer, interval, Mob.Context.DeltaTime);
                    if (Mob.Behavior == null) throw new InvalidOperationException("Behavior mob has no behavior");
                    if (!Mob.Behavior(Mob.Target)) Mob.SetIdle();
                }
            }
        }
        public sealed class DirectiveMoveState : State {
            private IMovementDirective directive;
            private Vector3? destination;
            private List<Vector3> path;
            private Fix64 timer;
            public DirectiveMoveState(BehaviorMob mob, IMovementDirective directive) : base(mob) { this.directive = directive; }
            private void UpdateDestination() { destination = directive.GetMoveTarget(Mob.Owner); path = destination.HasValue ? Mob.FindPathTo(destination.Value) : null; }
            public override void OnEnter() { UpdateDestination(); }
            public override void OnUpdate() {
                timer += Mob.Context.DeltaTime;
                if (timer > Fix64.One) {
                    var latest = Mob.ResolveHighestPriorityDirective(); if (latest == null || !latest.SuppressCombat) { Mob.SetIdle(); return; }
                    directive = latest; UpdateDestination(); timer = Fix64.Zero;
                }
                if (!destination.HasValue) { Mob.SetIdle(); return; }
                if (Vector3.Distance(Mob.Owner.Position, destination.Value) <= directive.ArrivalDistance) return;
                var next = Mob.StepAlong(path); if (!next.HasValue || !Mob.PushToward(next.Value)) Mob.SetIdle();
            }
        }
    }
    public class AttackMob : BehaviorMob {
        public AttackMob(SimObject owner, int hp, Fix64 speed, int mask, int damage, Fix64 interval, Fix64 range) : base(owner, hp, speed, mask, interval, range, target => {
            var damageable = target.GetComponent<IDamageable>(); if (damageable == null) return false;
            damageable.OnDamaged(new Hit(damage, owner.Id, owner.Elements.Total)); owner.SetStatus(ObjectStatus.Attack); return true;
        }) { }
    }
    public class MeleeAttackMob : AttackMob {
        public MeleeAttackMob(SimObject owner, int hp, Fix64 speed, int mask, int damage, Fix64 interval) : base(owner, hp, speed, mask, damage, interval, (Fix64)0.5m) { }
    }
    public sealed class DummyMob : MobComponent, IGaugeComponent {
        public Gauge GetGauge() => new Gauge(Hp, MaxHp, GaugeCategory.Hp);
        public DummyMob(SimObject owner, int hp) : base(owner, hp, Fix64.Zero) { }
        public override void OnDeath() { Owner.Destroy(); }
    }
}
