using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class RemainingSessionParityTests {
        private const string Source = "game/service/game_loop.rs";
        private static ConfirmedFrame Frame(uint tick) => new ConfirmedFrame {
            sessionId = "test", tick = tick, inputs = new[] { Array.Empty<CastCommand>(), Array.Empty<CastCommand>() }
        };
        private static void Throws(Action run) {
            bool thrown = false; try { run(); } catch (InvalidOperationException) { thrown = true; }
            Assert.True(thrown);
        }
        [PortedTest(Source)] public static void is_not_running_until_the_thread_ticks() {
            var runtime = BattleSessionChecks.Create(SessionType.PVP, 1, 2);
            var driver = new LockstepBattleDriver(runtime);
            Assert.True(!runtime.HasStarted && !runtime.IsRunning && !runtime.IsTerminated);
            driver.Receive(Frame(1));
            Assert.Equal(0, runtime.Context.FrameNumber); Assert.True(!runtime.IsRunning);
            driver.Receive(Frame(0));
            Assert.True(runtime.HasStarted && runtime.IsRunning); Assert.Equal(2, runtime.Context.FrameNumber);
            runtime.Close();
        }
        [PortedTest(Source)] public static void reports_running_once_started_and_stops_reporting_after_close() {
            var runtime = BattleSessionChecks.Create(SessionType.PVP, 1, 2);
            var driver = new LockstepBattleDriver(runtime);
            driver.Receive(Frame(0)); driver.Receive(Frame(1));
            Assert.True(runtime.IsRunning); Assert.Equal(2, runtime.Context.FrameNumber);
            // lockstep의 시간은 확정 tick으로 측정한다. 렌더링 FPS나 스레드 sleep으로 보충하지 않는다.
            Assert.Equal(100L, new FrameMillisClock(runtime.Context).NowMillis);
            runtime.Close();
            Assert.True(!runtime.IsRunning && runtime.IsTerminated);
            Throws(() => driver.Receive(Frame(2))); Assert.Equal(2, runtime.Context.FrameNumber);
        }
        [PortedTest(Source)] public static void await_start_returns_false_once_the_loop_has_already_terminated() {
            var runtime = BattleSessionChecks.Create(SessionType.PVP, 1, 2);
            runtime.Close(); int ended = 0; runtime.Terminated += () => ended++;
            var driver = new LockstepBattleDriver(runtime);
            Throws(() => driver.Receive(Frame(0)));
            Assert.True(!runtime.HasStarted && !runtime.IsRunning && runtime.IsTerminated);
            Assert.Equal(0, runtime.Context.FrameNumber); Assert.Equal(0, ended);
        }
        [PortedTest(Source)] public static void a_setup_panic_releases_the_creator() {
            bool failed = false;
            try {
                new BattleSessionRuntime(null, new SessionDefinition("bad", 1, 2, 0, new long[0], new long[0]), null, null, null, null, null, null, 1000, 0);
            } catch (ArgumentNullException) { failed = true; }
            Assert.True(failed);
            // 생성은 동기적이다. 실패한 생성자가 숨은 waiter나 실행 중인 loop를 남기지 않는다.
            var runtime = BattleSessionChecks.Create(SessionType.PVP, 1, 2);
            Assert.True(!runtime.HasStarted && !runtime.IsRunning);
            new LockstepBattleDriver(runtime).Receive(Frame(0)); Assert.True(runtime.IsRunning);
            runtime.Close();
        }
        [PortedTest(Source)] public static void interrupt_wakes_a_loop_waiting_for_its_next_frame() {
            var runtime = BattleSessionChecks.Create(SessionType.PVP, 1, 2);
            var driver = new LockstepBattleDriver(runtime); int ended = 0;
            runtime.Terminated += () => ended++;
            driver.Receive(Frame(1)); Assert.Equal(0, runtime.Context.FrameNumber);
            runtime.Close(); runtime.Close();
            Assert.Equal(1, ended); Assert.True(runtime.IsTerminated);
            Throws(() => driver.Receive(Frame(0))); Assert.Equal(0, runtime.Context.FrameNumber);
        }
        [PortedTest(Source)] public static void counts_frames_for_monitoring() {
            var runtime = BattleSessionChecks.Create(SessionType.PVP, 1, 2);
            var driver = new LockstepBattleDriver(runtime);
            for (uint tick = 0; tick < 3; tick++) driver.Receive(Frame(tick));
            Assert.Equal(3, runtime.Context.FrameNumber); Assert.Equal(150L, new FrameMillisClock(runtime.Context).NowMillis);
            driver.Receive(Frame(2)); Assert.Equal(3, runtime.Context.FrameNumber);
            runtime.Close();
        }
        [PortedTest(Source)] public static void broadcasts_result_and_marks_both_users_online_when_update_panics() {
            var runtime = BattleSessionChecks.Create(SessionType.PVP, 11, 22);
            var driver = new LockstepBattleDriver(runtime); int terminated = 0;
            var failure = new InvalidOperationException("frame failure");
            runtime.Terminated += () => terminated++;
            runtime.Loop.BeforeResultCheck = _ => throw failure;
            Throws(() => driver.Receive(Frame(0)));
            Assert.True(driver.Failed && runtime.IsTerminated && !runtime.IsRunning);
            Assert.True(ReferenceEquals(failure, runtime.Failure)); Assert.Equal(1, terminated);
            runtime.Close(); Assert.Equal(1, terminated);
            // Online 복귀·중단 전달은 서버 abort/outbox 통합 검사로 별도 검증한다. 실패한 재생은 보상을 확정하지 않는다.
        }
        [PortedTest(Source)] public static void does_not_end_twice_when_the_frame_panics_after_the_game_already_ended() {
            var runtime = BattleSessionChecks.Create(SessionType.PVP, 11, 22);
            var driver = new LockstepBattleDriver(runtime); int ended = 0, terminated = 0;
            var failure = new InvalidOperationException("completion callback failure");
            runtime.Terminated += () => terminated++;
            runtime.Loop.GameEnded += _ => ended++;
            runtime.Loop.HandleGameEnd = _ => throw failure;
            runtime.Context.Result.SetLoser(Master.RightPlayer);
            for (uint tick = 0; tick < 20; tick++) driver.Receive(Frame(tick));
            Assert.Equal(0, ended);
            Throws(() => driver.Receive(Frame(20)));
            Assert.Equal(1, ended); Assert.Equal(1, terminated);
            Assert.True(runtime.Context.Result.Ended && runtime.IsTerminated);
            Assert.True(ReferenceEquals(failure, runtime.Failure));
            Throws(() => driver.Receive(Frame(21))); runtime.Close();
            Assert.Equal(1, ended); Assert.Equal(1, terminated);
        }
        private sealed class PveSink : IPveSessionSink {
            public long LeftUserId => 7;
            public long RightUserId => -1;
            public void SendObjective(long id, PveObjectiveState state) { }
            public void SendScriptEvent(long id, PveScriptEvent message) { }
            public void SendState(long id, PveChannelState state) { }
        }
        [PortedTest("game/service/pve_loop.rs")] public static void a_cleared_scenario_reports_the_result_before_broadcasting_zero_mmr() {
            var runtime = BattleSessionChecks.Create(SessionType.PVE, 7, -1);
            var registry = new PveScenarioRegistry(id => new PveScenario(new PveScenarioEvent[0], rules: new PveScenarioRules(PveWinCondition.Survive, 1)));
            runtime.InitializePve(registry, new PveSink());
            var events = new List<string>();
            runtime.PveResultDecided += loser => { Assert.Equal<Master?>(Master.RightPlayer, loser); events.Add("decided"); };
            runtime.PveResultReady += (user, result) => {
                Assert.True(!runtime.Loop.StopRequested); Assert.Equal("Win", result.LeftPlayer); Assert.Equal("Lose", result.RightPlayer);
                Assert.Equal((short)0, result.Mmr.LastLeftMmr); Assert.Equal((short)0, result.Mmr.LastRightMmr);
                Assert.Equal((short)0, result.Mmr.NewLeftMmr); Assert.Equal((short)0, result.Mmr.NewRightMmr);
                events.Add("send:" + user);
            };
            for (int frame = 1; frame <= 40; frame++) runtime.AdvanceConfirmedFrame(frame);
            Assert.Sequence(new[] { "decided", "send:-1", "send:7" }, events);
            Assert.True(runtime.IsTerminated && !runtime.IsRunning);
            Throws(() => runtime.AdvanceConfirmedFrame(41)); Assert.Equal(3, events.Count);
        }
        private static BattleSessionRuntime OutputRuntime(BattleSessionOutput output) {
            var context = new SimulationContext();
            context.Players[Master.LeftPlayer] = new PlayerData(new ManaCharger(100));
            context.Players[Master.RightPlayer] = new PlayerData(new ManaCharger(100));
            context.GameParameters["player.hp"] = 100;
            PlayerHealthComponent.Register(context); TerrainPrefabs.Register(context);
            return new BattleSessionRuntime(context, new SessionDefinition("test", 11, 22, 1, new long[0], new long[0]),
                new TowerLookup(), new TestBotCatalog(), new Counters(), _ => null, _ => 0, _ => null, 100000, 0, output: output);
        }
        [PortedTest("game/domain/session_object.rs")] public static void has_spectators_follows_the_registry() {
            bool subscribed = false; var sent = new List<long>();
            using var output = new BattleSessionOutput(11, 22, (id, _) => sent.Add(id), () => subscribed);
            var runtime = OutputRuntime(output);
            runtime.AdvanceConfirmedFrame(1); Assert.True(output.SpectatorFrame == null); Assert.Sequence(new long[] { 11, 22 }, sent);
            subscribed = true; sent.Clear(); runtime.AdvanceConfirmedFrame(2);
            Assert.True(output.SpectatorFrame != null); Assert.Sequence(new long[] { 11, 22, 0 }, sent);
            Assert.Equal(0, output.SpectatorFrame.Mana); Assert.Equal(0, output.SpectatorFrame.Cards.Length);
            subscribed = false; sent.Clear(); runtime.AdvanceConfirmedFrame(3);
            Assert.True(output.SpectatorFrame == null); Assert.Sequence(new long[] { 11, 22 }, sent); runtime.Close();
        }
        [PortedTest(Source)] public static void hands_the_subscription_registry_to_the_session_on_init() {
            bool subscribed = false; int spectatorFrames = 0;
            using var output = new BattleSessionOutput(11, 22, (id, _) => { if (id == 0) spectatorFrames++; }, () => subscribed);
            var runtime = OutputRuntime(output);
            Assert.True(ReferenceEquals(runtime.Definition, runtime.Context.Service<SessionDefinition>()));
            runtime.AdvanceConfirmedFrame(1); Assert.Equal(0, spectatorFrames);
            subscribed = true; runtime.AdvanceConfirmedFrame(2); Assert.Equal(1, spectatorFrames);
            Assert.Equal(2, output.SpectatorFrame.Frame); runtime.Close();
            using var unwired = new BattleSessionOutput(11, 22, (id, _) => { if (id == 0) spectatorFrames++; });
            var other = OutputRuntime(unwired); other.AdvanceConfirmedFrame(1); Assert.Equal(1, spectatorFrames); other.Close();
        }
        private sealed class TowerLookup : IMagicLookup {
            private readonly BoundMagic tower = BuildMagics.Create("GroundTower", "tower", true).Bind(34, "tower", Element.Rock);
            public BoundMagic GetMagic(long id) => id == 34 ? tower : null;
            public BoundMagic ParseMagic(long user, long id) => GetMagic(id);
        }
        private sealed class Counters : IBotCounterScorer {
            public Fix64 Evaluate(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => 0;
            public Fix64 EvaluateVulnerability(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => 0;
        }
        [RequiresThreads]
        [PortedTest("tests/session_loop.rs")] public static void a_match_runs_on_its_own_thread_and_ends_after_the_grace_second() {
            using var ready = new ManualResetEventSlim(); using var queued = new ManualResetEventSlim();
            SimulationContext shared = null; int threadId = 0, inputThread = 0, terminated = 0, resultFrame = 0, inputResponses = 0;
            InputResponse response = default; var frames = new List<BattlePresentationFrame>();
            var job = Task.Run(() => {
                threadId = Thread.CurrentThread.ManagedThreadId;
                var context = new SimulationContext(); shared = context;
                context.Players[Master.LeftPlayer] = new PlayerData(new ManaCharger(100));
                context.Players[Master.RightPlayer] = new PlayerData(new ManaCharger(100));
                context.GameParameters["player.hp"] = 100; context.GameParameters["tower.mana_cost"] = 3; context.GameParameters["tower.range"] = 20;
                PlayerHealthComponent.Register(context); TerrainPrefabs.Register(context); context.RegisterPrefab("GroundTower", _ => { });
                using var output = new BattlePresentationOutput(Master.LeftPlayer, frames.Add);
                var runtime = new BattleSessionRuntime(context, new SessionDefinition("test", 11, 22, 1, new long[] { 34 }, new long[0]),
                    new TowerLookup(), new TestBotCatalog(), new Counters(), _ => null, _ => 0, _ => null, 100000, 0, output: output);
                runtime.Terminated += () => terminated++;
                runtime.Loop.GameEnded += c => { resultFrame = c.FrameNumber; Assert.Equal<Master?>(Master.RightPlayer, c.Result.Loser); };
                var driver = new LockstepBattleDriver(runtime);
                driver.Receive(Frame(0)); driver.Receive(Frame(1));
                ready.Set(); Assert.True(queued.Wait(TimeSpan.FromSeconds(5)));
                for (uint tick = 2; tick < 100 && !runtime.IsTerminated; tick++) driver.Receive(Frame(tick));
                Assert.True(runtime.IsTerminated && !runtime.IsRunning);
                Assert.Equal(23, resultFrame); Assert.Equal(23, context.FrameNumber);
            });
            if (!ready.Wait(TimeSpan.FromSeconds(5))) { job.GetAwaiter().GetResult(); throw new TimeoutException("Session startup did not signal readiness."); }
            Assert.True(shared.SubmitAction("useMagic", context => {
                inputThread = Thread.CurrentThread.ManagedThreadId;
                context.Players[Master.LeftPlayer].Mana = 10; context.Players[Master.LeftPlayer].AddCard(34);
                response = context.InputHandler.HandleInput(context, 11, 34, new Vector3(5, 0, 5), 1); inputResponses++;
            }));
            Assert.True(shared.SubmitAction("surrender", context => context.Result.SetLoser(Master.RightPlayer)));
            queued.Set(); Assert.True(job.Wait(TimeSpan.FromSeconds(10)));
            Assert.True(threadId != Thread.CurrentThread.ManagedThreadId); Assert.Equal(threadId, inputThread);
            Assert.Equal(1, terminated); Assert.Equal(1, inputResponses); Assert.Equal(InputResultCode.Success, response.ResultCode);
            Assert.True(frames[0].IsSync); Assert.True(frames.Any(frame => !frame.IsSync));
            Assert.True(frames.Any(frame => frame.Created.Any(owner => owner.Prefab == "GroundTower")));
        }
    }
}
