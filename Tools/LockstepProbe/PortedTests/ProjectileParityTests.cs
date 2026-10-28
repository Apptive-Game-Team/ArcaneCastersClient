using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class ProjectileParityTests {
        private const string Push = "game/domain/object/component/physic/timed_mass_push.rs";
        private const string Lifetime = "game/domain/object/component/timed_self_destroyer.rs";
        private const string Distance = "game/domain/object/component/distance_self_destroyer.rs";
        private static SimObject Body(SimulationContext context, int mass) {
            var owner = new SimObject(context, Master.LeftPlayer, "FireSlime", new Vector3(5, 0, 5)); owner.AddComponent(new RigidBody(owner, mass)); owner.FlushComponents(); return owner;
        }
        [PortedTest(Push)] public static void scales_push_speed_by_inverse_square_root_of_mass() {
            var context = new SimulationContext(0) { DeltaTime = (Fix64)0.1m }; var source = Body(context, 1); var target = Body(context, 10);
            TimedMassPush.Apply(target, source, Vector3.Right, 3, (Fix64)0.5m); var pending = target.GetPendingComponents<TimedMassPush>(); Assert.Equal(1, pending.Length); pending[0].Update();
            Assert.Near((Fix64)3 / Fix64.Sqrt(10), target.GetComponent<RigidBody>().Velocity.X, (Fix64)0.0001m);
        }
        [PortedTest(Push)] public static void immovable_mass_rejects_push() {
            var context = new SimulationContext(0); var source = Body(context, 1); var target = Body(context, -1); TimedMassPush.Apply(target, source, Vector3.Right, 3, (Fix64)0.5m); Assert.Equal(0, target.PendingComponents.Length);
        }
        [PortedTest(Push)] public static void a_second_push_from_the_same_source_refreshes_the_first() {
            var context = new SimulationContext(0); var source = Body(context, 1); var target = Body(context, 1);
            TimedMassPush.Apply(target, source, Vector3.Right, 3, (Fix64)0.5m); TimedMassPush.Apply(target, source, Vector3.Left, 2, (Fix64)0.5m);
            var pending = target.GetPendingComponents<TimedMassPush>(); Assert.Equal(1, pending.Length); Assert.Equal(Vector3.Left, pending[0].Direction);
        }
        [PortedTest(Push)] public static void expires_after_its_duration() {
            var context = new SimulationContext(0) { DeltaTime = (Fix64)0.1m }; var source = Body(context, 1); var target = Body(context, 1);
            TimedMassPush.Apply(target, source, Vector3.Right, 3, (Fix64)0.15m); target.FlushComponents(); target.Update(); target.Update(); target.FlushComponents(); Assert.Equal(false, target.HasComponent<TimedMassPush>());
        }
        private sealed class Recorder : SimComponent, IDamageable {
            public int Hits;
            public Recorder(SimObject owner) : base(owner) { }
            public void OnDamaged(Hit hit) { Hits++; }
            public void OnDamagedWithDelay(Hit hit, Fix64 delay) { Hits++; }
        }
        [PortedTest("game/domain/object/component/magic/push_shot.rs")] public static void refreshes_mass_scaled_push_but_damages_only_once() {
            var context = new SimulationContext(0); var shot = new SimObject(context, Master.LeftPlayer, "TideCall", Vector3.Zero); var target = new SimObject(context, Master.RightPlayer, "FireSlime", Vector3.Right);
            var recorder = new Recorder(target); target.AddComponentImmediately(recorder); target.AddComponentImmediately(new RigidBody(target, 10));
            var component = new PushShot(shot, 4, 3); component.SetTarget(Vector3.Right); component.OnCollisionWithEnemy(target); component.OnCollisionWithEnemy(target);
            Assert.Equal(1, recorder.Hits); Assert.Equal(1, target.GetPendingComponents<TimedMassPush>().Length);
        }
        private static TimedSelfDestroyer Timed(int duration = 3, int delta = 1) {
            var context = new SimulationContext(0) { DeltaTime = delta }; var owner = new SimObject(context, Master.LeftPlayer, "DragonFlame", Vector3.Zero); return new TimedSelfDestroyer(owner, duration);
        }
        [PortedTest(Lifetime)] public static void does_not_destroy_before_its_time_to_live_elapses() { var t = Timed(); t.Update(); t.Update(); Assert.Equal(false, t.Owner.IsDestroyed); }
        [PortedTest(Lifetime)] public static void destroys_once_elapsed_time_reaches_time_to_live() { var t = Timed(); t.Update(); t.Update(); t.Update(); Assert.True(t.Owner.IsDestroyed); }
        [PortedTest(Lifetime)] public static void recover_with_no_arguments_resets_elapsed_time_to_zero() { var t = Timed(); t.Update(); t.Update(); t.Recover(); Assert.Equal((Fix64)3, t.GetGauge().Value); }
        [PortedTest(Lifetime)] public static void recover_by_never_goes_below_zero() { var t = Timed(); t.Update(); t.RecoverBy(5); Assert.Equal((Fix64)3, t.GetGauge().Value); t.RecoverBy(-1); Assert.Equal((Fix64)3, t.GetGauge().Value); }
        [PortedTest(Lifetime)] public static void gauge_reports_remaining_time_against_time_to_live_as_ttl_category() { var t = Timed(5, 2); t.Update(); var g = t.GetGauge(); Assert.Equal((Fix64)3, g.Value); Assert.Equal((Fix64)5, g.MaxValue); Assert.Equal(GaugeCategory.Ttl, g.Category); }
        [PortedTest(Lifetime)] public static void freeze_skips_the_next_tick_without_advancing_elapsed_time() { var t = Timed(10); t.Update(); t.Freeze(); t.Update(); Assert.Equal((Fix64)9, t.GetGauge().Value); }
        [PortedTest(Lifetime)] public static void freeze_never_rewinds_elapsed_time_even_when_several_sources_freeze_the_same_frame() { var t = Timed(10); t.Update(); t.Freeze(); t.Freeze(); t.Freeze(); t.Update(); Assert.Equal((Fix64)9, t.GetGauge().Value); }
        [PortedTest(Lifetime)] public static void freeze_holds_for_one_tick_only_so_decay_resumes_without_it() { var t = Timed(10); t.Update(); t.Freeze(); t.Update(); t.Update(); Assert.Equal((Fix64)8, t.GetGauge().Value); }
        [PortedTest(Lifetime)] public static void freeze_every_tick_keeps_the_object_alive_indefinitely() { var t = Timed(); t.Update(); t.Update(); for (int i = 0; i < 100; i++) { t.Freeze(); t.Update(); } Assert.Equal(false, t.Owner.IsDestroyed); Assert.Equal(Fix64.One, t.GetGauge().Value); }
        [PortedTest(Lifetime)] public static void a_frozen_tick_cannot_destroy_the_object() { var t = Timed(); t.Update(); t.Update(); t.Freeze(); t.Update(); Assert.Equal(false, t.Owner.IsDestroyed); }
        private static SimObject Moving() => new SimObject(new SimulationContext(0), Master.LeftPlayer, "DragonFlame", new Vector3(4, 0, 5));
        [PortedTest(Distance)] public static void survives_until_it_has_travelled_the_whole_distance() { var o = Moving(); var d = new DistanceSelfDestroyer(o, o.Position, 8); o.SetPosition(new Vector3(11, 0, 5)); d.Update(); Assert.Equal(false, o.IsDestroyed); }
        [PortedTest(Distance)] public static void destroys_the_object_once_it_reaches_the_distance() { var o = Moving(); var d = new DistanceSelfDestroyer(o, o.Position, 8); o.SetPosition(new Vector3(12, 0, 5)); d.Update(); Assert.True(o.IsDestroyed); }
        [PortedTest(Distance)] public static void keeps_its_origin_when_the_vector_it_was_given_is_mutated() { var o = Moving(); var start = o.Position; var d = new DistanceSelfDestroyer(o, start, 8); start += new Vector3(8, 0, 0); o.SetPosition(new Vector3(12, 0, 5)); d.Update(); Assert.True(o.IsDestroyed); }
    }
}
