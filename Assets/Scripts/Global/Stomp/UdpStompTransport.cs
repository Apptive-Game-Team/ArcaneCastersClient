#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Global.Udp;
using UnityEngine;

namespace Global.Stomp
{
    /// <summary>
    /// 에디터 / 네이티브 플랫폼용 UDP 전송 구현. WebGL은 소켓을 열 수 없어 WebSocket을 쓴다.
    ///
    /// STOMP 프레임 대신 <see cref="UdpPacket"/>을 쓰지만 <see cref="IStompTransport"/>의 계약은 같다.
    /// 연결은 HELLO(토큰)와 HELLO_ACK(connectionId)로 맺고, 프레임 토픽 구독은 서버가 HELLO에서
    /// 이미 세션과 유저를 알고 있으므로 이쪽에서 구독 ID만 기억한다.
    ///
    /// 서버→클라이언트: FRAME은 오래된 것을 버리고, MESSAGE는 ack하고 중복을 버린다.
    /// 클라이언트→서버: 핑을 뺀 입력은 ack가 올 때까지 재전송한다.
    /// </summary>
    public class UdpStompTransport : MonoBehaviour, IStompTransport
    {
        private const float HelloIntervalSeconds = 0.3f;
        private const float HandshakeTimeoutSeconds = 1.5f;
        private const float ResendIntervalSeconds = 0.1f;
        private const int MaxResends = 20;

        /// <summary>
        /// 서버는 연결된 플레이어에게 초당 20프레임을 보낸다. 이 시간 동안 아무것도 오지 않으면
        /// UDP는 연결이 없으므로 끊김을 알 방법이 이것뿐이다.
        /// </summary>
        private const float SilenceTimeoutSeconds = 1.5f;

        private const string FrameTopicMarker = "/frameInfos/";
        private static readonly Regex PingType = new Regex("\"type\"\\s*:\\s*\"ping\"", RegexOptions.Compiled);

        public bool IsConnected { get; private set; }

        public event Action Connected;
        public event Action<string> Disconnected;
        public event Action<string> Errored;
        public event Action<string, string> MessageReceived;

        private enum State { Closed, Handshaking, Connected }

        [Serializable]
        internal class HelloBody
        {
            public string token;
            public string sessionId;
            public long userId;
        }

        /// <summary>소켓 하나의 수명. 다시 연결하면 새 링크를 만들어 옛 수신 스레드의 잔여물이 섞이지 않게 한다.</summary>
        private sealed class Link
        {
            public UdpClient Client;
            public readonly ConcurrentQueue<byte[]> Received = new ConcurrentQueue<byte[]>();
            public volatile bool Ready;
            public volatile bool Closed;
            public volatile string Failure;
        }

        private sealed class PendingInput
        {
            public byte[] Datagram;
            public float SentAt;
            public int Attempts;
        }

        private Link _link;
        private State _state = State.Closed;
        private string _helloJson;
        private float _startedAt;
        private float _nextHelloAt;
        private float _lastReceivedAt;
        private ulong _connectionId;
        private uint _inputSeq;
        private string _frameSubscriptionId;

        private bool _hasFrame;
        private uint _lastFrameBase;
        private readonly UdpFragmentAssembler _frameAssembler = new UdpFragmentAssembler();
        private readonly UdpFragmentAssembler _messageAssembler = new UdpFragmentAssembler();
        // 재연결하면 서버가 새 피어를 만들어 seq가 1부터 다시 시작한다. 창을 그대로 두면 새 메시지를 중복으로 버린다.
        private UdpReliableWindow _messageWindow = new UdpReliableWindow();
        private readonly Dictionary<uint, PendingInput> _pendingInputs = new Dictionary<uint, PendingInput>();

        // ─── IStompTransport ─────────────────────────────────────────────────

        public void Connect(string url, string token)
        {
            CloseLink();
            ResetSession();

            string host;
            int port;
            string sessionId;
            long userId;
            if (!UdpEndpoint.TryParse(url, out host, out port, out sessionId, out userId))
            {
                WDebug.LogError("[STOMP:UDP] 접속 주소가 올바르지 않습니다: " + url);
                Fail("invalid udp endpoint", false);
                return;
            }

            _helloJson = JsonUtility.ToJson(new HelloBody { token = token, sessionId = sessionId, userId = userId });
            _state = State.Handshaking;
            _startedAt = Time.unscaledTime;
            _nextHelloAt = 0f;

            Link link = new Link();
            _link = link;
            Task.Run(() => OpenAndReceive(link, host, port));
        }

        public void Subscribe(string topic, string subscriptionId)
        {
            if (topic != null && topic.Contains(FrameTopicMarker))
            {
                _frameSubscriptionId = subscriptionId;
                return;
            }
            // 서버는 HELLO로 알게 된 세션의 프레임만 이 채널로 보낸다. 다른 토픽은 받을 방법이 없다.
            WDebug.LogWarning("[STOMP:UDP] 지원하지 않는 구독입니다: " + topic);
        }

        public void Unsubscribe(string subscriptionId)
        {
            if (_frameSubscriptionId == subscriptionId) _frameSubscriptionId = null;
        }

        public void Send(string topic, string message)
        {
            if (_state != State.Connected) return;

            uint seq = ++_inputSeq;
            bool reliable = !PingType.IsMatch(message);
            byte[] body = Encoding.UTF8.GetBytes(message);
            var packet = new UdpPacket(UdpPacketType.Input, reliable ? UdpPacket.FlagReliable : 0, seq, _connectionId, body);
            byte[] datagram = packet.Encode();

            if (reliable)
            {
                _pendingInputs[seq] = new PendingInput { Datagram = datagram, SentAt = Time.unscaledTime };
            }
            Transmit(datagram);
        }

        public void Disconnect()
        {
            if (_state == State.Connected)
            {
                Transmit(new UdpPacket(UdpPacketType.Bye, 0, ++_inputSeq, _connectionId, null).Encode());
            }

            bool wasOpen = _state != State.Closed;
            CloseLink();
            ResetSession();
            if (wasOpen) Disconnected?.Invoke("Disconnected");
        }

        private void OnDestroy()
        {
            CloseLink();
        }

        // ─── 프레임 루프 ─────────────────────────────────────────────────────

        private void Update()
        {
            Link link = _link;
            if (link == null || _state == State.Closed) return;

            float now = Time.unscaledTime;

            if (link.Failure != null)
            {
                Fail(link.Failure, _state == State.Connected);
                return;
            }

            byte[] data;
            while (link.Received.TryDequeue(out data))
            {
                HandleDatagram(data, now);
                // 처리 중에 끊겼거나 다시 연결됐으면 이 링크의 잔여물은 버린다.
                if (_link != link || _state == State.Closed) return;
            }

            if (_state == State.Handshaking)
            {
                UpdateHandshake(link, now);
            }
            else
            {
                UpdateConnected(now);
            }
        }

        private void UpdateHandshake(Link link, float now)
        {
            if (now - _startedAt > HandshakeTimeoutSeconds)
            {
                Fail("udp handshake timeout", false);
                return;
            }

            if (!link.Ready || now < _nextHelloAt) return;

            _nextHelloAt = now + HelloIntervalSeconds;
            byte[] body = Encoding.UTF8.GetBytes(_helloJson);
            Transmit(new UdpPacket(UdpPacketType.Hello, 0, 1u, 0UL, body).Encode());
        }

        private void UpdateConnected(float now)
        {
            if (now - _lastReceivedAt > SilenceTimeoutSeconds)
            {
                Fail("udp silence", true);
                return;
            }

            if (_pendingInputs.Count == 0) return;

            List<uint> expired = null;
            foreach (KeyValuePair<uint, PendingInput> entry in _pendingInputs)
            {
                PendingInput pending = entry.Value;
                if (now - pending.SentAt < ResendIntervalSeconds) continue;

                if (pending.Attempts >= MaxResends)
                {
                    if (expired == null) expired = new List<uint>();
                    expired.Add(entry.Key);
                    continue;
                }

                pending.Attempts++;
                pending.SentAt = now;
                Transmit(pending.Datagram);
            }

            if (expired == null) return;
            foreach (uint seq in expired)
            {
                _pendingInputs.Remove(seq);
                WDebug.LogWarning("[STOMP:UDP] 입력 " + seq + "번이 ack 없이 재전송 한도를 넘겼습니다");
            }
        }

        // ─── 수신 처리 ───────────────────────────────────────────────────────

        private void HandleDatagram(byte[] data, float now)
        {
            UdpPacket packet;
            if (!UdpPacket.TryDecode(data, data.Length, out packet)) return;

            if (_state == State.Handshaking)
            {
                if (packet.Type == UdpPacketType.HelloAck) OnHelloAck(packet, now);
                else if (packet.Type == UdpPacketType.HelloReject) OnHelloReject(packet);
                return;
            }

            // 연결이 맺어진 뒤에는 서버가 발급한 connectionId를 단 패킷만 믿는다.
            if (packet.ConnectionId != _connectionId) return;
            _lastReceivedAt = now;

            switch (packet.Type)
            {
                case UdpPacketType.InputAck:
                    _pendingInputs.Remove(packet.Seq);
                    break;
                case UdpPacketType.Frame:
                    OnFrame(packet);
                    break;
                case UdpPacketType.Message:
                    OnMessage(packet);
                    break;
                case UdpPacketType.Bye:
                    Fail("server bye", true);
                    break;
            }
        }

        private void OnHelloAck(UdpPacket packet, float now)
        {
            _connectionId = packet.ConnectionId;
            _lastReceivedAt = now;
            _state = State.Connected;
            IsConnected = true;
            WDebug.Log("[STOMP:UDP] 연결됨");
            Connected?.Invoke();
        }

        private void OnHelloReject(UdpPacket packet)
        {
            string reason = Encoding.UTF8.GetString(packet.Body);
            WDebug.LogWarning("[STOMP:UDP] 서버가 접속을 거절했습니다: " + reason);
            Fail("udp rejected: " + reason, false);
        }

        private void OnFrame(UdpPacket packet)
        {
            byte[] payload;
            uint groupBase;
            if (!_frameAssembler.TryAdd(packet, out payload, out groupBase)) return;

            // 델타 프레임은 순서가 바뀌면 상태를 망가뜨린다. 이미 더 새로운 프레임을 적용했다면 버리고,
            // 놓친 만큼은 10프레임마다 오는 sync 프레임이 복구한다.
            if (_hasFrame && groupBase <= _lastFrameBase) return;

            _hasFrame = true;
            _lastFrameBase = groupBase;
            _frameAssembler.DropBelow(groupBase);
            Deliver(payload);
        }

        private void OnMessage(UdpPacket packet)
        {
            // ack는 매번 보낸다. 반복해서 왔다는 건 앞선 ack가 유실됐다는 뜻이다.
            Transmit(new UdpPacket(UdpPacketType.MessageAck, 0, packet.Seq, _connectionId, null).Encode());
            if (!_messageWindow.Accept(packet.Seq)) return;

            byte[] payload;
            uint groupBase;
            if (_messageAssembler.TryAdd(packet, out payload, out groupBase))
            {
                Deliver(payload);
            }
        }

        private void Deliver(byte[] payload)
        {
            if (_frameSubscriptionId == null) return;
            MessageReceived?.Invoke(_frameSubscriptionId, Encoding.UTF8.GetString(payload));
        }

        // ─── 소켓 ────────────────────────────────────────────────────────────

        private static void OpenAndReceive(Link link, string host, int port)
        {
            try
            {
                UdpClient client = new UdpClient();
                client.Connect(host, port);
                link.Client = client;
                if (link.Closed)
                {
                    client.Close();
                    return;
                }
                link.Ready = true;

                while (!link.Closed)
                {
                    IPEndPoint remote = null;
                    link.Received.Enqueue(client.Receive(ref remote));
                }
            }
            catch (Exception ex)
            {
                // 우리가 닫아서 Receive가 깨어난 경우는 실패가 아니다.
                if (!link.Closed) link.Failure = ex.Message;
            }
        }

        private void Transmit(byte[] datagram)
        {
            Link link = _link;
            if (link == null || !link.Ready) return;
            try
            {
                link.Client.Send(datagram, datagram.Length);
            }
            catch (Exception ex)
            {
                if (!link.Closed) link.Failure = ex.Message;
            }
        }

        private void CloseLink()
        {
            Link link = _link;
            _link = null;
            if (link == null) return;

            link.Closed = true;
            try
            {
                if (link.Client != null) link.Client.Close();
            }
            catch (Exception)
            {
                // 이미 닫힌 소켓이다.
            }
        }

        private void ResetSession()
        {
            _state = State.Closed;
            IsConnected = false;
            _connectionId = 0;
            _hasFrame = false;
            _lastFrameBase = 0;
            _pendingInputs.Clear();
            _frameAssembler.Clear();
            _messageAssembler.Clear();
            _messageWindow = new UdpReliableWindow();
        }

        /// <summary>
        /// 연결을 잃었다. 맺기 전이면 Errored, 맺은 뒤면 Disconnected로 알린다.
        /// 구독 ID는 유지해서 다시 연결됐을 때 재구독이 같은 ID를 쓸 수 있게 한다.
        /// </summary>
        private void Fail(string reason, bool wasConnected)
        {
            CloseLink();
            ResetSession();
            WDebug.LogWarning("[STOMP:UDP] " + reason);
            if (wasConnected) Disconnected?.Invoke(reason);
            else Errored?.Invoke(reason);
        }
    }
}
#endif
