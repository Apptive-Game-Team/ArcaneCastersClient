using System.Collections;
using Global;
using Global.Auth;
using Global.Util;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LoginScene
{
    public class LoginSceneInitializer : MonoBehaviour
    {
        [SerializeField] private GameObject penal;
        [SerializeField] private TMP_Text messageText;

        private IEnumerator Start()
        {
            using LoadingHandle loadingHandle = LoadingPage.Begin(this);

            bool isHealthy = false;
            yield return DeployStatusChecker.CheckDeployStatus((healthy, message) =>
            {
                isHealthy = healthy;
                if (!healthy)
                {
                    penal.SetActive(true);
                    messageText.text = message;
                }
            });

            if (!isHealthy) yield break;

            // 앱을 켤 때 refresh 를 한 번 시도한다. 성공하면 로그인 화면을 건너뛴다. WebGL 은 cookie 가
            // 있는지 미리 알 수 없어, 실패(401)했을 때만 평소대로 이 화면을 그대로 보여준다.
            bool sessionRestored = false;
            yield return AuthSession.Instance.TryRestoreSession(restored => sessionRestored = restored);

            if (!sessionRestored) yield break;

            yield return JwksService.FetchJwks();
            SceneManager.LoadScene("LobbyScene");
        }
    }
}
