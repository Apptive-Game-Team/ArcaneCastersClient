using System.Collections.Generic;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class BlastTargetParityTests {
        private const string Source = "game/domain/bot/blast_target_selector.rs";
        private static readonly Vector3 Origin = new Vector3(1, 0, 5);
        private static EnemyThreat Threat(int id, decimal x, decimal z, int hp, bool core = false) { var p = new Vector3((Fix64)x, 0, (Fix64)z); return new EnemyThreat(id, p, hp, (p - Origin).Length(), core); }
        [PortedTest(Source)] public static void prefers_the_cluster_over_the_nearest_single_enemy() {
            var impact = BlastTargetSelector.BestImpact(new[] { Threat(1, 4, 5, 20), Threat(2, 8, 5, 20), Threat(3, 8.5m, 5, 20), Threat(4, 9.9m, 5, 20) }, Origin, 18, (Fix64)1.5m, 5).Value;
            Assert.Equal(3, impact.CoveredCount); Assert.Equal((Fix64)8.5m, impact.Center.X);
        }
        [PortedTest(Source)] public static void caps_converted_damage_at_remaining_hit_points_so_overkill_does_not_win() {
            var impact = BlastTargetSelector.BestImpact(new[] { Threat(1, 4, 5, 2), Threat(2, 12, 5, 40) }, Origin, 18, (Fix64)0.5m, 20).Value;
            Assert.Equal((Fix64)12, impact.Center.X); Assert.Equal((Fix64)20, impact.ExpectedDamage);
        }
        [PortedTest(Source)] public static void weights_damage_on_the_enemy_player_above_the_same_damage_on_a_summon() {
            var impact = BlastTargetSelector.BestImpact(new[] { Threat(1, 4, 5, 100), Threat(2, 17, 5, 100, true) }, Origin, 18, (Fix64)0.5m, 15).Value;
            Assert.Equal((Fix64)17, impact.Center.X); Assert.Equal((Fix64)30, impact.ExpectedDamage);
        }
        [PortedTest(Source)] public static void reports_no_impact_when_every_enemy_is_out_of_cast_range() => Assert.True(!BlastTargetSelector.BestImpact(new[] { Threat(1, 15, 5, 20) }, Origin, 9, (Fix64)1.5m, 5).HasValue);
        [PortedTest(Source)] public static void counts_covered_enemies_the_blast_would_finish_off() {
            var impact = BlastTargetSelector.BestImpact(new[] { Threat(1, 6, 5, 2), Threat(2, 6.5m, 5, 30) }, Origin, 18, (Fix64)1.5m, 5).Value;
            Assert.Equal(2, impact.CoveredCount); Assert.Equal(1, impact.LethalCount);
        }
        public static void CheckTiesAndPrediction() {
            var near = Threat(1, 4, 5, 10); var far = Threat(2, 8, 5, 10);
            Assert.Equal(near.Position, BlastTargetSelector.BestImpact(new[] { far, near }, Origin, 18, (Fix64)0.5m, 5).Value.Center);
            var upper = Threat(3, 1, 8, 10); Assert.Equal(upper.Position, BlastTargetSelector.BestImpact(new[] { upper, near }, Origin, 18, (Fix64)0.5m, 5).Value.Center);
            var assessment = ThreatAssessment.Observe(new[] { new BotVisibleObject(1, Master.RightPlayer, "FireSpirit", new Vector3(8, 0, 5), 10, true, true) }, Master.RightPlayer, Origin, 100);
            var led = assessment.LedBy(new Dictionary<int, Vector3> { [1] = new Vector3(-2, 0, 0) }, (Fix64)0.5m);
            Assert.True(!BlastTargetSelector.BestImpact(assessment.Threats, Origin, 6, 1, 10).HasValue);
            var impact = BlastTargetSelector.BestImpact(led.Threats, Origin, 6, 1, 10).Value;
            Assert.Equal(new Vector3(7, 0, 5), impact.Center); Assert.Equal(1, impact.LethalCount);
            Assert.Equal(0, BlastTargetSelector.BestImpact(new[] { Threat(1, 4, 5, 1, true) }, Origin, 6, 1, 10).Value.LethalCount);
        }
    }
}
