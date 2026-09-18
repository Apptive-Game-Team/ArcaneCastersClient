using System.Collections;
using Data;
using Global;
using Global.Auth;
using Global.Button;
using UnityEngine.Localization;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace LobbyScene.SettingPage
{
    public class DeleteAccountButton : AsyncButtonBase
    {
        
        public LocalizedString confirmationMessage;
        
        protected override void OnClickButton()
        {
            StartCoroutine(DeleteAccount());
        }

        private IEnumerator DeleteAccount()
        {
            using UnityWebRequest requestToServer = UnityWebRequest.Delete($"{ServerList.MatchingServer.url}/api/users/mine");
            using UnityWebRequest requestToAccount = UnityWebRequest.Delete($"{ServerList.AccountServer.url}/api/members/me");
            Server.SetAcceptLanguage(requestToServer);
            Server.SetAcceptLanguage(requestToAccount);
            Server.SetAuthorization(requestToServer);
            Server.SetAuthorization(requestToAccount);
            
            yield return requestToServer.SendWebRequest();
            yield return requestToAccount.SendWebRequest();

            if (requestToServer.result == UnityWebRequest.Result.Success && requestToAccount.result == UnityWebRequest.Result.Success)
            {
                WDebug.Log("Account deleted successfully.");

                // 삭제 요청 두 개가 끝난 뒤에 부른다. Logout 이 SceneContext 를 비우므로, 먼저 부르면
                // 두 DELETE 가 쓰는 access token 이 사라진다. 계정이 이미 없어 취소 endpoint 는 실패할
                // 수 있지만 Logout 은 응답과 무관하게 refresh token 저장소를 비우고 갱신 loop 를 멈춘다.
                // 그러지 않으면 죽은 token 이 기기에 남아 다음 실행과 50분 주기 갱신이 계속 서버로 보낸다.
                yield return AuthSession.Instance.StartCoroutine(AuthSession.Instance.Logout());

                SystemMessageUI.Instance.ShowMessage(confirmationMessage, () =>
                {
                    SceneManager.LoadScene("LoginScene");
                });
                
                yield break;
            }
            WDebug.LogError($"Error deleting account: {requestToServer.error}");
            ResetButton();
        }
    }
}