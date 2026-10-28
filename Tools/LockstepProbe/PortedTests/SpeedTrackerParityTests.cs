using ArcaneCasters.Simulation;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class SpeedTrackerParityTests {
        private const string Source = "game/domain/object/component/mob/movement_speed_tracker.rs";
        [PortedTest(Source)] public static void maps_speed_percentage_to_charge_tier() {
            decimal[] speeds = { 0m, 2.99m, 3m, 5.99m, 6m, 9.99m, 10m };
            int[] tiers = { 1, 1, 2, 2, 3, 3, 4 };
            for (int i = 0; i < speeds.Length; i++) Assert.Equal(tiers[i], MovementSpeedTracker.TierFor((Fix64)speeds[i], 10));
        }
        [PortedTest(Source)] public static void stays_at_first_tier_when_max_speed_is_invalid() {
            Assert.Equal(1, MovementSpeedTracker.TierFor(10, 0));
        }
    }
}
