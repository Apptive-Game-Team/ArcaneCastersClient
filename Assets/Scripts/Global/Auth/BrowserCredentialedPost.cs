using System;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Global.Auth
{
    /// <summary>
    /// <c>Assets/Plugins/WebGL/RefreshToken.jslib</c> 의 <c>SendCredentialedPost</c> 를 부른다.
    /// <see cref="UnityEngine.Networking.UnityWebRequest"/> 에는 withCredentials 를 켜는 C# API 가
    /// 없어서, cross-origin 요청이 HttpOnly refresh token cookie 를 싣지 못한다. 그래서 브라우저의
    /// fetch 로 직접 보낸다.
    ///
    /// 응답은 <paramref name="callbackObjectName"/> 이름의 GameObject 에 SendMessage 로 돌아온다.
    /// 인자는 <see cref="CredentialedPostResponse"/> 모양의 JSON 문자열 하나다.
    /// </summary>
    public static class BrowserCredentialedPost
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void SendCredentialedPost(string url, string objectName, string callbackMethod);
#endif

        /// <summary>
        /// <paramref name="url"/> 로 body 없는 POST 를 보낸다. cookie 는 브라우저가 싣는다.
        /// 응답이 오면 <paramref name="callbackObjectName"/> 의 <paramref name="callbackMethodName"/>
        /// 이 JSON 문자열 하나를 받는다. callback 을 받을 GameObject 는 scene 에 살아 있어야 하고
        /// 이름이 유일해야 한다. SendMessage 는 이름으로 찾는다.
        /// </summary>
        public static void Send(string url, string callbackObjectName, string callbackMethodName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SendCredentialedPost(url, callbackObjectName, callbackMethodName);
#else
            // WebGL 빌드 밖에서는 cookie 를 쓰지 않는다. 다른 플랫폼은 refresh token 을 직접 들고
            // 있으므로 UnityWebRequest 로 body 에 실어 보낸다.
            WDebug.LogError($"[RefreshToken] BrowserCredentialedPost 는 WebGL 빌드에서만 동작한다: {url}");
#endif
        }
    }

    /// <summary>
    /// RefreshToken.jslib 이 SendMessage 로 돌려주는 JSON 의 모양.
    /// </summary>
    [Serializable]
    public class CredentialedPostResponse
    {
        /// <summary>HTTP status code. 응답을 받지 못한 network 실패는 0 이다.</summary>
        public int status;

        /// <summary>응답 body. status 가 0 이면 대신 오류 메시지가 들어온다.</summary>
        public string body;
    }
}
