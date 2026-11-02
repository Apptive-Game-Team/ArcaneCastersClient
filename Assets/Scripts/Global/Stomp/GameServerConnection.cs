using System;
using System.Collections;
using Data;
using UnityEngine;

namespace Global.Stomp
{
    /// <summary>로비에서 연 연결을 게임 씬에 인계한다. 씬 전환으로 transport를 다시 만들지 않는다.</summary>
    public sealed class GameServerConnection : MonoBehaviour
    {
        public const float HandshakeTimeoutSeconds = GameScene.ConnectAttemptSchedule.DefaultAttemptSeconds;
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

        public IStompTransport Transport => transport;
        public bool IsPreparing => preparing;
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

        public void ClaimForGameplay()
        {
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
            if (hasPreparingOwner && !gameplayOwnsConnection && preparingOwner == null) Close();
        }

        public void Close()
        {
            if (closed) return;
            closed = true;
            if (current == this) current = null;
            transport?.Disconnect();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (!closed) transport?.Disconnect();
            if (current == this) current = null;
        }
    }
}
