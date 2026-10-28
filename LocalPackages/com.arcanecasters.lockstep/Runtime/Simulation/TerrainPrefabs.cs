using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class WallCollision : SimComponent, ICollidable {
        public WallCollision(SimObject owner) : base(owner) { }
        public void OnCollision(SimObject other) { }
        public void OnCollisionWithEnemy(SimObject other) { }
    }
    public static class TerrainPrefabs {
        public static void Register(SimulationContext context) { context.RegisterPrefab("Wall", InitializeWall); context.RegisterPrefab("RiverWater", InitializeWater); context.RegisterPrefab("RiverBridge", owner => owner.Elements.AddNative(Element.None)); }
        public static void InitializeWall(SimObject owner) {
            owner.AddCollider(new EdgeCollider(owner, Vector3.Zero, new Vector3(0, 0, 10)));
            owner.AddCollider(new EdgeCollider(owner, new Vector3(18, 0, 10), new Vector3(0, 0, 10)));
            owner.AddCollider(new EdgeCollider(owner, new Vector3(18, 0, 10), new Vector3(18, 0, 0)));
            owner.AddCollider(new EdgeCollider(owner, Vector3.Zero, new Vector3(18, 0, 0)));
            owner.AddComponent(new WallCollision(owner)); owner.Elements.AddNative(Element.None);
        }
        public static void InitializeWater(SimObject owner) {
            owner.Elements.AddNative(Element.None);
            var cell = new TerrainCell((int)Fix64.Floor(owner.Position.X), (int)Fix64.Floor(owner.Position.Z));
            foreach (var side in owner.Context.Terrain.ExposedSides(cell)) owner.AddCollider(new EdgeCollider(owner, side.Start, side.End));
            owner.AddComponent(new WaterBarrier(owner));
        }
    }
}
