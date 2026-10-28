using System;
using System.Collections.Generic;
using ArcaneCasters.Simulation;

namespace ArcaneCasters.Parity {
    public static class BattleFrameChecks {
        private sealed class Phase : IGameSystem {
            private readonly Action<SimulationContext> action;
            public Phase(Action<SimulationContext> action) { this.action = action; }
            public void Update(SimulationContext context) => action(context);
        }
        private sealed class Output : IBattleFrameOutput {
            public readonly List<string> Log; public int Snapshots, Frames;
            public Output(List<string> log) { Log = log; }
            public void BeginFrame(SimulationContext context) => Log.Add("begin");
            public void BuildSnapshot(SimulationContext context) { Snapshots++; Log.Add("snapshot"); }
            public void EndFrame(SimulationContext context) { Frames++; Log.Add("end"); }
        }
        public static void Run() {
            foreach (int tickRate in new[] { 20, 60 }) {
                var context = new SimulationContext(); context.SetTickRate(tickRate);
                context.Players[Master.LeftPlayer] = new PlayerData(new ManaCharger(100)) { Hp = 10 };
                context.Players[Master.RightPlayer] = new PlayerData(new ManaCharger(100)) { Hp = 20 };
                var log = new List<string>(); var output = new Output(log); int bots = 0, results = 0, timerReads = 0;
                Phase Record(string name) => new Phase(_ => log.Add(name));
                var systems = new BattleFrameSystems {
                    Output = output, Bots = new Phase(_ => { bots++; log.Add("bots"); }), Fever = Record("fever"),
                    ResetStatus = Record("reset"), Components = Record("components"), Physics = Record("physics"), FlushObjects = Record("flush")
                };
                var loop = new BattleFrameLoop(context, systems, () => { timerReads++; return true; });
                loop.BeforeResultCheck = _ => log.Add("before-result");
                loop.HandleGameEnd = c => { results++; Assert.Equal((Master?)Master.LeftPlayer, c.Result.Loser); log.Add("result"); };
                context.SubmitAction("ordered-input", _ => log.Add("input")); context.FrameNumber = 1; loop.Update();
                Assert.Sequence(new[] { "input", "begin", "fever", "bots", "before-result", "reset", "components", "physics", "flush", "snapshot", "end" }, log);
                Assert.Equal(0, results); Assert.True(!loop.StopRequested); context.Players[Master.LeftPlayer].Hp = 30;
                context.Result.SetLoser(Master.RightPlayer);
                for (int frame = 2; frame <= tickRate; frame++) { context.FrameNumber = frame; loop.Update(); }
                Assert.Equal(0, results); context.FrameNumber++; loop.Update(); context.FrameNumber++; loop.Update();
                Assert.Equal(1, results); Assert.True(loop.StopRequested); Assert.Equal(1, timerReads); Assert.Equal(1, bots); Assert.Equal(tickRate + 2, output.Frames);
                Assert.Equal((Master?)Master.LeftPlayer, context.Result.Loser);
                int snapshots = 0;
                for (int frame = 1; frame <= tickRate * 3 / 2; frame++) if (BattleFrameLoop.IsSyncFrame(frame, tickRate)) snapshots++;
                Assert.Equal(4, snapshots);
                for (int frame = 2; frame < tickRate / 2; frame++) Assert.True(!BattleFrameLoop.IsSyncFrame(frame, tickRate));
            }
            CheckProductionSystems();
        }
        private sealed class EndOnUpdate : SimComponent {
            public EndOnUpdate(SimObject owner) : base(owner) { }
            public override void Update() { Context.Result.SetLoser(Master.RightPlayer); Owner.SetStatus(ObjectStatus.Attack); }
        }
        private static void CheckProductionSystems() {
            var context = new SimulationContext(); context.RegisterPrefab("Unit", o => o.AddComponent(new EndOnUpdate(o)));
            var owner = context.Spawn(Master.LeftPlayer, "Unit", new BEPUutilities.Vector3(5, 0, 5)); context.FlushObjects();
            var output = new Output(new List<string>()); var loop = new BattleFrameLoop(context, new BattleFrameSystems { Output = output }, () => throw new Exception("PVE must not read timeout")) { HasTimeLimit = false };
            int results = 0; loop.HandleGameEnd = _ => results++;
            context.FrameNumber = 1; loop.Update(); Assert.True(context.Result.Ended); Assert.Equal(ObjectStatus.Attack, owner.Status);
            for (int frame = 2; frame <= 21; frame++) { context.FrameNumber = frame; loop.Update(); }
            Assert.Equal(0, results); context.FrameNumber = 22; loop.Update(); Assert.Equal(1, results);
            Assert.Equal(22, output.Frames);
        }
    }
}
