using Global.LocalHosting;
using UnityEngine;

namespace LobbyScene.SettingPage
{
    /// <summary>
    /// 로컬 서버를 돌릴 수 없는 platform(WebGL, Android)에서 이 오브젝트를 숨긴다. 설정 화면의 "로컬 서버" 버튼에 붙는다.
    /// </summary>
    public class DesktopOnlyUI : MonoBehaviour
    {
        private void Awake()
        {
            if (!LocalServerPlatform.IsSupported) gameObject.SetActive(false);
        }
    }
}
