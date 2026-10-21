#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Global.Stomp
{
    /// <summary>
    /// Editor / 네이티브 플랫폼용 STOMP 전송 구현.
    /// System.Net.WebSockets.ClientWebSocket 위에 STOMP 1.2 프로토콜을 직접 구현.
    /// 에디터에서 실제 서버와 연결해 테스트할 수 있다.
    /// </summary>
    public class NativeStompTransport : MonoBehaviour, IStompTransport
    {
        public bool IsConnected { get; private set; }

        public event Action Connected;
        public event Action<string> Disconnected;
        public event Action<string> Errored;
        public event Action<string, string> MessageReceived;

        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;

        /// <summary>
        /// 우리 쪽에서 종료를 요청했는지. 요청한 종료는 <see cref="DisconnectAsync"/>가
        /// 이미 Disconnected를 올리므로, 수신 루프가 같은 사실을 한 번 더 알리지 않게 한다.
        /// </summary>
        private bool _closeRequested;

        // 백그라운드 스레드 → 메인 스레드 디스패치 큐
        private readonly Queue<Action> _mainThreadQueue = new Queue<Action>();
        private readonly object _queueLock = new object();

        private void Update()
        {
            lock (_queueLock)
            {
                while (_mainThreadQueue.Count > 0)
                    _mainThreadQueue.Dequeue().Invoke();
            }
        }

        private void EnqueueMainThread(Action action)
        {
            lock (_queueLock)
                _mainThreadQueue.Enqueue(action);
        }

        // ─── IStompTransport ─────────────────────────────────────────────────

        public void Connect(string url, string token)
        {
            _ = ConnectAsync(url, token);
        }

        public void Subscribe(string topic, string subscriptionId)
        {
            _ = SendFrameAsync($"SUBSCRIBE\ndestination:{topic}\nid:{subscriptionId}\n\n\0");
        }

        public void Unsubscribe(string subscriptionId)
        {
            _ = SendFrameAsync($"UNSUBSCRIBE\nid:{subscriptionId}\n\n\0");
        }

        public void Send(string topic, string message)
        {
            _ = SendFrameAsync($"SEND\ndestination:{topic}\ncontent-type:application/json\n\n{message}\0");
        }

        public void Disconnect()
        {
            _ = DisconnectAsync();
        }

        private void OnDestroy()
        {
            _closeRequested = true;
            _cts?.Cancel();
            _ws?.Dispose();
        }

        // ─── 내부 구현 ────────────────────────────────────────────────────────

        private async Task ConnectAsync(string url, string token)
        {
            try
            {
                _closeRequested = false;
                _cts = new CancellationTokenSource();

                var socket = new ClientWebSocket();
                _ws = socket;

                await socket.ConnectAsync(new Uri(url), _cts.Token);

                // STOMP CONNECT 프레임 전송
                string connectFrame = $"CONNECT\naccept-version:1.2\nAuthorization:Bearer {token}\n\n\0";
                await SendFrameAsync(connectFrame);

                // 수신 루프는 자기가 연 소켓만 담당한다. 필드를 매번 다시 읽으면 나중에
                // 열린 소켓의 상태를 옛 루프가 읽고 새 연결에 대해 종료를 보고하게 된다.
                _ = ReceiveLoop(socket, _cts.Token);
            }
            catch (Exception ex)
            {
                WDebug.LogError("[STOMP:Native] 연결 실패: " + ex.Message);
                EnqueueMainThread(() => Errored?.Invoke(ex.Message));
            }
        }

        private async Task ReceiveLoop(ClientWebSocket socket, CancellationToken token)
        {
            var buffer = new ArraySegment<byte>(new byte[65536]);
            var sb = new StringBuilder();

            // 아무 이벤트도 없이 빠지는 경로를 없앤다. 서버가 Close 프레임을 보낸 경우와
            // 소켓 상태가 Open을 벗어나 while 조건이 거짓이 된 경우가 정상 종료다.
            // 예외 경로는 이미 Errored를 올렸고, 취소는 우리가 의도한 종료라 조용히 빠진다.
            bool cleanClose = true;

            while (socket.State == WebSocketState.Open)
            {
                try
                {
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await socket.ReceiveAsync(buffer, token);
                        if (result.MessageType == WebSocketMessageType.Close) goto closed;
                        sb.Append(Encoding.UTF8.GetString(buffer.Array, 0, result.Count));
                    } while (!result.EndOfMessage);

                    ProcessIncomingData(sb.ToString());
                    sb.Clear();
                }
                catch (OperationCanceledException)
                {
                    cleanClose = false;
                    break;
                }
                catch (Exception ex)
                {
                    WDebug.LogError("[STOMP:Native] 수신 오류: " + ex.Message);
                    EnqueueMainThread(() => Errored?.Invoke(ex.Message));
                    cleanClose = false;
                    break;
                }
            }

            closed:
            // 이 루프가 담당하던 소켓이 이미 교체됐으면 새 연결의 상태를 건드리지 않는다.
            if (!ReferenceEquals(_ws, socket)) return;

            IsConnected = false;

            // 우리가 요청한 종료는 DisconnectAsync가 알린다. 여기서 또 알리면 중복이다.
            if (!cleanClose || _closeRequested) return;

            string reason = socket.CloseStatusDescription;
            string message = string.IsNullOrEmpty(reason) ? "socket closed" : reason;
            WDebug.Log("[STOMP:Native] 서버가 소켓을 닫았습니다: " + message);
            EnqueueMainThread(() => Disconnected?.Invoke(message));
        }

        private void ProcessIncomingData(string raw)
        {
            // 단일 WebSocket 메시지에 여러 STOMP 프레임이 포함될 수 있으므로 분리
            int start = 0;
            while (start < raw.Length)
            {
                int nullPos = raw.IndexOf('\0', start);
                if (nullPos < 0) break;

                string frame = raw.Substring(start, nullPos - start);
                start = nullPos + 1;
                if (!string.IsNullOrWhiteSpace(frame))
                    ParseAndDispatch(frame);
            }
        }

        private void ParseAndDispatch(string frame)
        {
            int headerEnd = frame.IndexOf("\n\n", StringComparison.Ordinal);
            string headerSection = headerEnd >= 0 ? frame.Substring(0, headerEnd) : frame;
            string body = headerEnd >= 0 ? frame.Substring(headerEnd + 2) : string.Empty;

            string[] lines = headerSection.Split('\n');
            if (lines.Length == 0) return;

            string command = lines[0].Trim();
            var headers = new Dictionary<string, string>();
            for (int i = 1; i < lines.Length; i++)
            {
                int colon = lines[i].IndexOf(':');
                if (colon > 0)
                    headers[lines[i].Substring(0, colon).Trim()] = lines[i].Substring(colon + 1).Trim();
            }

            switch (command)
            {
                case "CONNECTED":
                    IsConnected = true;
                    WDebug.Log("[STOMP:Native] 연결됨");
                    EnqueueMainThread(() => Connected?.Invoke());
                    break;

                case "MESSAGE":
                    headers.TryGetValue("subscription", out string subId);
                    string capturedBody = body;
                    string capturedSubId = subId;
                    EnqueueMainThread(() => MessageReceived?.Invoke(capturedSubId, capturedBody));
                    break;

                case "ERROR":
                    IsConnected = false;
                    string errorBody = body;
                    WDebug.LogError("[STOMP:Native] 서버 에러: " + errorBody);
                    EnqueueMainThread(() => Errored?.Invoke(errorBody));
                    break;
            }
        }

        private async Task SendFrameAsync(string frame)
        {
            var socket = _ws;
            if (socket == null || socket.State != WebSocketState.Open) return;
            var cancellationToken = _cts?.Token ?? CancellationToken.None;
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(frame);
                await socket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    cancellationToken
                );
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Disconnecting or destroying this transport cancels pending sends.
            }
            catch (Exception ex)
            {
                WDebug.LogError("[STOMP:Native] 전송 실패: " + ex.Message);
            }
        }

        private async Task DisconnectAsync()
        {
            _closeRequested = true;
            try
            {
                if (_ws?.State == WebSocketState.Open)
                {
                    await SendFrameAsync("DISCONNECT\n\n\0");
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "disconnect", CancellationToken.None);
                }
            }
            finally
            {
                IsConnected = false;
                _cts?.Cancel();
                EnqueueMainThread(() => Disconnected?.Invoke("Disconnected"));
            }
        }
    }
}
#endif
