using System;
using System.Collections;
using Data;
using Data.Net;
using GameScene.Dto;
using GameScene.Handler;
using Global;
using Global.Stomp;
using UnityEngine;
using UnityEngine.Localization;

namespace GameScene
{
    /// <summary>
    /// 게임씬 STOMP 오케스트레이터.
    /// 플랫폼별 IStompTransport를 주입받아 연결·구독·재연결을 관리한다.
    ///
    /// 의존 관계:
    ///   StompConnector (게임 로직)
    ///     ├─ IStompTransport            ← WebGLStompTransport | NativeStompTransport
    ///     ├─ StompSubscriptionRegistry  ← 구독 보관 및 재구독
    ///     ├─ StompReconnectController   ← 재연결 사다리 (인게임은 짧은 고정 간격)
    ///     ├─ ConnectAttemptSchedule     ← 최초 연결 재시도 예산
    ///     └─ FrameSilenceWatch          ← FrameInfo 무음 단계 판정
    ///
    /// 끊김 처리는 두 단계다. 무음 1초에는 씬을 유지한 채 제자리 재연결만 시도하고,
    /// 무음 3초 또는 재연결 사다리 소진에서 세션을 신고하고 메인으로 돌아간다.
    /// </summary>
    public class StompConnector : LocalSingletonObject<StompConnector>
    {
        private const string StompPath = "ws";
        private const string MessageTable = "SystemMessageUI";

        /// <summary>인게임 재연결 간격(초). 합이 무음 3초 이탈 예산 안에 들어가야 한다.</summary>
        private static readonly float[] DefaultReconnectDelays = { 0f, 0.5f, 1f };

        [SerializeField] private bool isSpectator;

        [Tooltip("인게임 재연결 간격(초). 비워두면 즉시 → 0.5초 → 1초를 쓴다.")]
        [SerializeField] private float[] inGameReconnectDelays = { 0f, 0.5f, 1f };

        public LocalizedString notConnectedToServer;
        public LocalizedString connectionClosed;
        public LocalizedString connectionDelayed;
        public LocalizedString frameTimeout;

        /// <summary>
        /// 인스펙터에 연결된 필드가 아니라 코드에서 테이블을 직접 가리킨다.
        /// 기존 프리팹을 다시 배선하지 않아도 문구가 나온다.
        /// </summary>
        private static readonly LocalizedString reconnecting = new LocalizedString
            { TableReference = MessageTable, TableEntryReference = "sessionReconnecting" };

        private IStompTransport _transport;
        private StompSubscriptionRegistry _registry;
        private StompReconnectController _reconnect;

        /// <summary>화면에 보인 PVE 대사 번호. 재연결 때 pveSync 에 실어 보낸다.</summary>
        private readonly PveSyncState _pveSync = new PveSyncState();

        private IFrameInfoHandler<string> _frameInfoHandler;

        /// <summary>frame 구독을 걸었는지. 그 전에 연결되는 것은 최초 연결이라 pveSync 를 보내지 않는다.</summary>
        private bool _frameSubscribed;
        private readonly FrameSilenceWatch _silence = new FrameSilenceWatch();
        private readonly ConnectAttemptSchedule _connectSchedule = new ConnectAttemptSchedule();
        private readonly StompConnectGate _connectGate = new StompConnectGate();

        /// <summary>이탈 신고가 진행 중인지. 여러 이탈 경로가 동시에 터져도 한 번만 신고한다.</summary>
        private bool _reportingSessionLoss;

        /// <summary>
        /// 우리가 의도해서 끊는 중인지. 씬을 떠나며 닫는 소켓은 끊김이 아니므로
        /// 재연결 사다리를 걸면 안 된다.
        /// </summary>
        private bool _intentionalDisconnect;

        /// <summary>
        /// 경기 결과를 받았는지. 서버는 결과를 보낸 뒤 FrameInfo를 더 보내지 않으므로
        /// 결과 화면으로 넘어가기 전까지의 무음과 끊김은 연결 문제가 아니다.
        /// </summary>
        private bool _matchEnded;

        // ─── 생명주기 ────────────────────────────────────────────────────────

        protected override void Awake()
        {
            gameObject.name = "StompConnector";
            base.Awake();

            _frameInfoHandler = new GeneralHandler(_pveSync);
            // 새 경기마다 tick rate 를 기본값으로 되돌린다. 실제 값은 첫 sync 가 알려 준다.
            GameConfig.SetTickRate(GameConfig.DEFAULT_TICK_RATE);

            _registry = new StompSubscriptionRegistry();
            _reconnect = gameObject.AddComponent<StompReconnectController>();

            // 이탈 예산이 3초뿐이라 기본 지수 백오프(1→2→4→8→16초)로는 첫 재시도도 들어가지 않는다.
            _reconnect.UseFixedSchedule(
                inGameReconnectDelays != null && inGameReconnectDelays.Length > 0
                    ? inGameReconnectDelays
                    : DefaultReconnectDelays);

#if UNITY_WEBGL && !UNITY_EDITOR
            _transport = gameObject.AddComponent<WebGLStompTransport>();
#else
            _transport = gameObject.AddComponent<NativeStompTransport>();
#endif

            _transport.Connected += HandleConnected;
            _transport.Disconnected += HandleDisconnected;
            _transport.Errored += HandleError;
            _transport.MessageReceived += _registry.Dispatch;

            _reconnect.OnReconnectAttempt += ConnectToServer;
            _reconnect.OnMaxRetriesExceeded += HandleMaxRetriesExceeded;
        }

        private void OnEnable()
        {
            Application.focusChanged += HandleFocusChanged;
        }

        private void OnDisable()
        {
            Application.focusChanged -= HandleFocusChanged;
        }

        private void Start()
        {
            ConnectToServer();
            StartCoroutine(GameFlowCoroutine(SceneContext.MatchInfo.sessionId));
        }

        private void OnDestroy()
        {
            // 플래그를 먼저 세운다. 닫는 과정에서 올라오는 Disconnected는 끊김이 아니다.
            _intentionalDisconnect = true;
            _transport.Disconnect();
        }

        /// <summary>
        /// 브라우저는 백그라운드 탭의 루프를 스로틀링한다. 돌아왔을 때 밀려 있는 시간은
        /// 연결이 끊겼다는 증거가 아니므로 무음 판정을 처음부터 다시 시작한다.
        /// </summary>
        private void HandleFocusChanged(bool hasFocus)
        {
            if (!hasFocus) return;
            _silence.NoteSignal(Time.unscaledTime);
        }

        // ─── 퍼블릭 API ─────────────────────────────────────────────────────

        public void ConnectToServer()
        {
            if (_intentionalDisconnect || _matchEnded) return;

            float now = Time.unscaledTime;
            switch (_connectGate.Decide(_transport.IsConnected, now))
            {
                case ConnectGateDecision.AlreadyConnected:
                    // 소켓이 열린 채 데이터만 끊긴 무음 정지가 여기로 온다. 소켓을 강제로 닫고
                    // 다시 여는 것은 두 transport 모두 종료가 비동기라 새 연결 상태를 덮어쓸 수
                    // 있어 하지 않는다. 이 경우 사다리는 그대로 소진되고 신고 경로로 넘어간다.
                    WDebug.Log("[STOMP] 소켓이 열려 있어 재연결을 건너뜁니다");
                    return;

                case ConnectGateDecision.HandshakeInFlight:
                    // 여기서 다시 열면 앞선 핸드셰이크가 닫히고 처음부터 다시 시작된다.
                    // 사다리 간격보다 느린 회선에서는 그러면 영영 연결되지 않는다.
                    WDebug.Log("[STOMP] 핸드셰이크가 진행 중이라 재연결을 건너뜁니다");
                    return;
            }

            MatchedInfoDto matchInfo = SceneContext.MatchInfo;
            if (!matchInfo.TryResolveWebSocket(StompPath, out ServerEndpoint gameServer))
            {
                _connectGate.NoteSettled();
                WDebug.LogError($"[STOMP] 게임 서버 URL을 해석하지 못했습니다: {matchInfo.ConnectionSource}");
                return;
            }

            string url = gameServer.Query("token", SceneContext.JwtToken);
            _connectGate.NoteHandshakeStarted(now);
            _transport.Connect(url, SceneContext.JwtToken);
        }

        public void SendMessageToServer(string topic, string message)
        {
            if (!_transport.IsConnected)
            {
                SystemMessageUI.Instance.ShowMessage(notConnectedToServer);
                WDebug.LogError("[STOMP] 미연결 상태에서 메시지 전송 시도: " + topic);
                return;
            }
            _transport.Send(topic, message);
        }

        public void SubscribeToTopic(string topic, Action<string> callback, string subscriptionId)
        {
            if (!_transport.IsConnected)
            {
                SystemMessageUI.Instance.ShowMessage(notConnectedToServer);
                WDebug.LogError("[STOMP] 미연결 상태에서 구독 시도: " + topic);
                return;
            }
            _registry.Register(topic, subscriptionId, callback);
            _transport.Subscribe(topic, subscriptionId);
        }

        public void UnsubscribeFromTopic(string subscriptionId)
        {
            _registry.Unregister(subscriptionId);
            if (_transport.IsConnected)
                _transport.Unsubscribe(subscriptionId);
        }

        /// <summary>
        /// 결과를 받았다고 알린다. 무음 감시와 재연결을 멈춰, 결과 화면으로 넘어가기 전
        /// 대기 시간에 재연결 문구가 뜨거나 이탈 신고가 나가지 않게 한다.
        /// </summary>
        public void NotifyMatchEnded()
        {
            _matchEnded = true;
            _reconnect.ResetRetries();
        }

        // ─── 게임 플로우 ─────────────────────────────────────────────────────

        private IEnumerator GameFlowCoroutine(string sessionId)
        {
            yield return AwaitConnection();
            if (!_transport.IsConnected) yield break;

            UnsubscribeFromTopic("match-sub");
            long userId = isSpectator ? 0 : SceneContext.UserID;
            _pveSync.Reset();
            SubscribeToTopic($"/game/{sessionId}/frameInfos/{userId}", OnFrameInfoReceived, "frame-sub");
            _frameSubscribed = true;
            RequestPveSync();

            yield return WatchFrameSilence();
        }

        /// <summary>
        /// 소켓이 열릴 때까지 기다린다. 예산 안에 열리지 않으면 다시 걸고, 시도를 모두 쓰면 신고한다.
        /// </summary>
        private IEnumerator AwaitConnection()
        {
            while (!_transport.IsConnected)
            {
                yield return null;

                ConnectAttemptDecision decision = _connectSchedule.Advance(Time.unscaledDeltaTime);
                if (decision == ConnectAttemptDecision.Wait) continue;

                if (decision == ConnectAttemptDecision.Retry)
                {
                    WDebug.LogWarning($"[STOMP] 연결 재시도 ({_connectSchedule.Attempt}/{_connectSchedule.MaxAttempts})");
                    SystemMessageUI.Instance.ShowMessage(reconnecting);
                    ConnectToServer();
                    continue;
                }

                WDebug.LogError($"[STOMP] 연결 실패 – {_connectSchedule.MaxAttempts}회 시도");
                SystemMessageUI.Instance.ShowMessage(connectionClosed);

                bool sessionAlive = false;
                yield return AbandonSession(SessionLossReason.ConnectTimeout, () => sessionAlive = true);
                if (!sessionAlive) yield break;

                // 세션이 살아있다는 판정이다. 이탈하지 않고 예산을 되돌려 다시 시도한다.
                _connectSchedule.Reset();
                ConnectToServer();
            }
        }

        /// <summary>
        /// FrameInfo 무음을 감시한다.
        ///
        /// 소켓이 열린 채 데이터만 끊기는 정지에서는 transport가 Errored를 내지 않아
        /// 재연결 사다리가 저절로 돌지 않는다. 무음 자체를 끊김 신호로 삼아 사다리를 건다.
        /// </summary>
        private IEnumerator WatchFrameSilence()
        {
            _silence.NoteSignal(Time.unscaledTime);

            while (true)
            {
                yield return null;

                if (_matchEnded) yield break;

                SilenceEscalation escalation = _silence.Tick(Time.unscaledTime, Time.unscaledDeltaTime);
                if (escalation == SilenceEscalation.None) continue;

                if (escalation == SilenceEscalation.Reconnect)
                {
                    WDebug.LogWarning($"[STOMP] FrameInfo 무음 {_silence.SilenceSeconds(Time.unscaledTime):F1}초 – 제자리 재연결");
                    SystemMessageUI.Instance.ShowMessage(reconnecting);
                    _reconnect.NotifyConnectionLost();
                    continue;
                }

                WDebug.LogError($"[STOMP] FrameInfo 무음 {_silence.SilenceSeconds(Time.unscaledTime):F1}초 – 세션 포기");
                SystemMessageUI.Instance.ShowMessage(frameTimeout.IsEmpty ? connectionDelayed : frameTimeout);

                bool sessionAlive = false;
                yield return AbandonSession(SessionLossReason.FrameInfoTimeout, () => sessionAlive = true);
                if (!sessionAlive) yield break;

                // 세션이 살아있다는 판정이다. 감시를 처음부터 다시 시작한다.
                _silence.NoteSignal(Time.unscaledTime);
                RetryFromScratch();
            }
        }

        private void OnFrameInfoReceived(string json)
        {
            _silence.NoteSignal(Time.unscaledTime);
            _frameInfoHandler.Handler(json);
        }

        // ─── 연결 이벤트 핸들러 ──────────────────────────────────────────────

        private void HandleConnected()
        {
            WDebug.Log("[STOMP] 연결됨 – 기존 구독 복구 중");
            _connectGate.NoteSettled();
            _reconnect.ResetRetries();
            _registry.ResubscribeAll(_transport);
            if (_frameSubscribed)
            {
                RequestPveSync();
            }
        }

        /// <summary>서버가 목표와 놓친 대사를 다시 보내게 한다. 관전자와 PVE 가 아닌 판은 보내지 않는다.</summary>
        private void RequestPveSync()
        {
            if (isSpectator) return;
            PveSyncSender.Send(_pveSync.LastEventSeq);
        }

        /// <summary>
        /// 소켓이 닫혔다. 서버 재시작·세션 정리·유휴 타임아웃으로 서버가 정상 종료(clean close)한
        /// 경우가 여기로 오며, 이는 에러 없이 연결이 사라지는 유일한 경로다. 사다리를 걸지 않으면
        /// 재연결이 단 한 번도 일어나지 않는다.
        /// </summary>
        private void HandleDisconnected(string message)
        {
            WDebug.Log("[STOMP] 연결 종료: " + message);
            _connectGate.NoteSettled();

            if (_intentionalDisconnect || _matchEnded)
            {
                // OnDestroy에서 우리가 닫은 소켓이거나 경기가 이미 끝났다. 씬을 떠나는 중에
                // 사다리를 걸면 의도한 종료가 재연결 루프로 바뀐다.
                return;
            }

            SystemMessageUI.Instance.ShowMessage(connectionClosed);
            _reconnect.NotifyConnectionLost();
        }

        private void HandleError(string error)
        {
            WDebug.LogError("[STOMP] 에러: " + error);
            _connectGate.NoteSettled();

            if (_intentionalDisconnect || _matchEnded) return;

            SystemMessageUI.Instance.ShowMessage(connectionDelayed);
            _reconnect.NotifyConnectionLost();
        }

        private void HandleMaxRetriesExceeded()
        {
            if (_matchEnded) return;

            WDebug.LogError("[STOMP] 재연결 불가 – 최대 횟수 초과");
            StartCoroutine(AbandonSession(SessionLossReason.ReconnectAttemptsExhausted, RetryFromScratch));
        }

        /// <summary>세션이 살아있다는 판정을 받았을 때 재연결 예산을 되돌리고 다시 붙는다.</summary>
        private void RetryFromScratch()
        {
            _reconnect.ResetRetries();
            ConnectToServer();
        }

        // ─── 이탈 ───────────────────────────────────────────────────────────

        /// <summary>
        /// 세션 소실을 서버에 신고하고 판정대로 처리한다. 이탈이면 로비로 돌아가고,
        /// 세션이 살아있다는 판정이면 <paramref name="onSessionAlive"/>를 부른 뒤 게임씬에 남는다.
        /// </summary>
        private IEnumerator AbandonSession(SessionLossReason reason, Action onSessionAlive)
        {
            if (_reportingSessionLoss)
            {
                // 다른 이탈 경로가 이미 신고 중이다. 중복 신고하지 않고 그 판정을 함께 따른다.
                // 이탈로 판정되면 로비 로드로 이 코루틴도 함께 사라진다.
                while (_reportingSessionLoss) yield return null;
                onSessionAlive?.Invoke();
                yield break;
            }

            _reportingSessionLoss = true;

            yield return SessionLossReporter.ReportAndLeave(reason, () =>
            {
                _reportingSessionLoss = false;
                onSessionAlive?.Invoke();
            });
        }
    }
}
