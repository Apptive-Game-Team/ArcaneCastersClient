using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class NavigationParityTests {
        private const string Source = "tests/flow_field_navigation.rs";
        [PortedTest("game/domain/object/vector3.rs")] public static void normalize_of_zero_is_zero() {
            Assert.Equal(Vector3.Zero, SimulationVector.Normalize(Vector3.Zero));
            // Q32.32 나눗셈은 가장 가까운 정수로 반올림한다. decimal 캐스트의 버림값과 구분한다.
            Assert.Equal(new Vector3(Fix64.FromRaw(2576980378), 0, Fix64.FromRaw(3435973837)), SimulationVector.Normalize(new Vector3(3, 0, 4)));
        }
        [PortedTest("game/domain/object/vector3.rs")] public static void distance_and_lerp() {
            Assert.Equal((Fix64)5, Vector3.Distance(Vector3.Zero, new Vector3(3, 4, 0)));
            Assert.Equal(new Vector3(1, 1, 1), SimulationVector.Lerp(Vector3.Zero, new Vector3(2, 2, 2), (Fix64)0.5m));
            Assert.Equal(Vector3.Right, SimulationVector.Lerp(Vector3.Zero, Vector3.Right, 7));
            Assert.Equal(Vector3.Zero, SimulationVector.Lerp(Vector3.Zero, Vector3.Right, -1));
        }
        private static readonly Vector3 Start = new Vector3(2, 0, 5), End = new Vector3(16, 0, 5);
        private sealed class World {
            public readonly List<SimObject> Objects = new List<SimObject>();
            public int Frame;
            public Terrain Terrain = Terrain.None;
            public readonly FlowFieldNavigation Navigation;
            public World() { Navigation = new FlowFieldNavigation(() => Frame, () => Objects.ToArray(), () => Terrain); }
            public SimObject Add(Fix64 x, Fix64 z, Fix64 radius) {
                var owner = new SimObject(new SimulationContext(0), Master.None, "Wall", new Vector3(x, 0, z));
                owner.AddComponent(new StaticObstacle(owner)); owner.AddCollider(new CircleCollider(owner, radius)); owner.FlushComponents(); owner.SetStatus(ObjectStatus.Idle); Objects.Add(owner); return owner;
            }
            public List<Vector3> Path() => Navigation.FindPath(Start, End);
        }
        [PortedTest(Source)] public static void an_open_arena_gives_the_straight_segment_from_start_to_end() { Assert.Sequence(new[] { Start, End }, new World().Path()); }
        [PortedTest(Source)] public static void an_obstacle_on_the_straight_line_is_walked_around() {
            var world = new World(); world.Add(9, 5, 1); var path = world.Path(); Assert.True(path.Count > 2); Assert.Equal(Start, path[0]); Assert.Equal(End, path.Last());
            var grid = new NavigationGrid(18, 10, new[] { new NavigationObstacle(new Vector3(9, 0, 5), 1) }, FlowFieldNavigation.ObstacleClearance);
            for (int i = 1; i < path.Count; i++) Assert.True(grid.IsSegmentClear(path[i - 1], path[i]));
        }
        [PortedTest(Source)] public static void the_returned_path_can_be_consumed_from_the_front() {
            var world = new World(); world.Add(9, 5, 1); var path = world.Path(); path.RemoveAt(0); Assert.True(path.Count > 0);
        }
        [PortedTest(Source)] public static void a_wall_across_the_whole_arena_falls_back_to_the_straight_line() {
            var world = new World(); for (int row = 0; row < 10; row++) world.Add(9, (Fix64)row + (Fix64)0.5m, (Fix64)0.5m);
            Assert.Sequence(new SimplePathFinder().FindPath(Start, End), world.Path());
        }
        [PortedTest(Source)] public static void a_walker_inside_an_obstacles_clearance_still_finds_its_way_out() {
            var world = new World(); world.Add(9, 5, 1); var start = new Vector3((Fix64)8.3m, 0, 5); var path = world.Navigation.FindPath(start, End);
            Assert.Equal(start, path[0]); Assert.Equal(End, path.Last()); Assert.True(path.Count > 2);
        }
        [PortedTest(Source)] public static void a_goal_inside_an_obstacles_clearance_can_still_be_approached() {
            var world = new World(); world.Add(9, 5, 1); var end = new Vector3((Fix64)9.7m, 0, 5); var path = world.Navigation.FindPath(Start, end);
            Assert.Equal(end, path.Last()); Assert.True(path.Count > 2);
        }
        [PortedTest(Source)] public static void an_obstacle_that_disappears_stops_bending_the_path_on_the_next_frame() {
            var world = new World(); world.Add(9, 5, 1); Assert.True(world.Path().Count > 2); world.Objects.Clear(); world.Frame++; Assert.Sequence(new[] { Start, End }, world.Path());
        }
        [PortedTest(Source)] public static void an_obstacle_that_appears_bends_the_path_on_the_next_frame() {
            var world = new World(); Assert.Sequence(new[] { Start, End }, world.Path()); world.Add(9, 5, 1); world.Frame++; Assert.True(world.Path().Count > 2);
        }
        [PortedTest(Source)] public static void an_inactive_obstacle_is_ignored() {
            var world = new World(); world.Add(9, 5, 1).SetStatus(ObjectStatus.Destroyed); Assert.Sequence(new[] { Start, End }, world.Path());
        }
        [PortedTest(Source)] public static void the_grid_is_not_rebuilt_on_frames_where_no_obstacle_changed() {
            var world = new World(); world.Add(9, 5, 1); world.Path(); for (int i = 0; i < 5; i++) { world.Frame++; world.Path(); } Assert.Equal(1, world.Navigation.GridBuilds);
        }
        [PortedTest(Source)] public static void an_arena_without_obstacles_never_builds_a_grid() {
            var world = new World(); for (int i = 0; i < 5; i++) { world.Frame++; world.Path(); } Assert.Equal(0, world.Navigation.GridBuilds);
        }
        [PortedTest(Source)] public static void an_obstacle_that_moves_is_noticed() {
            var world = new World(); var rock = world.Add(9, 5, 1); Assert.True(world.Path().Count > 2); rock.SetPosition(new Vector3(9, 0, 8)); world.Frame++;
            Assert.Sequence(new[] { Start, End }, world.Path()); Assert.Equal(2, world.Navigation.GridBuilds);
        }
        private static World RiverWorld() => new World { Terrain = Terrain.River };
        private static IEnumerable<Vector3> Points(List<Vector3> path) {
            for (int i = 1; i < path.Count; i++) for (int step = 0; step <= 40; step++) yield return path[i - 1] + (path[i] - path[i - 1]) * ((Fix64)step / 40);
        }
        [PortedTest(Source)] public static void a_ground_path_across_the_river_crosses_x_equals_nine_on_the_middle_of_a_bridge() {
            var path = RiverWorld().Path(); Assert.Equal(Start, path[0]); Assert.Equal(End, path.Last()); var crossings = new List<Fix64>();
            for (int i = 1; i < path.Count; i++) {
                var from = path[i - 1]; var to = path[i]; if ((from.X - 9) * (to.X - 9) >= Fix64.Zero && from.X != (Fix64)9) continue;
                Fix64 t = to.X == from.X ? Fix64.Zero : ((Fix64)9 - from.X) / (to.X - from.X); crossings.Add(from.Z + (to.Z - from.Z) * t);
            }
            Assert.True(crossings.Count > 0); Assert.True(crossings.All(z => Fix64.Abs(z - (Fix64)2.5m) <= (Fix64)0.3m || Fix64.Abs(z - (Fix64)7.5m) <= (Fix64)0.3m));
        }
        [PortedTest(Source)] public static void the_path_keeps_a_clearance_from_every_water_cell() { foreach (var point in Points(RiverWorld().Path())) Assert.True(Terrain.River.DistanceToWater(point) >= (Fix64)0.45m); }
        [PortedTest(Source)] public static void the_path_never_steps_into_a_water_cell() { foreach (var point in Points(RiverWorld().Path())) Assert.Equal(false, Terrain.River.IsWaterAt(point)); }
        private static NavigationGrid RiverGrid(params NavigationObstacle[] obstacles) => new NavigationGrid(18, 10, obstacles, FlowFieldNavigation.ObstacleClearance, Terrain.River);
        [PortedTest(Source)] public static void water_and_every_cell_within_the_clearance_of_it_are_blocked_and_only_the_middle_bridge_rows_stay_open() {
            var grid = RiverGrid(); foreach (var cell in Terrain.River.WaterCells) Assert.Equal(false, grid.IsWalkable(cell.Column, cell.Row));
            foreach (var cell in Terrain.River.BridgeCells) Assert.Equal(cell.Row == 2 || cell.Row == 7, grid.IsWalkable(cell.Column, cell.Row));
            Assert.Equal(false, grid.IsWalkable(7, 5)); Assert.Equal(false, grid.IsWalkable(10, 4)); Assert.True(grid.IsWalkable(7, 2)); Assert.True(grid.IsWalkable(10, 7)); Assert.True(grid.IsWalkable(6, 5));
        }
        [PortedTest(Source)] public static void both_banks_are_connected_through_the_middle_row_of_each_bridge_on_its_own() {
            foreach (var z in new[] { (Fix64)7.5m, (Fix64)2.5m }) {
                var grid = RiverGrid(new NavigationObstacle(new Vector3(9, 0, z), (Fix64)1.6m)); var field = new FlowField(grid, grid.CellOf(End)); int cell = grid.CellOf(Start); Assert.True(field.Reaches(cell)); var rows = new List<int>();
                while (field.NextCell(cell) != FlowField.NoCell) { if (grid.Column(cell) == 8 || grid.Column(cell) == 9) rows.Add(grid.Row(cell)); cell = field.NextCell(cell); }
                Assert.True(rows.Count > 0 && rows.All(row => row == (z == (Fix64)7.5m ? 2 : 7)));
            }
        }
        [PortedTest(Source)] public static void every_open_cell_of_the_left_bank_reaches_the_right_bank_through_the_middle_bridge_rows() {
            var grid = RiverGrid(); var field = new FlowField(grid, grid.CellOf(End));
            for (int row = 0; row < 10; row++) for (int col = 0; col < 8; col++) if (grid.IsWalkable(col, row)) Assert.True(field.Reaches(row * 18 + col));
        }
        [PortedTest(Source)] public static void a_terrain_set_after_the_navigation_was_built_is_picked_up() {
            var world = RiverWorld(); world.Terrain = Terrain.None; Assert.Sequence(new[] { Start, End }, world.Path()); world.Terrain = Terrain.River; world.Frame++; Assert.True(world.Path().Count > 2);
        }
        [PortedTest(Source)] public static void an_aerial_mobs_path_stays_straight_over_the_water() {
            var path = new SimplePathFinder().FindPath(Start, End); Assert.Equal(Start, path[0]); Assert.Equal(End, path.Last()); Assert.True(path.All(p => p.Z == (Fix64)5));
            for (int i = 1; i < path.Count; i++) Assert.True(path[i - 1].X <= path[i].X);
        }
        [PortedTest(Source)] public static void a_walker_standing_in_the_water_still_finds_its_way_out() {
            var path = RiverWorld().Navigation.FindPath(new Vector3((Fix64)8.5m, 0, (Fix64)5.5m), End); Assert.Equal(End, path.Last()); Assert.True(path.Count >= 2);
        }
    }
}
