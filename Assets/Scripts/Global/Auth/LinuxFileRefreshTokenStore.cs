#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Global.Auth
{
    /// <summary>
    /// Linux 에는 DPAPI 나 Keychain 처럼 항상 있는 secret 저장소가 없다. Secret Service
    /// (libsecret) 는 GNOME·KDE 데스크톱에만 있고 keyring 이 잠겨 있으면 실패하며, zip 으로
    /// 배포하는 빌드에 런타임 의존성을 하나 더한다. 그래서 refresh token 을 암호화하지 않고
    /// <see cref="Application.persistentDataPath"/> 아래 파일에 UTF-8 평문으로 두고, 파일
    /// 권한 0600 (rw-------) 으로 막는다. DPAPI 가 0600 파일보다 더 주는 보호는 "파일을
    /// 복사해 가도 다른 사용자가 복호화하지 못한다" 하나뿐이고, 같은 기계의 다른 사용자는
    /// 0600 으로 이미 막힌다. 백업이나 동기화로 빠져나간 사본만 평문으로 남는다.
    /// </summary>
    public sealed class LinuxFileRefreshTokenStore : IRefreshTokenStore
    {
        private const string FileName = "refresh_token.plain";

        /// <summary>0600 octal (rw-------) 을 10진수로 옮긴 값. C# 에는 8진수 리터럴이 없다.</summary>
        private const int Mode0600 = 384;

        public RefreshTokenDelivery Delivery => RefreshTokenDelivery.Body;

        public bool TryRead(out string refreshToken)
        {
            refreshToken = null;
            string path = ResolvePath();
            if (!File.Exists(path)) return false;

            try
            {
                refreshToken = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception exception)
            {
                WDebug.LogError($"[RefreshToken] Linux 파일을 읽지 못했다: {exception.Message}");
                return false;
            }

            return !string.IsNullOrEmpty(refreshToken);
        }

        public void Save(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                Clear();
                return;
            }

            string path = ResolvePath();
            try
            {
                if (!File.Exists(path))
                {
                    using (File.Create(path)) { }
                }

                if (!TryChmod0600(path)) return;

                File.WriteAllText(path, refreshToken, Encoding.UTF8);
            }
            catch (Exception exception)
            {
                WDebug.LogError($"[RefreshToken] Linux 파일을 쓰지 못했다: {exception.Message}");
            }
        }

        public void Clear()
        {
            try
            {
                string path = ResolvePath();
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception exception)
            {
                WDebug.LogError($"[RefreshToken] Linux 파일을 지우지 못했다: {exception.Message}");
            }
        }

        private static string ResolvePath()
        {
            return Path.Combine(Application.persistentDataPath, FileName);
        }

        /// <summary>
        /// 내용을 쓰기 전에 권한부터 0600 으로 좁힌다. 내용을 먼저 쓰고 나중에 chmod 를 걸면
        /// 그 사이에 같은 기계의 다른 사용자가 평문을 읽을 수 있는 틈이 생긴다. chmod 가
        /// 실패하면 token 을 쓰지 않는다 — 권한을 못 거는데 평문을 남기면 안 된다.
        /// </summary>
        private static bool TryChmod0600(string path)
        {
            int result = chmod(path, Mode0600);
            if (result == 0) return true;

            int error = Marshal.GetLastWin32Error();
            WDebug.LogError($"[RefreshToken] Linux 파일 권한을 0600 으로 걸지 못했다, errno={error}");
            return false;
        }

        // int chmod(const char *pathname, mode_t mode);
        [DllImport("libc", SetLastError = true)]
        private static extern int chmod(string pathname, int mode);
    }
}
#endif
