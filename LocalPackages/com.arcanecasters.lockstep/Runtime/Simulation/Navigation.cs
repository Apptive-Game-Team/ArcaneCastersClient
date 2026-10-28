using System;
using System.Collections.Generic;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public static class SimulationVector {
        public static Vector3 Normalize(Vector3 value) {
            var length = value.Length();
            return length == Fix64.Zero ? Vector3.Zero : new Vector3(value.X / length, value.Y / length, value.Z / length);
        }
        public static Vector3 Lerp(Vector3 start, Vector3 end, Fix64 progress) {
            return start + (end - start) * MathHelper.Clamp(progress, Fix64.Zero, Fix64.One);
        }
    }
    public interface IPathFinder { List<Vector3> FindPath(Vector3 start, Vector3 end); }
    public static class GroundVector {
        public static Vector3 Of(Vector3 value) => new Vector3(value.X, 0, value.Z);
    }
    public sealed class SimplePathFinder : IPathFinder {
        public List<Vector3> FindPath(Vector3 start, Vector3 end) {
            var path = new List<Vector3>(); Fix64 dx = end.X - start.X, dz = end.Z - start.Z;
            int steps = Math.Max(Math.Abs((int)(dx.RawValue / (1L << 32))), Math.Abs((int)(dz.RawValue / (1L << 32))));
            if (steps == 0) { path.Add(GroundVector.Of(end)); return path; }
            for (int i = 0; i <= steps; i++) path.Add(new Vector3(start.X + dx * i / steps, 0, start.Z + dz * i / steps));
            return path;
        }
    }
    public readonly struct NavigationObstacle {
        public readonly Vector3 Center;
        public readonly Fix64 Radius;
        public NavigationObstacle(Vector3 center, Fix64 radius) { Center = center; Radius = radius; }
    }
    public sealed class StaticObstacle : SimComponent, ICollidable {
        public StaticObstacle(SimObject owner) : base(owner) { }
        public void OnCollision(SimObject other) { }
        public void OnCollisionWithEnemy(SimObject other) { }
    }
    public sealed class NavigationGrid {
        private readonly bool[] blocked;
        public int Columns { get; }
        public int Rows { get; }
        public int CellCount => blocked.Length;
        public NavigationGrid(int columns, int rows, IEnumerable<NavigationObstacle> obstacles, Fix64 clearance, Terrain terrain = null) {
            Columns = columns; Rows = rows; blocked = new bool[checked(columns * rows)];
            foreach (var obstacle in obstacles) {
                Fix64 reach = obstacle.Radius + clearance;
                for (int cell = 0; cell < CellCount; cell++) {
                    var delta = CenterOf(cell) - obstacle.Center;
                    if (delta.X * delta.X + delta.Z * delta.Z <= reach * reach) blocked[cell] = true;
                }
            }
            if (terrain == null || terrain.IsEmpty) return;
            for (int cell = 0; cell < CellCount; cell++) if (terrain.DistanceToWater(CenterOf(cell)) <= clearance) blocked[cell] = true;
        }
        public int Column(int cell) => cell % Columns;
        public int Row(int cell) => cell / Columns;
        public bool IsBlocked(int cell) => blocked[cell];
        public bool IsWalkable(int column, int row) => column >= 0 && column < Columns && row >= 0 && row < Rows && !blocked[row * Columns + column];
        public int CellOf(Vector3 position) => Math.Max(0, Math.Min(Rows - 1, (int)Fix64.Floor(position.Z))) * Columns + Math.Max(0, Math.Min(Columns - 1, (int)Fix64.Floor(position.X)));
        public Vector3 CenterOf(int cell) => new Vector3((Fix64)Column(cell) + (Fix64)0.5m, 0, (Fix64)Row(cell) + (Fix64)0.5m);
        public bool IsSegmentClear(Vector3 from, Vector3 to) {
            Fix64 length = Vector3.Distance(GroundVector.Of(from), GroundVector.Of(to));
            int samples = Math.Max(1, (int)Fix64.Ceiling(length / (Fix64)0.25m));
            for (int i = 0; i <= samples; i++) if (blocked[CellOf(from + (to - from) * ((Fix64)i / samples))]) return false;
            return true;
        }
        public bool HasSameBlockingAs(NavigationGrid other) => Columns == other.Columns && Rows == other.Rows && blocked.SequenceEqual(other.blocked);
    }
    public sealed class FlowField {
        public const int NoCell = -1;
        private readonly Fix64[] cost;
        private readonly int[] next;
        private readonly struct Frontier {
            public readonly int Cell;
            public readonly Fix64 Cost;
            public Frontier(int cell, Fix64 cost) { Cell = cell; Cost = cost; }
        }
        // 동률일 때의 경로 선택을 보존하도록 Java PriorityQueue의 sift 순서를 유지한다.
        private sealed class Frontiers {
            private readonly List<Frontier> queue = new List<Frontier>();
            public void Add(Frontier item) {
                int k = queue.Count; queue.Add(item);
                while (k > 0) { int parent = (k - 1) >> 1; var e = queue[parent]; if (item.Cost >= e.Cost) break; queue[k] = e; k = parent; }
                queue[k] = item;
            }
            public bool Poll(out Frontier item) {
                item = default; if (queue.Count == 0) return false;
                item = queue[0]; var x = queue[queue.Count - 1]; queue.RemoveAt(queue.Count - 1); int n = queue.Count;
                if (n == 0) return true;
                int k = 0, half = n >> 1;
                while (k < half) {
                    int child = 2 * k + 1, right = child + 1; var c = queue[child];
                    if (right < n && c.Cost > queue[right].Cost) { child = right; c = queue[child]; }
                    if (x.Cost <= c.Cost) break; queue[k] = c; k = child;
                }
                queue[k] = x; return true;
            }
        }
        public FlowField(NavigationGrid grid, int goalCell) {
            cost = Enumerable.Repeat(Fix64.MaxValue, grid.CellCount).ToArray(); next = Enumerable.Repeat(NoCell, grid.CellCount).ToArray();
            var frontier = new Frontiers(); cost[goalCell] = Fix64.Zero; frontier.Add(new Frontier(goalCell, Fix64.Zero));
            Fix64 diagonal = Fix64.Sqrt(2);
            while (frontier.Poll(out var current)) {
                if (current.Cost > cost[current.Cell]) continue;
                int column = grid.Column(current.Cell), row = grid.Row(current.Cell);
                for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++) {
                    if (dx == 0 && dz == 0 || !grid.IsWalkable(column + dx, row + dz)) continue;
                    if (dx != 0 && dz != 0 && (!grid.IsWalkable(column + dx, row) || !grid.IsWalkable(column, row + dz))) continue;
                    int neighbor = (row + dz) * grid.Columns + column + dx;
                    Fix64 candidate = current.Cost + (dx != 0 && dz != 0 ? diagonal : Fix64.One);
                    if (candidate >= cost[neighbor]) continue;
                    cost[neighbor] = candidate; next[neighbor] = current.Cell; frontier.Add(new Frontier(neighbor, candidate));
                }
            }
        }
        public bool Reaches(int cell) => cost[cell] < Fix64.MaxValue;
        public Fix64 CostFrom(int cell) => cost[cell];
        public int NextCell(int cell) => next[cell];
    }
    public sealed class FlowFieldNavigation : IPathFinder {
        public static readonly Fix64 ObstacleClearance = (Fix64)0.5m;
        private readonly Func<int> frame;
        private readonly Func<SimObject[]> objects;
        private readonly Func<Terrain> terrainSource;
        private readonly SimplePathFinder straight = new SimplePathFinder();
        private readonly Dictionary<int, FlowField> fields = new Dictionary<int, FlowField>();
        private Terrain terrain = Terrain.None;
        private NavigationGrid grid = new NavigationGrid(18, 10, new NavigationObstacle[0], ObstacleClearance);
        private NavigationObstacle[] obstacles = new NavigationObstacle[0];
        private int lastFrame = -1;
        public int GridBuilds { get; private set; }
        public FlowFieldNavigation(Func<int> frame, Func<SimObject[]> objects, Func<Terrain> terrainSource = null) { this.frame = frame; this.objects = objects; this.terrainSource = terrainSource ?? (() => Terrain.None); }
        private void Refresh() {
            int current = frame(); if (current == lastFrame) return; lastFrame = current;
            var latestTerrain = terrainSource(); var latestObstacles = new List<NavigationObstacle>();
            foreach (var owner in objects()) {
                if (!owner.IsActive || !owner.HasComponent<StaticObstacle>()) continue;
                var radius = owner.FirstCircleRadius(false); if (radius.HasValue) latestObstacles.Add(new NavigationObstacle(owner.Position, radius.Value));
            }
            if (ReferenceEquals(terrain, latestTerrain) && obstacles.SequenceEqual(latestObstacles)) return;
            terrain = latestTerrain; obstacles = latestObstacles.ToArray(); GridBuilds++;
            var latestGrid = new NavigationGrid(18, 10, obstacles, ObstacleClearance, terrain);
            if (!latestGrid.HasSameBlockingAs(grid)) { grid = latestGrid; fields.Clear(); }
        }
        private int EntryCell(FlowField field, int start) {
            if (!grid.IsBlocked(start)) return field.Reaches(start) ? start : FlowField.NoCell;
            int best = FlowField.NoCell;
            for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++) {
                int x = grid.Column(start) + dx, z = grid.Row(start) + dz; if (!grid.IsWalkable(x, z)) continue;
                int neighbor = z * grid.Columns + x;
                if (field.Reaches(neighbor) && (best == FlowField.NoCell || field.CostFrom(neighbor) < field.CostFrom(best))) best = neighbor;
            }
            return best;
        }
        public List<Vector3> FindPath(Vector3 start, Vector3 end) {
            Refresh(); int goal = grid.CellOf(end);
            if (!fields.TryGetValue(goal, out var field)) fields.Add(goal, field = new FlowField(grid, goal));
            int cell = EntryCell(field, grid.CellOf(start)); if (cell == FlowField.NoCell) return straight.FindPath(start, end);
            var points = new List<Vector3> { start };
            while (field.NextCell(cell) != FlowField.NoCell) { points.Add(grid.CenterOf(cell)); cell = field.NextCell(cell); }
            points.Add(GroundVector.Of(end));
            var path = new List<Vector3> { points[0] }; int anchor = 0;
            while (anchor < points.Count - 1) {
                int furthest = anchor + 1;
                for (int candidate = points.Count - 1; candidate > anchor + 1; candidate--) if (grid.IsSegmentClear(points[anchor], points[candidate])) { furthest = candidate; break; }
                path.Add(points[furthest]); anchor = furthest;
            }
            return path;
        }
    }
}
