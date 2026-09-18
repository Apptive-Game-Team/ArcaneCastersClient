using System.Collections;
using Data;
using Global;
using Global.Auth;
using Global.Button;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Global.Serialization;

namespace LobbyScene.SettingPage
{

    public class GuestRegisterButton : AsyncButtonBase
    {
        [SerializeField] private LocalizedString registrationSuccessMessage;
        [SerializeField] private InputField nameInputField;
        [SerializeField] private InputField emailInputField;
        [SerializeField] private InputField passwordInputField;
        
        private IEnumerator RegisterCoroutine(MemberPutRequest memberPutRequest)
        {
            string jsonData = JsonCodec.Serialize(memberPutRequest);

            using UnityWebRequest webRequest = new UnityWebRequest(ServerList.AccountServer.url + "/api/members/me", "PUT");
            Server.SetAcceptLanguage(webRequest);
            Server.SetAuthorization(webRequest);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.downloadHandler = new DownloadHandlerBuffer();
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

            // 옛 SceneContext.ClearContext() 는 메모리만 비웠다. 그러면 guest 이던 시절의 refresh token
            // family 가 서버에서 60일 동안 살아 있고 값도 기기에 남는다. Logout 이 취소 endpoint 를
            // 부르고 저장소와 SceneContext(GuestContext 포함) 를 비운 다음 갱신 loop 를 멈춘다.
            yield return AuthSession.Instance.StartCoroutine(AuthSession.Instance.Logout());

            SystemMessageUI.Instance.ShowMessage(registrationSuccessMessage, () =>
            {
                SceneManager.LoadScene("LoginScene");
            });
        }

        protected override void OnClickButton()
        {
            StartCoroutine(RegisterCoroutine(
                new MemberPutRequest(nameInputField.text, emailInputField.text, passwordInputField.text, GuestContext.GuestPassword)
            ));
        }
    }
}