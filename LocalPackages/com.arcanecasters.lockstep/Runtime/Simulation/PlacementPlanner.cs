using System;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public readonly struct PlacementOptions {
        public readonly bool StrongestLane, Ranged;
        public PlacementOptions(bool strongestLane, bool ranged) { StrongestLane = strongestLane; Ranged = ranged; }
    }
    public static class PlacementPlanner {
        public static Vector3 Plan(Vector3 caster, Fix64 castRange, Master side, ThreatAssessment threats, JavaRandom random, PlacementOptions options = default) {
            int forward;
            switch (side) { case Master.LeftPlayer: forward = 1; break; case Master.RightPlayer: forward = -1; break; default: throw new ArgumentOutOfRangeException(nameof(side)); }
            var laneZ = options.StrongestLane ? threats.StrongestLaneZ : threats.ContestedLaneZ;
            Fix64 offset;
            if (threats.HasThreats) {
                var nearest = threats.Threats[0];
                if (options.StrongestLane) {
                    foreach (var threat in threats.Threats) {
                        if (!threat.PlayerCore && Fix64.Abs(threat.Position.Z - laneZ) <= ThreatAssessment.LaneGroupingGap) { nearest = threat; break; }
                    }
                }
                var intercept = nearest.DistanceToDefendedPosition * (options.Ranged ? (Fix64)0.35m : (Fix64)0.6m);
                offset = MathHelper.Min(castRange, intercept);
            } else {
                offset = castRange * (options.Ranged ? (Fix64)0.6m : (Fix64)0.9m);
                laneZ += (random.NextFraction() * 2 - Fix64.One) * (Fix64)1.5m;
            }
            var position = new Vector3(MathHelper.Clamp(caster.X + forward * offset, 0, 18), caster.Y, MathHelper.Clamp(laneZ, 0, 10));
            var delta = position - caster; var distance = delta.Length();
            return distance <= castRange || distance == Fix64.Zero ? position : caster + Vector3.Normalize(delta) * castRange;
        }
    }
}
