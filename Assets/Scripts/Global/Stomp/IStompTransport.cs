using System;

namespace Global.Stomp
{
    /// <summary>
    /// STOMP 전송 계층 추상화. 플랫폼별 구현(WebGL, Native)으로 교체 가능.
    /// </summary>
    public interface IStompTransport
    {
        bool IsConnected { get; }

        /// <summary>서버와 연결 성공 시 발생</summary>
        event Action Connected;

        /// <summary>연결이 종료되었을 때 발생 (message: 종료 사유)</summary>
        event Action<string> Disconnected;

        /// <summary>에러 발생 시 (error: 에러 메시지)</summary>
        event Action<string> Errored;
        event Action<string> Rejected;

        /// <summary>구독한 토픽에서 메시지 수신 시 (subscriptionId, body)</summary>
        event Action<string, string> MessageReceived;

        void Connect(string url, string token);
        void Subscribe(string topic, string subscriptionId);
        void Unsubscribe(string subscriptionId);
        void Send(string topic, string message);
        void Disconnect();
    }
}
