using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class VineMagicParityTests {
        private const string Source = "game/domain/magic/implement/shoot/vine_fan_magic.rs";
        private static SimulationContext Context(bool real = false) {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; if (real) VinePrefabs.Register(c); else c.RegisterPrefab("Vine", _ => { }); c.RegisterPrefab("Object", _ => { });
            c.GameParameters["vine_toss.vine_count"] = 9; c.GameParameters["vine_toss.vine_spacing"] = 1; c.GameParameters["vine_toss.vine_spawn_interval"] = (Fix64)0.12m; return c;
        }
        private static BoundMagic Magic(string name) { var r = new MagicRegistry(); VineMagics.Register(r); return r.GetBean(name).Bind(1, name, Element.None); }
        [PortedTest(Source)] public static void fans_three_vines_over_sixty_degrees_around_the_aim() {
            var c = Context(); Magic("vine_fan").Run(c, Master.LeftPlayer, Vector3.Zero, new Vector3(10, 0, 0)); var vines = c.PendingObjects; Assert.Equal(3, vines.Length);
            for (int i = 0; i < 3; i++) { Assert.Near(i == 1 ? Fix64.One : (Fix64)0.866025403784m, vines[i].Position.X, (Fix64)0.00001m); Assert.Near((Fix64)(i - 1) / 2, vines[i].Position.Z, (Fix64)0.00001m); Assert.Equal(Fix64.Zero, vines[i].Position.Y); }
        }
        [PortedTest(Source)] public static void casts_nothing_when_aimed_at_the_origin_itself() { var c = Context(); Magic("vine_fan").Run(c, Master.LeftPlayer, Vector3.Zero, Vector3.Zero); Assert.Equal(0, c.PendingObjects.Length); }
        [PortedTest(Source)] public static void can_be_cast_by_the_vine_witch_through_the_vine_fan_trait() { var c = Context(); IVineFan fan = new VineFanMagic(); fan.Run(c, Master.RightPlayer, Vector3.Zero, new Vector3(0, 0, 5)); Assert.Equal(3, c.PendingObjects.Length); }
        public static void CheckRealVinesAndWisp() {
            var c = Context(true); c.GameParameters["vine.radius"] = 1; c.GameParameters["vine.duration"] = 3; c.GameParameters["vine.damage"] = 10; c.GameParameters["vine_toss.vine_count"] = 3; c.GameParameters["vine_toss.vine_spacing"] = (Fix64)0.5m; c.GameParameters["vine_toss.vine_spawn_interval"] = (Fix64)0.25m;
            var target = c.Spawn(Master.RightPlayer, "Object", new Vector3(5, 0, 5)); target.AddCollider(new CircleCollider(target, (Fix64)0.25m)); var mob = new DummyMob(target, 100); target.AddComponentImmediately(mob); c.FlushObjects();
            var toss = Magic("vine_toss"); Assert.True(!toss.IsVineFan); toss.Run(c, Master.LeftPlayer, new Vector3(4, 0, 5), new Vector3(8, 0, 5)); var first = c.PendingObjects.Single(); Assert.Equal(90, mob.Hp); Assert.Equal(new Vector3((Fix64)4.5m, 0, 5), first.Position); Assert.True(first.HasComponent<OnStartAttacker>() && first.HasComponent<OnStartSeedSpiritEvolver>());
            c.FlushObjects(); c.FlushObjects(); c.FrameNumber = 1; c.DeltaTime = (Fix64)0.5m; first.GetComponent<SequentialLineSpawner>().Update(); Assert.Equal(2, c.PendingObjects.Length); Assert.Equal(90, mob.Hp); Assert.True(VineSpawnContext.CurrentTracker(c) == null);
            var fan = Magic("vine_fan"); Assert.True(fan.IsVineFan); c.FlushObjects(); ((IVineFan)fan).Run(c, Master.LeftPlayer, new Vector3(10, 0, 5), new Vector3(12, 0, 5)); Assert.Equal(3, c.PendingObjects.Length); c.FlushObjects();
            toss.Run(c, Master.LeftPlayer, null, new Vector3(5, 0, 5)); fan.Run(c, Master.LeftPlayer, null, new Vector3(5, 0, 5)); Assert.Equal(0, c.PendingObjects.Length);
            VineMagics.RegisterWillOWisp(c); c.GameParameters["will_o_wisp.radius"] = (Fix64)0.3m; c.GameParameters["will_o_wisp.speed"] = 4;
            var wispMagic = Magic("will_o_wisp"); wispMagic.Run(c, Master.LeftPlayer, null, target.Position); Assert.Equal(0, c.PendingObjects.Length);
            wispMagic.Run(c, Master.LeftPlayer, new Vector3(3, 0, 5), target.Position); var wisp = c.PendingObjects.Single(); Assert.Equal("WillOWisp", wisp.Prefab); Assert.True(wisp.Colliders.Single().IsTrigger); wisp.Update(); Assert.Equal(new Vector3(5, 0, 5), wisp.Position); wisp.GetComponent<MindControlShot>().OnCollisionWithEnemy(target); Assert.Equal(Master.LeftPlayer, target.Master); Assert.Equal(ObjectStatus.Idle, target.Status); Assert.True(wisp.IsDestroyed); Assert.Equal(90, mob.Hp);
        }
    }
}
