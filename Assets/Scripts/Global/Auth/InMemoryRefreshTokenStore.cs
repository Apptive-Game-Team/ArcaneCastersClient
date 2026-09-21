namespace Global.Auth
{
    /// <summary>
    /// 플랫폼 secret 저장소가 없는 자리에서 쓰는 마지막 수단. 값을 필드에만 들고 있으므로 앱을
    /// 끄면 사라지고, 로그인 유지라는 목적은 못 채운다. 그래도 <see cref="IRefreshTokenStore"/>
    /// 가 null 이 되는 것보다는 낫다. 부르는 쪽이 null 검사를 하지 않아도 되고, 실행 중인 session
    /// 안에서는 rotation 이 정상으로 돈다.
    ///
    /// 지금 여기로 오는 경우는 Linux Editor, Linux standalone, 그리고 cookie 를 저장할 수 없는
    /// page 에서 도는 WebGL 빌드다. 마지막 경우는
    /// <see cref="BrowserCookieRefreshTokenStore.SelectForCurrentPage"/> 를 보라.
    /// </summary>
    public sealed class InMemoryRefreshTokenStore : IRefreshTokenStore
    {
        private string refreshToken;

        public RefreshTokenDelivery Delivery => RefreshTokenDelivery.Body;

        public bool TryRead(out string refreshToken)
        {
            refreshToken = this.refreshToken;
            return !string.IsNullOrEmpty(refreshToken);
        }

        public void Save(string refreshToken)
        {
            this.refreshToken = refreshToken;
        }

        public void Clear()
        {
            refreshToken = null;
        }
    }
}
