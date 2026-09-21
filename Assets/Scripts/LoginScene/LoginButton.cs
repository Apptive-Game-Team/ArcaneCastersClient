using System.Collections;
using Data;
using Global;
using Global.Auth;
using Global.Button;
using Global.Util;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Global.Serialization;

namespace LoginScene
{
    public class LoginButton : AsyncButtonBase
    {
        [SerializeField] private InputField emailInputField;
        [SerializeField] private InputField passwordInputField;

        public LocalizedString loginErrorProcessingResponse;
    
        private IEnumerator LoginCoroutine(LoginRequestDto loginRequestDto)
        {
            string jsonData = JsonCodec.Serialize(loginRequestDto);
        
            using (UnityWebRequest webRequest = new UnityWebRequest(ServerList.AccountServer.url + "/api/members/login", "POST"))
            {
                Server.SetAcceptLanguage(webRequest);
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                Server.SetTokenDelivery(webRequest);
                webRequest.timeout = 10;
            
                yield return webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    WDebug.LogError("Error: " + webRequest.error);
                    SystemMessageUI.Instance.ShowMessage(loginErrorProcessingResponse);
                    ResetButton();
                    yield break;
                }
            
                WDebug.Log("Response: " + webRequest.downloadHandler.text);

                try
                {
                    AuthResponseDto authResponseDto = JsonCodec.Deserialize<AuthResponseDto>(webRequest.downloadHandler.text);
                    AuthSession.Instance.BeginSession(authResponseDto.jwt, authResponseDto.refreshToken, authResponseDto.expiresIn);
                } catch (System.Exception e)
                {
                    WDebug.LogError("Parsing Error: " + e.Message);
                    SystemMessageUI.Instance.ShowMessage(loginErrorProcessingResponse);
                    ResetButton();
                }
                
                yield return JwksService.FetchJwks();
                SceneManager.LoadScene("LobbyScene");
            }
        }

        protected override void OnClickButton()
        {
            StartCoroutine(LoginCoroutine(
                new LoginRequestDto(emailInputField.text, passwordInputField.text)
            ));
        }
    }
}
