using System.Collections;
using Data;
using Global;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Global.Serialization;

namespace LobbyScene
{
    public class UserInfoGetter
    {
        public static IEnumerator GetUserInfo()
        {
            AccountUser accountUser;
            GameUser gameUser;
            using (UnityWebRequest webRequest = new UnityWebRequest(ServerList.AccountServer.url + "/api/members/me", "GET"))
            {
                Server.SetAcceptLanguage(webRequest);
                Server.SetAuthorization(webRequest);
            
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                yield return webRequest.SendWebRequest();
                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    WDebug.LogError("Error: " + webRequest.error);
                    SystemMessageUI.Instance.ShowMessage("Failed to retrieve user data. Please log in again.");
                    SceneManager.LoadScene("LoginScene");
                    yield break;
                }

                string body = webRequest.downloadHandler.text;
                if (!JsonCodec.TryDeserialize(body, out accountUser, out string error))
                {
                    WDebug.LogError($"GetUserInfo account parse error: {error} / {JsonCodec.Excerpt(body)}");
                    SystemMessageUI.Instance.ShowMessage("Failed to retrieve user data. Please log in again.");
                    SceneManager.LoadScene("LoginScene");
                    yield break;
                }
            }
        
            using (UnityWebRequest webRequest = new UnityWebRequest(ServerList.MatchingServer.url + "/api/users/mine", "GET"))
            {
                Server.SetAcceptLanguage(webRequest);
                Server.SetAuthorization(webRequest);
            
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                yield return webRequest.SendWebRequest();
                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    WDebug.LogError("Error: " + webRequest.error + webRequest.downloadHandler.text);
                    SystemMessageUI.Instance.ShowMessage("Failed to retrieve user data. Please log in again.");
                    SceneManager.LoadScene("LoginScene");
                    yield break;
                }

                string body = webRequest.downloadHandler.text;
                if (!JsonCodec.TryDeserialize(body, out gameUser, out string error))
                {
                    WDebug.LogError($"GetUserInfo game parse error: {error} / {JsonCodec.Excerpt(body)}");
                    SystemMessageUI.Instance.ShowMessage("Failed to retrieve user data. Please log in again.");
                    SceneManager.LoadScene("LoginScene");
                    yield break;
                }
            }
            
            WDebug.Log(accountUser);
            WDebug.Log(gameUser);
            
            SceneContext.User = new User(accountUser, gameUser);
        }
    }
}
