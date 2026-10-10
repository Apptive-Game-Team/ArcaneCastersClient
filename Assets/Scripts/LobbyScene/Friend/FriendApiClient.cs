using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Data;
using Data.Net;
using Global;
using Global.Serialization;
using UnityEngine;
using UnityEngine.Networking;

namespace LobbyScene
{
    public class FriendApiClient : MonoBehaviour
    {
        private static ServerEndpoint FriendsEndpoint =>
            ServerList.MatchingServer.Api.Path("api", "friends");

        public IEnumerator GetFriends(Action<bool, List<FriendSummary>> callback)
        {
            using var webRequest = UnityWebRequest.Get(FriendsEndpoint);
            yield return SendRequest<List<FriendSummary>>(webRequest, callback, "GetFriends");
        }

        public IEnumerator SearchUsers(string query, Action<bool, List<FriendSearchResult>> callback)
        {
            string url = FriendsEndpoint.Path("search").Query("query", query);
            using var webRequest = UnityWebRequest.Get(url);
            yield return SendRequest<List<FriendSearchResult>>(webRequest, callback, "SearchUsers");
        }

        public IEnumerator SearchMembers(string query, Action<bool, List<FriendSearchResult>> callback)
        {
            return SearchUsers(query, callback);
        }

        public IEnumerator GetReceivedRequests(Action<bool, List<FriendRequestItem>> callback)
        {
            using var webRequest = UnityWebRequest.Get(FriendsEndpoint.Path("requests", "received"));
            yield return SendRequest<List<FriendRequestItem>>(webRequest, callback, "GetReceivedRequests");
        }

        public IEnumerator GetSentRequests(Action<bool, List<FriendRequestItem>> callback)
        {
            using var webRequest = UnityWebRequest.Get(FriendsEndpoint.Path("requests", "sent"));
            yield return SendRequest<List<FriendRequestItem>>(webRequest, callback, "GetSentRequests");
        }

        public IEnumerator SendFriendRequest(string targetQuery, Action<bool, FriendRequestItem> callback)
        {
            var payload = new SendFriendRequestPayload(targetQuery);
            string json = JsonCodec.Serialize(payload);

            using var webRequest = new UnityWebRequest(FriendsEndpoint.Path("requests"), "POST");
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return SendRequest<FriendRequestItem>(webRequest, callback, "SendFriendRequest");
        }

        public IEnumerator AcceptFriendRequest(long requestId, Action<bool, FriendRequestItem> callback)
        {
            using var webRequest = new UnityWebRequest(FriendsEndpoint.Path("requests", requestId.ToString(), "accept"), "POST");
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            yield return SendRequest<FriendRequestItem>(webRequest, callback, "AcceptFriendRequest");
        }

        public IEnumerator RejectFriendRequest(long requestId, Action<bool, FriendRequestItem> callback)
        {
            using var webRequest = new UnityWebRequest(FriendsEndpoint.Path("requests", requestId.ToString(), "reject"), "POST");
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            yield return SendRequest<FriendRequestItem>(webRequest, callback, "RejectFriendRequest");
        }

        public IEnumerator CancelFriendRequest(long requestId, Action<bool> callback)
        {
            using var webRequest = UnityWebRequest.Delete(FriendsEndpoint.Path("requests", requestId.ToString()));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            yield return SendEmptyRequest(webRequest, callback, "CancelFriendRequest");
        }

        public IEnumerator RemoveFriend(long friendId, Action<bool> callback)
        {
            using var webRequest = UnityWebRequest.Delete(FriendsEndpoint.Path(friendId.ToString()));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            yield return SendEmptyRequest(webRequest, callback, "RemoveFriend");
        }

        public IEnumerator DeleteFriend(long friendId, Action<bool> callback)
        {
            return RemoveFriend(friendId, callback);
        }

        public IEnumerator InviteFriend(long friendId, Action<bool, FriendInviteItem> callback)
        {
            using var webRequest = new UnityWebRequest(FriendsEndpoint.Path(friendId.ToString(), "invite"), "POST");
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            yield return SendRequest<FriendInviteItem>(webRequest, callback, "InviteFriend");
        }

        public IEnumerator AcceptInvite(string inviteId, Action<bool, MatchedInfoDto> callback)
        {
            using var webRequest = new UnityWebRequest(FriendsEndpoint.Path("invites", inviteId, "accept"), "POST");
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            yield return SendRequest<MatchedInfoDto>(webRequest, callback, "AcceptInvite");
        }

        public IEnumerator RejectInvite(string inviteId, Action<bool, FriendInviteItem> callback)
        {
            using var webRequest = new UnityWebRequest(FriendsEndpoint.Path("invites", inviteId, "reject"), "POST");
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            yield return SendRequest<FriendInviteItem>(webRequest, callback, "RejectInvite");
        }

        public IEnumerator CancelInvite(string inviteId, Action<bool, FriendInviteItem> callback)
        {
            using var webRequest = UnityWebRequest.Delete(FriendsEndpoint.Path("invites", inviteId));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            yield return SendRequest<FriendInviteItem>(webRequest, callback, "CancelInvite");
        }

        public IEnumerator GetPendingInvites(Action<bool, List<FriendInviteItem>> callback)
        {
            using var webRequest = UnityWebRequest.Get(FriendsEndpoint.Path("invites", "pending"));
            yield return SendRequest<List<FriendInviteItem>>(webRequest, callback, "GetPendingInvites");
        }

        private static IEnumerator SendRequest<T>(
            UnityWebRequest webRequest,
            Action<bool, T> callback,
            string operation)
        {
            Server.SetAcceptLanguage(webRequest);
            Server.SetAuthorization(webRequest);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                WDebug.LogError($"[{operation}] HTTP error: {webRequest.responseCode} / {webRequest.error}");
                callback?.Invoke(false, default);
                yield break;
            }

            string body = webRequest.downloadHandler?.text;
            if (string.IsNullOrEmpty(body))
            {
                callback?.Invoke(true, default);
                yield break;
            }

            if (!JsonCodec.TryDeserialize(body, out T result, out string error))
            {
                WDebug.LogError($"[{operation}] JSON deserialize error: {error} / {JsonCodec.Excerpt(body)}");
                callback?.Invoke(false, default);
                yield break;
            }

            callback?.Invoke(true, result);
        }

        private static IEnumerator SendEmptyRequest(
            UnityWebRequest webRequest,
            Action<bool> callback,
            string operation)
        {
            Server.SetAcceptLanguage(webRequest);
            Server.SetAuthorization(webRequest);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                WDebug.LogError($"[{operation}] HTTP error: {webRequest.responseCode} / {webRequest.error}");
                callback?.Invoke(false);
                yield break;
            }

            callback?.Invoke(true);
        }
    }
}
