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

        private int _generation;

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

        private bool IsCurrent(ClientWebSocket socket, int generation)
            => generation == Volatile.Read(ref _generation) && ReferenceEquals(_ws, socket);

        private void EnqueueMainThread(ClientWebSocket socket, int generation, Action action)
        {
            lock (_queueLock)
                _mainThreadQueue.Enqueue(() => {
                    if (IsCurrent(socket, generation)) action();
                });
        }

        // ─── IStompTransport ─────────────────────────────────────────────────

        public void Connect(string url, string token)
        {
            Disconnect();
            int generation = Interlocked.Increment(ref _generation);
            var socket = new ClientWebSocket();
            var cancellation = new CancellationTokenSource();
            _ws = socket;
            _cts = cancellation;
            _ = ConnectAsync(socket, cancellation, generation, url, token);
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
            // 종료 중인 비동기 작업이 다음 연결의 상태와 CTS를 건드리지 못하게 먼저 분리한다.
            Interlocked.Increment(ref _generation);
            IsConnected = false;
            var socket = _ws;
            var cancellation = _cts;
            _ws = null;
            _cts = null;
            cancellation?.Cancel();
            socket?.Abort();
            socket?.Dispose();
            cancellation?.Dispose();
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        // ─── 내부 구현 ────────────────────────────────────────────────────────

        private async Task ConnectAsync(ClientWebSocket socket, CancellationTokenSource cancellation,
            int generation, string url, string token)
        {
            CancellationToken cancellationToken = cancellation.Token;
            try
            {
                await socket.ConnectAsync(new Uri(url), cancellationToken);
                if (!IsCurrent(socket, generation)) return;

                // STOMP CONNECT 프레임 전송
                string connectFrame = $"CONNECT\naccept-version:1.2\nAuthorization:Bearer {token}\n\n\0";
                await SendFrameAsync(socket, cancellationToken, generation, connectFrame);
                if (!IsCurrent(socket, generation)) return;

                // 수신 루프는 자기가 연 소켓만 담당한다. 필드를 매번 다시 읽으면 나중에
                // 열린 소켓의 상태를 옛 루프가 읽고 새 연결에 대해 종료를 보고하게 된다.
                _ = ReceiveLoop(socket, cancellationToken, generation);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                // 예외 메시지에는 인증 query가 들어갈 수 있으므로 종류만 전달한다.
                EnqueueMainThread(socket, generation, () => {
                    IsConnected = false;
                    Errored?.Invoke("STOMP connection failed: " + ex.GetType().Name);
                });
            }
        }

        private async Task ReceiveLoop(ClientWebSocket socket, CancellationToken token, int generation)
        {
            var buffer = new ArraySegment<byte>(new byte[65536]);
            var sb = new StringBuilder();

            // 아무 이벤트도 없이 빠지는 경로를 없앤다. 서버가 Close 프레임을 보낸 경우와
            // 소켓 상태가 Open을 벗어나 while 조건이 거짓이 된 경우가 정상 종료다.
            // 예외 경로는 Errored를 큐에 넣고, 취소는 의도한 종료라 조용히 빠진다.
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

                    ProcessIncomingData(sb.ToString(), socket, generation);
                    sb.Clear();
                }
                catch (OperationCanceledException)
                {
                    cleanClose = false;
                    break;
                }
                catch (Exception ex)
                {
                    EnqueueMainThread(socket, generation, () => {
                        IsConnected = false;
                        Errored?.Invoke("STOMP receive failed: " + ex.GetType().Name);
                    });
                    cleanClose = false;
                    break;
                }
            }

            closed:
            // 이 루프가 담당하던 소켓이 이미 교체됐으면 새 연결의 상태를 건드리지 않는다.
            if (!cleanClose || !IsCurrent(socket, generation)) return;
            EnqueueMainThread(socket, generation, () => {
                IsConnected = false;
                Disconnected?.Invoke("WebSocket closed");
            });
        }

        private void ProcessIncomingData(string raw, ClientWebSocket socket, int generation)
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
                    ParseAndDispatch(frame, socket, generation);
            }
        }

        private void ParseAndDispatch(string frame, ClientWebSocket socket, int generation)
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
                    EnqueueMainThread(socket, generation, () => {
                        IsConnected = true;
                        Connected?.Invoke();
                    });
                    break;

                case "MESSAGE":
                    headers.TryGetValue("subscription", out string subId);
                    string capturedBody = body;
                    string capturedSubId = subId;
                    EnqueueMainThread(socket, generation, () => MessageReceived?.Invoke(capturedSubId, capturedBody));
                    break;

                case "ERROR":
                    EnqueueMainThread(socket, generation, () => {
                        IsConnected = false;
                        Errored?.Invoke("STOMP server error");
                    });
                    break;
            }
        }

        private Task SendFrameAsync(string frame)
        {
            var socket = _ws;
            int generation = Volatile.Read(ref _generation);
            var cancellationToken = _cts?.Token ?? CancellationToken.None;
            return SendFrameAsync(socket, cancellationToken, generation, frame);
        }

        private async Task SendFrameAsync(ClientWebSocket socket, CancellationToken cancellationToken,
            int generation, string frame)
        {
            if (socket == null || !IsCurrent(socket, generation) || socket.State != WebSocketState.Open) return;
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
                // 의도한 종료는 전송 취소를 오류로 통보하지 않는다.
            }
            catch (Exception ex)
            {
                EnqueueMainThread(socket, generation, () => {
                    IsConnected = false;
                    Errored?.Invoke("STOMP send failed: " + ex.GetType().Name);
                });
            }
        }
    }
}
#endif
