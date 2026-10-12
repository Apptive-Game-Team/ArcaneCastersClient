using System;
using System.Collections;
using System.Collections.Generic;
using LobbyScene;
using Data;
using Global;
using UnityEngine;
using UnityEngine.Networking;
using Global.Serialization;

namespace Adventures
{
    public class AdventureApiService : MonoBehaviour
    {
        private const string DebugBaseUrl = "http://localhost:7777";

        public IEnumerator RequestPVE(long scenarioId, Action<MatchedInfoDto> callback)
        {
            List<ServerPing> pings = null;
            yield return GameServerPinger.Measure(result => pings = result);

            using var www = UnityWebRequest.Get(
                GameServerPinger.AppendQuery($"{ServerList.MatchingServer.url}/api/scenarios/{scenarioId}/play", pings));
            
            Server.SetAcceptLanguage(www);
            Server.SetAuthorization(www);
            www.downloadHandler = new DownloadHandlerBuffer();
            
            yield return www.SendWebRequest();
            
            if (www.result != UnityWebRequest.Result.Success)            
            {
                WDebug.LogError($"[RequestPVE] fail: {www.responseCode} / {www.error}");
                callback?.Invoke(null);
                yield break;
            }
            
            string body = www.downloadHandler.text;
            if (!JsonCodec.TryDeserialize(body, out MatchedInfoDto dto, out string error))
            {
                WDebug.LogError($"[RequestPVE] parse error: {error} / {JsonCodec.Excerpt(body)}");
            }

            callback?.Invoke(dto);
        }

        public IEnumerator RequestDebugPVE(long scenarioId, Action<string> onSuccess, Action<string> onFailure)
        {
            using var request = UnityWebRequest.PostWwwForm(
                $"{DebugBaseUrl}/api/debug/game/pve/{scenarioId}", "");

            Server.SetAcceptLanguage(request);
            Server.SetAuthorization(request);
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMessage = string.IsNullOrWhiteSpace(request.downloadHandler.text)
                    ? request.error
                    : request.downloadHandler.text;
                onFailure?.Invoke(errorMessage);
                yield break;
            }

            onSuccess?.Invoke(request.downloadHandler.text);
        }
    }
}
