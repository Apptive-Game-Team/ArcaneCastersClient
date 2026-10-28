using System;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class PlacementPlannerParityTests {
        private const string Source = "game/domain/bot/placement_planner.rs";
        private static readonly Vector3 Left = new Vector3(1, 0, 5), Right = new Vector3(17, 0, 5);
        private static ThreatAssessment Observe(Master side, params Vector3[] positions) {
            var enemy = side == Master.LeftPlayer ? Master.RightPlayer : Master.LeftPlayer;
            return ThreatAssessment.Observe(positions.Select((position, id) => new BotVisibleObject(id, enemy, "RockSlime", position, 10, false, true)), enemy, side == Master.LeftPlayer ? Left : Right, 100);
        }
        [PortedTest(Source)] public static void drops_the_blocker_in_the_contested_lane_ahead_of_the_defended_player() {
            var threats = Observe(Master.LeftPlayer, new Vector3(7, 0, 2)); var placement = PlacementPlanner.Plan(Left, 6, Master.LeftPlayer, threats, new JavaRandom(1));
            Assert.Equal((Fix64)2, placement.Z); Assert.True(placement.X > Left.X);
        }
        [PortedTest(Source)] public static void places_towards_the_enemy_for_the_right_side_bot() {
            var threats = Observe(Master.RightPlayer, new Vector3(11, 0, 8)); var placement = PlacementPlanner.Plan(Right, 6, Master.RightPlayer, threats, new JavaRandom(1));
            Assert.Equal((Fix64)8, placement.Z); Assert.True(placement.X < Right.X);
        }
        [PortedTest(Source)] public static void advances_towards_the_enemy_when_the_field_is_clear() {
            var placement = PlacementPlanner.Plan(Left, 6, Master.LeftPlayer, Observe(Master.LeftPlayer), new JavaRandom(1)); Assert.True(placement.X > Left.X + 4);
        }
        [PortedTest(Source)] public static void never_places_outside_the_map_or_beyond_the_cast_range() {
            var placement = PlacementPlanner.Plan(Right, 18, Master.RightPlayer, Observe(Master.RightPlayer, new Vector3(17, 0, 10)), new JavaRandom(1));
            Assert.True(placement.X >= Fix64.Zero && placement.X <= (Fix64)18 && placement.Z >= Fix64.Zero && placement.Z <= (Fix64)10); Assert.True((placement - Right).Length() <= (Fix64)18);
        }
        [PortedTest(Source)] public static void places_a_ranged_unit_behind_where_a_blocker_would_stand() {
            var threats = Observe(Master.LeftPlayer, new Vector3(9, 0, 5));
            var blocker = PlacementPlanner.Plan(Left, 10, Master.LeftPlayer, threats, new JavaRandom(1));
            var ranged = PlacementPlanner.Plan(Left, 10, Master.LeftPlayer, threats, new JavaRandom(1), new PlacementOptions(false, true));
            Assert.True(ranged.X > Left.X && ranged.X < blocker.X);
        }
        [PortedTest(Source)] public static void defends_the_strongest_lane_instead_of_the_middle_between_two() {
            var threats = Observe(Master.LeftPlayer, new Vector3(4, 0, 1), new Vector3(4, 0, (Fix64)1.5m), new Vector3(4, 0, 9));
            var averaged = PlacementPlanner.Plan(Left, 10, Master.LeftPlayer, threats, new JavaRandom(1));
            var strongest = PlacementPlanner.Plan(Left, 10, Master.LeftPlayer, threats, new JavaRandom(1), new PlacementOptions(true, false));
            Assert.True(averaged.Z >= (Fix64)3 && averaged.Z <= (Fix64)5); Assert.True(strongest.Z < (Fix64)2);
        }
        public static void CheckRandomConsumption() {
            var random = new JavaRandom(7); long initial = random.State;
            PlacementPlanner.Plan(Left, 6, Master.LeftPlayer, Observe(Master.LeftPlayer, new Vector3(7, 0, 2)), random); Assert.Equal(initial, random.State);
            var expectedRandom = new JavaRandom(7); expectedRandom.NextFraction();
            var actual = PlacementPlanner.Plan(Left, 6, Master.LeftPlayer, Observe(Master.LeftPlayer), random);
            Assert.Equal(expectedRandom.State, random.State); Assert.Equal(actual, PlacementPlanner.Plan(Left, 6, Master.LeftPlayer, Observe(Master.LeftPlayer), new JavaRandom(7)));
            bool rejected = false; try { PlacementPlanner.Plan(Left, 6, Master.None, Observe(Master.LeftPlayer), random); } catch (ArgumentOutOfRangeException) { rejected = true; } Assert.True(rejected);
        }
    }
}
