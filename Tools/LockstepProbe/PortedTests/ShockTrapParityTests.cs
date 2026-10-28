using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class ShockTrapParityTests {
        private const string Source = "game/domain/object/component/magic/shock_trap_detector.rs";
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext(1) { DeltaTime = 2 };
            public readonly SimObject Trap, Enemy;
            public readonly ShockTrapDetector Detector;
            public Fixture(bool receiver = true, bool damageable = true) {
                Context.RegisterPrefab("Object", _ => { }); Trap = Context.Spawn(Master.LeftPlayer, "Object", Vector3.Zero); Enemy = Context.Spawn(Master.RightPlayer, "Object", Vector3.Right);
                Enemy.AddCollider(new CircleCollider(Enemy, (Fix64)0.5m)); if (damageable) Enemy.AddComponentImmediately(new DummyMob(Enemy, 10)); if (receiver) Enemy.AddComponentImmediately(new CommonEffectReceiver(Enemy));
                Context.FlushObjects(); Detector = new ShockTrapDetector(Trap, 3, 2, (Fix64)1.5m);
            }
            public bool Stunned => Enemy.GetComponent<CommonEffectReceiver>()?.GetEffectByKey(StatusEffectKey.TrapStunReceive) != null;
            public void Fire() { Detector.Update(); Detector.Update(); }
            public void Leave() { Enemy.SetPosition(new Vector3(10, 0, 5)); }
            public void Event() { var events = Context.DrainEvents(); Assert.Equal(1, events.Length); Assert.Equal("shock", events[0].Type); Assert.Equal(Trap.Id, events[0].SourceId); Assert.Equal(0, events[0].TargetId); }
        }
        [PortedTest(Source)] public static void stuns_targets_still_in_range_when_trigger_delay_elapses() { var f = new Fixture(); f.Detector.Update(); Assert.True(!f.Stunned); f.Detector.Update(); Assert.True(f.Stunned); }
        [PortedTest(Source)] public static void does_not_stun_a_target_that_left_before_trigger_delay_elapses() { var f = new Fixture(); f.Detector.Update(); f.Leave(); f.Detector.Update(); Assert.True(!f.Stunned); }
        [PortedTest(Source)] public static void emits_exactly_one_shock_event_carrying_the_trap_id_when_the_trigger_fires() { var f = new Fixture(); f.Detector.Update(); Assert.Equal(0, f.Context.DrainEvents().Length); f.Detector.Update(); f.Event(); }
        [PortedTest(Source)] public static void emits_a_shock_event_even_when_the_enemy_left_before_trigger_delay_elapsed() { var f = new Fixture(); f.Detector.Update(); f.Leave(); f.Detector.Update(); Assert.True(!f.Stunned); f.Event(); }
        [PortedTest(Source)] public static void adds_the_arming_effect_when_an_enemy_enters_range_and_removes_it_when_the_trigger_fires() { var f = new Fixture(); f.Detector.Update(); Assert.True(f.Trap.HasEffect(VisualEffect.ShockTrapArming)); f.Detector.Update(); Assert.True(!f.Trap.HasEffect(VisualEffect.ShockTrapArming)); }
        [PortedTest(Source)] public static void removes_the_arming_effect_when_the_trap_is_destroyed_mid_countdown() { var f = new Fixture(false); f.Detector.Update(); Assert.True(f.Trap.HasEffect(VisualEffect.ShockTrapArming)); f.Detector.OnDestroy(); Assert.True(!f.Trap.HasEffect(VisualEffect.ShockTrapArming)); }
        [PortedTest(Source)] public static void destroys_itself_when_the_trigger_fires_and_does_not_stun_a_second_time() { var f = new Fixture(); f.Fire(); Assert.True(f.Trap.IsDestroyed); f.Event(); f.Detector.Update(); Assert.Equal(0, f.Context.DrainEvents().Length); }
        [PortedTest(Source)] public static void does_not_destroy_itself_before_the_trigger_fires() { var f = new Fixture(); f.Detector.Update(); Assert.True(!f.Trap.IsDestroyed); }
        [PortedTest(Source)] public static void an_enemy_without_a_receiver_gets_a_stun_effect_added_directly() { var f = new Fixture(false); f.Fire(); Assert.Equal(1, f.Enemy.GetPendingComponents<StunStatusEffect>().Length); }
        [PortedTest(Source)] public static void ignores_an_enemy_field_without_hit_points() { var f = new Fixture(false, false); f.Fire(); Assert.True(!f.Trap.HasEffect(VisualEffect.ShockTrapArming)); Assert.Equal(0, f.Context.DrainEvents().Length); Assert.True(!f.Trap.IsDestroyed); }
        public static void CheckPrefabAndConcurrentStuns() {
            var f = new Fixture(false); ShockTrapDetector.Register(f.Context);
            foreach (var key in new[] { "mass", "radius", "trigger_delay" }) f.Context.GameParameters["shock_trap." + key] = 1;
            f.Context.GameParameters["shock_trap.hp"] = 10; f.Context.GameParameters["shock_trap.effect_radius"] = 3; f.Context.GameParameters["shock_trap.stun_duration"] = (Fix64)1.5m;
            var trap = f.Context.Spawn(Master.LeftPlayer, "ShockTrap", Vector3.Zero); Assert.True(!trap.IsDestroyed && trap.HasComponent<BuildingEffectReceiver>()); Assert.True(!trap.HasComponent<TimedSelfDestroyer>());
            var detector = trap.GetComponent<ShockTrapDetector>(); detector.Update(); detector.Update(); f.Fire(); Assert.Equal(1, f.Enemy.GetPendingComponents<StunStatusEffect>().Length);
            f.Enemy.FlushComponents(); Assert.True(f.Enemy.HasEffect(VisualEffect.Shock)); Assert.Equal((Fix64)1.5m, f.Enemy.GetComponent<StunStatusEffect>().Remaining);
            f.Context.DeltaTime = (Fix64)1.5m; f.Enemy.GetComponent<StunStatusEffect>().Update(); f.Enemy.FlushComponents(); Assert.True(!f.Enemy.HasEffect(VisualEffect.Shock));
        }
    }
}
