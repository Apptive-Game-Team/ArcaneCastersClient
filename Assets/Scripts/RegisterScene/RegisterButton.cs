using System.Collections;
using Data;
using Global;
using Global.Auth;
using Global.Button;
using LoginScene;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Global.Serialization;

namespace RegisterScene
{
    public class RegisterButton : AsyncButtonBase
    {
        [SerializeField] private InputField nameInputField;
        [SerializeField] private InputField emailInputField;
        [SerializeField] private InputField passwordInputField;
    
        private IEnumerator RegisterCoroutine(RegisterRequestDto registerRequestDto)
        {
            string jsonData = JsonCodec.Serialize(registerRequestDto);
        
            using (UnityWebRequest webRequest = new UnityWebRequest(ServerList.AccountServer.url + "/api/members", "POST"))
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
                    WDebug.LogError("Error: " + webRequest.downloadHandler.text);
                    SystemMessageUI.Instance.ShowMessage(webRequest.downloadHandler.text);
                    ResetButton();
                    yield break;
                }
            
                WDebug.Log("Response: " + webRequest.downloadHandler.text);
            
                string body = webRequest.downloadHandler.text;
                if (!JsonCodec.TryDeserialize(body, out AuthResponseDto authResponseDto, out string error))
                {
                    WDebug.LogError($"Register parse error: {error} / {JsonCodec.Excerpt(body)}");
                    ResetButton();
                    yield break;
                }

                AuthSession.Instance.BeginSession(authResponseDto.jwt, authResponseDto.refreshToken, authResponseDto.expiresIn);

                SceneManager.LoadScene("TutorialScene");
            }
        }

        protected override void OnClickButton()
        {
            StartCoroutine(RegisterCoroutine(
                new RegisterRequestDto(emailInputField.text, passwordInputField.text, nameInputField.text)
            ));
        }
    }
}
