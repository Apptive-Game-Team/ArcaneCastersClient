using System;
using System.Collections;
using Data;
using Global;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Global.Serialization;

namespace LobbyScene
{
    public static class StatusTracker
    {
        [Serializable]
        public class SessionDto
        {
            public string sessionId;
            public User leftUser;
            public User rightUser;
        }

        public static IEnumerator RecoverGameSession()
        {
            var getSessionUrl = $"{ServerList.MatchingServer.url}/api/users/mine/match-info";
                
            var getSessionReq = UnityWebRequest.Get(getSessionUrl);
            Server.SetAcceptLanguage(getSessionReq);
            Server.SetAuthorization(getSessionReq);
                
            yield return getSessionReq.SendWebRequest();

            if (getSessionReq.result != UnityWebRequest.Result.Success)
            {
                WDebug.LogError($"[EnterInGameByMine] fail: {getSessionReq.responseCode} / {getSessionReq.error}\n{getSessionReq.downloadHandler.text}");
                yield return ReportRecoveryFailure();
                yield break;
            }

            string body = getSessionReq.downloadHandler.text;

            if (string.IsNullOrWhiteSpace(body))
            {
                WDebug.LogError("[EnterInGameByMine] Response body is empty.");
                yield return ReportRecoveryFailure();
                yield break;
            }

            if (!JsonCodec.TryDeserialize(body, out MatchedInfoDto matchedInfoDto, out string parseError) ||
                matchedInfoDto == null)
            {
                WDebug.LogError($"[EnterInGameByMine] parse error: {parseError} / {JsonCodec.Excerpt(body)}");
                yield return ReportRecoveryFailure();
                yield break;
            }

            WDebug.Log("[EnterInGameByMine] successfully recover session: " + body);

            SceneContext.MatchInfo = matchedInfoDto;
            // A recovered session is never an adventure match; guarantee the forest default
            // in case a previous adventure match left a theme set.
            SceneContext.BattleTheme = null;
            yield return GameDataRefresh.Refresh();
            SceneManager.LoadScene("GameScene");
        }

        /// <summary>
        /// 복구에 실패했다고 서버에 신고한다. 이미 로비에 있으므로 씬은 옮기지 않는다.
        /// 세션 소실이 확정되면 매칭 UI만 다시 읽어 바로 재매칭할 수 있게 한다.
        /// </summary>
        private static IEnumerator ReportRecoveryFailure()
        {
            yield return SessionLossReporter.Report(SessionLossReason.SessionRecoveryFailed, verdict =>
            {
                WDebug.Log($"[EnterInGameByMine] 세션 복구 실패 신고 판정: {verdict}");

                // 판정이 보류면 상태를 단정하지 않는다. 서버가 정리한 뒤 스냅샷이 따라온다.
                if (!verdict.ClearsMatchState()) return;
                if (LobbySceneViewModel.Instance == null) return;
                LobbySceneViewModel.Instance.CheckIfInQueue();
            });
        }
    }
}
