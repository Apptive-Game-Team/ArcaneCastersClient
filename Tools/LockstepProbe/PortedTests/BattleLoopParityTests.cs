using System;
using System.Collections.Generic;
using ArcaneCasters.Simulation;

namespace ArcaneCasters.Parity {
    public static class BattleLoopParityTests {
        private const string Source = "game/service/word_online_loop.rs";
        [PortedTest("game/service/game_loop.rs")] public static void stops_reporting_running_when_the_frame_panics() {
            var runtime = BattleSessionChecks.Create(SessionType.PVP, 1, 2);
            Assert.True(!runtime.HasStarted && !runtime.IsRunning && !runtime.IsTerminated);
            int terminated = 0; runtime.Terminated += () => terminated++;
            var failure = new InvalidOperationException("frame blew the stack");
            runtime.Terminated += () => throw new InvalidOperationException("cleanup callback failed");
            runtime.Loop.BeforeResultCheck = _ => { Assert.True(runtime.IsRunning); throw failure; };
            bool rejected = false;
            try { runtime.AdvanceConfirmedFrame(1); } catch (InvalidOperationException error) { Assert.True(ReferenceEquals(failure, error)); rejected = true; }
            Assert.True(rejected && runtime.HasStarted && !runtime.IsRunning && runtime.IsTerminated);
            Assert.True(ReferenceEquals(failure, runtime.Failure));
            rejected = false;
            try { runtime.AdvanceConfirmedFrame(2); } catch (InvalidOperationException) { rejected = true; }
            Assert.True(rejected); Assert.Equal(1, runtime.Context.FrameNumber);
            runtime.Close(); runtime.Close(); Assert.Equal(1, terminated);
        }
        [PortedTest("game/service/game_loop.rs")] public static void on_terminated_runs_once_the_loop_ends() {
            var runtime = BattleSessionChecks.Create(SessionType.PVP, 1, 2); runtime.Context.SetTickRate(20);
            int terminated = 0;
            runtime.Terminated += () => { Assert.True(!runtime.IsRunning); terminated++; };
            runtime.Loop.BeforeResultCheck = _ => { runtime.Close(); Assert.Equal(0, terminated); };
            runtime.AdvanceConfirmedFrame(1);
            Assert.True(runtime.IsTerminated); Assert.Equal(1, terminated); Assert.Equal(1, runtime.Context.FrameNumber);
            Assert.Equal(FixMath.NET.Fix64.One / 20, runtime.Context.DeltaTime);
            runtime.Close(); Assert.Equal(1, terminated);
            var unopened = BattleSessionChecks.Create(SessionType.PVP, 1, 2); unopened.Close();
            Assert.True(!unopened.HasStarted && !unopened.IsRunning && unopened.IsTerminated);
            var cleanup = BattleSessionChecks.Create(SessionType.PVP, 1, 2);
            var error = new InvalidOperationException("cleanup callback failed"); int remainingCleanup = 0;
            cleanup.Terminated += () => throw error; cleanup.Terminated += () => remainingCleanup++;
            cleanup.Close(); cleanup.Close(); Assert.Equal(1, remainingCleanup); Assert.True(ReferenceEquals(error, cleanup.Failure));
        }
        private sealed class ManualClock : IMillisClock { public long NowMillis { get; set; } }
        [PortedTest("game/service/game_timer.rs")] public static void counts_down_and_enters_fever() {
            var clock = new ManualClock { NowMillis = 1000 };
            var timer = new GameTimer(180000, 60000, clock);
            Assert.Equal(180L, timer.RemainingSeconds); Assert.True(!timer.Fever);
            clock.NowMillis = 121000; Assert.True(!timer.Fever);
            clock.NowMillis = 121001; Assert.True(timer.Fever); Assert.True(!timer.Ended);
            clock.NowMillis = 181000; Assert.True(timer.Ended);
            bool rejected = false;
            try { timer.OverrideDuration(0); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Assert.True(rejected); Assert.Equal(180000L, timer.Duration); Assert.Equal(1000L, timer.StartTime);
            timer.OverrideDuration(5000); Assert.Equal(5000L, timer.RemainingMilliseconds);
            Assert.Equal(181000L, timer.StartTime); Assert.Equal(5000L, timer.Duration);
            clock.NowMillis += 6000; Assert.Equal(-1000L, timer.RemainingMilliseconds); Assert.True(timer.Ended);
        }
        private sealed class NoOp : IGameSystem { public void Update(SimulationContext c) { } }
        private sealed class Output : IBattleFrameOutput {
            public int Snapshots, Frames;
            public void BeginFrame(SimulationContext c) { }
            public void BuildSnapshot(SimulationContext c) { Snapshots++; }
            public void EndFrame(SimulationContext c) { Frames++; }
        }
        private sealed class Counter : IGameSystem {
            public int Updates;
            public void Update(SimulationContext c) { Updates++; }
        }
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext();
            public readonly Output Output = new Output();
            public readonly Counter Bots = new Counter();
            public readonly List<BattleResultMessage> Sent = new List<BattleResultMessage>();
            public readonly BattleFrameLoop Loop;
            public int Results, ClockReads;
            public Fixture(bool timeout = false) {
                Context.SetTickRate(20); Context.Players[Master.LeftPlayer] = new PlayerData(new ManaCharger(100)) { Hp = 10 }; Context.Players[Master.RightPlayer] = new PlayerData(new ManaCharger(100)) { Hp = 20 };
                var noOp = new NoOp();
                Loop = new BattleFrameLoop(Context, new BattleFrameSystems { Actions = noOp, Economy = noOp, Bots = Bots, Fever = noOp, ResetStatus = noOp, Components = noOp, Physics = noOp, FlushObjects = noOp, Output = Output }, () => { ClockReads++; return timeout; });
                Loop.HandleGameEnd = c => { Results++; c.Result.Broadcast(1,2,new BattleResultMmr(),(id, message) => Sent.Add(message)); };
            }
            public void Update(int frame) { Context.FrameNumber = frame; Loop.Update(); }
        }
        [PortedTest(Source)] public static void broadcasts_once_after_twenty_frames_and_keeps_the_first_loser() {
            var f = new Fixture(); f.Context.Result.SetLoser(Master.LeftPlayer); f.Update(1); Assert.Equal(0, f.Sent.Count); Assert.Equal(0, f.Bots.Updates);
            f.Context.Result.SetLoser(Master.RightPlayer); for (int frame = 2; frame <= 20; frame++) f.Update(frame); Assert.Equal(0, f.Sent.Count);
            f.Update(21); Assert.Equal(2, f.Sent.Count); f.Update(22); Assert.Equal<Master?>(Master.LeftPlayer, f.Context.Result.Loser); Assert.Equal(2, f.Sent.Count);
            Assert.Equal(1, f.Results); Assert.Equal("Lose", f.Sent[0].LeftPlayer); Assert.Equal(22, f.Output.Frames); Assert.True(f.Loop.StopRequested);
        }
        [PortedTest(Source)] public static void timed_out_match_resolves_only_once_and_uses_the_same_grace_period() {
            var f = new Fixture(true); f.Update(1); Assert.Equal<Master?>(Master.LeftPlayer, f.Context.Result.Loser); Assert.Equal(0, f.Sent.Count);
            f.Context.Players[Master.LeftPlayer].Hp = 30; for (int frame = 2; frame <= 21; frame++) f.Update(frame);
            Assert.Equal<Master?>(Master.LeftPlayer, f.Context.Result.Loser); Assert.Equal(1, f.ClockReads); Assert.Equal(2, f.Sent.Count);
        }
        private static int Snapshots(int tickRate, int first, int last) {
            var c = new SimulationContext(); c.SetTickRate(tickRate); var output = new Output(); var noOp = new NoOp();
            var loop = new BattleFrameLoop(c, new BattleFrameSystems {
                Actions = noOp, Economy = noOp, Bots = noOp, Fever = noOp, ResetStatus = noOp,
                Components = noOp, Physics = noOp, FlushObjects = noOp, Output = output
            }, () => false);
            for (int frame = first; frame <= last; frame++) { c.FrameNumber = frame; loop.Update(); }
            return output.Snapshots;
        }
        [PortedTest(Source)] public static void builds_the_snapshot_on_the_first_frame_and_every_half_second() {
            foreach (int tickRate in new[] { 20, 60 }) Assert.Equal(4, Snapshots(tickRate, 1, tickRate * 3 / 2));
        }
        [PortedTest(Source)] public static void builds_no_snapshot_between_the_first_frame_and_half_a_second() {
            foreach (int tickRate in new[] { 20, 60 }) Assert.Equal(0, Snapshots(tickRate, 2, tickRate / 2 - 1));
        }
        [PortedTest("game/service/game_loop.rs")] public static void syncs_every_half_second_of_game_time() {
            Assert.Equal(10, BattleFrameLoop.SyncIntervalFrames(20)); Assert.Equal(30, BattleFrameLoop.SyncIntervalFrames(60));
            Assert.Equal(60, BattleFrameLoop.SyncIntervalFrames(120)); Assert.Equal(1, BattleFrameLoop.SyncIntervalFrames(1));
            Assert.True(BattleFrameLoop.IsSyncFrame(1, 60)); Assert.True(!BattleFrameLoop.IsSyncFrame(29, 60)); Assert.True(BattleFrameLoop.IsSyncFrame(30, 60));
        }
        [PortedTest(Source)] public static void a_full_frame_runs_with_the_production_systems() {
            var c = PrefabParameterFixture.Create(); c.InputHandler = new MagicInputHandler(null);
            var session = new SessionDefinition("loop", 5, -9, 1, Array.Empty<long>(), Array.Empty<long>(), SessionType.Practice);
            session.InitializeIdentityAndDecks(c); BattleFieldInitializer.Spawn(c, session);
            var loop = new BattleFrameLoop(c, new BattleFrameSystems { Bots = new NoOp() }, () => false);
            c.FrameNumber = 1; loop.Update(); Assert.True(c.Objects.Length >= 3);
        }
    }
}
