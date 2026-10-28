using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;

namespace ArcaneCasters.Parity {
    public static class PveObjectiveParityTests {
        private const string Source = "tests/pve_engine.rs";
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext(0);
            public readonly PveRuntimeState Runtime = new PveRuntimeState();
            public readonly PveResultChecker Checker;
            public readonly PveObjectiveSystem System;
            public readonly List<KeyValuePair<long, PveObjectiveState>> Sent = new List<KeyValuePair<long, PveObjectiveState>>();
            public Fixture() { Context.RegisterPrefab("ZapMouse", _ => { }); Checker = new PveResultChecker { Runtime = Runtime }; System = new PveObjectiveSystem(Checker, 1, 2, (id, state) => Sent.Add(new KeyValuePair<long, PveObjectiveState>(id, state))); }
            public SimObject Install(string id) { var owner = Context.Spawn(Master.RightPlayer, "ZapMouse", Vector3.Zero); Runtime.Register(id, owner); return owner; }
            public PveObjectiveState[] To(long id) => Sent.Where(item => item.Key == id).Select(item => item.Value).ToArray();
            public void Update(int frame) { Context.FrameNumber = frame; System.Update(Context); }
        }
        [PortedTest(Source)] public static void first_update_sends_to_both_users_with_type_field() {
            var f = new Fixture(); f.Checker.SetObjectiveInstallerIds(new[] { "a", "b" }); f.Install("a"); f.Update(0);
            Assert.Equal(1, f.To(1).Length); Assert.Equal(1, f.To(2).Length); var state = f.To(1)[0];
            Assert.Equal("pveObjective", state.Type); Assert.Equal(PveWinCondition.DestroyObjectives, state.WinCondition); Assert.Equal(0, state.SurviveSeconds); Assert.Equal(0, state.RemainingSeconds); Assert.Equal(2, state.ObjectivesTotal); Assert.Equal(2, state.ObjectivesRemaining);
        }
        [PortedTest(Source)] public static void does_not_resend_while_nothing_changes() {
            var f = new Fixture(); f.Checker.SetObjectiveInstallerIds(new[] { "a" }); f.Install("a"); foreach (int frame in new[] { 0, 1, 100, 200 }) f.Update(frame); Assert.Equal(2, f.Sent.Count);
        }
        [PortedTest(Source)] public static void send_current_to_answers_one_user_without_disturbing_change_detection() {
            var f = new Fixture(); f.Checker.SetObjectiveInstallerIds(new[] { "a", "b" }); f.Install("a"); f.Update(0); f.Sent.Clear();
            f.Context.FrameNumber = 5; f.System.SendCurrentTo(f.Context, 3); Assert.Equal(1, f.Sent.Count); Assert.Equal(0, f.To(1).Length);
            var state = f.To(3)[0]; Assert.Equal("pveObjective", state.Type); Assert.Equal(2, state.ObjectivesTotal); Assert.Equal(2, state.ObjectivesRemaining); f.Update(6); Assert.Equal(1, f.Sent.Count);
        }
        [PortedTest(Source)] public static void send_current_to_before_the_first_update_does_not_suppress_the_first_broadcast() {
            var f = new Fixture(); f.Checker.SetObjectiveInstallerIds(new[] { "a" }); f.Install("a"); f.System.SendCurrentTo(f.Context, 1); f.Update(0); Assert.Equal(2, f.To(1).Length);
        }
        [PortedTest(Source)] public static void resends_when_an_objective_is_destroyed() {
            var f = new Fixture(); f.Checker.SetObjectiveInstallerIds(new[] { "a", "b" }); var a = f.Install("a"); f.Install("b"); f.Update(0); a.Destroy(); f.Update(1); f.Update(2);
            Assert.Sequence(new[] { 2, 1 }, f.To(1).Select(state => state.ObjectivesRemaining)); Assert.Equal(2, f.To(1)[1].ObjectivesTotal);
        }
        [PortedTest(Source)] public static void survive_counts_down_rounding_up_and_resends_once_per_second() {
            var f = new Fixture(); f.Checker.ConfigureRules(new PveScenarioRules(PveWinCondition.Survive, 10)); foreach (int frame in new[] { 0, 1, 19, 20, 199, 200, 250 }) f.Update(frame);
            Assert.Sequence(new[] { 10, 9, 1, 0 }, f.To(1).Select(state => state.RemainingSeconds));
            foreach (var state in f.To(1)) { Assert.Equal(PveWinCondition.Survive, state.WinCondition); Assert.Equal(10, state.SurviveSeconds); Assert.Equal(0, state.ObjectivesTotal); Assert.Equal(0, state.ObjectivesRemaining); }
        }
    }
}
