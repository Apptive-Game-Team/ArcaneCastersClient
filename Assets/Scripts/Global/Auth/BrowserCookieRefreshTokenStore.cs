namespace Global.Auth
{
    /// <summary>
    /// WebGL 용 구현. account server 가 refresh token 을 HttpOnly cookie 로 심기 때문에
    /// JavaScript 도 client 도 값을 볼 수 없다. 이 class 는 값을 다루지 않고 전달 방식만 알린다.
    /// 실제 저장과 만료는 브라우저와 서버가 맡는다.
    /// </summary>
    public sealed class BrowserCookieRefreshTokenStore : IRefreshTokenStore
    {
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
