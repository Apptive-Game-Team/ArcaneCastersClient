using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class GrassSpreadParityTests {
        private const string Source = "game/domain/object/component/magic/grass_spread.rs";
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext { DeltaTime = 2 };
            public readonly SimObject Building;
            public readonly GrassSpread Spread;
            public Fixture(Fix64? radius = null, Vector3? center = null, bool timers = false) {
                Context.RegisterPrefab("Object", _ => { }); Context.RegisterPrefab("LeafField", o => { if (timers) o.AddComponentImmediately(new TimedSelfDestroyer(o, 3)); });
                Building = Context.Spawn(Master.LeftPlayer, "Object", center ?? Vector3.Zero); Context.FlushObjects(); Spread = new GrassSpread(Building, 2, radius ?? (Fix64)5, 4, (Fix64)0.5m);
            }
            public SimObject[] Fields => Context.PendingObjects.Where(o => o.Prefab == "LeafField").ToArray();
            public void Tick(int count) { for (int i = 0; i < count; i++) Spread.Update(); }
        }
        [PortedTest(Source)] public static void spreads_exactly_one_leaf_field_per_attack_interval() { var f = new Fixture(); f.Context.DeltaTime = (Fix64)0.5m; f.Tick(3); Assert.Equal(0, f.Fields.Length); f.Tick(1); Assert.Equal(1, f.Fields.Length); f.Tick(3); Assert.Equal(1, f.Fields.Length); f.Tick(1); Assert.Equal(2, f.Fields.Length); }
        [PortedTest(Source)] public static void a_stalled_frame_catches_up_instead_of_dropping_spreads() { var f = new Fixture(); f.Context.DeltaTime = 7; f.Tick(1); Assert.Equal(3, f.Fields.Length); }
        [PortedTest(Source)] public static void every_spread_position_stays_within_radius_of_the_building_as_leaf_field() {
            var center = new Vector3(10, 0, -3); var f = new Fixture(center: center); f.Tick(12); Assert.Equal(12, f.Fields.Length);
            foreach (var o in f.Fields) { Assert.Equal("LeafField", o.Prefab); Assert.True(Vector3.Distance(o.Position, center) <= (Fix64)5.01m); }
        }
        [PortedTest(Source)] public static void stops_spreading_once_the_building_is_destroyed() { var f = new Fixture(); f.Tick(1); f.Building.Destroy(); f.Tick(2); Assert.Equal(1, f.Fields.Length); }
        [PortedTest(Source)] public static void fills_innermost_ring_first_in_slot_order() {
            var f = new Fixture(); f.Tick(5); Assert.Equal(5, f.Fields.Length);
            var expected = new[] { new Vector3((Fix64)1.75m, 0, 0), new Vector3(0, 0, (Fix64)1.75m), new Vector3((Fix64)(-1.75m), 0, 0), new Vector3(0, 0, (Fix64)(-1.75m)), new Vector3((Fix64)2.474873734m, 0, (Fix64)2.474873734m) };
            for (int i = 0; i < expected.Length; i++) Assert.Near(0, Vector3.Distance(expected[i], f.Fields[i].Position), (Fix64)0.00001m);
        }
        [PortedTest(Source)] public static void planted_fields_survive_far_past_leaf_field_duration_while_the_generator_lives() {
            var f = new Fixture(timers: true); f.Tick(1); var field = f.Fields[0]; var timer = field.GetComponent<TimedSelfDestroyer>(); timer.Update();
            for (int i = 0; i < 30; i++) { f.Tick(1); timer.Update(); } Assert.True(!field.IsDestroyed);
        }
        [PortedTest(Source)] public static void fields_own_timer_resumes_and_destroys_after_the_generator_is_destroyed() {
            var f = new Fixture(timers: true); f.Tick(1); var field = f.Fields[0]; var timer = field.GetComponent<TimedSelfDestroyer>(); timer.Update(); f.Building.Destroy(); timer.Update(); Assert.True(field.IsDestroyed);
        }
        [PortedTest(Source)] public static void destroyed_field_frees_exactly_its_slot_and_the_next_spread_refills_it() {
            var f = new Fixture((Fix64)1.75m); f.Tick(4); Assert.Equal(4, f.Fields.Length); var position = f.Fields[2].Position; f.Fields[2].Destroy(); f.Tick(1); Assert.Equal(5, f.Fields.Length); Assert.Equal(position, f.Fields[4].Position);
        }
        [PortedTest(Source)] public static void skips_a_slot_covered_by_a_burn_effect_provider_and_plants_it_once_it_is_gone() {
            var f = new Fixture(); var fire = f.Context.Spawn(Master.None, "Object", new Vector3((Fix64)1.75m, 0, 0)); fire.AddCollider(new CircleCollider(fire, (Fix64)0.5m, true)); fire.AddComponentImmediately(new EffectProvider(fire, VisualEffect.Burn)); f.Context.FlushObjects();
            f.Tick(1); Assert.Near(0, Vector3.Distance(new Vector3(0, 0, (Fix64)1.75m), f.Fields[0].Position), (Fix64)0.00001m);
            f.Context.RemoveObject(fire); f.Tick(1); Assert.Equal(2, f.Fields.Length); Assert.Near(0, Vector3.Distance(new Vector3((Fix64)1.75m, 0, 0), f.Fields[1].Position), (Fix64)0.00001m);
        }
        [PortedTest("game/domain/object/prefab/implement/build/tests.rs")] public static void wires_configured_grass_generator_parameters_into_its_components() {
            var c = new SimulationContext(); GrassSpread.RegisterPrefab(c); c.InitializationFailed += (o, e) => throw e;
            c.GameParameters["grass_generator.mass"] = 99999; c.GameParameters["grass_generator.radius"] = 5; c.GameParameters["grass_generator.effect_radius"] = 7;
            c.GameParameters["grass_generator.hp"] = 60; c.GameParameters["grass_generator.attack_interval"] = 3; c.GameParameters["grass_generator.quantity"] = 6;
            c.GameParameters["grass_generator.duration"] = 20; c.GameParameters["leaf_field.radius"] = (Fix64)0.5m;
            var o = c.Spawn(Master.LeftPlayer, "GrassGenerator", Vector3.Zero);
            Assert.True(o.HasComponent<RigidBody>() && o.HasComponent<DummyMob>() && o.HasComponent<GrassSpread>() && o.HasComponent<BuildingEffectReceiver>());
            Assert.Equal((Fix64)7, o.GetComponent<SeedSpiritVineSummoner>().Radius); Assert.Equal((Fix64)5, o.FirstCircleRadius(false).Value);
            var gauge = o.GetComponent<TimedSelfDestroyer>().GetGauge(); Assert.Equal((Fix64)20, gauge.MaxValue); Assert.Equal(GaugeCategory.Ttl, gauge.Category); Assert.True(o.Elements.Has(Element.Nature));
        }
    }
}
