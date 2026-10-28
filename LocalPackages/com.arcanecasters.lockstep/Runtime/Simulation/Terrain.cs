using System.Collections.Generic;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public readonly struct TerrainCell {
        public readonly int Column, Row;
        public TerrainCell(int column, int row) { Column = column; Row = row; }
        public Vector3 Center => new Vector3((Fix64)Column + (Fix64)0.5m, 0, (Fix64)Row + (Fix64)0.5m);
    }
    public readonly struct TerrainSide {
        public readonly Vector3 Start, End;
        public TerrainSide(Vector3 start, Vector3 end) { Start = start; End = end; }
    }
    public sealed class Terrain {
        private readonly TerrainCell[] waterCells, bridgeCells;
        private readonly bool[,] water = new bool[18, 10];
        public static readonly Terrain None = new Terrain(new TerrainCell[0], new TerrainCell[0]);
        public static readonly Terrain River = BuildRiver();
        public IReadOnlyList<TerrainCell> WaterCells => System.Array.AsReadOnly(waterCells);
        public IReadOnlyList<TerrainCell> BridgeCells => System.Array.AsReadOnly(bridgeCells);
        public bool IsEmpty => waterCells.Length == 0;
        private Terrain(TerrainCell[] waterCells, TerrainCell[] bridgeCells) {
            this.waterCells = waterCells; this.bridgeCells = bridgeCells;
            foreach (var cell in waterCells) water[cell.Column, cell.Row] = true;
        }
        private static Terrain BuildRiver() {
            var water = new List<TerrainCell>(); var bridges = new List<TerrainCell>();
            foreach (int column in new[] { 8, 9 }) for (int row = 0; row < 10; row++) {
                var destination = row >= 1 && row <= 3 || row >= 6 && row <= 8 ? bridges : water;
                destination.Add(new TerrainCell(column, row));
            }
            return new Terrain(water.ToArray(), bridges.ToArray());
        }
        public bool IsWater(int column, int row) => column >= 0 && column < 18 && row >= 0 && row < 10 && water[column, row];
        public bool IsWaterAt(Vector3 position) => IsWater((int)Fix64.Floor(position.X), (int)Fix64.Floor(position.Z));
        // 물이 없으면 무한대에 대응하는 최댓값을 반환한다. 거리 비교에만 사용한다.
        public Fix64 DistanceToWater(Vector3 position) {
            Fix64 nearest = Fix64.MaxValue;
            foreach (var cell in waterCells) {
                Fix64 x = MathHelper.Max(Fix64.Zero, MathHelper.Max((Fix64)cell.Column - position.X, position.X - (cell.Column + 1)));
                Fix64 z = MathHelper.Max(Fix64.Zero, MathHelper.Max((Fix64)cell.Row - position.Z, position.Z - (cell.Row + 1)));
                nearest = MathHelper.Min(nearest, Fix64.Sqrt(x * x + z * z));
            }
            return nearest;
        }
        public TerrainSide[] ExposedSides(TerrainCell cell) {
            if (!IsWater(cell.Column, cell.Row)) return new TerrainSide[0];
            var result = new List<TerrainSide>(); Fix64 h = (Fix64)0.5m;
            var corners = new[] { new Vector3(h, 0, -h), new Vector3(h, 0, h), new Vector3(-h, 0, h), new Vector3(-h, 0, -h) };
            int[] dx = { 1, 0, -1, 0 }, dz = { 0, 1, 0, -1 };
            for (int i = 0; i < 4; i++) {
                int x = cell.Column + dx[i], z = cell.Row + dz[i];
                if (x >= 0 && x < 18 && z >= 0 && z < 10 && !IsWater(x, z)) result.Add(new TerrainSide(corners[i], corners[(i + 1) % 4]));
            }
            return result.ToArray();
        }
        public bool[,] CopyWaterGrid() => (bool[,])water.Clone();
    }
}
