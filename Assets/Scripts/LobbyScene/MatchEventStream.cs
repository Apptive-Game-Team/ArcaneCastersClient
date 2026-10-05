using System;
using System.Collections.Generic;
using System.Text;
using Data;
using Global;
using Global.Serialization;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace LobbyScene
{
    public sealed class MatchEventStream : MonoBehaviour, IDisposable
    {
        private readonly Queue<Action> pendingCallbacks = new Queue<Action>();
        private UnityWebRequest request;

        public bool IsConnected { get; private set; }
        public event Action<MatchTicket> TicketReceived;
        public event Action Disconnected;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void ConnectMatchEventStream(string objectName, string url, string token);

        [DllImport("__Internal")]
        private static extern void DisconnectMatchEventStream();
#endif

        public UnityWebRequestAsyncOperation Connect()
        {
            DisposeRequest();
            string url = ServerList.MatchingServer.Api.Path("api", "match", "events");

#if UNITY_WEBGL && !UNITY_EDITOR
            IsConnected = true;
            ConnectMatchEventStream(gameObject.name, url, SceneContext.JwtToken);
            return null;
#else
            request = UnityWebRequest.Get(url);
            request.downloadHandler = new SseDownloadHandler(QueueEvent);
            request.SetRequestHeader("Accept", "text/event-stream");
            Server.SetAcceptLanguage(request);
            Server.SetAuthorization(request);
            IsConnected = true;
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            operation.completed += _ => QueueCallback(HandleCompleted);
            return operation;
#endif
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

        private void QueueEvent(string json)
        {
            QueueCallback(() =>
            {
                if (!JsonCodec.TryDeserialize(json, out MatchTicket ticket, out string error))
                {
                    WDebug.LogError($"[Match SSE] Invalid event: {error} / {JsonCodec.Excerpt(json)}");
                    return;
                }

                TicketReceived?.Invoke(ticket);
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
            DisconnectMatchEventStream();
#endif
            request?.Abort();
            request?.Dispose();
            request = null;
        }

        // Called by MatchEventStream.jslib through Unity SendMessage.
        public void OnMatchSseEvent(string envelopeJson)
        {
            if (!JsonCodec.TryDeserialize(envelopeJson, out SseEnvelope envelope, out string error))
            {
                WDebug.LogError($"[Match SSE] Invalid envelope: {error} / {JsonCodec.Excerpt(envelopeJson)}");
                return;
            }

            QueueEvent(envelope.data);
        }

        public void OnMatchSseDisconnected(string error)
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
