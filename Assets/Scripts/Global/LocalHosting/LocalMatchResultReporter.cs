using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Data;
using Data.Net;
using Global.Serialization;
using LocalServer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Global.LocalHosting
{
    /// <summary>
    /// 로컬 서버가 stdout 으로 넘긴 <c>matchResult</c> 를 사용자 JWT 로 lobby
    /// <c>POST /api/local-sessions/{sessionId}/result</c> 에 보고한다(계약 문서 2번).
    ///
    /// 전송 전에 <c>pending-results/&lt;originHash&gt;-&lt;sessionId&gt;.json</c> 에 저장하고 원래 lobby 에 재전송한다.
    /// 404(세션 기록 없음)와 409(이미 보고됨)는 다시 보내도 소용없어 버리고, 2시간(lobby 세션 기록 TTL)이 지난
    /// 것도 버린다.
    /// </summary>
    public class LocalMatchResultReporter : MonoBehaviour
    {
        private const int TimeoutSeconds = 10;

        private readonly Dictionary<string, string> sessionLobbies = new Dictionary<string, string>();

        public void RegisterSession(string sessionId, string lobbyUrl)
        {
            sessionLobbies[sessionId] = lobbyUrl;
        }

        public void Report(string sessionId, JObject body)
        {
            sessionLobbies.TryGetValue(sessionId, out string lobbyUrl);
            var pending = new PendingLocalResult
            {
                sessionId = sessionId,
                lobbyUrl = lobbyUrl,
                savedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                body = body
            };
            string path = PendingPath(sessionId, lobbyUrl);
            // Persist synchronously: shutdown can stop the coroutine at its first network wait.
            SavePending(path, pending);
            StartCoroutine(Send(pending, path));
        }

        /// <summary>남겨 둔 결과를 차례로 다시 보낸다.</summary>
        public IEnumerator RetryPending()
        {
            if (!Directory.Exists(LocalServerPaths.PendingResultsDir)) yield break;

            string[] files;
            try
            {
                files = Directory.GetFiles(LocalServerPaths.PendingResultsDir, "*.json");
            }
            catch (Exception exception)
            {
                WDebug.LogWarning($"[LocalServer] could not list pending results: {exception.Message}");
                yield break;
            }

            foreach (string file in files)
            {
                PendingLocalResult pending = ReadPending(file);
                if (pending == null || string.IsNullOrEmpty(pending.sessionId) || pending.body == null)
                {
                    TryDelete(file);
                    continue;
                }

                DateTime savedAt = ParseSavedAt(pending.savedAtUtc, file);
                if (!LocalHostingRules.IsPendingResultFresh(savedAt, DateTime.UtcNow))
                {
                    WDebug.Log($"[LocalServer] dropping stale pending result {pending.sessionId}");
                    TryDelete(file);
                    continue;
                }

                yield return Send(pending, file);
            }
        }

        private IEnumerator Send(PendingLocalResult pending, string path)
        {
            // Old records have no origin. Never guess the currently selected lobby and lose them on 404.
            if (!ServerEndpoint.TryOf(pending.lobbyUrl, out ServerEndpoint lobby))
            {
                WDebug.LogWarning($"[LocalServer] keeping result {pending.sessionId}: originating lobby is unknown");
                yield break;
            }
            string sessionId = pending.sessionId;
            JObject body = pending.body;
            ServerEndpoint endpoint = lobby.Path("api", "local-sessions", sessionId, "result");
            string json = body.ToString(Formatting.None);

            using var request = new UnityWebRequest(endpoint, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = TimeoutSeconds;
            request.SetRequestHeader("Content-Type", "application/json");
            Server.SetAcceptLanguage(request);
            Server.SetAuthorization(request);

            yield return request.SendWebRequest();

            bool reachedServer = request.result == UnityWebRequest.Result.Success ||
                                 request.result == UnityWebRequest.Result.ProtocolError;
            PendingResultAction action = LocalHostingRules.DecideAfterReport(reachedServer, request.responseCode);

            if (request.result == UnityWebRequest.Result.Success)
                WDebug.Log($"[LocalServer] result {sessionId} reported: {JsonCodec.Excerpt(request.downloadHandler.text)}");
            else
                WDebug.LogWarning($"[LocalServer] result {sessionId} report failed: {request.responseCode} / {request.error} → {action}");

            if (action != PendingResultAction.Keep)
                TryDelete(path);
        }

        private static string PendingPath(string sessionId, string lobbyUrl)
        {
            // sessionId 는 lobby 가 만든 local-<n> 이지만, 파일 이름에 못 쓰는 문자가 오더라도 폴더 밖에 쓰지 않게 바꾼다.
            var safe = new StringBuilder(sessionId.Length);
            foreach (char c in sessionId)
            {
                safe.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            }

            // Different lobbies can issue the same local-N identifier.
            using var sha = SHA256.Create();
            string origin = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(lobbyUrl ?? ""))).Replace("-", "");
            return Path.Combine(LocalServerPaths.PendingResultsDir, origin + "-" + safe + ".json");
        }

        private static void SavePending(string path, PendingLocalResult pending)
        {
            try
            {
                Directory.CreateDirectory(LocalServerPaths.PendingResultsDir);
                File.WriteAllText(path, JsonCodec.Serialize(pending));
            }
            catch (Exception exception)
            {
                WDebug.LogWarning($"[LocalServer] could not save pending result {pending.sessionId}: {exception.Message}");
            }
        }

        private static PendingLocalResult ReadPending(string file)
        {
            try
            {
                return JsonCodec.TryDeserialize(File.ReadAllText(file), out PendingLocalResult pending) ? pending : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static DateTime ParseSavedAt(string value, string file)
        {
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime parsed))
                return parsed;

            try
            {
                return File.GetLastWriteTimeUtc(file);
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception)
            {
                // 다음 진입 때 다시 본다.
            }
        }
    }
}
