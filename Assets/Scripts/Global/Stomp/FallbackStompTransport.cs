using System;

namespace Global.Stomp
{
    /// <summary>
    /// 선호 전송(UDP)으로 먼저 붙고, 그쪽이 한 번이라도 실패하면 대체 전송(WebSocket)으로 넘어간다.
    ///
    /// 실패 이벤트는 그대로 위로 올린다. 재연결 사다리와 의도한 종료 판정은 StompConnector가
    /// 이미 갖고 있으므로, 여기서는 다음 <see cref="Connect"/>가 어느 전송을 쓸지만 바꾼다.
    /// 한 번 넘어가면 이 씬에서는 되돌아가지 않는다. UDP가 막힌 망에서 두 전송을 오가면
    /// 그때마다 핸드셰이크 시간을 잃기 때문이다.
    /// </summary>
    public sealed class FallbackStompTransport : IStompTransport
    {
        private readonly IStompTransport _preferred;
        private readonly IStompTransport _fallback;
        private readonly Func<string> _preferredUrl;

        private IStompTransport _active;
        private bool _preferredFailed;
        private bool _closing;

        /// <param name="preferredUrl">선호 전송의 접속 주소. 서버가 알려주지 않았으면 null을 돌려준다.</param>
        public FallbackStompTransport(IStompTransport preferred, IStompTransport fallback, Func<string> preferredUrl)
        {
            _preferred = preferred;
            _fallback = fallback;
            _preferredUrl = preferredUrl;

            Wire(preferred, isPreferred: true);
            Wire(fallback, isPreferred: false);
        }

        public bool IsConnected
        {
            get { return _active != null && _active.IsConnected; }
        }

        public event Action Connected;
        public event Action<string> Disconnected;
        public event Action<string> Errored;
        public event Action<string, string> MessageReceived;

        public void Connect(string url, string token)
        {
            _closing = false;

            string preferredUrl = _preferredFailed ? null : _preferredUrl();
            if (preferredUrl != null)
            {
                _active = _preferred;
                WDebug.Log("[STOMP] UDP로 접속합니다");
                _preferred.Connect(preferredUrl, token);
                return;
            }

            _active = _fallback;
            WDebug.Log(_preferredFailed ? "[STOMP] UDP가 실패해 WebSocket으로 접속합니다" : "[STOMP] WebSocket으로 접속합니다");
            _fallback.Connect(url, token);
        }

        public void Subscribe(string topic, string subscriptionId)
        {
            if (_active != null) _active.Subscribe(topic, subscriptionId);
        }

        public void Unsubscribe(string subscriptionId)
        {
            if (_active != null) _active.Unsubscribe(subscriptionId);
        }

        public void Send(string topic, string message)
        {
            if (_active != null) _active.Send(topic, message);
        }

        public void Disconnect()
        {
            _closing = true;
            // 쓰지 않은 쪽도 닫는다. 이벤트는 활성 전송의 것만 위로 올라간다.
            _preferred.Disconnect();
            _fallback.Disconnect();
        }

        private void Wire(IStompTransport transport, bool isPreferred)
        {
            transport.Connected += () =>
            {
                if (_active == transport) Connected?.Invoke();
            };
            transport.Disconnected += message =>
            {
                if (_active != transport) return;
                if (isPreferred && !_closing) _preferredFailed = true;
                Disconnected?.Invoke(message);
            };
            transport.Errored += error =>
            {
                if (_active != transport) return;
                if (isPreferred && !_closing) _preferredFailed = true;
                Errored?.Invoke(error);
            };
            transport.MessageReceived += (subscriptionId, body) =>
            {
                if (_active == transport) MessageReceived?.Invoke(subscriptionId, body);
            };
        }
    }
}
