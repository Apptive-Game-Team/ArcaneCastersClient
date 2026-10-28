using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class TerrainParityTests {
        private const string Source = "game/domain/map/terrain.rs";
        [PortedTest(Source)] public static void the_river_has_eight_water_cells_and_twelve_bridge_cells() { Assert.Equal(8, Terrain.River.WaterCells.Count); Assert.Equal(12, Terrain.River.BridgeCells.Count); }
        [PortedTest(Source)] public static void water_is_columns_eight_and_nine_on_rows_zero_four_five_and_nine() {
            var expected = new HashSet<TerrainCell>(); foreach (int x in new[] { 8, 9 }) foreach (int z in new[] { 0, 4, 5, 9 }) expected.Add(new TerrainCell(x, z)); Assert.True(expected.SetEquals(Terrain.River.WaterCells));
        }
        [PortedTest(Source)] public static void bridges_are_columns_eight_and_nine_on_rows_one_to_three_and_six_to_eight() {
            var expected = new HashSet<TerrainCell>(); foreach (int x in new[] { 8, 9 }) foreach (int z in new[] { 1, 2, 3, 6, 7, 8 }) expected.Add(new TerrainCell(x, z)); Assert.True(expected.SetEquals(Terrain.River.BridgeCells));
        }
        [PortedTest(Source)] public static void water_and_bridge_cells_do_not_share_a_cell() { Assert.True(!Terrain.River.WaterCells.Intersect(Terrain.River.BridgeCells).Any()); }
        [PortedTest(Source)] public static void every_cell_is_inside_the_arena_and_the_layout_is_mirrored() {
            foreach (var cell in Terrain.River.WaterCells.Concat(Terrain.River.BridgeCells)) Assert.True(cell.Column >= 0 && cell.Column < 18 && cell.Row >= 0 && cell.Row < 10);
            foreach (var cell in Terrain.River.WaterCells) Assert.True(Terrain.River.WaterCells.Contains(new TerrainCell(17 - cell.Column, cell.Row)));
            foreach (var cell in Terrain.River.BridgeCells) Assert.True(Terrain.River.BridgeCells.Contains(new TerrainCell(17 - cell.Column, cell.Row)));
        }
        [PortedTest(Source)] public static void the_open_arena_has_no_water() {
            Assert.True(Terrain.None.IsEmpty); Assert.Equal(false, Terrain.None.IsWater(8, 0)); Assert.Equal(Fix64.MaxValue, Terrain.None.DistanceToWater(new Vector3(9, 0, (Fix64)0.5m)));
        }
        [PortedTest(Source)] public static void a_cell_center_tells_water_from_bridge() {
            Assert.True(Terrain.River.IsWaterAt(new Vector3((Fix64)8.5m, 0, (Fix64)0.5m))); Assert.True(Terrain.River.IsWaterAt(new Vector3((Fix64)9.5m, 3, (Fix64)4.5m)));
            Assert.Equal(false, Terrain.River.IsWaterAt(new Vector3((Fix64)8.5m, 0, (Fix64)2.5m))); Assert.Equal(false, Terrain.River.IsWaterAt(new Vector3((Fix64)7.99m, 0, 5))); Assert.Equal(false, Terrain.River.IsWaterAt(new Vector3((Fix64)10.01m, 0, 5)));
        }
        [PortedTest(Source)] public static void distance_to_water_is_zero_inside_a_cell_and_measured_to_the_nearest_point_of_the_square() {
            Assert.Equal(Fix64.Zero, Terrain.River.DistanceToWater(new Vector3(9, 0, 5)));
            Assert.Near(Fix64.One, Terrain.River.DistanceToWater(new Vector3(7, 0, 5)), (Fix64)0.000001m);
            Assert.Near((Fix64)1.5m, Terrain.River.DistanceToWater(new Vector3((Fix64)8.5m, 0, (Fix64)2.5m)), (Fix64)0.000001m);
            Assert.Near(Fix64.Sqrt(2), Terrain.River.DistanceToWater(new Vector3(7, 0, 3)), (Fix64)0.000001m);
        }
        [PortedTest(Source)] public static void a_water_cell_has_sides_only_toward_land_and_bridges() {
            Fix64 h = (Fix64)0.5m; var sides = Terrain.River.ExposedSides(new TerrainCell(8, 0)); Assert.Equal(2, sides.Length);
            Assert.True(sides.Contains(new TerrainSide(new Vector3(-h, 0, h), new Vector3(-h, 0, -h)))); Assert.True(sides.Contains(new TerrainSide(new Vector3(h, 0, h), new Vector3(-h, 0, h))));
            Assert.Equal(2, Terrain.River.ExposedSides(new TerrainCell(8, 4)).Length); Assert.Equal(2, Terrain.River.ExposedSides(new TerrainCell(9, 5)).Length);
            Assert.Equal(0, Terrain.River.ExposedSides(new TerrainCell(8, 2)).Length); Assert.Equal(0, Terrain.River.ExposedSides(new TerrainCell(3, 3)).Length); Assert.Equal(0, Terrain.None.ExposedSides(new TerrainCell(8, 0)).Length);
        }
        [PortedTest(Source)] public static void the_sides_are_sixteen_segments_and_every_end_is_shared_or_on_the_arena_boundary() {
            var ends = new List<Vector3>(); int count = 0;
            foreach (var cell in Terrain.River.WaterCells) foreach (var side in Terrain.River.ExposedSides(cell)) { count++; ends.Add(cell.Center + side.Start); ends.Add(cell.Center + side.End); }
            Assert.Equal(16, count);
            foreach (var end in ends) Assert.True(end.X == Fix64.Zero || end.X == (Fix64)18 || end.Z == Fix64.Zero || end.Z == (Fix64)10 || ends.Count(other => other == end) == 2);
        }
    }
}
