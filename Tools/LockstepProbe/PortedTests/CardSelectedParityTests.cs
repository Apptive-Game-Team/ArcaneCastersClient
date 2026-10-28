using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class CardSelectedParityTests {
        private const string Source = "game/domain/object/component/effect/statuseffect/card_selected_status_effect.rs";
        private static SimObject Owner() { var c = new SimulationContext(); c.RegisterPrefab("Player", _ => { }); return c.Spawn(Master.LeftPlayer, "Player", Vector3.Zero); }
        [PortedTest(Source)] public static void unselecting_the_last_card_ends_the_effect() {
            var o = Owner(); var effect = new CardSelectedStatusEffect(o); o.AddComponentImmediately(effect); Assert.True(o.HasEffect(VisualEffect.CardSelected)); effect.Select(); effect.Unselect(); Assert.True(!effect.IsExpired); effect.Unselect(); Assert.True(effect.IsExpired && !o.HasEffect(VisualEffect.CardSelected));
        }
        [PortedTest(Source)] public static void a_cast_ends_the_effect() {
            var o = Owner(); var effect = new CardSelectedStatusEffect(o); o.AddComponentImmediately(effect); o.SetStatus(ObjectStatus.Attack); Assert.True(effect.IsExpired); o.FlushComponents(); Assert.True(!o.HasComponent<CardSelectedStatusEffect>());
        }
        [PortedTest(Source)] public static void it_never_runs_out_by_time() { var o = Owner(); o.Context.DeltaTime = 1000; var effect = new CardSelectedStatusEffect(o); effect.Update(); Assert.True(!effect.IsExpired); Assert.Equal(Fix64.MaxValue, effect.Remaining); }
    }
}
