using System;
using System.Collections;
using System.Collections.Generic;
using Data;
using Global;
using Global.Serialization;
using UnityEngine;
using UnityEngine.Networking;

namespace RewardChest
{
    /// <summary>Calls the lobby's chest endpoints.</summary>
    public class ChestApiClient : MonoBehaviour
    {
        /// <summary>Gives the unopened chests, oldest first, or null when the request or the parse failed.</summary>
        public IEnumerator GetChests(Action<ChestDto[]> callback)
        {
            string url = $"{ServerList.MatchingServer.url}/api/users/mine/chests";
            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            Server.SetAcceptLanguage(webRequest);
            Server.SetAuthorization(webRequest);
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                WDebug.LogError($"Failed to get chests: {webRequest.responseCode} / {webRequest.error}");
                callback?.Invoke(null);
                yield break;
            }

            string body = webRequest.downloadHandler.text;
            if (!ChestPayloads.TryParseChestList(body, out ChestDto[] chests, out string error))
            {
                WDebug.LogError($"Failed to parse chests: {error} / {JsonCodec.Excerpt(body)}");
                callback?.Invoke(null);
                yield break;
            }

            callback?.Invoke(chests);
        }

        /// <summary>
        /// Opens one owned chest. The rewards list is empty for every outcome other than
        /// <see cref="ChestOpenOutcome.Opened"/>.
        /// </summary>
        public IEnumerator OpenChest(long ownedChestId, Action<ChestOpenOutcome, List<RewardView>> callback)
        {
            string url = $"{ServerList.MatchingServer.url}/api/users/mine/chests/{ownedChestId}/open";
            using var webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            webRequest.uploadHandler = new UploadHandlerRaw(Array.Empty<byte>());
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            Server.SetAcceptLanguage(webRequest);
            Server.SetAuthorization(webRequest);

            yield return webRequest.SendWebRequest();

            ChestOpenOutcome outcome = ChestPayloads.OutcomeFromStatusCode(webRequest.responseCode);
            if (webRequest.result != UnityWebRequest.Result.Success || outcome != ChestOpenOutcome.Opened)
            {
                if (outcome == ChestOpenOutcome.Opened)
                {
                    outcome = ChestOpenOutcome.Failed;
                }

                WDebug.LogWarning($"Chest {ownedChestId} was not opened: {outcome} ({webRequest.responseCode} / {webRequest.error})");
                callback?.Invoke(outcome, new List<RewardView>());
                yield break;
            }

            string body = webRequest.downloadHandler.text;
            if (!ChestPayloads.TryParseOpenResponse(body, out List<RewardView> rewards, out string error))
            {
                // The chest is open on the server either way; the list refresh will show that.
                WDebug.LogError($"Failed to parse opened chest {ownedChestId}: {error} / {JsonCodec.Excerpt(body)}");
            }

            callback?.Invoke(ChestOpenOutcome.Opened, rewards);
        }
    }
}
