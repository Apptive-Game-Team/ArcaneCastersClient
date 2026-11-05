using System;
using System.Collections;
using System.Text;
using Data;
using Global;
using Global.Serialization;
using UnityEngine.Networking;

namespace LobbyScene
{
    public static class PlayerProgressionApi
    {
        public static IEnumerator Rewards(Action<ProgressionRewardsDto> complete)
        {
            var context = Data.Profile.RewardRequestContext.Capture();
            using var request = UnityWebRequest.Get($"{ServerList.MatchingServer.url}/api/users/mine/progression/rewards");
            request.timeout = 15;
            Server.SetAuthorization(request);
            Server.SetAcceptLanguage(request);
            Data.Profile.ManualRewardClaims.Apply(request);
            yield return request.SendWebRequest();
            ProgressionRewardsDto result = null;
            if (request.result == UnityWebRequest.Result.Success)
                JsonCodec.TryDeserialize(request.downloadHandler.text, out result, out _);
            complete(context.IsCurrent ? result : null);
        }

        public static IEnumerator Sync(Action<PlayerProgressionDto> complete) => Request("sync", "{}", complete);

        public static IEnumerator Choose(string faction, Action<PlayerProgressionDto> complete) =>
            Request("faction", JsonCodec.Serialize(new { faction }), complete);

        private static IEnumerator Request(string path, string body, Action<PlayerProgressionDto> complete)
        {
            var context = Data.Profile.RewardRequestContext.Capture();
            using var request = new UnityWebRequest($"{ServerList.MatchingServer.url}/api/users/mine/progression/{path}", "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 15;
            request.SetRequestHeader("Content-Type", "application/json");
            Server.SetAuthorization(request);
            Server.SetAcceptLanguage(request);
            Data.Profile.ManualRewardClaims.Apply(request);
            yield return request.SendWebRequest();
            PlayerProgressionDto result = null;
            if (request.result == UnityWebRequest.Result.Success)
                JsonCodec.TryDeserialize(request.downloadHandler.text, out result, out _);
            complete(context.IsCurrent ? result : null);
        }

        public static IEnumerator Pending(Action<ProgressionPendingRewardsDto, long> complete)
        {
            using var request = UnityWebRequest.Get($"{ServerList.MatchingServer.url}/api/users/mine/progression/rewards/pending");
            request.timeout = 15;
            Server.SetAuthorization(request);
            Server.SetAcceptLanguage(request);
            Data.Profile.ManualRewardClaims.Apply(request);
            yield return request.SendWebRequest();
            ProgressionPendingRewardsDto result = null;
            if (request.result == UnityWebRequest.Result.Success) JsonCodec.TryDeserialize(request.downloadHandler.text, out result, out _);
            complete(result, request.responseCode);
        }

        public static IEnumerator Claim(long id, Action<ProgressionClaimReceiptDto, long> complete)
        {
            using var request = new UnityWebRequest($"{ServerList.MatchingServer.url}/api/users/mine/progression/rewards/{id}/claim", "POST");
            request.timeout = 15;
            request.uploadHandler = new UploadHandlerRaw(Array.Empty<byte>());
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            Server.SetAuthorization(request);
            Server.SetAcceptLanguage(request);
            Data.Profile.ManualRewardClaims.Apply(request);
            yield return request.SendWebRequest();
            ProgressionClaimReceiptDto result = null;
            if (request.result == UnityWebRequest.Result.Success) JsonCodec.TryDeserialize(request.downloadHandler.text, out result, out _);
            complete(result, request.responseCode);
        }
    }
}
