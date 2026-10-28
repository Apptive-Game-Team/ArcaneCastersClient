using System;
using System.Collections.Generic;
using ArcaneCasters.Simulation;
using BEPUutilities;

namespace ArcaneCasters.Parity {
    public static class PveMatchParityTests {
        private const string Source = "tests/pve_engine.rs";
        [PortedTest("game/service/pve_loop.rs")] public static void init_installs_the_default_scenario_and_registers_the_pve_sync_service() {
            var runtime = BattleSessionChecks.Create(SessionType.PVE,1,2);
            var sink = new Sink(); long requestedId = 0;
            runtime.InitializePve(new PveScenarioRegistry(id => {
                requestedId = id;
                return new PveScenario(Array.Empty<PveScenarioEvent>(),installers: new[] { new PveInstallObject("marker","Wall",Master.None,Vector3.Zero) },rules: new PveScenarioRules(PveWinCondition.Survive,30));
            }),sink);
            Assert.Equal(1L,requestedId); Assert.True(!runtime.Loop.HasTimeLimit);
            Assert.True(runtime.Pve.Installer.GetInstalledObject("marker") != null);
            Assert.Equal(PveWinCondition.Survive,runtime.Pve.ResultChecker.WinCondition);
            var sync = runtime.Context.Service<PveSync>(); Assert.True(sync != null);
            sync.SendPveStateTo(runtime.Context,1,0);
            Assert.Equal(1,sink.Sent.Count); Assert.Equal(1L,sink.Sent[0].Key);
            Assert.Equal(PveWinCondition.Survive,((PveObjectiveState)sink.Sent[0].Value).WinCondition);
        }
        [PortedTest("game/service/pve_loop.rs")] public static void an_unknown_scenario_fails_the_session() {
            var runtime = BattleSessionChecks.Create(SessionType.PVE,1,2,9);
            bool rejected = false;
            try { runtime.InitializePve(new PveScenarioRegistry(_ => null),new Sink()); }
            catch (ArgumentException error) { Assert.Equal("Unknown scenarioId: 9",error.Message); rejected = true; }
            Assert.True(rejected); Assert.True(runtime.Pve == null); Assert.True(runtime.Context.Service<PveSync>() == null);
            rejected = false;
            try { runtime.AdvanceConfirmedFrame(1); } catch (InvalidOperationException) { rejected = true; }
            Assert.True(rejected); Assert.Equal(0,runtime.Context.FrameNumber);
        }
        [PortedTest(Source)] public static void game_end_helpers_match_handle_game_end() {
            Assert.Equal(false, new Fixture().Match.HasTimeLimit);
            var mmr = PveMatch.GameEndMmr(); Assert.Equal((short)0, mmr.LastLeftMmr); Assert.Equal((short)0, mmr.LastRightMmr); Assert.Equal((short)0, mmr.NewLeftMmr); Assert.Equal((short)0, mmr.NewRightMmr);
            Assert.Sequence(new long[] { 4 }, PveMatch.UsersToMarkOnline(4, -1)); Assert.Sequence(new long[] { 4, 9 }, PveMatch.UsersToMarkOnline(4, 9)); Assert.Equal(0, PveMatch.UsersToMarkOnline(-1, -1).Length);
        }
        private sealed class Sink : IPveSessionSink {
            public long LeftUserId => 1;
            public long RightUserId => 2;
            public readonly List<KeyValuePair<long, object>> Sent = new List<KeyValuePair<long, object>>();
            public void SendObjective(long id, PveObjectiveState state) => Sent.Add(new KeyValuePair<long, object>(id, state));
            public void SendScriptEvent(long id, PveScriptEvent state) => Sent.Add(new KeyValuePair<long, object>(id, state));
            public void SendState(long id, PveChannelState state) => Sent.Add(new KeyValuePair<long, object>(id, state));
        }
        private static PveScenario Scenario(long id) => id != 1 && id != 5 ? null : new PveScenario(
            new[] { new PveScenarioEvent("intro", PveTriggerType.FrameNumGte, 0, key: "key.intro", lines: new[] { "hello" }) },
            new[] { "boss" }, new[] { new PveInstallObject("boss", "ZapMouse", Master.RightPlayer, Vector3.Zero) },
            id == 5 ? new PveScenarioRules(PveWinCondition.Survive, 30) : new PveScenarioRules());
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext(0);
            public readonly Sink Sink = new Sink();
            public readonly PveMatch Match;
            public Fixture(long? id = null) { Context.RegisterPrefab("ZapMouse", _ => { }); Match = new PveMatch(new PveScenarioRegistry(Scenario), id, Context, Sink, new PveScenarioComponents()); }
        }
        [PortedTest(Source)] public static void send_pve_state_to_sends_the_objective_then_the_missed_events_to_the_asking_user() {
            var f = new Fixture(); f.Match.BeforeResultCheck(f.Context, SessionType.PVE); f.Sink.Sent.Clear(); f.Match.SendPveStateTo(f.Context, 7, 0);
            Assert.Equal(2, f.Sink.Sent.Count); Assert.Equal(7L, f.Sink.Sent[0].Key); Assert.Equal(7L, f.Sink.Sent[1].Key);
            Assert.Equal(1, ((PveObjectiveState)f.Sink.Sent[0].Value).ObjectivesTotal); Assert.Equal(1, ((PveScriptEvent)f.Sink.Sent[1].Value).Seq);
            f.Sink.Sent.Clear(); f.Match.SendPveStateTo(f.Context, 7, 1); Assert.Equal(1, f.Sink.Sent.Count);
        }
        [PortedTest(Source)] public static void before_result_check_runs_only_for_pve_sessions() {
            var f = new Fixture(); f.Match.BeforeResultCheck(f.Context, SessionType.PVP); Assert.Equal(0, f.Sink.Sent.Count); f.Match.BeforeResultCheck(f.Context, SessionType.PVE);
            Assert.Equal(4, f.Sink.Sent.Count); Assert.True(f.Sink.Sent[0].Value is PveScriptEvent); Assert.True(f.Sink.Sent[2].Value is PveObjectiveState);
        }
        [PortedTest(Source)] public static void setup_uses_the_session_scenario_and_rules_and_clears_when_the_boss_dies() {
            var f = new Fixture(5); Assert.Equal(PveWinCondition.Survive, f.Match.ResultChecker.WinCondition); Assert.Equal(30, f.Match.ResultChecker.SurviveSeconds);
            var g = new Fixture(); Assert.Equal(PveWinCondition.DestroyObjectives, g.Match.ResultChecker.WinCondition); Assert.Equal(false, g.Match.ResultChecker.CheckResult(g.Context));
            g.Match.Installer.GetInstalledObject("boss").Destroy(); Assert.True(g.Match.ResultChecker.CheckResult(g.Context)); Assert.Equal<Master?>(Master.RightPlayer, g.Context.Result.Loser);
        }
        [PortedTest(Source)] public static void setup_with_an_unknown_scenario_fails_with_the_java_message() {
            try { new Fixture(99); throw new Exception("Expected unknown scenario"); } catch (ArgumentException error) { Assert.Equal("Unknown scenarioId: 99", error.Message); }
        }
        [PortedTest("game/service/pve/pve_scenario_registry.rs")] public static void unknown_scenario_id_fails_with_the_java_message() {
            var registry = new PveScenarioRegistry(id => id == 1 ? Scenario(1) : null); Assert.True(registry.GetScenario(1) != null);
            try { registry.GetScenario(7); throw new Exception("Expected unknown scenario"); } catch (ArgumentException error) { Assert.Equal("Unknown scenarioId: 7", error.Message); }
        }
    }
}
