#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Global.Auth
{
    /// <summary>
    /// WebGL 용 구현. account server 가 refresh token 을 HttpOnly cookie 로 심기 때문에
    /// JavaScript 도 client 도 값을 볼 수 없다. 이 class 는 값을 다루지 않고 전달 방식만 알린다.
    /// 실제 저장과 만료는 브라우저와 서버가 맡는다.
    /// </summary>
    public sealed class BrowserCookieRefreshTokenStore : IRefreshTokenStore
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        /// <summary>
        /// <c>Assets/Plugins/WebGL/RefreshToken.jslib</c> 참고. 이 page 에서 account server 의
        /// cookie 가 저장되면 1, 아니면 0 이다.
        /// </summary>
        [DllImport("__Internal")]
        private static extern int CanUseCookieDelivery(string accountHost);
#endif

        /// <summary>
        /// 이 page 에 맞는 저장소를 고른다. 서버가 심는 cookie 는 page 의 domain 이
        /// <paramref name="accountHost"/> 와 같을 때만 저장된다. itch.io 가 서비스하는
        /// html-classic.itch.zone 과 release play 주소인 github.io 는 여기서 갈린다.
        ///
        /// 그때 cookie 방식을 그대로 쓰면 50분 주기 갱신이 매번 401 을 받아
        /// <see cref="AuthSession"/> 이 경기 중에도 로그인 화면으로 돌려보낸다. 그래서 값을 session
        /// 동안 메모리에 들고 있는 <see cref="InMemoryRefreshTokenStore"/> 로 떨어진다. 새로고침하면
        /// 로그인은 사라지지만 켜 둔 동안은 끊기지 않는다.
        /// </summary>
        public static IRefreshTokenStore SelectForCurrentPage(string accountHost)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (CanUseCookieDelivery(accountHost) != 0) return new BrowserCookieRefreshTokenStore();

            WDebug.LogWarning("[RefreshToken] 이 page 의 domain 이 account server 와 달라 cookie 를 쓸 수 없다."
                              + " 이 session 동안만 메모리에 들고 간다.");
            return new InMemoryRefreshTokenStore();
#else
            // 브라우저가 없으면 판정할 것도 없다. 이 method 는 WebGL 빌드에서만 부른다.
            return new InMemoryRefreshTokenStore();
#endif
        }

        public RefreshTokenDelivery Delivery => RefreshTokenDelivery.Cookie;

        /// <summary>
        /// HttpOnly cookie 는 document.cookie 에도 나오지 않으므로 읽을 방법이 없다. 값이 있는지
        /// 조차 client 는 모르고, refresh 요청을 보내 봐야 서버가 알려준다. 그래서 항상 false 다.
        /// </summary>
        public bool TryRead(out string refreshToken)
        {
            refreshToken = null;
            return false;
        }

        /// <summary>
        /// 저장은 서버가 Set-Cookie 로 이미 끝냈다. rotation 으로 새 token 이 나와도 같은 응답의
        /// Set-Cookie 가 브라우저 쪽 값을 바꾸므로 client 가 할 일이 없다.
        /// </summary>
        public void Save(string refreshToken)
        {
        }

        /// <summary>
        /// cookie 를 지우는 것도 서버 몫이다. logout endpoint 가 Max-Age=0 인 Set-Cookie 로
        /// 지운다. client 가 지울 수 있었다면 HttpOnly 가 아니었을 것이다.
        /// </summary>
        public void Clear()
        {
        }
    }
}
