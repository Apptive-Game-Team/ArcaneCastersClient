using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class VineColonyParityTests {
        private const string Source = "game/domain/object/prefab/implement/misc/vine_colony_prefab_initializer.rs";
        private static SimObject Build() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; SummonMob.RegisterPrefabs(c); VinePrefabs.Register(c);
            string[] keys = { "mass", "radius", "hp", "damage", "attack_interval", "attack_range", "duration" }; decimal[] values = { 3, 1, 40, 9, 1, 3, 15 };
            for (int i = 0; i < keys.Length; i++) c.GameParameters["vine_colony." + keys[i]] = (Fix64)values[i];
            c.GameParameters["vine.radius"] = 1; c.GameParameters["vine.damage"] = 6; c.GameParameters["vine.duration"] = 2;
            return c.Spawn(Master.LeftPlayer, "VineColony", new Vector3(5, 0, 5));
        }
        [PortedTest(Source)] public static void uses_vine_colony_duration_for_lifetime() {
            var o = Build(); var gauge = o.GetComponent<TimedSelfDestroyer>().GetGauge(); Assert.Equal((Fix64)15, gauge.MaxValue); Assert.Equal(GaugeCategory.Ttl, gauge.Category);
            Assert.True(o.Elements.NativeHas(Element.Nature) && o.HasComponent<RigidBody>() && o.HasComponent<SummonMob>()); Assert.Equal(Fix64.One, o.Colliders.Single().CircleRadius.Value);
        }
        [PortedTest(Source)] public static void classifies_vine_colony_as_building_by_leaving_summon_mob_speed_at_zero() { var o = Build(); Assert.Equal(Fix64.Zero, o.GetComponent<SummonMob>().Speed.Total); Assert.Equal(TargetCategory.Building, PriorityEnemyDetector.CategoryOf(o)); }
        public static void CheckActualSummons() {
            var o = Build(); var c = o.Context; var mob = o.GetComponent<SummonMob>(); c.RegisterPrefab("Target", _ => { });
            SimObject Target(int x, int y, Master master) { var t = c.Spawn(master, "Target", new Vector3(x, y, 5)); t.AddCollider(new CircleCollider(t, (Fix64)0.5m)); t.AddComponentImmediately(new DummyMob(t, 100)); return t; }
            var air = Target(5, 3, Master.RightPlayer); var ally = Target(6, 0, Master.LeftPlayer); var far = Target(11, 0, Master.RightPlayer); c.FlushObjects(); c.DeltaTime = 1; mob.Update(); Assert.Equal(0, c.PendingObjects.Length);
            var enemy = Target(8, 0, Master.RightPlayer); c.FlushObjects(); c.DeltaTime = 0; mob.Update(); var vine = c.PendingObjects.Single(); Assert.Equal("Vine", vine.Prefab); Assert.Equal(o.Master, vine.Master); Assert.Equal(enemy.Position, vine.Position); Assert.Equal(94, enemy.GetComponent<MobComponent>().Hp); Assert.Equal(100, air.GetComponent<MobComponent>().Hp); Assert.Equal(100, ally.GetComponent<MobComponent>().Hp); Assert.Equal(100, far.GetComponent<MobComponent>().Hp);
            c.FlushObjects(); c.DeltaTime = (Fix64)0.5m; mob.Update(); Assert.Equal(0, c.PendingObjects.Length); mob.Update(); Assert.Equal(1, c.PendingObjects.Length); Assert.Equal(88, enemy.GetComponent<MobComponent>().Hp);
            c.DeltaTime = 15; o.GetComponent<TimedSelfDestroyer>().Update(); Assert.True(o.IsDestroyed);
        }
    }
}
