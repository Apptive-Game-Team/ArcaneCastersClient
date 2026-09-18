namespace Global.Auth
{
    /// <summary>
    /// refresh token 을 어디에 두는지는 플랫폼마다 다르다. Windows, macOS, Android 는 값을
    /// 직접 보관하다가 요청 body 에 실어 보내고, WebGL 은 account server 가 심은 HttpOnly
    /// cookie 가 저장소 역할을 해서 값을 읽을 수단이 아예 없다. 그래서 읽기는 성공하지 못할 수
    /// 있는 연산으로 두고, 실제 전달 방식은 <see cref="Delivery"/> 가 알린다.
    /// </summary>
    public interface IRefreshTokenStore
    {
        RefreshTokenDelivery Delivery { get; }

        /// <summary>
        /// 보관 중인 refresh token 을 꺼낸다. 저장된 값이 없거나 이 플랫폼이 값을 읽을 수
        /// 없으면 false 를 돌려준다.
        /// </summary>
        bool TryRead(out string refreshToken);

        /// <summary>
        /// 새로 받은 refresh token 으로 덮어쓴다. rotation 때문에 로그인 한 번에 여러 번
        /// 불린다. <see cref="RefreshTokenDelivery.Cookie"/> 인 구현에서는 서버가 이미
        /// 심었으므로 아무 일도 하지 않는다.
        /// </summary>
        void Save(string refreshToken);

        /// <summary>
        /// 보관 중인 값을 지운다. 로그아웃과 refresh 실패에서 부른다.
        /// </summary>
        void Clear();
    }

    public enum RefreshTokenDelivery
    {
        /// <summary>client 가 값을 들고 있다가 요청 body 에 실어 보낸다.</summary>
        Body,

        /// <summary>브라우저가 cookie 를 자동으로 실어 보낸다. client 는 값을 모른다.</summary>
        Cookie
    }
}
