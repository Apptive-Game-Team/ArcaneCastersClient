using System.Collections;
using Data;
using UnityEngine;
using UnityEngine.Networking;

namespace Global
{
    // 로비 밖에서도 접속 상태를 유지한다. 종료·통신 단절은 서버 TTL로 정리한다.
    public sealed class FriendPresenceHeartbeat : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var host = new GameObject(nameof(FriendPresenceHeartbeat));
            DontDestroyOnLoad(host);
            host.AddComponent<FriendPresenceHeartbeat>();
        }

        private IEnumerator Start()
        {
            while (true)
            {
                if (SceneContext.User == null || string.IsNullOrEmpty(SceneContext.JwtToken))
                {
                    yield return new WaitForSecondsRealtime(1f);
                    continue;
                }

                using (var request = new UnityWebRequest(
                    ServerList.MatchingServer.Api.Path("api", "friends", "presence"), "POST"))
                {
                    request.timeout = 5;
                    Server.SetAuthorization(request);
                    yield return request.SendWebRequest();
                }
                yield return new WaitForSecondsRealtime(10f);
            }
        }
    }
}
