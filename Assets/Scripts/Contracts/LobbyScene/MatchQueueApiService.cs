using System;
using System.Collections;
using System.Collections.Generic;
using Data;
using Data.Net;
using Global;
using UnityEngine;
using UnityEngine.Networking;
using Global.Serialization;

namespace LobbyScene
{
    public class MatchQueueApiService : MonoBehaviour
    {
        private static ServerEndpoint MatchTickets =>
            ServerList.MatchingServer.Api.Path("api", "match", "tickets");

        public virtual IEnumerator CreateTicket(string deckMode, Action<MatchTicket> callback)
        {
            // 핑 측정은 매칭을 막지 않는다. 실패하면 빈 목록으로 큐에 들어간다.
            List<ServerPing> pings = null;
            yield return GameServerPinger.Measure(result => pings = result);

            using var webRequest = new UnityWebRequest(MatchTickets, "POST");
            string json = JsonCodec.Serialize(new MatchTicketRequest { deckMode = deckMode, serverPings = pings });
            webRequest.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            yield return SendTicketRequest(webRequest, callback, "CreateTicket");
        }

        public virtual IEnumerator GetActiveTicket(Action<bool, MatchTicket> callback)
        {
            using var webRequest = UnityWebRequest.Get(MatchTickets.Path("active"));
            Server.SetAcceptLanguage(webRequest);
            Server.SetAuthorization(webRequest);
            yield return webRequest.SendWebRequest();

            if (webRequest.responseCode == 404)
            {
                callback(true, null);
                yield break;
            }

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                WDebug.LogError($"GetActiveTicket error: {webRequest.responseCode} / {webRequest.error}");
                callback(false, null);
                yield break;
            }

            string body = webRequest.downloadHandler.text;
            if (!JsonCodec.TryDeserialize(body, out MatchTicket ticket, out string error))
            {
                WDebug.LogError($"GetActiveTicket parse error: {error} / {JsonCodec.Excerpt(body)}");
                callback(false, null);
                yield break;
            }

            callback(true, ticket);
        }

        public virtual IEnumerator CancelTicket(string ticketId, Action<MatchCancelResult> callback)
        {
            using var webRequest = UnityWebRequest.Delete(MatchTickets.Path(ticketId));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            Server.SetAcceptLanguage(webRequest);
            Server.SetAuthorization(webRequest);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                WDebug.LogError($"CancelTicket error: {webRequest.responseCode} / {webRequest.error}");
                callback(null);
                yield break;
            }

            string body = webRequest.downloadHandler.text;
            if (!JsonCodec.TryDeserialize(body, out MatchCancelResult result, out string error))
            {
                WDebug.LogError($"CancelTicket parse error: {error} / {JsonCodec.Excerpt(body)}");
                callback(null);
                yield break;
            }

            callback(result);
        }

        private static IEnumerator SendTicketRequest(
            UnityWebRequest webRequest,
            Action<MatchTicket> callback,
            string operation)
        {
            Server.SetAcceptLanguage(webRequest);
            Server.SetAuthorization(webRequest);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                WDebug.LogError($"{operation} error: {webRequest.responseCode} / {webRequest.error}");
                callback(null);
                yield break;
            }

            string body = webRequest.downloadHandler.text;
            if (!JsonCodec.TryDeserialize(body, out MatchTicket ticket, out string error))
            {
                WDebug.LogError($"{operation} parse error: {error} / {JsonCodec.Excerpt(body)}");
                callback(null);
                yield break;
            }

            callback(ticket);
        }

        public IEnumerator MatchPractice(Action<MatchedInfoDto> callback)
        {
            using var webRequest = UnityWebRequest.Get(ServerList.MatchingServer.Api.Path("api", "match", "practice", "me"));
            Server.SetAcceptLanguage(webRequest);
            Server.SetAuthorization(webRequest);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                WDebug.LogError($"MatchPractice error: {webRequest.error}");
                callback(null);
                yield break;
            }

            string body = webRequest.downloadHandler.text;
            if (!JsonCodec.TryDeserialize(body, out MatchedInfoDto matchedInfoDto, out string error))
            {
                WDebug.LogError($"MatchPractice parse error: {error} / {JsonCodec.Excerpt(body)}");
                callback(null);
                yield break;
            }

            callback(matchedInfoDto);
        }

    }
}
