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
            // 경로에는 사용자 계정 이름이 들어가고 그 안에 ASCII 밖의 글자가 있을 수 있다.
            // 기본 ANSI marshalling 은 locale 에 따라 다른 byte 를 내보내므로 UTF-8 로 직접
            // 옮기고 NUL 을 붙여 넘긴다.
            byte[] pathBytes = Encoding.UTF8.GetBytes(path + "\0");

            if (!TryCallChmod(pathBytes, out int result)) return false;
            if (result == 0) return true;

            int error = Marshal.GetLastWin32Error();
            WDebug.LogError($"[RefreshToken] Linux 파일 권한을 0600 으로 걸지 못했다, errno={error}");
            return false;
        }

        /// <summary>
        /// glibc 에서 `libc.so` 는 공유 라이브러리가 아니라 링커 스크립트라서 dlopen 이
        /// 거부한다. 실제로 열리는 이름은 `libc.so.6` 다. 짧은 이름 `libc` 는 runtime 의
        /// dllmap 이 옮겨 줄 때만 열리고 Unity player 가 그 설정을 싣는다는 보장이 없어서,
        /// soname 을 먼저 시도하고 열리지 않을 때만 짧은 이름으로 물러선다.
        /// </summary>
        private static bool TryCallChmod(byte[] pathBytes, out int result)
        {
            try
            {
                result = ChmodInLibcSo6(pathBytes, Mode0600);
                return true;
            }
            catch (Exception exception) when (exception is DllNotFoundException || exception is EntryPointNotFoundException)
            {
                // soname 이 없는 libc 구현으로 넘어간다.
            }

            try
            {
                result = ChmodInLibc(pathBytes, Mode0600);
                return true;
            }
            catch (Exception exception) when (exception is DllNotFoundException || exception is EntryPointNotFoundException)
            {
                WDebug.LogError($"[RefreshToken] libc 의 chmod 를 부르지 못해 파일 권한을 걸 수 없다: {exception.Message}");
                result = -1;
                return false;
            }
        }

        // int chmod(const char *pathname, mode_t mode);
        [DllImport("libc.so.6", EntryPoint = "chmod", SetLastError = true)]
        private static extern int ChmodInLibcSo6(byte[] pathname, int mode);

        [DllImport("libc", EntryPoint = "chmod", SetLastError = true)]
        private static extern int ChmodInLibc(byte[] pathname, int mode);
    }
}
#endif
