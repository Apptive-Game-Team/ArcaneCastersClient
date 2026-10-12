using System;
using System.Collections;
using LocalServer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Global.LocalHosting
{
    /// <summary>
    /// desktop client 의 로컬 game server 기능을 묶는다. 앱이 켜질 때 스스로를 만들고 씬이 바뀌어도 남는다.
    ///
    /// <list type="bullet">
    /// <item>로비에 들어오면 offline-pack 을 받고(정적 데이터 자동 갱신), 보고하지 못한 결과를 다시 보내고, 설치돼 있고
    /// 설정이 켜져 있으면 로컬 서버를 background 로 미리 띄운다. 기동 시간(5~10초)을 경기 시작 전에 숨기기 위해서다.</item>
    /// <item>봇전과 모험 시작 앞의 로컬 경로(<see cref="LocalMatchRoute"/>)를 <see cref="LocalMatchRouter"/> 로 채운다.</item>
    /// <item>로컬 서버의 <c>matchStats</c> 로 다음 경기 tick rate 를 정하고, <c>matchResult</c> 를 lobby 에 보고한다.</item>
    /// </list>
    ///
    /// WebGL 과 Android 에서는 만들어지지 않는다(<see cref="LocalServerPlatform.IsSupported"/>).
    /// </summary>
    public class LocalServerService : SingletonObject<LocalServerService>
    {
        private const string LobbySceneName = "LobbyScene";

        public LocalServerHost Host { get; private set; }
        public LocalServerPackStore PackStore { get; private set; }
        public LocalMatchResultReporter Reporter { get; private set; }

        /// <summary>설정 화면이 다시 그려야 할 때. 서버 상태, 설치 상태, 설정 중 하나가 바뀌었다.</summary>
        public event Action Changed;

        private bool lobbyEntryRunning;
        // 진행 중에 로비에 다시 들어왔다. 서버 선택 dropdown 은 로비를 다시 불러 lobby 주소를 바꾸므로,
        // 첫 진입의 요청이 이전 주소로 가 있는 동안 들어온 진입을 버리면 바뀐 주소로는 묻지 못한다.
        private bool lobbyEntryPending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstance()
        {
            if (!LocalServerPlatform.IsSupported) return;
            if (Instance != null) return;
            new GameObject(nameof(LocalServerService)).AddComponent<LocalServerService>();
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return; // 중복 생성돼 파괴될 인스턴스다.

            LocalServerPaths.Initialize();

            Host = gameObject.AddComponent<LocalServerHost>();
            PackStore = gameObject.AddComponent<LocalServerPackStore>();
            Reporter = gameObject.AddComponent<LocalMatchResultReporter>();

            Host.StateChanged += RaiseChanged;
            Host.MatchStatsReceived += HandleMatchStats;
            Host.MatchResultReceived += Reporter.Report;
            PackStore.Changed += RaiseChanged;
            PackStore.InstallFinished += HandleInstallFinished;
            LocalServerSettings.Changed += HandleSettingsChanged;
            SceneManager.sceneLoaded += HandleSceneLoaded;

            LocalMatchRoute.Starter = (type, scenarioId, callback) => LocalMatchRouter.TryStart(this, type, scenarioId, callback);

            Host.SetInstalled(PackStore.FindInstalledBundle() != null);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            LocalMatchRoute.Starter = null;
            LocalServerSettings.Changed -= HandleSettingsChanged;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        /// <summary>설치된 묶음. 없으면 null.</summary>
        public InstalledBundle Installed => PackStore.FindInstalledBundle();

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != LobbySceneName) return;
            if (lobbyEntryRunning)
            {
                lobbyEntryPending = true;
                return;
            }
            StartCoroutine(OnLobbyEntered());
        }

        private IEnumerator OnLobbyEntered()
        {
            lobbyEntryRunning = true;
            do
            {
                lobbyEntryPending = false;
                yield return PackStore.RefreshOfflinePack();
                yield return Reporter.RetryPending();
            } while (lobbyEntryPending);
            lobbyEntryRunning = false;

            EnsureServer();
        }

        /// <summary>
        /// 설정, 설치된 묶음, 정적 데이터에 맞게 로컬 서버를 띄우거나 멈추거나 다시 띄운다.
        /// </summary>
        public void EnsureServer()
        {
            InstalledBundle installed = PackStore.FindInstalledBundle();

            if (!LocalServerSettings.Enabled || installed == null)
            {
                if (Host.IsRunning) Host.StopServer();
                Host.SetInstalled(installed != null);
                return;
            }

            string staticPath = PackStore.SelectStaticDataPath(out StaticDataHeader header);

            if (Host.IsRunning)
            {
                bool bundleChanged = Host.BundleVersion != installed.Version;

                // 기동할 때 넣은 정적 데이터가 lobby 가 지금 요구하는 것과 다르고, 맞는 파일이 생겼으면 다시 띄운다.
                string wanted = PackStore.Latest?.staticVersion;
                bool staticChanged = Host.State == LocalServerState.Ready &&
                                     !string.IsNullOrEmpty(wanted) &&
                                     header.StaticVersion == wanted &&
                                     !string.Equals(Host.StaticVersion, wanted, StringComparison.OrdinalIgnoreCase);

                if (!bundleChanged && !staticChanged) return;

                WDebug.Log($"[LocalServer] restarting (bundle changed: {bundleChanged}, static changed: {staticChanged})");
                Host.StopServer();
            }

            if (staticPath == null)
            {
                WDebug.LogWarning("[LocalServer] no usable static data, not starting");
                Host.SetInstalled(true);
                return;
            }

            if (!PackStore.Installing) PackStore.DeleteOtherBundles(installed.Version);
            Host.StartServer(installed.Root, installed.Version, staticPath);
        }

        private void HandleInstallFinished(InstalledBundle installed)
        {
            if (installed == null)
            {
                RaiseChanged();
                return;
            }

            EnsureServer();
            RaiseChanged();
        }

        private void HandleSettingsChanged()
        {
            EnsureServer();
            RaiseChanged();
        }

        private void HandleMatchStats(LocalServerMessage stats)
        {
            WDebug.Log($"[LocalServer] matchStats {stats.sessionId}: target {stats.targetTickRate}, mean {stats.meanFps:F1}, p10 {stats.p10Fps:F1}");
            LocalServerSettings.ApplyMatchStats(stats.targetTickRate, stats.p10Fps);
        }

        private void RaiseChanged()
        {
            Changed?.Invoke();
        }
    }
}
