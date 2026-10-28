using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class RepairAura : SimComponent {
        private readonly Fix64 radius;
        public RepairAura(SimObject owner, Fix64 radius) : base(owner) { this.radius = radius; }
        public override void Update() {
            if (Owner.Master == Master.None) return;
            foreach (var target in SimulationQueries.OverlapSphereAll(Context, Owner.Position, radius)) {
                if (ReferenceEquals(target, Owner) || target.Master != Owner.Master || PriorityEnemyDetector.CategoryOf(target) != TargetCategory.Building || target.HasComponent<RepairAura>()) continue;
                target.GetComponent<TimedSelfDestroyer>()?.Freeze();
            }
        }
    }
}
