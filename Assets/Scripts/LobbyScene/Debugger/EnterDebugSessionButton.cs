using System;
using System.Collections;
using Data;
using Global;
using Global.Button;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Global.Serialization;

namespace LobbyScene.Debugger
{
    public class EnterDebugSessionButton : ButtonBase
    {
        private const string DebugBaseUrl = "http://localhost:7777";

        [SerializeField] private string side;

        protected override void OnClickButton()
        {
            if (side.Equals("Practice"))
            {
                StartCoroutine(EnterPracticeDebugSession());
                return;
            }

            StartCoroutine(EnterDebugSession());
        }

        private IEnumerator EnterPracticeDebugSession()
        {
            using var request = UnityWebRequest.PostWwwForm($"{DebugBaseUrl}/api/debug/game/practice", "");
            Server.SetAcceptLanguage(request);
            Server.SetAuthorization(request);
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                WDebug.LogError($"Error entering debug session: {request.error}");
                yield break;
            }

            string json = request.downloadHandler.text;
            WDebug.Log(json);

            if (!JsonCodec.TryDeserialize(json, out DebugGameResponse response, out string error))
            {
                WDebug.LogError($"Error parsing debug session: {error} / {JsonCodec.Excerpt(json)}");
                yield break;
            }

            SceneContext.MatchInfo = MatchedInfoDto.CreateDebugSession(response.sessionId, "left", SceneContext.UserID);
            // A debug practice session is never an adventure match; guarantee the forest
            // default in case a previous adventure match left a theme set.
            SceneContext.ClearAdventureMatch();
            yield return GameDataRefresh.Refresh();
            SceneManager.LoadScene("GameScene");
        }

        private IEnumerator EnterDebugSession()
        {
            using var request = UnityWebRequest.PostWwwForm($"{DebugBaseUrl}/api/debug/game/{side}", "");
            Server.SetAcceptLanguage(request);
            Server.SetAuthorization(request);
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                WDebug.LogError($"Error entering debug session: {request.error}");
                yield break;
            }

            string json = request.downloadHandler.text;
            WDebug.Log(json);

            if (!JsonCodec.TryDeserialize(json, out DebugGameResponse response, out string error))
            {
                WDebug.LogError($"Error parsing debug session: {error} / {JsonCodec.Excerpt(json)}");
                yield break;
            }

            SceneContext.MatchInfo = MatchedInfoDto.CreateDebugSession(response.sessionId, side, SceneContext.UserID);
            // A debug 1v1 session is never an adventure match; guarantee the forest default
            // in case a previous adventure match left a theme set.
            SceneContext.ClearAdventureMatch();
            yield return GameDataRefresh.Refresh();
            SceneManager.LoadScene("GameScene");
        }
    }
}
