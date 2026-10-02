using System;
using System.Collections;
using System.Text;
using Data.Appearances;
using Global;
using Global.Serialization;
using UnityEngine;
using UnityEngine.Networking;

namespace Data.Profile
{
    /// <summary>Reads the appearance catalog and changes the selected appearance.</summary>
    public class AppearanceApiClient : MonoBehaviour
    {
        /// <summary>Calls back with the catalog, or null when the request or the parse failed.</summary>
        public void GetAppearances(Action<AppearanceDto[]> callback)
        {
            StartCoroutine(GetAppearancesCoroutine(callback));
        }

        /// <summary>
        /// Calls back with the outcome and, on success, the appearance key the server echoed back
        /// (falling back to <paramref name="appearanceKey"/> when the body cannot be read).
        /// </summary>
        public void SelectAppearance(string appearanceKey, Action<AppearanceSelectionOutcome, string> callback)
        {
            StartCoroutine(SelectAppearanceCoroutine(appearanceKey, callback));
        }

        private static IEnumerator GetAppearancesCoroutine(Action<AppearanceDto[]> callback)
        {
            string url = $"{ServerList.MatchingServer.url}/api/users/mine/appearances";
            using UnityWebRequest webRequest = UnityWebRequest.Get(url);

            Server.SetAcceptLanguage(webRequest);
            Server.SetAuthorization(webRequest);
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                WDebug.LogError($"Failed to get appearances: {webRequest.responseCode} / {webRequest.error}");
                callback?.Invoke(null);
                yield break;
            }

            string body = webRequest.downloadHandler.text;
            if (!JsonCodec.TryDeserialize(body, out AppearanceDto[] appearances, out string error))
            {
                WDebug.LogError($"Failed to parse appearances: {error} / {JsonCodec.Excerpt(body)}");
                callback?.Invoke(null);
                yield break;
            }

            callback?.Invoke(appearances);
        }

        private static IEnumerator SelectAppearanceCoroutine(string appearanceKey, Action<AppearanceSelectionOutcome, string> callback)
        {
            string url = $"{ServerList.MatchingServer.url}/api/users/mine/appearance";
            string json = JsonCodec.Serialize(new AppearanceSelectionDto { appearance = appearanceKey });

            using var webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer()
            };

            Server.SetAcceptLanguage(webRequest);
            Server.SetAuthorization(webRequest);
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            AppearanceSelectionOutcome outcome = AppearanceCatalog.ClassifyResponse(
                webRequest.responseCode,
                webRequest.result == UnityWebRequest.Result.Success);

            if (outcome != AppearanceSelectionOutcome.Success)
            {
                WDebug.LogError($"Failed to select appearance '{appearanceKey}': {webRequest.responseCode} / {webRequest.error}");
                callback?.Invoke(outcome, null);
                yield break;
            }

            string body = webRequest.downloadHandler.text;
            string selectedKey = appearanceKey;
            if (JsonCodec.TryDeserialize(body, out AppearanceSelectionDto response, out string error))
            {
                if (!string.IsNullOrWhiteSpace(response.appearance))
                {
                    selectedKey = response.appearance;
                }
            }
            else
            {
                WDebug.LogWarning($"Appearance selection response unreadable, keeping '{appearanceKey}': {error} / {JsonCodec.Excerpt(body)}");
            }

            callback?.Invoke(AppearanceSelectionOutcome.Success, selectedKey);
        }
    }
}
