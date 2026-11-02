#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using System;
using UnityEngine;
using Global.Serialization;

namespace Global.Stomp
{
    /// <summary>
    /// WebGL 빌드 전용 STOMP 전송 구현.
    /// JavaScript(StompPlugin.jslib)와 Unity SendMessage로 통신.
    ///
    /// JS → C# 콜백 메서드: OnConnected, OnError, OnDisconnected, OnSubscriptionMessage
    /// (이 컴포넌트의 GameObject에 SendMessage로 전달됨)
    /// </summary>
    public class WebGLStompTransport : MonoBehaviour, IStompTransport
    {
        public bool IsConnected { get; private set; }

        public event Action Connected;
        public event Action<string> Disconnected;
        public event Action<string> Errored;
        public event Action<string, string> MessageReceived;
        private string callbackObject;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void ConnectStompSocket(string url, string token, string callbackObject);

        [DllImport("__Internal")]
        private static extern void SubscribeStomp(string topic, string subscriptionId, string callbackObject);

        [DllImport("__Internal")]
        private static extern void SendStomp(string topic, string message, string callbackObject);

        [DllImport("__Internal")]
        private static extern void UnsubscribeStomp(string subscriptionId, string callbackObject);

        [DllImport("__Internal")]
        private static extern void DisconnectStomp(string callbackObject);
#endif

        public void Connect(string url, string token)
        {
            IsConnected = false;
            callbackObject = gameObject.name;
#if UNITY_WEBGL && !UNITY_EDITOR
            ConnectStompSocket(url, token, callbackObject);
#endif
        }

        public void Subscribe(string topic, string subscriptionId)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SubscribeStomp(topic, subscriptionId, callbackObject);
#endif
        }

        public void Unsubscribe(string subscriptionId)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            UnsubscribeStomp(subscriptionId, callbackObject);
#endif
        }

        public void Send(string topic, string message)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SendStomp(topic, message, callbackObject);
#endif
        }

        public void Disconnect()
        {
            IsConnected = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (callbackObject != null) DisconnectStomp(callbackObject);
#endif
        }

        // ─── JS → C# 콜백 (Unity SendMessage로 호출됨) ───────────────────────

        private void OnDestroy() => Disconnect();

        /// <summary>JS가 현재 연결의 STOMP CONNECTED를 전달한다.</summary>
        private void OnConnected(string frame)
        {
            WDebug.Log("[STOMP:WebGL] 연결됨");
            IsConnected = true;
            Connected?.Invoke();
        }

        /// <summary>JS가 현재 연결의 오류를 전달한다.</summary>
        private void OnError(string error)
        {
            WDebug.LogError("[STOMP:WebGL] 에러: " + error);
            IsConnected = false;
            Errored?.Invoke(error);
        }

        /// <summary>JS가 서버에서 닫은 현재 연결을 전달한다.</summary>
        private void OnDisconnected(string message)
        {
            WDebug.Log("[STOMP:WebGL] 연결 종료: " + message);
            IsConnected = false;
            Disconnected?.Invoke(message);
        }

        /// <summary>
        /// JS가 현재 연결의 구독 메시지를 전달한다.
        /// json 형식: {"id":"frame-sub","body":"..."}
        /// </summary>
        private void OnSubscriptionMessage(string json)
        {
            if (!JsonCodec.TryDeserialize(json, out SubscriptionMessageDto msg))
            {
                return;
            }

            MessageReceived?.Invoke(msg.id, msg.body);
        }

        [Serializable]
        private class SubscriptionMessageDto
        {
            public string id;
            public string body;
        }
    }
}
