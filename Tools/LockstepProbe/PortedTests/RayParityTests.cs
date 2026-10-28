using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class RayParityTests {
        private const string Source = "game/util/simple_physics.rs";
        private static SimulationContext Context() { var c = new SimulationContext(); c.RegisterPrefab("Object", _ => { }); return c; }
        private static SimObject Circle(SimulationContext c, Vector3 position, bool trigger = false) { var o = c.Spawn(Master.RightPlayer, "Object", position); o.AddCollider(new CircleCollider(o, (Fix64)0.5m, trigger)); return o; }
        [PortedTest(Source)] public static void raycast_all_returns_hits_ordered_by_the_distance_to_the_body_surface() {
            var c = Context(); var far = Circle(c, new Vector3(8, 0, 0)); var off = Circle(c, new Vector3(5, 0, (Fix64)0.45m)); var line = Circle(c, new Vector3((Fix64)5.2m, 0, 0)); c.FlushObjects();
            var hits = RayQueries.RaycastAll(c, Vector3.Zero, Vector3.Right, 18); Assert.Sequence(new[] { line, off, far }, hits.Select(hit => hit.Object)); Assert.Near((Fix64)4.7m, hits[0].Distance, (Fix64)0.0001m); Assert.Near((Fix64)4.7m, hits[0].Point.X, (Fix64)0.0001m);
        }
        [PortedTest(Source)] public static void raycast_all_skips_objects_behind_beside_or_beyond_the_ray() {
            var c = Context(); Circle(c, new Vector3((Fix64)(-0.5m), 0, 0)); Circle(c, new Vector3(5, 0, (Fix64)0.6m)); Circle(c, new Vector3((Fix64)18.6m, 0, 0)); var edge = Circle(c, new Vector3((Fix64)18.4m, 0, 0)); c.FlushObjects(); Assert.Sequence(new[] { edge }, RayQueries.RaycastAll(c, Vector3.Zero, Vector3.Right, 18).Select(hit => hit.Object));
        }
        [PortedTest(Source)] public static void raycast_all_takes_height_into_account() {
            var c = Context(); var air = Circle(c, new Vector3(4, 2, 0)); var ground = Circle(c, new Vector3(6, 0, 0)); c.FlushObjects(); Assert.Sequence(new[] { ground }, RayQueries.RaycastAll(c, Vector3.Zero, Vector3.Right, 18).Select(hit => hit.Object)); Assert.Sequence(new[] { air }, RayQueries.RaycastAll(c, new Vector3(0, 2, 0), Vector3.Right, 18).Select(hit => hit.Object));
        }
        [PortedTest(Source)] public static void raycast_all_skips_objects_without_a_collider() { var c = Context(); c.Spawn(Master.RightPlayer, "Object", new Vector3(2, 0, 0)); c.FlushObjects(); Assert.Equal(0, RayQueries.RaycastAll(c, Vector3.Zero, Vector3.Right, 18).Length); }
        public static void CheckBoundaries() {
            var c = Context(); var inside = Circle(c, Vector3.Zero); var first = Circle(c, new Vector3(2, 0, 0), true); var second = Circle(c, new Vector3(2, 0, 0)); var tangent = Circle(c, new Vector3(3, 0, (Fix64)0.5m));
            var edgeFirst = c.Spawn(Master.RightPlayer, "Object", new Vector3(1, 0, 0)); edgeFirst.AddCollider(new EdgeCollider(edgeFirst, Vector3.Zero, Vector3.Right)); edgeFirst.AddCollider(new CircleCollider(edgeFirst, 1)); c.FlushObjects(); second.Destroy();
            var hits = RayQueries.RaycastAll(c, Vector3.Zero, new Vector3(2, 0, 0), 3); Assert.Sequence(new[] { first, second, tangent }, hits.Select(h => h.Object)); Assert.Equal((Fix64)3, hits.Last().Distance); Assert.Equal(0, RayQueries.RaycastAll(c, Vector3.Zero, Vector3.Zero, 3).Length);
            Assert.Sequence(new[] { first, second }, RayQueries.RaycastAll(c, Vector3.Zero, Vector3.Right, (Fix64)1.5m).Select(h => h.Object)); Assert.Equal(0, RayQueries.RaycastAll(c, Vector3.Zero, Vector3.Right, (Fix64)1.5m - Fix64.FromRaw(1)).Length);
        }
    }
}
