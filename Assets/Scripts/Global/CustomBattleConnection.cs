using System;
using System.Collections;
using System.Text;
using Data;
using Global.Serialization;
using UnityEngine;
using UnityEngine.Networking;

namespace Global {
    public sealed class CustomBattleConnection : MonoBehaviour {
        public const string BaseUrl = "http://127.0.0.1:18741";
        private static CustomBattleConnection instance;
        private string sessionId;
        private string token;
        private string socketUrl;
        private bool ending;
        private LocalMagicIdMap magicIds;
        public static string Incoming(string body) => IsLocalMatch ? instance.magicIds.Incoming(body) : body;
        public static string Outgoing(string body) => IsLocalMatch ? instance.magicIds.Outgoing(body) : body;
        public static bool IsLocalMatch => instance != null && SceneContext.MatchInfo != null
            && instance.sessionId == SceneContext.MatchInfo.sessionId;

        public static UnityWebRequest Request(string path, string body = null) {
            var request = new UnityWebRequest(BaseUrl + path, body == null ? "GET" : "POST");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 4;
            if (body != null) {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.SetRequestHeader("Content-Type", "application/json");
            }
#if !UNITY_WEBGL || UNITY_EDITOR
            request.SetRequestHeader("Origin", "http://127.0.0.1:18742");
#endif
            return request;
        }

        public static void Begin(string session, string secret, string url, LocalMagicIdMap ids) {
            if (ids == null) throw new InvalidOperationException("Local magic identity map required");
            if (instance != null && instance.ending) {
                Destroy(instance.gameObject);
                instance = null;
            }
            if (instance != null || string.IsNullOrEmpty(session) || !session.StartsWith("custom-"))
                throw new InvalidOperationException("Local battle already active or invalid session");
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "ws"
                || uri.Host != "127.0.0.1" || uri.AbsolutePath != "/ws" || uri.Query != ""
                || uri.UserInfo != "" || string.IsNullOrEmpty(secret))
                throw new InvalidOperationException("Invalid local game endpoint");
            instance = new GameObject("CustomBattleConnection").AddComponent<CustomBattleConnection>();
            instance.sessionId = session;
            instance.token = secret;
            instance.socketUrl = url;
            instance.magicIds = ids;
            DontDestroyOnLoad(instance.gameObject);
            instance.StartCoroutine(instance.Heartbeat());
        }

        public static string TokenFor(MatchedInfoDto match, string fallback) {
            if (!IsLocalMatch) return fallback;
            if (match.webSocketUrl != instance.socketUrl) throw new InvalidOperationException("Local endpoint changed");
            return instance.token;
        }

        private string Body => JsonCodec.Serialize(new { sessionId });
        private IEnumerator Heartbeat() {
            while (!ending) {
                using (var request = Request("/heartbeat", Body)) {
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success) { End(); yield break; }
                }
                yield return new WaitForSecondsRealtime(2);
            }
        }
        public static void End() {
            if (instance == null || instance.ending) return;
            instance.ending = true;
            instance.StartCoroutine(instance.Close());
        }
        private IEnumerator Close() {
            using (var request = Request("/stop", Body)) yield return request.SendWebRequest();
            token = null;
            // 결과 화면의 로비 보고도 로컬 경기로 구분할 수 있게 장면 전환까지 식별자를 유지합니다.
        }
        private void Update() {
            if (ending && SceneContext.MatchInfo != null && SceneContext.MatchInfo.sessionId != sessionId)
                Destroy(gameObject);
        }
        private void OnDestroy() { token = null; if (instance == this) instance = null; }
    }
}
