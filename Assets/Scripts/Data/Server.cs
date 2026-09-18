using System;
using System.Collections;
using System.Diagnostics;
using Data.Net;
using Global;
using Global.Auth;
using UnityEngine.Localization.Settings;
using UnityEngine.Networking;

namespace Data
{
    public class Server
    {
        public Server(string name, string host, int port, bool isSecure = false)
        {
            this.name = name;
            this.host = host;
            this.port = port;
            this.isSecure = isSecure;
        }

        public string name;
        public string host;
        public int port;
        public bool isSecure;

        /// <summary>
        /// REST 요청용 엔드포인트. 경로와 쿼리는 <see cref="ServerEndpoint"/>로 이어 붙인다.
        /// </summary>
        public ServerEndpoint Api => ServerEndpoint.Of(url);

        /// <summary>STOMP 등 웹소켓 접속용 엔드포인트.</summary>
        public ServerEndpoint WebSocket => Api.AsWebSocket();

        /// <summary>
        /// 문자열 보간으로 URL을 조립하는 기존 호출부용 호환 프로퍼티.
        /// 새 코드는 <see cref="Api"/>를 쓴다.
        /// </summary>
        public string url => $"{(isSecure ? "https" : "http")}://{host}:{port}";

        public static void SetAcceptLanguage(UnityWebRequest req)
        {
            var locale = LocalizationSettings.SelectedLocale;
            
            var lang = locale?.Identifier.Code;

            if (!string.IsNullOrWhiteSpace(lang))
            {
                req.SetRequestHeader("Accept-Language", lang);
            }
            else
            {
                WDebug.LogWarning("Accept-Language header skipped (null or empty).");
            }
        }
        
        public static void SetAuthorization(UnityWebRequest webRequest)
        {
            webRequest.SetRequestHeader("Authorization", "Bearer " + SceneContext.JwtToken);
        }

        /// <summary>
        /// 계정 서버가 refresh token 을 body 로 받을지 cookie 로 받을지 판가름하는 header.
        /// <see cref="AuthSession"/> 이 없을 수 있는 시점(가장 이른 부팅 단계)에는 body 로
        /// fallback 한다 — WebGL 이 아닌 모든 플랫폼의 실제 전달 방식과 같다.
        /// </summary>
        public static void SetTokenDelivery(UnityWebRequest webRequest)
        {
            bool isCookie = AuthSession.Instance != null &&
                            AuthSession.Instance.TokenDelivery == RefreshTokenDelivery.Cookie;
            webRequest.SetRequestHeader("X-Token-Delivery", isCookie ? "cookie" : "body");
        }

        public IEnumerator GetPing(Action<int> callback)
        {
            Stopwatch stopwatch = new Stopwatch();
            using var www = new UnityWebRequest(Api.Path("healthcheck"), "GET");
            
            stopwatch.Start();
            yield return www.SendWebRequest();
            stopwatch.Stop();

            if (www.result == UnityWebRequest.Result.Success)
            {
                int ping = (int)stopwatch.ElapsedMilliseconds;
                callback?.Invoke(ping);
            }
            else
            {
                callback?.Invoke(-1);
            }
        }
    }
}