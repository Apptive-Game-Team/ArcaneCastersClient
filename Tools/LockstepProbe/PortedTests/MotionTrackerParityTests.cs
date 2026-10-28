using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class MotionTrackerParityTests {
        private const string Source = "game/domain/bot/motion_tracker.rs";
        private static BotEye Eye(Fix64? time, int x) => new BotEye(new[] { new BotVisibleObject(1, Master.RightPlayer, "FireSpirit", new Vector3(x, 0, 5), 10, true, true) }, new long[0], 0, 100, time);
        [PortedTest(Source)] public static void measures_velocity_between_two_snapshots() {
            var tracker = new MotionTracker(); Assert.Equal(0, tracker.Observe(Eye(1, 10)).Count);
            Assert.Equal(new Vector3(-2, 0, 0), tracker.Observe(Eye((Fix64)1.5m, 9))[1]);
        }
        [PortedTest(Source)] public static void snapshots_without_game_time_are_never_measured() {
            var tracker = new MotionTracker(); tracker.Observe(Eye(null, 10)); Assert.Equal(0, tracker.Observe(Eye(null, 9)).Count);
        }
        [PortedTest(Source)] public static void snapshots_too_far_apart_say_nothing() {
            var tracker = new MotionTracker(); tracker.Observe(Eye(1, 10)); Assert.Equal(0, tracker.Observe(Eye((Fix64)3.1m, 9)).Count);
        }
        [PortedTest(Source)] public static void ignores_jumps_faster_than_any_unit_walks() {
            var tracker = new MotionTracker(); tracker.Observe(Eye(1, 2)); Assert.Equal(0, tracker.Observe(Eye((Fix64)1.1m, 12)).Count);
        }
    }
}
