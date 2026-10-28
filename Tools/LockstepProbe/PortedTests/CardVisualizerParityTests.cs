using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;

namespace ArcaneCasters.Parity {
    public static class CardVisualizerParityTests {
        private const string Source = "game/service/card_select_visualizer.rs";
        private sealed class Parser : IMagicLookup {
            private static readonly Element[] Elements = { Element.None, Element.Fire, Element.Water, Element.Nature, Element.Lightning, Element.Rock, Element.Wind };
            public BoundMagic ParseMagic(long user, long id) => GetMagic(id);
            public BoundMagic GetMagic(long id) { if (id != 1 && id != 2 && (id < 10 || id > 16)) return null; var element = id == 1 ? Element.None : id == 2 ? Element.Fire : Elements[id - 10]; return new BoundMagic(id, "m", (c, m, o, p) => { }, element: element); }
        }
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext(); public readonly SimObject Player; public readonly CardSelectVisualizer Visualizer = new CardSelectVisualizer(new Parser());
            public Fixture() { Context.RegisterPrefab("Player", _ => { }); Context.UserIds[Master.LeftPlayer] = 7; Context.UserIds[Master.RightPlayer] = 8; Player = Context.Spawn(Master.LeftPlayer, "Player", Vector3.Zero); Context.FlushObjects(); }
            public void Select(long id) => Visualizer.SelectCard(Context, 7, id);
            public void Unselect(long id) => Visualizer.UnselectCard(Context, 7, id);
        }
        [PortedTest(Source)] public static void every_element_marks_the_selection() { var f = new Fixture(); for (int id = 10; id <= 16; id++) { f.Select(id); Assert.True(f.Player.HasEffect(VisualEffect.CardSelected)); f.Unselect(id); f.Player.FlushComponents(); Assert.True(!f.Player.HasEffect(VisualEffect.CardSelected)); } }
        [PortedTest(Source)] public static void an_elementless_magic_still_marks_the_selection() { var f = new Fixture(); f.Select(1); Assert.Sequence(new[] { VisualEffect.CardSelected }, f.Player.Effects); }
        [PortedTest(Source)] public static void an_element_magic_keeps_its_idle_aura() { var f = new Fixture(); f.Select(2); Assert.True(f.Player.HasEffect(VisualEffect.FireIdleAura) && f.Player.HasEffect(VisualEffect.CardSelected)); f.Unselect(2); Assert.True(!f.Player.HasEffect(VisualEffect.FireIdleAura) && !f.Player.HasEffect(VisualEffect.CardSelected)); }
        [PortedTest(Source)] public static void the_marker_stays_until_the_last_selected_card_goes() { var f = new Fixture(); f.Select(2); f.Select(1); f.Unselect(2); Assert.True(f.Player.HasEffect(VisualEffect.CardSelected)); f.Unselect(1); Assert.True(!f.Player.HasEffect(VisualEffect.CardSelected)); }
        [PortedTest(Source)] public static void a_successful_cast_clears_the_marker() { var f = new Fixture(); f.Select(1); f.Player.FlushComponents(); f.Player.SetStatus(ObjectStatus.Attack); Assert.True(!f.Player.HasEffect(VisualEffect.CardSelected)); }
        [PortedTest(Source)] public static void an_unknown_magic_id_still_marks_the_selection_and_the_rejected_cast_clears_it() { var f = new Fixture(); f.Select(99); Assert.Sequence(new[] { VisualEffect.CardSelected }, f.Player.Effects); f.Player.FlushComponents(); f.Player.SetStatus(ObjectStatus.Hindered); Assert.True(!f.Player.HasEffect(VisualEffect.CardSelected)); }
        [PortedTest(Source)] public static void a_card_selected_in_the_same_frame_as_a_cast_builds_a_fresh_marker() { var f = new Fixture(); f.Select(1); f.Player.FlushComponents(); f.Player.SetStatus(ObjectStatus.Attack); f.Select(1); Assert.True(f.Player.HasEffect(VisualEffect.CardSelected)); f.Unselect(1); Assert.True(!f.Player.HasEffect(VisualEffect.CardSelected)); }
    }
}
