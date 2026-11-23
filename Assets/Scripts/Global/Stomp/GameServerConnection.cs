using System;
using System.Collections;
using Data;
using UnityEngine;
using Newtonsoft.Json.Linq;
using UnityEngine.SceneManagement;

namespace Global.Stomp
{
    /// <summary>로비에서 연 연결을 게임 씬에 인계한다. 씬 전환으로 transport를 다시 만들지 않는다.</summary>
    public sealed class GameServerConnection : MonoBehaviour
    {
        public const float HandshakeTimeoutSeconds = 20f;
        public const float PreparationTimeoutSeconds = 180f;
        public const float RecoveryTimeoutSeconds = 30f;
        public const int MaxAttempts = 3;
        private static GameServerConnection current;
        private IStompTransport transport;
        private string matchKey;
        private UnityEngine.Object preparingOwner;
        private bool gameplayOwnsConnection;
        private bool preparing;
        private bool failed;
        private bool closed;
        private bool hasPreparingOwner;

        private MatchedInfoDto match;
        private bool protocolAttached;
        private bool spectator;
        private bool loadingScene;
        private bool sceneReady;
        private bool started;
        private bool everStarted;
        private bool startFailed;
        private bool loadAllowed;
        private string epoch;
        private string appliedToken;
        private float preparationDeadline;
        private float handshakeDeadline;
        private float nextAttempt;
        private float nextReady;
        private float lastControl;
        private float recoveryDeadline;
        public bool UsesStartProtocol => match?.startProtocolVersion == 1 && !spectator;
        public bool HasStarted => !UsesStartProtocol || started;
        public bool StartFailed => startFailed;
        public string PreparationPhase { get; private set; } = "connecting";
        public int Countdown { get; private set; }
        public event Action<string, int> PreparationChanged;

        private void SetPreparationPhase(string phase, int count = 0)
        {
            if (PreparationPhase == phase && Countdown == count) return;
            PreparationPhase = phase;
            Countdown = count;
            PreparationChanged?.Invoke(phase, count);
        }
        public Func<string, bool> ApplyStartSnapshot { get; set; }

        public IStompTransport Transport => transport;
        public bool IsPreparing => preparing || (loadingScene && !gameplayOwnsConnection);
        public bool IsClaimed => gameplayOwnsConnection;

        private static string Key(MatchedInfoDto match) =>
            match == null ? null : $"{match.sessionId}\n{match.ConnectionSource}\n{SceneContext.User?.id ?? 0}";

        public static GameServerConnection ForMatch(MatchedInfoDto match)
        {
            string key = Key(match);
            if (current != null && current.matchKey != key) current.Close();
            if (current == null)
            {
                var root = new GameObject("GameServerConnection");
                current = root.AddComponent<GameServerConnection>();
                current.matchKey = key;
                current.match = match;
                root.name = $"GameServerConnection-{root.GetInstanceID()}";
                DontDestroyOnLoad(root);
#if UNITY_WEBGL && !UNITY_EDITOR
                current.transport = root.AddComponent<WebGLStompTransport>();
#else
                current.transport = root.AddComponent<NativeStompTransport>();
#endif
            }
            return current;
        }

        public void ClaimForGameplay(bool isSpectator = false)
        {
            spectator = isSpectator;
            AttachStartProtocol();
            gameplayOwnsConnection = true;
            preparingOwner = null;
        }

        public IEnumerator Prepare(MatchedInfoDto match, UnityEngine.Object owner, Action<bool> complete)
        {
            if (preparing || gameplayOwnsConnection || owner == null)
            {
                complete(false);
                yield break;
            }
            if (UsesStartProtocol)
            {
                preparing = true;
                preparingOwner = owner;
                hasPreparingOwner = true;
                AttachStartProtocol();
                while (owner != null && !closed && !startFailed && !loadAllowed)
                {
                    MaintainStartConnection();
                    yield return null;
                }
                preparing = false;
                bool ready = owner != null && !closed && !startFailed && loadAllowed && transport.IsConnected;
                if (!ready) Close();
                complete(ready);
                yield break;
            }
            preparing = true;
            preparingOwner = owner;
            hasPreparingOwner = true;
            if (!match.TryResolveWebSocket("ws", out var endpoint))
            {
                preparing = false;
                complete(false);
                Close();
                yield break;
            }
            string token = CustomBattleConnection.TokenFor(match, match.GameServerToken(SceneContext.JwtToken));
            string url = endpoint.Query("token", token);
            transport.Errored += OnFailed;
            transport.Disconnected += OnFailed;
            bool connected = false;
            try
            {
                for (int attempt = 1; attempt <= MaxAttempts && owner != null && !closed; attempt++)
                {
                    if (transport.IsConnected) { connected = true; break; }
                    failed = false;
                    Debug.Log($"[GameConnection] STOMP 연결 시작 session={match.sessionId}, attempt={attempt}/{MaxAttempts}");
                    transport.Connect(url, token);
                    float deadline = Time.realtimeSinceStartup + HandshakeTimeoutSeconds;
                    while (owner != null && !closed && !transport.IsConnected && !failed && Time.realtimeSinceStartup < deadline)
                        yield return null;
                    if (owner != null && !closed && transport.IsConnected)
                    {
                        connected = true;
                        Debug.Log($"[GameConnection] STOMP 연결 완료 session={match.sessionId}");
                        break;
                    }
                    if (closed) break;
                    transport.Disconnect();
                    if (attempt < MaxAttempts && owner != null) yield return new WaitForSecondsRealtime(1f);
                }
            }
            finally
            {
                transport.Errored -= OnFailed;
                transport.Disconnected -= OnFailed;
                preparing = false;
            }
            if (!connected) Close();
            complete(connected);
        }

        private void OnFailed(string _) => failed = true;

        private void Update()
        {
            // 다른 화면으로 이동하면 미완료 준비와 아직 인계하지 않은 연결을 정리한다.
            if (hasPreparingOwner && !gameplayOwnsConnection && !loadingScene && preparingOwner == null) Close();
            if (UsesStartProtocol && protocolAttached && !closed) MaintainStartConnection();
        }

        public void BeginSceneLoad()
        {
            // 이전 씬 소유자의 파괴와 새 씬 Awake 사이에서도 transport를 유지한다.
            loadingScene = true;
        }

        public void MarkSceneReady()
        {
            sceneReady = true;
            nextReady = 0f;
        }

        public void EnsureStartConnection() => MaintainStartConnection();

        public void RetryPreparation()
        {
            if (!UsesStartProtocol || closed) return;
            startFailed = false;
            SetPreparationPhase(everStarted ? "reconnecting" : "connecting");
            preparationDeadline = Time.realtimeSinceStartup + RecoveryTimeoutSeconds;
            recoveryDeadline = Time.realtimeSinceStartup + RecoveryTimeoutSeconds;
            transport.Disconnect();
            StartDisconnected("retry");
        }

        public void CancelPreparation() { if (!everStarted) SendStart("cancel"); }

        public void RecoverSilentConnection()
        {
            if (!UsesStartProtocol || closed || !started) return;
            transport.Disconnect();
            StartDisconnected("frame timeout");
        }

        private void AttachStartProtocol()
        {
            if (!UsesStartProtocol || protocolAttached) return;
            protocolAttached = true;
            preparationDeadline = Time.realtimeSinceStartup + PreparationTimeoutSeconds;
            lastControl = Time.realtimeSinceStartup;
            transport.Connected += StartConnected;
            transport.Disconnected += StartDisconnected;
            transport.Errored += StartDisconnected;
            transport.MessageReceived += ReceiveStartControl;
            if (transport.IsConnected) StartConnected();
        }

        private void MaintainStartConnection()
        {
            if (!UsesStartProtocol || !protocolAttached || closed || startFailed) return;
            float now = Time.realtimeSinceStartup;
            if ((!everStarted && now >= preparationDeadline) ||
                (everStarted && !started && now >= recoveryDeadline && recoveryDeadline > 0f))
            {
                startFailed = true;
                SetPreparationPhase("failed");
                Debug.LogWarning($"[SessionStart] 준비 또는 복구 대기 시간 초과 session={match.sessionId}");
                return;
            }
            if (!transport.IsConnected)
            {
                if (handshakeDeadline > now || nextAttempt > now) return;
                if (handshakeDeadline > 0f) transport.Disconnect();
                if (!match.TryResolveWebSocket("ws", out var endpoint)) { startFailed = true; return; }
                string token = CustomBattleConnection.TokenFor(match, match.GameServerToken(SceneContext.JwtToken));
                handshakeDeadline = now + HandshakeTimeoutSeconds;
                nextAttempt = now + 2f;
                transport.Connect(endpoint.Query("token", token), token);
                return;
            }
            if (now - lastControl >= HandshakeTimeoutSeconds)
            {
                transport.Disconnect();
                StartDisconnected("control timeout");
                return;
            }
            if (now < nextReady || string.IsNullOrEmpty(epoch)) return;
            nextReady = now + 2f;
            SendStart("connectionReady");
            if (sceneReady) SendStart("sceneReady");
            if (!string.IsNullOrEmpty(appliedToken)) SendStart("startReady");
        }

        private void StartConnected()
        {
            handshakeDeadline = 0f;
            lastControl = Time.realtimeSinceStartup;
            nextReady = 0f;
            epoch = null;
            appliedToken = null;
            started = false;
            transport.Subscribe($"/game/{match.sessionId}/start/{SceneContext.UserID}", "start-sub");
        }

        private void StartDisconnected(string _)
        {
            if (closed) return;
            if (everStarted && started) recoveryDeadline = Time.realtimeSinceStartup + RecoveryTimeoutSeconds;
            started = false;
            SetPreparationPhase(everStarted ? "reconnecting" : "connecting");
            loadAllowed = false;
            epoch = null;
            appliedToken = null;
            handshakeDeadline = 0f;
            nextAttempt = Time.realtimeSinceStartup + 2f;
        }

        private void SendStart(string type)
        {
            if (string.IsNullOrEmpty(epoch) || !transport.IsConnected) return;
            var message = new JObject { ["type"] = type, ["epoch"] = epoch };
            if (type == "startReady") message["snapshotToken"] = appliedToken;
            transport.Send($"/app/game/start/{match.sessionId}/{SceneContext.UserID}", message.ToString(Newtonsoft.Json.Formatting.None));
        }

        private void ReceiveStartControl(string subscription, string json)
        {
            if (subscription != "start-sub" || closed) return;
            try
            {
                var message = JObject.Parse(json);
                if ((int?)message["version"] != 1 || string.IsNullOrEmpty((string)message["epoch"])) return;
                lastControl = Time.realtimeSinceStartup;
                string receivedEpoch = (string)message["epoch"];
                if (receivedEpoch != epoch) { epoch = receivedEpoch; appliedToken = null; started = false; }
                switch ((string)message["phase"])
                {
                    case "cancelled":
                        startFailed = true;
                        SetPreparationPhase("failed");
                        Debug.LogWarning($"[SessionStart] 준비 취소 session={match.sessionId}, reason={message["reason"]}");
                        break;
                    case "waiting":
                        SetPreparationPhase("waiting");
                        break;
                    case "loadGame":
                        loadAllowed = true;
                        SetPreparationPhase(string.IsNullOrEmpty(appliedToken) ? "loading" : "waiting");
                        break;
                    case "snapshot":
                        loadAllowed = true;
                        SetPreparationPhase("applying");
                        if (sceneReady && ApplyStartSnapshot != null && appliedToken != (string)message["snapshotToken"] && !string.IsNullOrEmpty((string)message["snapshotToken"]) && message["snapshot"] is JObject snapshot &&
                            ApplyStartSnapshot(snapshot.ToString(Newtonsoft.Json.Formatting.None)))
                            appliedToken = (string)message["snapshotToken"];
                        if (!string.IsNullOrEmpty(appliedToken)) SendStart("startReady");
                        break;
                    case "countdown":
                        if (!string.IsNullOrEmpty(appliedToken)) SetPreparationPhase("countdown", (int?)message["countdown"] ?? 3);
                        break;
                    case "running":
                        if (string.IsNullOrEmpty(appliedToken)) break;
                        started = true;
                        everStarted = true;
                        recoveryDeadline = 0f;
                        SetPreparationPhase("running");
                        break;
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[SessionStart] 준비 응답 적용 실패: {error.Message}");
            }
        }

        public void Close()
        {
            if (closed) return;
            if (UsesStartProtocol && !everStarted) SendStart("cancel");
            closed = true;
            if (current == this) current = null;
            transport?.Disconnect();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (protocolAttached)
            {
                transport.Connected -= StartConnected;
                transport.Disconnected -= StartDisconnected;
                transport.Errored -= StartDisconnected;
                transport.MessageReceived -= ReceiveStartControl;
            }
            if (!closed) transport?.Disconnect();
            if (current == this) current = null;
        }
    }
}
