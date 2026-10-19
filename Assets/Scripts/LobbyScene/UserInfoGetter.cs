using System.Collections;
using Data;
using Global;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

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

                bool isSuccess = webRequest.result == UnityWebRequest.Result.Success;
                string body = webRequest.downloadHandler.text;

                if (!UserInfoProcessor.TryProcessAccountResponse(isSuccess, body, out accountUser, out string error))
                {
                    WDebug.LogError(isSuccess ? error : "Error: " + webRequest.error);
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

                bool isSuccess = webRequest.result == UnityWebRequest.Result.Success;
                string body = webRequest.downloadHandler.text;

                if (!UserInfoProcessor.TryProcessGameResponse(isSuccess, body, out gameUser, out string error))
                {
                    WDebug.LogError(isSuccess ? error : "Error: " + webRequest.error + webRequest.downloadHandler.text);
                    SystemMessageUI.Instance.ShowMessage("Failed to retrieve user data. Please log in again.");
                    SceneManager.LoadScene("LoginScene");
                    yield break;
                }
            }
            
            WDebug.Log(accountUser);
            WDebug.Log(gameUser);
            
            SceneContext.User = UserInfoProcessor.CreateUser(accountUser, gameUser);
        }

        /// <summary>
        /// Rereads the matching server's view of the signed-in user into <see cref="SceneContext.User"/>.
        /// The lobby places the deck dropdown by <c>selectedDeckId</c>, and the game server deals the deck
        /// the matching server has selected, so a value kept from login can show one deck and play another.
        /// On failure the current values stay, because the lobby is still usable with them.
        /// </summary>
        public static IEnumerator RefreshGameUser()
        {
            using UnityWebRequest webRequest = new UnityWebRequest(ServerList.MatchingServer.url + "/api/users/mine", "GET");
            Server.SetAcceptLanguage(webRequest);
            Server.SetAuthorization(webRequest);

            webRequest.downloadHandler = new DownloadHandlerBuffer();
            yield return webRequest.SendWebRequest();

            bool isSuccess = webRequest.result == UnityWebRequest.Result.Success;
            string body = webRequest.downloadHandler.text;

            if (!UserInfoProcessor.TryProcessGameResponse(isSuccess, body, out GameUser gameUser, out string error))
            {
                WDebug.LogWarning($"Keeping cached user data: {(isSuccess ? error : webRequest.error)}");
                yield break;
            }

            User user = SceneContext.User;
            if (user == null)
            {
                yield break;
            }

            user.selectedDeckId = gameUser.selectedDeckId;
            user.mmr = gameUser.mmr;
            user.appearance = gameUser.appearance;
        }
    }
}
