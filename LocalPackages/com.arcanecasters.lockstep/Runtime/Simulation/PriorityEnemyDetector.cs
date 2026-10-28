using System;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public enum TargetCategory { Unit, Building, Player, Unknown }
    public sealed class PriorityEnemyDetector : IDetector {
        private readonly SimulationContext context;
        private readonly int mask;
        private readonly Fix64 range;
        private readonly TargetCategory[] priority;
        public PriorityEnemyDetector(SimulationContext context, int mask, Fix64 range, params TargetCategory[] priority) {
            this.context = context; this.mask = mask; this.range = range; this.priority = (TargetCategory[])priority.Clone();
        }
        public static TargetCategory CategoryOf(SimObject target) {
            if (target.HasComponent<IPlayerHealth>()) return TargetCategory.Player;
            var mob = target.GetComponent<MobComponent>();
            if (mob != null) return mob.Speed.Total > Fix64.Zero ? TargetCategory.Unit : TargetCategory.Building;
            return target.HasComponent<IDamageable>() ? TargetCategory.Building : TargetCategory.Unknown;
        }
        public SimObject Detect(SimObject source, Func<SimObject, bool> filter = null) {
            SimObject best = null; int bestIndex = int.MaxValue; Fix64 bestDistance = Fix64.MaxValue;
            foreach (var target in context.Objects) {
                if (target.IsDestroyed || !TargetRelation.CanAttack(source, target) || !target.HasComponent<IDamageable>() || (CombatRange.TargetMask(target.Position) & mask) == 0) continue;
                if (!CombatRange.Contains(source.Position, source.FirstCircleRadius(false) ?? Fix64.Zero, target.Position, target.FirstCircleRadius(false) ?? Fix64.Zero, range)) continue;
                int index = Array.IndexOf(priority, CategoryOf(target));
                if (index < 0 || filter != null && !filter(target)) continue;
                Fix64 distance = Vector3.Distance(source.Position, target.Position);
                if (best == null || index < bestIndex || index == bestIndex && distance < bestDistance) { best = target; bestIndex = index; bestDistance = distance; }
            }
            return best;
        }
    }
}
