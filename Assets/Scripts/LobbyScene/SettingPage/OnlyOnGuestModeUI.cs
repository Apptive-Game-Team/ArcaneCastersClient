using Global;
using Global.Util;
using UnityEngine;

namespace LobbyScene.SettingPage
{
    /// <summary>
    /// 이 GameObject 를 guest 에게만 보인다. guest 여부는 access token 의 `guest` claim 에서
    /// 읽는다(<see cref="JwtHelper.IsGuest"/>) — <see cref="Data.GuestContext.IsGuest"/> 는
    /// <c>GuestLoginButton</c> 을 실제로 거친 세션에서만 채워지고, refresh token 으로 복원된
    /// 세션에서는 비어 있어 정식 계정 전환 버튼이 숨어버렸다(#74).
    /// </summary>
    public class OnlyOnGuestModeUI : MonoBehaviour
    {
        private void Start()
        {
            gameObject.SetActive(JwtHelper.IsGuest(SceneContext.JwtToken));
        }
    }
}
