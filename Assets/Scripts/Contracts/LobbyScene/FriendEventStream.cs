using System;
using System.Collections.Generic;
using System.Text;
using Data.Net;
using Global.Serialization;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace LobbyScene
{
    public sealed class FriendEventStream : MonoBehaviour, IDisposable
    {
        private readonly Queue<Action> pendingCallbacks = new Queue<Action>();
        private UnityWebRequest request;

        public bool IsConnected { get; private set; }
        public event Action<FriendEventPayload> EventReceived;
        public event Action Disconnected;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void ConnectFriendEventStream(string objectName, string url, string token);

        [DllImport("__Internal")]
        private static extern void DisconnectFriendEventStream();
#endif

        private void Update()
        {
            DrainCallbacks();
        }

        public UnityWebRequestAsyncOperation Connect(ServerEndpoint endpoint = default, string token = null)
        {
            DisposeRequest();
            if (!endpoint.HasValue)
            {
                endpoint = ServerEndpoint.Of("http://localhost:7777");
            }

            string url = endpoint.Path("api", "friends", "events");

#if UNITY_WEBGL && !UNITY_EDITOR
            IsConnected = true;
            ConnectFriendEventStream(gameObject.name, url, token);
            return null;
#else
            request = UnityWebRequest.Get(url);
            request.downloadHandler = new SseDownloadHandler(QueueEvent);
            request.SetRequestHeader("Accept", "text/event-stream");
            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader("Authorization", $"Bearer {token}");
            }
            IsConnected = true;
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            operation.completed += _ => QueueCallback(HandleCompleted);
            return operation;
#endif
        }

        public void SetConnectedForTesting(bool isConnected)
        {
            IsConnected = isConnected;
        }

        public void DrainCallbacks()
        {
            while (true)
            {
                Action callback;
                lock (pendingCallbacks)
                {
                    if (pendingCallbacks.Count == 0) return;
                    callback = pendingCallbacks.Dequeue();
                }
                callback.Invoke();
            }
        }

        public void Dispose()
        {
            DisposeRequest();
        }

        private void OnDestroy()
        {
            DisposeRequest();
        }

        private void QueueEvent(string json)
        {
            QueueCallback(() =>
            {
                if (!JsonCodec.TryDeserialize(json, out FriendEventPayload payload, out string error))
                {
                    Debug.LogError($"[Friend SSE] Invalid event: {error} / {JsonCodec.Excerpt(json)}");
                    return;
                }

                EventReceived?.Invoke(payload);
            });
        }

        private void QueueCallback(Action callback)
        {
            lock (pendingCallbacks) pendingCallbacks.Enqueue(callback);
        }

        private void HandleCompleted()
        {
            if (!IsConnected) return;
            IsConnected = false;
            Disconnected?.Invoke();
        }

        private void DisposeRequest()
        {
            IsConnected = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            DisconnectFriendEventStream();
#endif
            request?.Abort();
            request?.Dispose();
            request = null;
        }

        // Called by FriendEventStream.jslib through Unity SendMessage.
        public void OnFriendSseEvent(string envelopeJson)
        {
            if (!JsonCodec.TryDeserialize(envelopeJson, out SseEnvelope envelope, out string error))
            {
                Debug.LogError($"[Friend SSE] Invalid envelope: {error} / {JsonCodec.Excerpt(envelopeJson)}");
                return;
            }

            QueueEvent(envelope.data);
        }

        public void OnFriendSseDisconnected(string error)
        {
            QueueCallback(HandleCompleted);
        }

        [Serializable]
        private class SseEnvelope
        {
            public string data;
        }

        private sealed class SseDownloadHandler : DownloadHandlerScript
        {
            private readonly Action<string> eventCallback;
            private readonly StringBuilder buffer = new StringBuilder();

            public SseDownloadHandler(Action<string> eventCallback) : base(new byte[4096])
            {
                this.eventCallback = eventCallback;
            }

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (data == null || dataLength == 0) return true;
                buffer.Append(Encoding.UTF8.GetString(data, 0, dataLength).Replace("\r\n", "\n"));
                ParseCompleteEvents();
                return true;
            }

            private void ParseCompleteEvents()
            {
                int boundary;
                while ((boundary = buffer.ToString().IndexOf("\n\n", StringComparison.Ordinal)) >= 0)
                {
                    string block = buffer.ToString(0, boundary);
                    buffer.Remove(0, boundary + 2);
                    ParseEvent(block);
                }
            }

            private void ParseEvent(string block)
            {
                StringBuilder json = new StringBuilder();
                foreach (string line in block.Split('\n'))
                {
                    if (line.StartsWith("data:"))
                    {
                        if (json.Length > 0) json.Append('\n');
                        json.Append(line.Substring(5).TrimStart());
                    }
                }
                if (json.Length > 0) eventCallback.Invoke(json.ToString());
            }
        }
    }
}
