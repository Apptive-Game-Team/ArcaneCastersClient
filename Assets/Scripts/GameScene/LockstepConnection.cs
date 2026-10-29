using System;
using System.Collections;
using System.Collections.Generic;
using ArcaneCasters.Simulation;
using Global.Serialization;
using Global.Stomp;
using UnityEngine;
using UnityEngine.Networking;

namespace GameScene {
    // 전투 규칙은 이 컴포넌트 밖에서 실행한다. 콜백은 Unity 메인 스레드에서만 호출된다.
    public sealed class LockstepConnection : MonoBehaviour {
        public event Action<SessionStart> Initialized;
        public event Action<ConfirmedFrame> Confirmed;
        public event Action Recovered;
        public event Action<string> Stopped;
        public event Action<BattleSessionRuntime> BattleInitialized;
        public event Action<BattleSessionRuntime> BattleCompleted;
        public event Action<Dto.ServerMessage> PresentationReceived;
        public event Action<long, PveObjectiveState> PveObjectiveReceived;
        public event Action<long, PveScriptEvent> PveScriptReceived;
        public event Action<long, PveChannelState> PveStateReceived;
        public BattleSessionRuntime Battle => battleSession?.Driver?.Runtime;
        public bool IsStopped => stopped;
        public bool CanQueueCommands(int count) => !spectator && !stopped && !recovering && stream != null
            && Battle != null && Battle.Context.FrameNumber > 0 && !Battle.Loop.StopRequested && localInputs.HasCapacity(count);
        private Func<SessionStart, BattleSessionRuntime> createBattle;
        private LockstepBattleSession battleSession;
        private IStompTransport transport;
        private ConfirmedInputStream stream;
        // v1 경기는 모델 필드를 싣지 않는 배치를 만든다. 실제 버전은 Begin에서 정한다.
        private LockstepLocalInputs localInputs = new LockstepLocalInputs();
        private int memberSlot;
        private string baseUrl, token, sessionId, version;
        private long memberId;
        private bool recovering, stopped, begun;
        private bool checkpointSending, checkpointFailed, abortReporting;
        private readonly SortedDictionary<uint, StateCheckpoint> checkpoints = new SortedDictionary<uint, StateCheckpoint>();
        private bool reconnectPending;
        private float retryDeadline;
        private float connectDeadline;
        private Coroutine polling;
        private bool completionPending, resultReceived;
        private bool spectator;

        public void ConfigureSpectator() {
            if (begun) throw new InvalidOperationException("Configure the spectator before starting the connection.");
            spectator = true;
        }

        public void ConfigureBattle(Func<SessionStart, BattleSessionRuntime> factory) {
            if (begun) throw new InvalidOperationException("Configure the battle before starting the connection.");
            createBattle = factory ?? throw new ArgumentNullException(nameof(factory));
        }
        public void ConfigureSnapshotBattle(Func<SessionStart, IBattleFrameOutput> output = null,
            Func<SessionStart, IPveSessionSink> pveSink = null, BotDecisionSourceFactory decisions = null) {
            ConfigureBattle(initial => {
                var snapshot = JsonCodec.Deserialize<BattleInitialState>(initial.initialStateJson);
                if (snapshot == null) throw new InvalidOperationException("Missing battle initial snapshot.");
                var presentation = output?.Invoke(initial) ?? new BattlePresentationOutput(
                    spectator ? (Master?)null : memberId == initial.participants[0] ? Master.LeftPlayer : Master.RightPlayer,
                    frame => PublishPresentation(LockstepPresentationMapper.Frame(frame)));
                var runtime = snapshot.CreateRuntime(initial, presentation, pveSink?.Invoke(initial) ?? new ConnectionPveSink(this, initial), decisions);
                runtime.Emotes.Sent += (side, emote) => PublishPresentation(LockstepPresentationMapper.Emote(side, emote));
                runtime.Bots.Thought += thought => PublishPresentation(LockstepPresentationMapper.Thought(thought));
                return runtime;
            });
        }
        private sealed class ConnectionPveSink : IPveSessionSink {
            private readonly LockstepConnection connection;
            public long LeftUserId { get; }
            public long RightUserId { get; }
            public ConnectionPveSink(LockstepConnection connection, SessionStart initial) { this.connection = connection; LeftUserId = initial.participants[0]; RightUserId = initial.participants[1]; }
            public void SendObjective(long id, PveObjectiveState state) {
                connection.PveObjectiveReceived?.Invoke(id, state);
                if (id == (connection.spectator ? LeftUserId : connection.memberId)) connection.PublishPresentation(LockstepPresentationMapper.Objective(state));
            }
            public void SendScriptEvent(long id, PveScriptEvent message) {
                connection.PveScriptReceived?.Invoke(id, message);
                if (id == (connection.spectator ? LeftUserId : connection.memberId)) connection.PublishPresentation(LockstepPresentationMapper.Script(message));
            }
            public void SendState(long id, PveChannelState state) {
                connection.PveStateReceived?.Invoke(id, state);
                if (id == (connection.spectator ? LeftUserId : connection.memberId)) connection.PublishPresentation(LockstepPresentationMapper.State(state));
            }
        }

        private void Awake() {
#if UNITY_WEBGL && !UNITY_EDITOR
            transport = gameObject.AddComponent<WebGLStompTransport>();
#else
            transport = gameObject.AddComponent<NativeStompTransport>();
#endif
            transport.Connected += OnConnected;
            transport.Disconnected += OnDisconnected;
            transport.Errored += OnTransportError;
            transport.Rejected += Fail;
            transport.MessageReceived += OnMessage;
        }

        public void Begin(string serverUrl, string jwt, string id, long uid, string simulationVersion) {
            if (begun) throw new InvalidOperationException("Lockstep connection already started");
            if (!LockstepVersion.IsSupported(simulationVersion)) throw new InvalidOperationException("Unsupported lockstep simulation version");
            var uri = new Uri(serverUrl);
            if (uri.Scheme != "http" && uri.Scheme != "https") throw new ArgumentException(nameof(serverUrl));
            if (!LockstepVersion.ValidSessionId(id)) throw new ArgumentException(nameof(id));
            if (createBattle == null) ConfigureSnapshotBattle();
            begun = true; baseUrl = serverUrl.TrimEnd('/'); token = jwt; sessionId = id; memberId = uid; version = simulationVersion;
            localInputs = new LockstepLocalInputs(LockstepVersion.UsesModelProtocol(simulationVersion));
            if (createBattle != null) battleSession = new LockstepBattleSession(id, uid, simulationVersion, createBattle, spectator);
            Connect();
        }

        public bool QueueCast(long magicId, long xRaw, long zRaw, int? requestId = null) {
            return CanQueueCommands(1) && localInputs.Queue(magicId, xRaw, zRaw, requestId);
        }
        public bool QueueCardSelection(long magicId, bool selected) {
            return CanQueueCommands(1) && localInputs.QueueSelection(magicId, selected);
        }
        public bool QueueEmote(string name) => CanQueueCommands(1) && localInputs.QueueEmote(name);
        public bool TrySendPveSync(int lastEventSeq) {
            if (stopped || recovering || lastEventSeq < 0 || Battle == null || Battle.Context.FrameNumber == 0) return false;
            var sync = Battle.Context.Service<PveSync>();
            if (sync == null) return false;
            sync.SendPveStateTo(Battle.Context,spectator ? Battle.Definition.LeftUserId : memberId,lastEventSeq);
            return true;
        }
        private void OnInputHandled(long userId, InputResponse response) {
            // 아직 응답을 못 받은 로컬 입력만 처리하므로, 복구 중에도 카드의 대기 상태를 해제한다.
            if (userId == memberId && localInputs.TryAcknowledge(response, out var local))
                PresentationReceived?.Invoke(LockstepPresentationMapper.Input(local));
        }
        private void PublishPresentation(Dto.ServerMessage message) {
            // 과거 틱은 시뮬레이션만 복원한다. 화면에는 복구 완료 시 현재 상태를 한 번 전달한다.
            if (!recovering) PresentationReceived?.Invoke(message);
        }
        private void CompleteRecovery() {
            if (!recovering) return;
            recovering = false; retryDeadline = 0;
            if (Battle != null && Battle.Context.FrameNumber > 0) {
                Master? viewer = spectator ? (Master?)null : memberId == Battle.Definition.LeftUserId ? Master.LeftPlayer : Master.RightPlayer;
                var snapshot = BattlePresentationOutput.RecoverySnapshot(Battle.Context,viewer,Battle.DurationMillis);
                PresentationReceived?.Invoke(LockstepPresentationMapper.Frame(snapshot));
            }
            Recovered?.Invoke();
        }

        private void Connect() {
            if (stopped) return;
            connectDeadline = Time.realtimeSinceStartup + 10f;
            var uri = new UriBuilder(baseUrl + "/ws") { Scheme = baseUrl.StartsWith("https:", StringComparison.Ordinal) ? "wss" : "ws", Query = "token=" + Uri.EscapeDataString(token) };
            transport.Connect(uri.Uri.AbsoluteUri, token);
        }
        private void OnConnected() {
            connectDeadline = 0;
            reconnectPending = false;
            recovering = true;
            transport.Subscribe("/game/lockstep/" + sessionId, "lockstep");
            if (polling != null) StopCoroutine(polling);
            polling = StartCoroutine(RecoverAndPoll());
        }
        private void OnDisconnected(string _) {
            if (stopped || reconnectPending) return;
            connectDeadline = 0;
            reconnectPending = true;
            if (retryDeadline == 0) retryDeadline = Time.realtimeSinceStartup + 25f;
            if (Time.realtimeSinceStartup >= retryDeadline) { Fail("Lockstep reconnect timed out"); return; }
            recovering = true;
            if (polling != null) StopCoroutine(polling);
            polling = StartCoroutine(Reconnect());
        }
        private void OnTransportError(string reason) {
            if (stopped || reconnectPending) return;
            OnDisconnected(reason);
            transport.Disconnect();
        }
        private IEnumerator Reconnect() {
            yield return new WaitForSecondsRealtime(1.1f);
            reconnectPending = false;
            Connect();
        }

        private void Update() {
            if (!stopped && connectDeadline > 0 && Time.realtimeSinceStartup >= connectDeadline)
                OnTransportError("Lockstep handshake timed out");
        }

        private UnityWebRequest Request(string path) {
            var request = UnityWebRequest.Get(baseUrl + path);
            request.SetRequestHeader("Authorization", "Bearer " + token);
            request.timeout = 10;
            return request;
        }

        private IEnumerator RecoverAndPoll() {
            using (var request = Request("/api/lockstep/" + sessionId)) {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success) { HttpFailed(request); yield break; }
                try {
                    var initial = JsonCodec.Deserialize<SessionStart>(request.downloadHandler.text);
                    memberSlot = initial.Validate(sessionId, memberId, version, spectator);
                    if (battleSession != null) {
                        battleSession.Initialize(initial);
                        battleSession.Driver.InputHandled += OnInputHandled;
                        BattleInitialized?.Invoke(Battle);
                    }
                    stream = new ConfirmedInputStream(sessionId, LockstepVersion.UsesModelProtocol(version));
                    Initialized?.Invoke(initial);
                } catch (System.Exception exception) { Fail(exception.Message); yield break; }
            }
            while (!stopped && transport.IsConnected) {
                uint from = stream.NextTick;
                using (var request = Request("/api/lockstep/" + sessionId + "/frames?from=" + from)) {
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success) { HttpFailed(request); yield break; }
                    bool caughtUp = false;
                    try {
                        var page = JsonCodec.Deserialize<HistoryPage>(request.downloadHandler.text);
                        if (page == null || page.sessionId != sessionId || page.fromTick != from || page.frames == null
                            || page.frames.Length > 128 || page.nextTick != from + page.frames.Length || page.nextTick > page.confirmedCount)
                            throw new InvalidOperationException("Lockstep history gap");
                        for (int i = 0; i < page.frames.Length && !stopped; i++) {
                            if (page.frames[i].tick != from + i) throw new InvalidOperationException("Lockstep history gap");
                            stream.Enqueue(page.frames[i]); Drain();
                        }
                        if (stopped) yield break;
                        caughtUp = page.nextTick == page.confirmedCount;
                        if (caughtUp && page.ended != null) { Fail(page.ended); yield break; }
                        if (caughtUp) {
                            CompleteRecovery();
                            SendNext();
                        }
                    } catch (System.Exception exception) { Fail(exception.Message); yield break; }
                    if (caughtUp) yield return new WaitForSecondsRealtime(0.5f);
                }
            }
        }

        private void OnMessage(string subscription, string body) {
            if (subscription != "lockstep" || recovering || stopped || stream == null) return;
            try {
                var envelope = Newtonsoft.Json.Linq.JObject.Parse(body);
                if (envelope["ended"] != null) {
                    if ((string)envelope["sessionId"] != sessionId || envelope["ended"].Type != Newtonsoft.Json.Linq.JTokenType.String)
                        throw new InvalidOperationException("Invalid lockstep terminal event");
                    Fail((string)envelope["ended"]); return;
                }
                stream.Enqueue(JsonCodec.Deserialize<ConfirmedFrame>(body)); Drain(); SendNext();
            }
            catch (System.Exception exception) { Fail(exception.Message); }
        }
        private void Drain() {
            while (!stopped && stream.TryTake(out var frame)) {
                battleSession?.Receive(frame);
                var checkpoint = battleSession?.Driver?.LastCheckpoint;
                if (!spectator && checkpoint != null) {
                    if (checkpoints.TryGetValue(checkpoint.tick, out var pending) && pending.hash != checkpoint.hash)
                        throw new InvalidOperationException("Recovered state checkpoint mismatch");
                    checkpoints[checkpoint.tick] = checkpoint;
                    if (!checkpointSending) StartCoroutine(FlushCheckpoints());
                }
                Confirmed?.Invoke(frame);
                if (Battle != null && Battle.Loop.StopRequested) {
                    CompleteRecovery();
                    stopped = true; localInputs.Clear();
                    transport.Disconnect(); BattleCompleted?.Invoke(Battle);
                    if (begun) { completionPending = true; StartCoroutine(CompleteBattle()); }
                }
            }
        }
        private void SendNext() {
            if (spectator || stopped || recovering || !transport.IsConnected || stream.NextTick >= 12000) return;
            // 직전 확정 프레임의 Think가 만든 모델 요청 manifest를 이 배치의 필수 항목으로 싣는다.
            var manifest = LockstepVersion.UsesModelProtocol(version) ? Battle?.Models.PendingManifest(stream.NextTick) : null;
            var batch = localInputs.Prepare(stream.NextTick, memberSlot, manifest);
            if (batch != null) transport.Send("/app/lockstep/" + sessionId + "/input", JsonCodec.Serialize(batch));
        }
        private IEnumerator FlushCheckpoints() {
            checkpointSending = true;
            float deadline = Time.realtimeSinceStartup + 25;
            while (checkpoints.Count > 0 && !checkpointFailed && Time.realtimeSinceStartup < deadline) {
                StateCheckpoint checkpoint = null;
                foreach (var entry in checkpoints) { checkpoint = entry.Value; break; }
                using (var request = Request("/api/lockstep/" + sessionId + "/checkpoint")) {
                    request.method = UnityWebRequest.kHttpVerbPOST;
                    request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonCodec.Serialize(checkpoint)));
                    request.SetRequestHeader("Content-Type", "application/json");
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success) { checkpoints.Remove(checkpoint.tick); deadline = Time.realtimeSinceStartup + 25; continue; }
                    if (request.responseCode >= 400 && request.responseCode < 500 && request.responseCode != 408 && request.responseCode != 429) {
                        checkpointFailed = true; break;
                    }
                }
                yield return new WaitForSecondsRealtime(0.25f);
            }
            checkpointSending = false;
            if (checkpoints.Count > 0 || checkpointFailed) {
                checkpointFailed = true; completionPending = false;
                Battle?.Fail(new InvalidOperationException("State checkpoint rejected or timed out"));
                if (!stopped) Fail("State checkpoint rejected or timed out");
                else { ReportSimulationFailure(); Stopped?.Invoke("State checkpoint rejected or timed out"); }
            }
        }
        private IEnumerator CompleteBattle() {
            float deadline = Time.realtimeSinceStartup + 90;
            while (!checkpointFailed && (checkpointSending || checkpoints.Count > 0)) yield return null;
            if (checkpointFailed) { completionPending = false; yield break; }
            while (completionPending && Time.realtimeSinceStartup < deadline) {
                using (var request = Request("/api/lockstep/" + sessionId + (spectator ? "/result" : "/complete"))) {
                    request.method = spectator ? UnityWebRequest.kHttpVerbGET : UnityWebRequest.kHttpVerbPOST;
                    request.timeout = Mathf.Max(1, Mathf.Min(45, Mathf.CeilToInt(deadline - Time.realtimeSinceStartup)));
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success && request.responseCode != 202) {
                        completionPending = false;
                        if (!ReceiveResult(request.downloadHandler.text)) Stopped?.Invoke("Invalid verified battle result");
                        yield break;
                    }
                    if (request.responseCode >= 400 && request.responseCode < 500 && request.responseCode != 408 && request.responseCode != 429) {
                        completionPending = false; Stopped?.Invoke("Battle completion rejected"); yield break;
                    }
                }
                yield return new WaitForSecondsRealtime(2);
            }
            if (completionPending) { completionPending = false; Stopped?.Invoke("Battle completion timed out"); }
        }
        private bool ReceiveResult(string json) {
            if (resultReceived) return true;
            if (Battle == null || !Battle.Loop.StopRequested || !Battle.Context.Result.Ended || Battle.Failure != null) return false;
            Dto.ResultInfo result;
            try {
                var fields = Newtonsoft.Json.Linq.JObject.Parse(json);
                foreach (string key in new[] { "lastLeftPlayerMmr", "lastRightPlayerMmr", "newLeftPlayerMmr", "newRightPlayerMmr" })
                    if (fields[key]?.Type != Newtonsoft.Json.Linq.JTokenType.Integer) return false;
                if (!JsonCodec.TryDeserialize(json, out result) || result == null || result.type != "result") return false;
                var loser = Battle.Context.Result.Loser;
                string left = !loser.HasValue ? "Draw" : loser == Master.LeftPlayer ? "Lose" : "Win";
                string right = !loser.HasValue ? "Draw" : loser == Master.RightPlayer ? "Lose" : "Win";
                if (result.leftPlayer != left || result.rightPlayer != right) return false;
            } catch { return false; }
            // 화면 처리 오류를 서버 응답 형식 오류로 숨기지 않는다.
            resultReceived = true; PresentationReceived?.Invoke(result); return true;
        }
        private void ReportSimulationFailure() {
            if (spectator || !begun || Battle?.Failure == null || abortReporting) return;
            abortReporting = true; StartCoroutine(ReportAbort());
        }
        private IEnumerator ReportAbort() {
            float deadline = Time.realtimeSinceStartup + 15;
            while (Time.realtimeSinceStartup < deadline) {
                using (var request = Request("/api/lockstep/" + sessionId + "/abort")) {
                    request.method = UnityWebRequest.kHttpVerbPOST;
                    request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes("{\"reason\":\"simulationFailure\"}"));
                    request.SetRequestHeader("Content-Type", "application/json"); request.timeout = 5;
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success || request.responseCode == 403 || request.responseCode == 409) yield break;
                }
                yield return new WaitForSecondsRealtime(1);
            }
        }
        private void Fail(string reason) {
            if (stopped) return;
            ReportSimulationFailure();
            stopped = true; Battle?.Close(); localInputs.Clear(); transport.Disconnect(); Stopped?.Invoke(reason);
        }
        private void HttpFailed(UnityWebRequest request) {
            if (request.responseCode >= 400 && request.responseCode < 500) Fail("Lockstep request rejected");
            else OnTransportError("Lockstep request interrupted");
        }
        private void OnDestroy() {
            stopped = true;
            completionPending = false;
            Battle?.Close();
            if (transport == null) return;
            transport.Connected -= OnConnected; transport.Disconnected -= OnDisconnected;
            transport.Errored -= OnTransportError; transport.Rejected -= Fail;
            transport.MessageReceived -= OnMessage; transport.Disconnect();
        }
    }
}
