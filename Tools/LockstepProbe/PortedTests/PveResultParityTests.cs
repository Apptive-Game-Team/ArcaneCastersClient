using ArcaneCasters.Simulation;
using BEPUutilities;

namespace ArcaneCasters.Parity {
    public static class PveResultParityTests {
        private const string Source = "tests/pve_engine.rs";
        public static void CheckSharedResultAndRate() {
            var f = new Fixture(); f.Context.SetTickRate(60); f.Checker.ConfigureRules(new PveScenarioRules(PveWinCondition.Survive, 10));
            f.Context.FrameNumber = 599; Assert.Equal(false, f.Checker.CheckResult(f.Context));
            f.Context.FrameNumber = 600; f.Context.Result.SetLoser(Master.LeftPlayer);
            Assert.True(f.Checker.CheckResult(f.Context)); Assert.Equal<Master?>(Master.LeftPlayer, f.Checker.Loser);
            var g = new Fixture(); g.Checker.SetObjectiveInstallerIds(new[] { "a", "b" });
            Assert.Equal(2, g.Checker.ObjectivesTotal); Assert.Equal(2, g.Checker.CountObjectivesRemaining());
            g.Boss("a").Destroy(); Assert.Equal(1, g.Checker.CountObjectivesRemaining());
            g.Boss("a"); Assert.Equal(2, g.Checker.CountObjectivesRemaining());
            g.Checker.ConfigureRules(new PveScenarioRules(PveWinCondition.Survive, 10)); Assert.Equal(600, g.Checker.GetSurviveFrameThreshold(60));
            g.Checker.ConfigureRules(); Assert.Equal(0, g.Checker.SurviveSeconds); Assert.Equal(int.MaxValue, g.Checker.GetSurviveFrameThreshold(60));
        }
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext(0);
            public readonly PveRuntimeState Runtime = new PveRuntimeState();
            public readonly PveResultChecker Checker;
            public Fixture() { Context.RegisterPrefab("ZapMouse", _ => { }); Checker = new PveResultChecker(Context.Result) { Runtime = Runtime }; }
            public SimObject Boss(string id) { var owner = Context.Spawn(Master.RightPlayer, "ZapMouse", Vector3.Zero); Runtime.Register(id, owner); return owner; }
        }
        [PortedTest(Source)] public static void left_player_death_fails_regardless_of_objectives() {
            var f = new Fixture(); f.Checker.SetObjectiveInstallerIds(new[] { "boss" }); f.Checker.SetLoser(Master.LeftPlayer); Assert.True(f.Checker.CheckResult(f.Context)); Assert.Equal<Master?>(Master.LeftPlayer, f.Checker.Loser);
        }
        [PortedTest(Source)] public static void destroy_objectives_clears_once_every_objective_is_terminal() {
            var f = new Fixture(); var boss = f.Boss("boss"); f.Checker.SetObjectiveInstallerIds(new[] { "boss" }); f.Checker.ConfigureRules(); Assert.Equal(false, f.Checker.CheckResult(f.Context)); boss.Destroy(); Assert.True(f.Checker.CheckResult(f.Context)); Assert.Equal<Master?>(Master.RightPlayer, f.Checker.Loser);
        }
        [PortedTest(Source)] public static void an_objective_never_installed_blocks_the_win() {
            var f = new Fixture(); f.Boss("boss").Destroy(); f.Checker.SetObjectiveInstallerIds(new[] { "boss", "extra" }); f.Checker.ConfigureRules(); Assert.Equal(false, f.Checker.CheckResult(f.Context)); f.Boss("extra").Destroy(); Assert.True(f.Checker.CheckResult(f.Context));
        }
        [PortedTest(Source)] public static void survive_clears_once_the_frame_threshold_is_reached_while_an_objective_is_never_installed() {
            var f = new Fixture(); f.Checker.SetObjectiveInstallerIds(new[] { "never-installed" }); f.Checker.ConfigureRules(new PveScenarioRules(PveWinCondition.Survive, 10)); f.Context.FrameNumber = 199; Assert.Equal(false, f.Checker.CheckResult(f.Context)); f.Context.FrameNumber = 200; Assert.True(f.Checker.CheckResult(f.Context)); Assert.Equal<Master?>(Master.RightPlayer, f.Checker.Loser);
        }
        [PortedTest(Source)] public static void survive_clears_before_the_timer_once_every_objective_is_terminal() {
            var f = new Fixture(); var boss = f.Boss("boss"); f.Checker.SetObjectiveInstallerIds(new[] { "boss" }); f.Checker.ConfigureRules(new PveScenarioRules(PveWinCondition.Survive, 90)); f.Context.FrameNumber = 100; Assert.Equal(false, f.Checker.CheckResult(f.Context)); boss.Destroy(); Assert.True(f.Checker.CheckResult(f.Context)); Assert.Equal<Master?>(Master.RightPlayer, f.Checker.Loser);
        }
        [PortedTest(Source)] public static void survive_does_not_clear_before_the_timer_while_an_objective_is_alive() {
            var f = new Fixture(); var boss = f.Boss("boss"); f.Boss("structure"); boss.Destroy(); f.Checker.SetObjectiveInstallerIds(new[] { "boss", "structure" }); f.Checker.ConfigureRules(new PveScenarioRules(PveWinCondition.Survive, 90)); f.Context.FrameNumber = 1799; Assert.Equal(false, f.Checker.CheckResult(f.Context)); f.Context.FrameNumber = 1800; Assert.True(f.Checker.CheckResult(f.Context));
        }
        [PortedTest(Source)] public static void survive_without_objectives_clears_only_on_the_timer() {
            var f = new Fixture(); f.Checker.SetObjectiveInstallerIds(new string[0]); f.Checker.ConfigureRules(new PveScenarioRules(PveWinCondition.Survive, 10)); f.Context.FrameNumber = 199; Assert.Equal(false, f.Checker.CheckResult(f.Context)); f.Context.FrameNumber = 200; Assert.True(f.Checker.CheckResult(f.Context)); Assert.Equal<Master?>(Master.RightPlayer, f.Checker.Loser);
        }
        [PortedTest(Source)] public static void an_objective_not_yet_in_the_world_list_does_not_clear() {
            var f = new Fixture(); f.Boss("boss"); f.Checker.SetObjectiveInstallerIds(new[] { "boss" }); f.Checker.ConfigureRules(); Assert.Equal(0, f.Context.Objects.Length); Assert.Equal(false, f.Checker.CheckResult(f.Context));
        }
        [PortedTest(Source)] public static void the_first_loser_stays_the_loser() { var checker = new PveResultChecker(); checker.SetCleared(); checker.SetFailed(); Assert.Equal<Master?>(Master.RightPlayer, checker.Loser); Assert.True(checker.IsEnd); }
    }
}
