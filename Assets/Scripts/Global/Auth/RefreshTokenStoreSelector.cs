namespace Global.Auth
{
    /// <summary>
    /// 지금 실행 중인 플랫폼에 맞는 <see cref="IRefreshTokenStore"/> 를 만든다. 각 구현은 그
    /// 플랫폼에서만 compile 되도록 파일 전체가 guard 안에 있다. Windows 의 crypt32 P/Invoke 나
    /// macOS 의 Security framework P/Invoke 는 다른 플랫폼 빌드에 들어가면 실행 중에
    /// DllNotFoundException 으로 터진다.
    /// </summary>
    public static class RefreshTokenStoreSelector
    {
        /// <summary>
        /// Editor 를 먼저 가른다. Editor 는 build target 의 define 도 함께 켜므로, build target 이
        /// Android 나 WebGL 이어도 UNITY_ANDROID / UNITY_WEBGL 이 잡힌다. 그대로 두면 play mode
        /// 에서 AndroidJavaClass 나 jslib 을 부르다 실패한다. 그래서 Editor 에서는 build target 이
        /// 아니라 Editor 가 도는 운영체제에 맞춰 desktop 구현을 고른다.
        /// </summary>
        public static IRefreshTokenStore SelectForCurrentPlatform()
        {
#if UNITY_EDITOR_WIN
            return new WindowsDpapiRefreshTokenStore();
#elif UNITY_EDITOR_OSX
            return new MacKeychainRefreshTokenStore();
#elif UNITY_EDITOR
            // Linux Editor. DPAPI 도 Keychain 도 없어서 session 안에서만 유지한다.
            return new InMemoryRefreshTokenStore();
#elif UNITY_WEBGL
            return new BrowserCookieRefreshTokenStore();
#elif UNITY_ANDROID
            return new AndroidKeyStoreRefreshTokenStore();
#elif UNITY_STANDALONE_WIN
            return new WindowsDpapiRefreshTokenStore();
#elif UNITY_STANDALONE_OSX
            return new MacKeychainRefreshTokenStore();
#else
            return new InMemoryRefreshTokenStore();
#endif
        }
    }
}
