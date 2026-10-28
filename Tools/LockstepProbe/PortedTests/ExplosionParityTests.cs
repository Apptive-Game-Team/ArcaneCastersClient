using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class ExplosionParityTests {
        private const string Source = "game/domain/object/component/magic/explode.rs";
        private const string ShockSource = "game/domain/object/component/magic/shock_overload_explosion.rs";
        private sealed class Recorder : SimComponent, IDamageable {
            public readonly List<Hit> Hits = new List<Hit>(); public Recorder(SimObject owner) : base(owner) { }
            public void OnDamaged(Hit hit) { Hits.Add(hit); } public void OnDamagedWithDelay(Hit hit, Fix64 delay) { Hits.Add(hit); }
        }
        private static SimulationContext Context() { var c = new SimulationContext(1) { DeltaTime = (Fix64)0.5m }; c.RegisterPrefab("Object", _ => { }); return c; }
        private static SimObject Object(SimulationContext c, Master master, Vector3 position) { var o = c.Spawn(master, "Object", position); o.AddCollider(new CircleCollider(o, (Fix64)0.1m)); c.FlushObjects(); return o; }
        public static void CheckPrefabDamageAndElements() {
            var c = Context(); ExplosionPrefabs.Register(c);
            var prefabs = new[] { "FireExplode", "RockExplode", "WindExplode", "WaterExplode", "LeafExplode", "ElectricExplode" };
            var keys = new[] { "fire_explode", "rock_explode", "wind_explode", "water_explode", "leaf_explode", "electric_explode" };
            var elements = new[] { Element.Fire, Element.Rock, Element.Wind, Element.Water, Element.Nature, Element.Lightning };
            var ally = Object(c, Master.LeftPlayer, new Vector3(5, 0, 5)); var receiver = new Recorder(ally); ally.AddComponentImmediately(receiver);
            for (int i = 0; i < prefabs.Length; i++) {
                c.GameParameters[keys[i] + ".radius"] = 2; c.GameParameters[keys[i] + ".damage"] = 6;
                var owner = c.Spawn(Master.LeftPlayer, prefabs[i], new Vector3(5, 0, 5)); Assert.True(!owner.IsDestroyed); Assert.True(owner.Colliders[0].IsTrigger); Assert.True(owner.Elements.Has(elements[i]));
                if (elements[i] == Element.Wind) Assert.True(owner.HasComponent<KnockbackEffectProvider>());
                var explosion = owner.GetComponent<Explode>(); Assert.True(explosion != null); explosion.Update(); Assert.Equal(i, receiver.Hits.Count); explosion.Update(); explosion.Update(); Assert.Equal(i + 1, receiver.Hits.Count); Assert.Sequence(new[] { elements[i] }, receiver.Hits[i].Elements);
            }
        }
        [PortedTest(Source)] public static void waits_out_the_delay_then_damages_everything_damageable_in_range_and_destroys_itself() {
            var c = Context(); c.DeltaTime = (Fix64)0.25m; var owner = Object(c, Master.LeftPlayer, Vector3.Zero); owner.Elements.AddNative(Element.Rock); var explode = new Explode(owner, 6);
            var target = Object(c, Master.RightPlayer, Vector3.Right); var receiver = new Recorder(target); target.AddComponentImmediately(receiver); Object(c, Master.RightPlayer, Vector3.Right);
            explode.Update(); explode.Update(); Assert.Equal(0, receiver.Hits.Count); Assert.True(!owner.IsDestroyed); explode.Update();
            Assert.Equal(1, receiver.Hits.Count); Assert.Equal(6, receiver.Hits[0].Damage); Assert.Equal(owner.Id, receiver.Hits[0].AttackerId); Assert.True(receiver.Hits[0].Elements.Contains(Element.Rock)); Assert.True(owner.IsDestroyed); Assert.Equal(ObjectStatus.Damaged, target.Status);
        }
        [PortedTest(Source)] public static void reports_its_radius_through_the_area_explode_trait() { var c = Context(); var owner = Object(c, Master.LeftPlayer, Vector3.Zero); owner.AddComponentImmediately(new Explode(owner, 1, (Fix64)2.5m)); Assert.Equal((Fix64)2.5m, owner.GetComponent<IAreaExplode>().Radius); }
        [PortedTest("game/domain/object/component/magic/water_explode.rs")] public static void scales_vertical_impulse_by_inverse_square_root_of_mass() {
            var c = Context(); c.DeltaTime = (Fix64)0.1m; var owner = Object(c, Master.LeftPlayer, Vector3.Zero); var target = Object(c, Master.RightPlayer, Vector3.Zero);
            target.AddComponentImmediately(new RigidBody(target, 10)); var physics = new ZPhysics(target); target.AddComponentImmediately(physics);
            new WaterExplode(owner, 3, (Fix64)1.5m, 10).HandleGameObject(target); physics.ApplyZForce();
            Assert.Near((10 / Fix64.Sqrt(10) - physics.Gravity * c.DeltaTime) * c.DeltaTime, target.Position.Y, (Fix64)0.0001m);
        }
        private sealed class Fixture {
            public readonly SimulationContext Context = ExplosionParityTests.Context(); public readonly SimObject Owner, Marked; public readonly Recorder Receiver; public readonly ShockOverloadExplosion Explosion;
            public Fixture() { Owner = Object(Context, Master.LeftPlayer, Vector3.Zero); Owner.Elements.AddNative(Element.Lightning); Marked = Object(Context, Master.RightPlayer, Vector3.Right); Receiver = new Recorder(Marked); Marked.AddComponentImmediately(Receiver); Explosion = new ShockOverloadExplosion(Owner, 10, 4); }
        }
        [PortedTest(ShockSource)] public static void primary_explosion_marks_exactly_the_enemies_it_damages_instead_of_stunning_them() { var f = new Fixture(); f.Explosion.Update(); Assert.Equal(1, f.Receiver.Hits.Count); Assert.Equal(10, f.Receiver.Hits[0].Damage); Assert.Equal(f.Owner.Id, f.Receiver.Hits[0].AttackerId); Assert.True(f.Marked.HasEffect(VisualEffect.Shock)); Assert.True(!f.Owner.IsDestroyed); }
        [PortedTest(ShockSource)] public static void primary_explosion_skips_allies() { var f = new Fixture(); var ally = Object(f.Context, Master.LeftPlayer, Vector3.Zero); var recorder = new Recorder(ally); ally.AddComponentImmediately(recorder); f.Explosion.Update(); Assert.Equal(0, recorder.Hits.Count); Assert.True(!ally.HasEffect(VisualEffect.Shock)); Assert.Equal(1, f.Receiver.Hits.Count); }
        [PortedTest(ShockSource)] public static void secondary_explosion_uses_current_target_position_and_reduced_damage_and_radius() {
            var f = new Fixture(); f.Explosion.Update(); var moved = new Vector3(7, 0, 3); f.Marked.SetPosition(moved);
            var nearby = Object(f.Context, Master.RightPlayer, moved); var recorder = new Recorder(nearby); nearby.AddComponentImmediately(recorder); f.Explosion.Update(); f.Explosion.Update();
            Assert.True(!f.Marked.HasEffect(VisualEffect.Shock)); var events = f.Context.DrainEvents(); Assert.Equal(1, events.Length); Assert.Equal("ShockOverloadSecondary", events[0].Type); Assert.Equal((Fix64)0.8m, events[0].Duration); Assert.Equal((Vector3?)moved, events[0].StartPosition);
            Assert.Equal(1, recorder.Hits.Count); Assert.Equal(5, recorder.Hits[0].Damage); Assert.Sequence(new[] { Element.Lightning }, recorder.Hits[0].Elements); Assert.Equal(f.Owner.Id, recorder.Hits[0].AttackerId); Assert.True(f.Owner.IsDestroyed);
        }
        [PortedTest(ShockSource)] public static void destroyed_marked_target_does_not_create_a_secondary_explosion() { var f = new Fixture(); f.Explosion.Update(); f.Marked.Destroy(); f.Explosion.Update(); f.Explosion.Update(); Assert.True(!f.Marked.HasEffect(VisualEffect.Shock)); Assert.Equal(0, f.Context.DrainEvents().Length); Assert.Equal(1, f.Receiver.Hits.Count); Assert.True(f.Owner.IsDestroyed); }
    }
}
