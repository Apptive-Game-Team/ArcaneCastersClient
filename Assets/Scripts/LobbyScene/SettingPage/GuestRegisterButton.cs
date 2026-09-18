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
            // lastPassword 는 정식 회원이 자기 password 를 바꿀 때 옛 값을 대조하려고 두는 필드다.
            // guest 의 password 는 joinGuest 가 난수로 만들어 준 값이라 사용자가 정한 적도, 본 적도
            // 없다. 서버는 이제 guest 전환에서 이 대조를 건너뛰고 access token 의 memberId 로만
            // 판단하므로(#74), 여기서는 값을 보내지 않는다. NullValueHandling.Ignore(JsonCodec) 라
            // null 은 필드 자체가 요청 본문에서 빠진다. GuestContext.GuestPassword 는 복원된 세션에서는
            // 비어 있어 어차피 못 쓴다 — 이 버튼은 GuestContext.GuestPassword 유무에 기대지 않는다.
            StartCoroutine(RegisterCoroutine(
                new MemberPutRequest(nameInputField.text, emailInputField.text, passwordInputField.text, null)
            ));
        }
    }
}