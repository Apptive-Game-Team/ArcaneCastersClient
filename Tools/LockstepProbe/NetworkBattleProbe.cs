using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ArcaneCasters.Simulation;

// 테스트 서버의 HTTP 초기 상태와 실제 STOMP 확정 입력으로 클라이언트 전투를 끝까지 실행한다.
internal static class NetworkBattleProbe {
    private static readonly JsonSerializerOptions Json = new() { IncludeFields = true };
    private sealed class OutputRecorder : IDisposable {
        public BattleSessionOutput Output;
        public int LeftFrames, RightFrames, SpectatorFrames;
        public void Reset(SessionStart initial) {
            Output?.Dispose(); LeftFrames = RightFrames = SpectatorFrames = 0;
            Output = new BattleSessionOutput(initial.participants[0],initial.participants[1],(viewer,frame) => {
                if (viewer == 0) {
                    if (frame.Mana != 0 || frame.Cards.Length != 0) throw new InvalidDataException("Spectator frame exposed private player data");
                    SpectatorFrames++;
                } else if (viewer == initial.participants[0]) LeftFrames++;
                else if (viewer == initial.participants[1]) RightFrames++;
                else throw new InvalidDataException("Unknown frame recipient");
            },() => true);
        }
        public void Dispose() => Output?.Dispose();
    }
    private static string VisibleState(BattleSessionRuntime runtime) => JsonSerializer.Serialize(new {
        frame = runtime.Context.FrameNumber, rng = runtime.Context.Random.State,
        players = runtime.Context.Players.OrderBy(pair => pair.Key).Select(pair => new { side = pair.Key, pair.Value.Hp, pair.Value.Mana, cards = pair.Value.Cards.ToArray() }),
        objects = runtime.Context.Objects.OrderBy(owner => owner.Id).Select(owner => new {
            owner.Id, owner.Prefab, owner.Master, owner.Status, x = owner.Position.X.RawValue, y = owner.Position.Y.RawValue, z = owner.Position.Z.RawValue,
            effects = owner.Effects, gauges = new ObjectSnapshot(owner).Gauges.Select(gauge => new { gauge.Category, value = gauge.Value.RawValue, max = gauge.MaxValue.RawValue })
        })
    });
    private sealed class PveSink : IPveSessionSink {
        public long LeftUserId { get; }
        public long RightUserId { get; }
        public int Objectives { get; private set; }
        public PveSink(long left, long right) { LeftUserId = left; RightUserId = right; }
        public void SendObjective(long id, PveObjectiveState state) { Objectives++; }
        public void SendScriptEvent(long id, PveScriptEvent message) { }
        public void SendState(long id, PveChannelState state) { }
    }
    public static async Task<string> Run(bool requireModel = false) {
        string Required(string key) => Environment.GetEnvironmentVariable(key) ?? throw new InvalidOperationException(key);
        string server = Required("AC_LOCKSTEP_URL"), id = Required("AC_LOCKSTEP_SESSION"), token = Required("AC_LOCKSTEP_TOKEN");
        long member = long.Parse(Required("AC_LOCKSTEP_USER"));
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(requireModel ? 300 : 60));
        var ct = deadline.Token;
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var initial = JsonSerializer.Deserialize<SessionStart>(await http.GetStringAsync(server + "/api/lockstep/" + id, ct), Json);
        if (initial == null || !LockstepVersion.IsSupported(initial.simulationVersion)
            || requireModel && !LockstepVersion.UsesModelProtocol(initial.simulationVersion))
            throw new InvalidDataException("Invalid network probe simulation version");
        bool modelProtocol = LockstepVersion.UsesModelProtocol(initial.simulationVersion);
        PveSink pveSink = null;
        int humanEmotes = 0, botCasts = 0;
        using var recorder = new OutputRecorder();
        var battle = new LockstepBattleSession(id, member, initial.simulationVersion, start => {
            recorder.Reset(start); pveSink = new PveSink(start.participants[0],start.participants[1]);
            var runtime = JsonSerializer.Deserialize<BattleInitialState>(start.initialStateJson,Json).CreateRuntime(start,output:recorder.Output,pveSink:pveSink);
            humanEmotes = 0; botCasts = 0;
            runtime.Context.MagicUsed += (_, user, _) => { if (user < 0) botCasts++; };
            runtime.Emotes.Sent += (side, emote) => { if (side == Master.LeftPlayer && emote == Emote.Greet) humanEmotes++; };
            return runtime;
        });
        battle.Initialize(initial);
        int acceptedCasts = 0;
        battle.Driver.InputHandled += (_, response) => { if (response.Valid) acceptedCasts++; else throw new InvalidOperationException("Network cast was rejected: " + response.ResultCode); };
        bool attack = Environment.GetEnvironmentVariable("AC_LOCKSTEP_CAST") == "1";
        bool castSent = false;
        bool reconnect = Environment.GetEnvironmentVariable("AC_LOCKSTEP_RECONNECT") == "1";
        int reconnects = 0;
        int modelRequests = 0, modelAccepted = 0, modelCompleted = 0, modelRejected = 0, modelFailed = 0;
        using var originalSocket = new ClientWebSocket();
        using var recoveredSocket = new ClientWebSocket();
        var socket = originalSocket;
        var ws = new UriBuilder(server) { Scheme = server.StartsWith("https:") ? "wss" : "ws", Path = "/ws", Query = "token=" + Uri.EscapeDataString(token) };
        async Task Send(string text) => await socket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, ct);
        string pending = "";
        var buffer = new byte[16384];
        async Task<(string Command, string Body)> Read() {
            while (true) {
                int end = pending.IndexOf('\0');
                if (end >= 0) {
                    string frame = pending.Substring(0, end).TrimStart('\n', '\r'); pending = pending.Substring(end + 1);
                    int line = frame.IndexOf('\n'), body = frame.IndexOf("\n\n", StringComparison.Ordinal);
                    if (line < 0 || body < 0) throw new InvalidDataException("Malformed STOMP frame");
                    string command = frame.Substring(0, line);
                    if (command == "ERROR") throw new InvalidDataException("Server rejected network probe frame");
                    return (command, frame.Substring(body + 2));
                }
                var received = await socket.ReceiveAsync(buffer, ct);
                if (received.MessageType == WebSocketMessageType.Close) throw new IOException("Socket closed before battle completion");
                pending += Encoding.UTF8.GetString(buffer, 0, received.Count);
                if (pending.Length > 1024 * 1024) throw new InvalidDataException("STOMP frame too large");
            }
        }
        async Task Connect() {
            await socket.ConnectAsync(ws.Uri,ct);
            await Send($"CONNECT\naccept-version:1.2\nAuthorization:Bearer {token}\n\n\0");
            if ((await Read()).Command != "CONNECTED") throw new InvalidDataException("Missing CONNECTED");
            await Send($"SUBSCRIBE\nid:probe\nreceipt:sub\ndestination:/game/lockstep/{id}\n\n\0");
            if ((await Read()).Command != "RECEIPT") throw new InvalidDataException("Missing subscription receipt");
        }
        await Connect();
        uint tick = 0;
        while (!battle.Driver.Runtime.Loop.StopRequested && tick < (requireModel ? ModelProtocol.MaxTick : 300)) {
            if (reconnect && reconnects == 0 && tick == 35) {
                string before = VisibleState(battle.Driver.Runtime);
                socket.Abort(); socket = recoveredSocket; pending = "";
                await Connect();
                var recoveredInitial = JsonSerializer.Deserialize<SessionStart>(await http.GetStringAsync(server + "/api/lockstep/" + id,ct),Json);
                battle.Initialize(recoveredInitial); acceptedCasts = 0;
                battle.Driver.InputHandled += (_,response) => { if (response.Valid) acceptedCasts++; else throw new InvalidOperationException("Replayed cast was rejected"); };
                uint from = 0;
                while (true) {
                    var page = JsonSerializer.Deserialize<HistoryPage>(await http.GetStringAsync(server + "/api/lockstep/" + id + "/frames?from=" + from,ct),Json);
                    if (page.sessionId != id || page.fromTick != from || page.nextTick != from + page.frames.Length || page.nextTick > page.confirmedCount || page.ended != null)
                        throw new InvalidDataException("Invalid recovery history");
                    foreach (var frame in page.frames) battle.Receive(frame);
                    from = page.nextTick;
                    if (from == page.confirmedCount) break;
                }
                if (from != tick || before != VisibleState(battle.Driver.Runtime)) throw new InvalidDataException("Replay changed visible battle state");
                reconnects++;
            }
            var commands = attack && !castSent && member == initial.participants[0]
                && battle.Driver.Runtime.Context.Players[Master.LeftPlayer].Cards.Contains(7)
                ? new[] { new CastCommand { magicId = 7, xRaw = 17L << 32, zRaw = 5L << 32 } } : Array.Empty<CastCommand>();
            if (commands.Length != 0) castSent = true;
            if (tick == 0 && member == initial.participants[0]) commands = new[] { new CastCommand { kind = "emote", emote = "Greet" } };
            var manifest = modelProtocol ? battle.Driver.Runtime.Models.PendingManifest(tick) : null;
            modelRequests += manifest?.Length ?? 0;
            // v1 배치는 기존 두 필드만 쓴다. v2 요청은 provider 입력 payload까지 모두 직렬화한다.
            string body = manifest == null ? JsonSerializer.Serialize(new { tick, commands }, Json)
                : JsonSerializer.Serialize(new InputBatch { tick = tick, commands = commands, modelRequests = manifest }, Json);
            await Send($"SEND\ndestination:/app/lockstep/{id}/input\n\n{body}\0");
            var message = await Read();
            if (message.Command != "MESSAGE") throw new InvalidDataException("Missing confirmed frame");
            var confirmed = JsonSerializer.Deserialize<ConfirmedFrame>(message.Body, Json);
            if (confirmed.tick != tick) throw new InvalidDataException("Unexpected confirmed tick");
            battle.Receive(confirmed);
            foreach (var item in confirmed.modelEvents ?? Array.Empty<ModelEvent>()) {
                if (item.kind == ModelProtocol.Accepted) modelAccepted++;
                if (item.kind == ModelProtocol.Completed) modelCompleted++;
                if (item.kind == ModelProtocol.Rejected) modelRejected++;
                if (item.kind == ModelProtocol.Failed || item.kind == ModelProtocol.BackedOff) modelFailed++;
            }
            tick++;
        }
        var runtime = battle.Driver.Runtime;
        if (!runtime.Loop.StopRequested || !runtime.Context.Result.Ended || battle.Driver.Failed)
            throw new InvalidOperationException("Battle did not complete");
        if (recorder.LeftFrames != runtime.Context.FrameNumber || recorder.RightFrames != recorder.LeftFrames || recorder.SpectatorFrames != recorder.LeftFrames)
            throw new InvalidOperationException("Battle output lost a confirmed frame");
        bool verifiedComplete = false;
        if (requireModel) {
            if (modelAccepted == 0 || modelCompleted == 0)
                throw new InvalidOperationException("Model battle completed without an accepted provider decision: accepted=" + modelAccepted
                    + " completed=" + modelCompleted + " rejected=" + modelRejected + " failed=" + modelFailed);
            while (!verifiedComplete) {
                using var response = await http.PostAsync(server + "/api/lockstep/" + id + "/complete", null, ct);
                if (response.StatusCode == System.Net.HttpStatusCode.Accepted) {
                    await Task.Delay(500, ct); continue;
                }
                response.EnsureSuccessStatusCode();
                using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                if (!result.RootElement.TryGetProperty("type", out var type) || type.GetString() != "result")
                    throw new InvalidDataException("Model battle completion returned no verified result");
                verifiedComplete = true;
            }
        }
        if (requireModel) return JsonSerializer.Serialize(new { completed = true, simulationVersion = initial.simulationVersion,
            frames = runtime.Context.FrameNumber, acceptedCasts, botCasts, reconnects, humanEmotes, modelRequests, modelAccepted, modelCompleted, modelRejected, modelFailed, verifiedComplete,
            leftHp = runtime.Context.Players[Master.LeftPlayer].Hp, rightHp = runtime.Context.Players[Master.RightPlayer].Hp,
            loser = runtime.Context.Result.Loser?.ToString(), initialHash = initial.initialHash, pveObjectives = pveSink.Objectives });
        return JsonSerializer.Serialize(new { completed = true, frames = runtime.Context.FrameNumber, acceptedCasts, reconnects, humanEmotes,
            leftHp = runtime.Context.Players[Master.LeftPlayer].Hp, rightHp = runtime.Context.Players[Master.RightPlayer].Hp,
            loser = runtime.Context.Result.Loser?.ToString(), initialHash = initial.initialHash, pveObjectives = pveSink.Objectives });
    }
}
