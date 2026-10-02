using System;
using System.Collections;
using Data.Quests;
using Global;
using Global.Serialization;
using UnityEngine;
using UnityEngine.Networking;

namespace Data.Profile
{
    /// <summary>Reads <c>GET /api/users/mine/quests</c>. Read-only; rewards are granted by the lobby elsewhere.</summary>
    public class QuestApiClient : MonoBehaviour
    {
        /// <summary>Calls back with the quests, or null when the request or the parse failed.</summary>
        public void GetQuests(Action<QuestDto[]> callback)
        {
            StartCoroutine(GetQuestsCoroutine(callback));
        }

        private static IEnumerator GetQuestsCoroutine(Action<QuestDto[]> callback)
        {
            string url = $"{ServerList.MatchingServer.url}/api/users/mine/quests";
            using UnityWebRequest webRequest = UnityWebRequest.Get(url);

            Server.SetAcceptLanguage(webRequest);
            Server.SetAuthorization(webRequest);
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                WDebug.LogError($"Failed to get quests: {webRequest.responseCode} / {webRequest.error}");
                callback?.Invoke(null);
                yield break;
            }

            string body = webRequest.downloadHandler.text;
            if (!JsonCodec.TryDeserialize(body, out QuestDto[] quests, out string error))
            {
                WDebug.LogError($"Failed to parse quests: {error} / {JsonCodec.Excerpt(body)}");
                callback?.Invoke(null);
                yield break;
            }

            callback?.Invoke(quests);
        }
    }
}
