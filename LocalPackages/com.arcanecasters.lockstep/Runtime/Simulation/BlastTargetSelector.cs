using System.Collections.Generic;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public readonly struct BlastImpact {
        public readonly Vector3 Center;
        public readonly Fix64 ExpectedDamage;
        public readonly int CoveredCount, LethalCount;
        public BlastImpact(Vector3 center, Fix64 expectedDamage, int coveredCount, int lethalCount) { Center = center; ExpectedDamage = expectedDamage; CoveredCount = coveredCount; LethalCount = lethalCount; }
    }
    public static class BlastTargetSelector {
        public static readonly Fix64 PlayerCoreWeight = 2;
        public static BlastImpact? BestImpact(IReadOnlyList<EnemyThreat> threats, Vector3 origin, Fix64 range, Fix64 radius, Fix64 damagePerTarget) {
            BlastImpact? best = null;
            foreach (var candidate in threats) {
                if ((candidate.Position - origin).Length() > range) continue;
                Fix64 damage = 0; int covered = 0, lethal = 0;
                foreach (var target in threats) {
                    if ((target.Position - candidate.Position).Length() > radius) continue;
                    covered++; var converted = MathHelper.Min(damagePerTarget, target.Hp);
                    damage += target.PlayerCore ? converted * PlayerCoreWeight : converted;
                    if (!target.PlayerCore && damagePerTarget >= (Fix64)target.Hp) lethal++;
                }
                var impact = new BlastImpact(candidate.Position, damage, covered, lethal);
                if (!best.HasValue || Better(impact, best.Value, origin)) best = impact;
            }
            return best;
        }
        private static bool Better(BlastImpact candidate, BlastImpact current, Vector3 origin) {
            if (candidate.ExpectedDamage != current.ExpectedDamage) return candidate.ExpectedDamage > current.ExpectedDamage;
            if (candidate.CoveredCount != current.CoveredCount) return candidate.CoveredCount > current.CoveredCount;
            return (candidate.Center - origin).Length() < (current.Center - origin).Length();
        }
    }
}
