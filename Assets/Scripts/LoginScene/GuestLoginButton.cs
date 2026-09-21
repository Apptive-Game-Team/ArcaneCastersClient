using System.Collections;
using Data;
using Global;
using Global.Auth;
using Global.Button;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Global.Serialization;

namespace LoginScene
{
    public class GuestLoginButton : AsyncButtonBase
    {
    
        private IEnumerator GuestLoginCoroutine()
        {
        
            using (UnityWebRequest webRequest = new UnityWebRequest(ServerList.AccountServer.url + "/api/members/guest", "POST"))
            {
                Server.SetAcceptLanguage(webRequest);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.timeout = 10;
                webRequest.SetRequestHeader("Content-Type", "application/json");
                Server.SetTokenDelivery(webRequest);
            
                yield return webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    WDebug.LogError("Error: " + webRequest.error);
                    SystemMessageUI.Instance.ShowMessage(webRequest.downloadHandler.text);
                    ResetButton();
                    yield break;
                }
            
                WDebug.Log("Response: " + webRequest.downloadHandler.text);
            
                string body = webRequest.downloadHandler.text;
                if (!JsonCodec.TryDeserialize(body, out GuestAuthResponseDto authResponseDto, out string error))
                {
                    // The body is a 200 payload the client could not read, so it is not worth showing.
                    WDebug.LogError($"GuestLogin parse error: {error} / {JsonCodec.Excerpt(body)}");
                    ResetButton();
                    yield break;
                }

                AuthSession.Instance.BeginSession(authResponseDto.jwt, authResponseDto.refreshToken, authResponseDto.expiresIn);
                GuestContext.GuestPassword = authResponseDto.password;
            
                SceneManager.LoadScene("TutorialScene");
            }
        }

        protected override void OnClickButton()
        {
            StartCoroutine(GuestLoginCoroutine());
        }
    }
}
