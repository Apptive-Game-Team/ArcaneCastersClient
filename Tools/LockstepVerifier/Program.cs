using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ArcaneCasters.Simulation;

internal static class Program {
    private sealed class Request {
        public SessionStart start;
        public ConfirmedFrame[] frames;
    }
    private sealed class PveSink : IPveSessionSink {
        public long LeftUserId { get; }
        public long RightUserId { get; }
        public PveSink(SessionStart start) { LeftUserId = start.participants[0]; RightUserId = start.participants[1]; }
        public void SendObjective(long userId, PveObjectiveState state) { }
        public void SendScriptEvent(long userId, PveScriptEvent message) { }
        public void SendState(long userId, PveChannelState state) { }
    }
    private sealed class DiscardOutput : IBattleFrameOutput {
        public void BeginFrame(SimulationContext context) { context.DrainEvents(); }
        public void BuildSnapshot(SimulationContext context) { }
        public void EndFrame(SimulationContext context) { }
    }
    private static int Main() {
        try {
            // 서버가 소유한 초기 상태와 확정 입력만 받는다. 클라이언트의 승패 주장은 입력에 없다.
            using var input = Console.OpenStandardInput();
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];
            int count;
            while ((count = input.Read(chunk, 0, chunk.Length)) != 0) {
                if (buffer.Length + count > 3 * 1024 * 1024) throw new InvalidDataException("Replay exceeds size limit.");
                buffer.Write(chunk, 0, count);
            }
            var json = new JsonSerializerOptions { IncludeFields = true };
            var request = JsonSerializer.Deserialize<Request>(buffer.ToArray(), json);
            var start = request?.start;
            if (start == null || !LockstepVersion.ValidSessionId(start.sessionId)
                || request.frames == null || request.frames.Length == 0 || request.frames.Length > 12000)
                throw new InvalidDataException("Invalid replay envelope.");
            start.Validate(start.sessionId, start.participants?.FirstOrDefault(id => id > 0) ?? 0, start.simulationVersion);
            var initial = JsonSerializer.Deserialize<BattleInitialState>(start.initialStateJson, json)
                ?? throw new InvalidDataException("Missing battle content.");
            var runtime = initial.CreateRuntime(start, output: new DiscardOutput(), pveSink: new PveSink(start));
            var magicUses = new SortedDictionary<(long user, long magic), int>();
            runtime.Context.MagicUsed += (_, user, magic) => {
                var key = (user, magic);
                magicUses.TryGetValue(key, out int previous);
                magicUses[key] = checked(previous + 1);
            };
            var driver = new LockstepBattleDriver(runtime, modelProtocol: LockstepVersion.UsesModelProtocol(start.simulationVersion));
            for (int i = 0; i < request.frames.Length; i++) {
                var frame = request.frames[i];
                if (frame == null || frame.tick != (uint)i || runtime.Loop.StopRequested)
                    throw new InvalidDataException("Replay must contain consecutive frames through battle completion only.");
                driver.Receive(frame);
                if (runtime.Failure != null) throw new InvalidOperationException("Battle simulation failed.", runtime.Failure);
            }
            if (!runtime.Loop.StopRequested || !runtime.Context.Result.Ended || driver.Failed)
                throw new InvalidDataException("Replay does not complete the battle.");
            Console.WriteLine(JsonSerializer.Serialize(new {
                schemaVersion = 1, sessionId = start.sessionId, simulationVersion = start.simulationVersion,
                initialHash = start.initialHash, frames = runtime.Context.FrameNumber,
                loser = runtime.Context.Result.Loser?.ToString(),
                leftHp = runtime.Context.Players[Master.LeftPlayer].Hp,
                rightHp = runtime.Context.Players[Master.RightPlayer].Hp,
                magics = magicUses.Select(pair => new { userId = pair.Key.user, magicId = pair.Key.magic, count = pair.Value }).ToArray()
            }));
            return 0;
        } catch (Exception error) {
            Console.Error.WriteLine("Replay verification failed: " + error.GetType().Name);
            if (Environment.GetEnvironmentVariable("AC_LOCKSTEP_VERIFIER_DIAGNOSTICS") == "1")
                for (var cause = error; cause != null; cause = cause.InnerException)
                    Console.Error.WriteLine(cause.GetType().Name + "\n" + cause.StackTrace);
            return 1;
        }
    }
}
