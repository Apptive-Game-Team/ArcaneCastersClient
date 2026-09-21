using System;
using System.Collections;
using Global;
using UnityEngine;
using UnityEngine.Networking;
using Global.Serialization;

namespace Data.Profile
{
    public class UserProfileApiClient : MonoBehaviour
    {
        public void GetProfile(Action<UserProfileData> callback)
        {
            StartCoroutine(GetProfileCoroutine(callback));
        }

        public void GetOverview(Action<UserStatisticsOverviewDto> callback)
        {
            StartCoroutine(GetOverviewCoroutine(callback));
        }

        public void GetGameHistory(Action<UserGameHistoryDto[]> callback)
        {
            StartCoroutine(GetGameHistoryCoroutine(callback));
        }

        public void GetGameHistoryPage(int page, int size, Action<UserGameHistoryResponseDto> callback)
        {
            StartCoroutine(GetGameHistoryPageCoroutine(page, size, callback));
        }

        private IEnumerator GetProfileCoroutine(Action<UserProfileData> callback)
        {
            UserStatisticsOverviewDto overview = null;
            UserGameHistoryDto[] games = null;

            yield return GetOverviewCoroutine(response => overview = response);
            yield return GetGameHistoryCoroutine(response => games = response);

            callback?.Invoke(new UserProfileData(SceneContext.User, overview, games));
        }

        private static IEnumerator GetOverviewCoroutine(Action<UserStatisticsOverviewDto> callback)
        {
            string url = $"{ServerList.MatchingServer.url}/api/users/mine/statistics/overview";
            using UnityWebRequest webRequest = UnityWebRequest.Get(url);

            Server.SetAcceptLanguage(webRequest);
            Server.SetAuthorization(webRequest);
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string body = webRequest.downloadHandler.text;
                if (!JsonCodec.TryDeserialize(body, out UserStatisticsOverviewDto overview, out string error))
                {
                    WDebug.LogError($"Failed to parse user statistics overview: {error} / {JsonCodec.Excerpt(body)}");
                }

                callback?.Invoke(overview);
            }
            else
            {
                WDebug.LogError($"Failed to get user statistics overview: {webRequest.responseCode} / {webRequest.error}");
                callback?.Invoke(null);
            }
        }

        private static IEnumerator GetGameHistoryCoroutine(Action<UserGameHistoryDto[]> callback)
        {
            UserGameHistoryResponseDto page = null;
            yield return GetGameHistoryPageCoroutine(0, 20, response => page = response);
            callback?.Invoke(page?.Items ?? Array.Empty<UserGameHistoryDto>());
        }

        private static IEnumerator GetGameHistoryPageCoroutine(int page, int size, Action<UserGameHistoryResponseDto> callback)
        {
            string url = $"{ServerList.MatchingServer.url}/api/users/mine/statistics/games?page={Math.Max(0, page)}&size={Math.Max(1, size)}";
            using UnityWebRequest webRequest = UnityWebRequest.Get(url);

            Server.SetAcceptLanguage(webRequest);
            Server.SetAuthorization(webRequest);
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string body = webRequest.downloadHandler.text;
                if (!JsonCodec.TryDeserialize(body, out UserGameHistoryResponseDto response, out string error))
                {
                    WDebug.LogError($"Failed to parse user game history: {error} / {JsonCodec.Excerpt(body)}");
                }

                callback?.Invoke(response ?? new UserGameHistoryResponseDto());
            }
            else
            {
                WDebug.LogError($"Failed to get user game history: {webRequest.responseCode} / {webRequest.error}");
                callback?.Invoke(new UserGameHistoryResponseDto { games = Array.Empty<UserGameHistoryDto>(), last = true });
            }
        }
    }
}
