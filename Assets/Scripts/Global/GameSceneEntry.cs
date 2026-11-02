using System;
using System.Collections;
using Global.Stomp;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;

namespace Global
{
    public static class GameSceneEntry
    {
        /// <summary>STOMP CONNECTED를 받은 연결을 유지한 채 게임 씬을 연다.</summary>
        public static IEnumerator PrepareAndLoad(UnityEngine.Object owner, Action onFailure = null,
            string sceneName = "GameScene")
        {
            var match = SceneContext.MatchInfo;
            if (owner == null || match == null)
            {
                onFailure?.Invoke();
                yield break;
            }
            var connection = GameServerConnection.ForMatch(match);
            // 중복 진입 요청은 기존 준비나 게임 씬이 소유한 연결을 취소하지 않는다.
            if (connection.IsPreparing || connection.IsClaimed)
            {
                onFailure?.Invoke();
                yield break;
            }
            bool ready = false;
            yield return connection.Prepare(match, owner, result => ready = result);
            if (!ready || owner == null || !ReferenceEquals(SceneContext.MatchInfo, match) ||
                connection == null || !connection.Transport.IsConnected)
            {
                if (connection != null) connection.Close();
                if (owner != null)
                {
                    SystemMessageUI.Instance?.ShowMessage(new LocalizedString("SystemMessageUI", "connectionClosed"));
                    onFailure?.Invoke();
                }
                yield break;
            }
            SceneManager.LoadScene(sceneName);
        }
    }
}
