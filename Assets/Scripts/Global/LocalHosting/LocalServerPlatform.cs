using System.IO;
using LocalServer;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Global.LocalHosting
{
    /// <summary>
    /// 로컬 서버를 돌릴 수 있는 platform 과, 그 platform 의 서버 묶음 이름과 저장 위치.
    ///
    /// desktop player(Windows, macOS, Linux)와 Editor 에서만 켜진다. WebGL 과 Android build 에서는
    /// <see cref="IsSupported"/> 가 컴파일 단계에서 false 라 로컬 서버 코드가 한 줄도 돌지 않는다.
    /// </summary>
    public static class LocalServerPlatform
    {
        public static bool IsSupported
        {
            get
            {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
                return PlatformKey != null;
#else
                return false;
#endif
            }
        }

        public static bool IsWindows =>
            Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.WindowsEditor;

        /// <summary>
        /// lobby 가 내려 주는 <c>localServer.assets</c> 의 key. 묶음이 없는 조합(Windows arm64 등)이면 null.
        /// </summary>
        public static string PlatformKey
        {
            get
            {
                Architecture architecture = RuntimeInformation.OSArchitecture;
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.WindowsEditor:
                        return architecture == Architecture.X64 ? "windows-x64" : null;
                    case RuntimePlatform.OSXPlayer:
                    case RuntimePlatform.OSXEditor:
                        return architecture == Architecture.Arm64 ? "macos-arm64" : "macos-x64";
                    case RuntimePlatform.LinuxPlayer:
                    case RuntimePlatform.LinuxEditor:
                        return architecture == Architecture.X64 ? "linux-x64" : null;
                    default:
                        return null;
                }
            }
        }

        /// <summary>묶음 root 기준 Rust 서버 실행 파일 이름.</summary>
        public static string ExecutableName => LocalServerBundle.ExecutableName(IsWindows);
    }

    /// <summary>
    /// <c>Application.persistentDataPath/local-server/</c> 아래 저장 위치(계약 문서 5번).
    /// <c>persistentDataPath</c> 는 main thread 에서만 읽을 수 있어 <see cref="Initialize"/> 에서 한 번 읽어 둔다.
    /// </summary>
    public static class LocalServerPaths
    {
        public static string Root { get; private set; }

        /// <summary>Rust 묶음. Java와 version 번호가 겹쳐도 서로 덮어쓰지 않는다.</summary>
        public static string BundlesDir => Path.Combine(Root, "bundles-rust");

        /// <summary>lobby 에서 내려받은 정적 데이터.</summary>
        public static string StaticDataPath => Path.Combine(Root, "static.json");

        /// <summary>lobby 에 보고하지 못한 경기 결과.</summary>
        public static string PendingResultsDir => Path.Combine(Root, "pending-results");

        /// <summary>로컬 서버 <c>init</c> 의 <c>dataDir</c>. 서버는 log 를 이 아래 <c>logs/</c> 에 쓴다.</summary>
        public static string DataDir => Path.Combine(Root, "data");

        public static string LogsDir => Path.Combine(DataDir, "logs");

        /// <summary>내려받는 중인 zip.</summary>
        public static string DownloadsDir => Path.Combine(Root, "downloads");

        /// <summary>설치본에 들어 있는 정적 데이터. release workflow 가 build 전에 넣는다.</summary>
        public static string BundledStaticDataPath { get; private set; }

        public static void Initialize()
        {
            Root = Path.Combine(Application.persistentDataPath, "local-server");
            BundledStaticDataPath = Path.Combine(Application.streamingAssetsPath, "Offline", "static.json");
        }
    }
}
