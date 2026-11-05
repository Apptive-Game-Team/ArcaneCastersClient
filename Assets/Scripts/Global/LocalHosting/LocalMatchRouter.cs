using System;
using System.Collections;
using System.Text;
using Data;
using Data.Net;
using Global.Serialization;
using LocalServer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace Global.LocalHosting
{
    /// <summary>
    /// 봇전과 모험을 자기 PC 의 로컬 서버에서 시작한다(계약 문서 2·4·5번).
    ///
    /// <list type="number">
    /// <item>조건 확인: desktop, 설정 켜짐, lobby 의 <c>localSessionsEnabled</c>, 설치된 version ≥ <c>minimumVersion</c>,
    /// 서버 <c>ready</c>, 서버의 정적 데이터 version = lobby 가 요구하는 version.</item>
    /// <item>lobby <c>POST /api/local-sessions</c>(사용자 JWT).</item>
    /// <item>로컬 서버 <c>POST /api/local/sessions</c>(init token). body 는 lobby 응답 전체에 <c>tickRate</c> 를 더한 것.</item>
    /// <item>응답으로 <see cref="MatchedInfoDto"/> 를 만들어 기존 경기 진입 흐름에 넘긴다.</item>
    /// </list>
    ///
    /// 어느 단계든 실패하면(lobby 503 포함) null 을 넘기고, 호출부가 조용히 공유 서버 endpoint 로 간다.
    /// 이 coroutine 은 예외를 밖으로 던지지 않는다. 던지면 바깥 매칭 coroutine 까지 멈춰 화면이 매칭 중에 걸린다.
    /// </summary>
    public static class LocalMatchRouter
    {
        private const int TimeoutSeconds = 10;

        public static IEnumerator TryStart(LocalServerService service, string type, long? scenarioId, Action<MatchedInfoDto> callback)
        {
            LocalServerHost host = service.Host;
            LocalServerPackStore packStore = service.PackStore;
            OfflinePackResponse pack = packStore.Latest;
            InstalledBundle installed = packStore.FindInstalledBundle();

            LocalHostingBlock block = LocalHostingRules.Evaluate(new LocalHostingConditions
            {
                PlatformSupported = LocalServerPlatform.IsSupported,
                SettingEnabled = LocalServerSettings.Enabled,
                LocalSessionsEnabled = pack != null && pack.localSessionsEnabled,
                InstalledVersion = installed?.Version,
                MinimumVersion = pack?.localServer?.minimumVersion,
                ServerReady = host.State == LocalServerState.Ready,
                ServerStaticVersion = host.StaticVersion,
                LobbyStaticVersion = pack?.staticVersion
            });

            if (block != LocalHostingBlock.None)
            {
                WDebug.Log($"[LocalServer] shared server for {type}: {block}");
                // 정적 데이터가 바뀌었으면 다음 경기부터 로컬로 할 수 있게 지금 다시 띄워 둔다.
                if (block == LocalHostingBlock.StaticDataOutdated) service.EnsureServer();
                callback(null);
                yield break;
            }

            // 요청 사이에 서버가 다시 뜨면 port 와 token 이 바뀌므로 지금 값을 잡아 둔다.
            int port = host.Port;
            string token = host.Token;
            int tickRate = LocalServerSettings.TickRatePolicy.TickRate;

            // 1. lobby 세션 발급
            var lobbyBody = new JObject
            {
                ["type"] = type,
                ["scenarioId"] = scenarioId.HasValue ? new JValue(scenarioId.Value) : JValue.CreateNull()
            };

            ServerEndpoint lobby = ServerList.MatchingServer.Api;
            ServerEndpoint lobbyEndpoint = lobby.Path("api", "local-sessions");
            using var lobbyRequest = CreateJsonPost(lobbyEndpoint, lobbyBody);
            Server.SetAcceptLanguage(lobbyRequest);
            Server.SetAuthorization(lobbyRequest);
            yield return lobbyRequest.SendWebRequest();

            if (lobbyRequest.result != UnityWebRequest.Result.Success)
            {
                WDebug.LogWarning($"[LocalServer] local-sessions failed: {lobbyRequest.responseCode} / {lobbyRequest.error}, using shared server");
                callback(null);
                yield break;
            }

            JObject session = ParseObject(lobbyRequest.downloadHandler.text);
            string sessionId = (string)session?["sessionId"];
            if (string.IsNullOrEmpty(sessionId))
            {
                WDebug.LogWarning($"[LocalServer] local-sessions response has no sessionId: {JsonCodec.Excerpt(lobbyRequest.downloadHandler.text)}");
                callback(null);
                yield break;
            }

            // Bind the result destination before starting the local session or waiting again.
            service.Reporter.RegisterSession(sessionId, lobby.ToString());

            // 2. 로컬 서버 세션 생성
            if (host.State != LocalServerState.Ready || host.Port != port || host.Token != token)
            {
                WDebug.LogWarning("[LocalServer] server restarted while issuing the session, using shared server");
                callback(null);
                yield break;
            }

            var localBody = (JObject)session.DeepClone();
            localBody["tickRate"] = tickRate;

            string serverBase = $"http://127.0.0.1:{port}";
            ServerEndpoint localEndpoint = ServerEndpoint.Of(serverBase).Path("api", "local", "sessions");
            using var localRequest = CreateJsonPost(localEndpoint, localBody);
            localRequest.SetRequestHeader("Authorization", "Bearer " + token);
            yield return localRequest.SendWebRequest();

            if (localRequest.result != UnityWebRequest.Result.Success)
            {
                WDebug.LogWarning($"[LocalServer] local server session failed: {localRequest.responseCode} / {localRequest.error}, using shared server");
                callback(null);
                yield break;
            }

            if (!JsonCodec.TryDeserialize(localRequest.downloadHandler.text, out LocalServerSessionResponse created, out string error) ||
                created == null)
            {
                WDebug.LogWarning($"[LocalServer] local server session parse error: {error}");
                callback(null);
                yield break;
            }

            // 3. 기존 경기 진입 흐름이 쓰는 형태로 바꾼다.
            MatchedInfoDto matched = BuildMatchedInfo(session, created, serverBase, token);
            if (matched == null)
            {
                callback(null);
                yield break;
            }

            WDebug.Log($"[LocalServer] local {type} session {matched.sessionId} at {matched.ConnectionSource}, tick rate {tickRate}");
            callback(matched);
        }

        private static MatchedInfoDto BuildMatchedInfo(JObject session, LocalServerSessionResponse created, string serverBase, string token)
        {
            try
            {
                User left = session["leftUser"]?.ToObject<User>();
                User right = session["rightUser"]?.ToObject<User>();
                if (left == null || right == null)
                {
                    WDebug.LogWarning("[LocalServer] local-sessions response has no leftUser or rightUser");
                    return null;
                }

                return new MatchedInfoDto
                {
                    message = "local session",
                    server = serverBase,
                    webSocketUrl = created.webSocketUrl,
                    leftUser = left,
                    rightUser = right,
                    sessionId = string.IsNullOrEmpty(created.sessionId) ? (string)session["sessionId"] : created.sessionId,
                    mapType = (string)session["mapType"],
                    localToken = token
                };
            }
            catch (Exception exception)
            {
                WDebug.LogWarning($"[LocalServer] could not read local-sessions users: {exception.Message}");
                return null;
            }
        }

        private static UnityWebRequest CreateJsonPost(ServerEndpoint endpoint, JObject body)
        {
            var request = new UnityWebRequest(endpoint, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body.ToString(Formatting.None))),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = TimeoutSeconds
            };
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }

        private static JObject ParseObject(string text)
        {
            try
            {
                return JObject.Parse(text);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
