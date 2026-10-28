using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class RallyMoveDirective : SimComponent, IMovementDirective {
        private readonly SimObject rallyTarget;
        public int Priority => 100;
        public Fix64 ArrivalDistance { get; }
        public bool SuppressCombat => true;
        private bool TargetAlive => rallyTarget != null && !rallyTarget.IsDestroyed;
        public RallyMoveDirective(SimObject owner, SimObject rallyTarget, Fix64 zoneRadius) : base(owner) { this.rallyTarget = rallyTarget; ArrivalDistance = zoneRadius; }
        public Vector3? GetMoveTarget(SimObject source) => TargetAlive ? rallyTarget.Position : (Vector3?)null;
        public override void Update() { if (!TargetAlive) Owner.RemoveComponent(this); }
        public bool AllowsCombatTarget(SimObject source, SimObject target) {
            if (!TargetAlive) return false;
            var mob = source.GetComponent<BehaviorMob>(); if (mob == null) return false;
            Fix64 distance = Vector3.Distance(GroundVector.Of(rallyTarget.Position), GroundVector.Of(target.Position));
            return distance - (target.FirstCircleRadius(false) ?? Fix64.Zero) <= ArrivalDistance + (source.FirstCircleRadius(false) ?? Fix64.Zero) + mob.AttackRange;
        }
    }
}
