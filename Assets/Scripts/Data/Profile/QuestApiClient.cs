using System;
using System.Collections;
using System.Collections.Generic;
using Data.Quests;
using Global;
using Global.Serialization;
using RewardChest;
using UnityEngine;
using UnityEngine.Networking;

namespace Data.Profile
{
    /// <summary>
    /// Reads <c>GET /api/users/mine/quests</c> and claims a <c>MANUAL</c> quest with
    /// <c>POST /api/users/mine/quests/{questId}/claim</c>. <c>AUTO</c> quests are still granted by the lobby's
    /// <c>POST /api/users/mine/quests/check</c>.
    /// </summary>
    public class QuestApiClient : MonoBehaviour
    {
        /// <summary>Calls back with the quests, or null when the request or the parse failed.</summary>
        public void GetQuests(Action<QuestDto[]> callback)
        {
            StartCoroutine(FetchQuests(callback));
        }

        /// <summary>
        /// <see cref="GetQuests"/> for a caller that runs it inside its own coroutine
        /// (<c>yield return client.FetchQuests(...)</c>).
        /// </summary>
        public IEnumerator FetchQuests(Action<QuestDto[]> callback)
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

        /// <summary>
        /// Claims one quest. Never throws: every status code becomes a <see cref="QuestClaimOutcome"/>, and the
        /// rewards list is empty for every outcome other than <see cref="QuestClaimOutcome.Claimed"/>.
        /// </summary>
        public IEnumerator ClaimQuest(long questId, Action<QuestClaimOutcome, List<RewardView>> callback)
        {
            string url = $"{ServerList.MatchingServer.url}/api/users/mine/quests/{questId}/claim";
            using var webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            webRequest.uploadHandler = new UploadHandlerRaw(Array.Empty<byte>());
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            Server.SetAcceptLanguage(webRequest);
            Server.SetAuthorization(webRequest);

            yield return webRequest.SendWebRequest();

            // A connection error leaves responseCode at 0, which maps to Failed.
            QuestClaimOutcome outcome = QuestClaimPayloads.OutcomeFromStatusCode(webRequest.responseCode);
            if (webRequest.result != UnityWebRequest.Result.Success || outcome != QuestClaimOutcome.Claimed)
            {
                if (outcome == QuestClaimOutcome.Claimed)
                {
                    outcome = QuestClaimOutcome.Failed;
                }

                WDebug.LogWarning($"Quest {questId} was not claimed: {outcome} ({webRequest.responseCode} / {webRequest.error})");
                callback?.Invoke(outcome, new List<RewardView>());
                yield break;
            }

            string body = webRequest.downloadHandler.text;
            if (!QuestClaimPayloads.TryParseClaimResponse(body, out List<RewardView> rewards, out string error))
            {
                // The rewards were granted either way; the caller falls back to the quest's own reward list.
                WDebug.LogError($"Failed to parse claimed quest {questId}: {error} / {JsonCodec.Excerpt(body)}");
            }

            callback?.Invoke(QuestClaimOutcome.Claimed, rewards);
        }
    }
}
