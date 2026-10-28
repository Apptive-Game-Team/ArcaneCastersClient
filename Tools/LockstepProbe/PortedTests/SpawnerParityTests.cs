using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class SpawnerParityTests {
        private const string Source = "game/domain/object/component/magic/spawner.rs";
        private const string SequenceSource = "game/domain/object/component/magic/limited_sequence_spawner.rs";
        public static void CheckRandomAreaReplay() {
            var first = Context((Fix64)0.1m); var second = Context((Fix64)0.1m);
            first.Random = new JavaRandom(0); second.Random = new JavaRandom(0);
            var position = new Vector3(5, 0, 5);
            var a = new SimObject(first, Master.RightPlayer, "Rain", position); var b = new SimObject(second, Master.RightPlayer, "Rain", position);
            var sa = new RandomAreaSpawner(a, "FireTadpole", 1, (Fix64)0.2m, 3); var sb = new RandomAreaSpawner(b, "FireTadpole", 1, (Fix64)0.2m, 3);
            for (int i = 0; i < 11; i++) { sa.Update(); sb.Update(); }
            Assert.True(a.IsDestroyed && b.IsDestroyed); Assert.Equal(5, first.PendingObjects.Length);
            Assert.Sequence(first.PendingObjects.Select(item => item.Position), second.PendingObjects.Select(item => item.Position));
            foreach (var spawned in first.PendingObjects) {
                Assert.Equal((Fix64)10, spawned.Position.Y); Assert.Equal(Master.RightPlayer, spawned.Master);
                var ground = GroundVector.Of(spawned.Position); Assert.True(Vector3.Distance(ground, position) <= 3); Assert.True(ground.Z >= 5);
            }
            var expected = new JavaRandom(0); for (int i = 0; i < 10; i++) expected.NextFraction();
            Assert.Equal(expected.NextBound(100000), first.Random.NextBound(100000));
        }
        private static SimulationContext Context(Fix64 delta) {
            var context = new SimulationContext(0) { DeltaTime = delta };
            foreach (var prefab in new[] { "SeedNest", "SeedSpirit", "FireTadpole", "FireSlime", "CraterEmber" }) context.RegisterPrefab(prefab, _ => { });
            return context;
        }
        [PortedTest("game/domain/object/component/magic/crater_spawner.rs")] public static void spawns_one_ember_per_interval_and_catches_up_after_a_long_frame() {
            var context = Context((Fix64)0.4m); var owner = new SimObject(context, Master.LeftPlayer, "CraterEmber", new Vector3(2, 0, 2));
            var spawner = new CraterSpawner(owner, (Fix64)0.5m); spawner.Update(); Assert.Equal(0, context.PendingObjects.Length);
            context.DeltaTime = (Fix64)1.2m; spawner.Update(); Assert.Equal(3, context.PendingObjects.Length);
            Assert.True(context.PendingObjects.All(ember => ember.Prefab == "CraterEmber" && ember.Master == Master.LeftPlayer && ember.Position == new Vector3(2, 0, 2)));
        }
        [PortedTest("game/domain/object/component/magic/crater_spawner.rs")] public static void the_interval_never_drops_below_one_twentieth_of_a_second() {
            var context = Context((Fix64)0.1m); var owner = new SimObject(context, Master.LeftPlayer, "CraterEmber", Vector3.Zero);
            new CraterSpawner(owner, 0).Update(); Assert.Equal(2, context.PendingObjects.Length);
        }
        [PortedTest(Source)] public static void spawning_does_not_consume_spawner_hp() {
            var context = Context((Fix64)0.1m); var owner = context.Spawn(Master.LeftPlayer, "SeedNest", Vector3.Zero); int before = context.PendingObjects.Length;
            var spawner = new Spawner(owner, 20, "SeedSpirit", (Fix64)0.1m, true, 1); spawner.Update();
            Assert.Equal(before + 1, context.PendingObjects.Length); Assert.Equal(20, spawner.Hp);
        }
        [PortedTest(Source)] public static void spawns_as_often_over_thirty_seconds_at_any_tick_rate() {
            foreach (int rate in new[] { 20, 60 }) {
                var context = Context(Fix64.One / rate); var owner = context.Spawn(Master.LeftPlayer, "SeedNest", Vector3.Zero); int before = context.PendingObjects.Length;
                var spawner = new Spawner(owner, 20, "SeedSpirit", (Fix64)0.7m, true, 1);
                for (int i = 0; i < 30 * rate; i++) spawner.Update();
                Assert.Equal(42, context.PendingObjects.Length - before);
            }
        }
        [PortedTest(Source)] public static void an_indestructible_spawner_ignores_damage_and_reports_no_gauge() {
            var context = Context(1); var owner = new SimObject(context, Master.RightPlayer, "SeedNest", Vector3.Zero);
            var spawner = new Spawner(owner, 20, "SeedSpirit", 1, false, 1); spawner.OnDamaged(new Hit(100, 0, Element.None));
            Assert.Equal(20, spawner.Hp); Assert.Equal((Fix64)(-1), spawner.GetGauge().MaxValue);
        }
        [PortedTest(Source)] public static void a_burst_spreads_around_the_nest_in_half_unit_steps() {
            var context = Context(1); var owner = new SimObject(context, Master.RightPlayer, "SeedNest", new Vector3(5, 0, 5));
            new Spawner(owner, 20, "SeedSpirit", 1, true, 3).Update();
            Assert.Sequence(new[] { (Fix64)4.5m, (Fix64)5, (Fix64)5.5m }, context.PendingObjects.Select(item => item.Position.X));
        }
        [PortedTest(SequenceSource)] public static void spawns_on_start_constructor_spawns_on_first_update_then_waits_a_full_interval_for_the_next() {
            var context = Context((Fix64)0.5m); var owner = new SimObject(context, Master.LeftPlayer, "DimensionToad", Vector3.Zero);
            var spawner = new LimitedSequenceSpawner(owner, 2, 0, new[] { "FireTadpole" }, true);
            spawner.Update(); Assert.Equal(1, context.PendingObjects.Length);
            for (int i = 0; i < 3; i++) spawner.Update(); Assert.Equal(1, context.PendingObjects.Length);
            spawner.Update(); Assert.Equal(2, context.PendingObjects.Length);
        }
        [PortedTest(SequenceSource)] public static void legacy_constructor_still_waits_a_full_interval_before_its_first_spawn() {
            var context = Context((Fix64)0.5m); var owner = new SimObject(context, Master.LeftPlayer, "DimensionToad", Vector3.Zero);
            var spawner = new LimitedSequenceSpawner(owner, 2, 0, new[] { "FireTadpole" });
            for (int i = 0; i < 3; i++) spawner.Update(); Assert.Equal(0, context.PendingObjects.Length);
            spawner.Update(); Assert.Equal(1, context.PendingObjects.Length);
        }
        [PortedTest(SequenceSource)] public static void stops_at_the_maximum_and_cycles_prefab_types_and_offsets() {
            var context = Context(1); var owner = new SimObject(context, Master.LeftPlayer, "DimensionToad", new Vector3(5, 1, 5));
            var spawner = new LimitedSequenceSpawner(owner, 1, 3, new[] { "FireTadpole", "FireSlime" }); for (int i = 0; i < 5; i++) spawner.Update();
            var created = context.PendingObjects; Assert.Sequence(new[] { "FireTadpole", "FireSlime", "FireTadpole" }, created.Select(item => item.Prefab));
            Assert.Near((Fix64)(-0.35m), created[0].Position.X - 5, (Fix64)0.000001m); Assert.Near((Fix64)(-0.175m), created[0].Position.Y - 1, (Fix64)0.000001m);
            Assert.Equal(Fix64.Zero, created[1].Position.X - 5); Assert.Near((Fix64)0.35m, created[2].Position.X - 5, (Fix64)0.000001m);
        }
    }
}
