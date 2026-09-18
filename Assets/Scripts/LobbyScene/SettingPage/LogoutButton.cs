using System.Collections;
using Global;
using Global.Auth;
using Global.Button;
using Global.Util;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;

namespace LobbyScene.SettingPage
{
    /// <summary>
    /// 로비 설정 화면의 로그아웃 버튼. <see cref="AuthSession.Logout"/> 이 서버의 취소 endpoint 를
    /// 부르고 refresh token 저장소와 <see cref="SceneContext"/> 를 비운 뒤 LoginScene 으로 돌아간다.
    ///
    /// member 는 다시 로그인하면 되므로 확인 절차가 없다. guest 는 이메일이
    /// <c>guest_&lt;UUID&gt;@example.com</c> 이고 password 도 사용자가 모르므로, 로그아웃하면 그 계정으로
    /// 다시 들어올 수 없다. 그래서 guest 일 때만 첫 번째 누름이 경고를 띄우고 아무것도 하지 않으며,
    /// <see cref="guestConfirmWindowSeconds"/> 안에 한 번 더 눌러야 실제로 로그아웃한다. 로비 scene 에는
    /// <see cref="ConfirmationDialogController"/> 가 배치돼 있지 않아 버튼 자체에서 확인을 받는다.
    ///
    /// guest 여부는 access token 의 `guest` claim 에서 읽는다(<see cref="JwtHelper.IsGuest"/>).
    /// <see cref="Data.GuestContext.IsGuest"/> 는 <c>GuestLoginButton</c> 을 실제로 거친 세션에서만
    /// 채워지고, refresh token 으로 복원된 세션에서는 비어 있어 이 경고가 뜨지 않았다(#74).
    /// </summary>
    public class LogoutButton : AsyncButtonBase
    {
        private const string LoginSceneName = "LoginScene";

        [SerializeField] private LocalizedString guestLogoutWarning;

        /// <summary>경고를 띄운 뒤 두 번째 누름을 확인으로 받아 주는 시간. SystemMessageUI 가 안내를 띄우는 5초와 맞춘다.</summary>
        [SerializeField] private float guestConfirmWindowSeconds = 5f;

        private float guestConfirmDeadline;

        protected override void OnClickButton()
        {
            if (JwtHelper.IsGuest(SceneContext.JwtToken) && Time.realtimeSinceStartup > guestConfirmDeadline)
            {
                WarnGuest();
                return;
            }

            StartCoroutine(LogoutAndReturnToLogin());
        }

        private void WarnGuest()
        {
            guestConfirmDeadline = Time.realtimeSinceStartup + guestConfirmWindowSeconds;

            // scene 마다 SystemMessageUI 가 있으리라 보장할 수 없다. 안내를 못 띄워도 두 번째 누름은 받는다.
            if (SystemMessageUI.Instance != null) SystemMessageUI.Instance.ShowMessage(guestLogoutWarning);

            StartCoroutine(ReArmAfterWarning());
        }

        /// <summary>
        /// <see cref="AsyncButtonBase.ButtonEvent"/> 는 <see cref="OnClickButton"/> 이 끝난 뒤에
        /// 버튼을 잠그므로, 같은 frame 에 되살리면 그 잠금이 덮어쓴다. 한 frame 기다렸다 되살린다.
        /// </summary>
        private IEnumerator ReArmAfterWarning()
        {
            yield return null;
            ResetButton();
        }

        private IEnumerator LogoutAndReturnToLogin()
        {
            // AuthSession 은 DontDestroyOnLoad 라, 이 버튼이 사라져도 취소 요청이 끝까지 간다.
            yield return AuthSession.Instance.StartCoroutine(AuthSession.Instance.Logout());

            SceneManager.LoadScene(LoginSceneName);
        }
    }
}
