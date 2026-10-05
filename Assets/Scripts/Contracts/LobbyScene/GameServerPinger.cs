using System;
using System.Collections;
using System.Collections.Generic;
using Data;
using Data.Net;
using Global;
using Global.Serialization;
using UnityEngine;
using UnityEngine.Networking;

namespace LobbyScene
{
    /// <summary>
    /// 매칭 직전에 lobby 가 알려 주는 게임 서버들까지의 핑을 잰다.
    /// lobby 는 이 값으로 두 유저의 최대 핑이 낮은 서버를 고른다.
    /// 어떤 실패도 예외로 올리지 않는다. 못 잰 서버는 결과에서 빠지고, 매칭은 그대로 진행된다.
    /// </summary>
    public static class GameServerPinger
    {
        private const int Rounds = 2;
        private const int TimeoutSeconds = 2;

        private static ServerEndpoint PingTargets =>
            ServerList.MatchingServer.Api.Path("api", "match", "servers");

        public static IEnumerator Measure(Action<List<ServerPing>> callback)
        {
            List<GameServerEndpoint> targets = null;
            yield return FetchTargets(result => targets = result);
            if (targets == null || targets.Count == 0)
            {
                callback(null);
                yield break;
            }

            // 첫 요청은 연결/TLS 설정 시간이 섞여 부풀려지므로 여러 번 재서 가장 작은 값을 쓴다.
            var best = new Dictionary<long, long>();
            for (int round = 0; round < Rounds; round++)
            {
                yield return MeasureRound(targets, best);
            }

            var pings = new List<ServerPing>(best.Count);
            foreach (var pair in best)
            {
                pings.Add(new ServerPing { serverId = pair.Key, rttMs = pair.Value });
            }
            callback(pings);
        }

        private static IEnumerator FetchTargets(Action<List<GameServerEndpoint>> callback)
        {
            using var webRequest = UnityWebRequest.Get(PingTargets);
            webRequest.timeout = TimeoutSeconds;
            Server.SetAcceptLanguage(webRequest);
            Server.SetAuthorization(webRequest);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success ||
                !JsonCodec.TryDeserialize(webRequest.downloadHandler.text, out List<GameServerEndpoint> targets))
            {
                WDebug.LogWarning($"GameServerPinger: could not fetch ping targets: {webRequest.responseCode} / {webRequest.error}");
                callback(null);
                yield break;
            }

            callback(targets);
        }

        /// <summary>모든 서버에 동시에 요청해 한 라운드의 시간을 잰다. 성공한 서버만 <paramref name="best"/> 를 갱신한다.</summary>
        private static IEnumerator MeasureRound(List<GameServerEndpoint> targets, Dictionary<long, long> best)
        {
            var requests = new List<UnityWebRequest>(targets.Count);
            var elapsedMs = new long[targets.Count];
            var pending = targets.Count;

            for (int i = 0; i < targets.Count; i++)
            {
                int index = i;
                var request = UnityWebRequest.Get(targets[i].url.TrimEnd('/') + "/healthcheck");
                request.timeout = TimeoutSeconds;
                requests.Add(request);

                float startedAt = Time.realtimeSinceStartup;
                request.SendWebRequest().completed += _ =>
                {
                    elapsedMs[index] = (long)Math.Round((Time.realtimeSinceStartup - startedAt) * 1000f);
                    pending--;
                };
            }

            while (pending > 0)
            {
                yield return null;
            }

            for (int i = 0; i < requests.Count; i++)
            {
                if (requests[i].result == UnityWebRequest.Result.Success)
                {
                    long serverId = targets[i].serverId;
                    if (!best.TryGetValue(serverId, out long current) || elapsedMs[i] < current)
                    {
                        best[serverId] = elapsedMs[i];
                    }
                }
                requests[i].Dispose();
            }
        }
    }
}
