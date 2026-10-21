using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Global.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Global
{
    /// <summary>Optional full-build updates. The existing installation is changed only by the external helper.</summary>
    public sealed class DesktopUpdateDialog : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private UnityEngine.UI.Button confirmButton;
        [SerializeField] private UnityEngine.UI.Button cancelButton;
        private int choice;
        private static bool checkedThisRun;
        private bool korean;

        private void Awake()
        {
            root.SetActive(false);
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            transform.localScale = Vector3.one;
            messageText.richText = false;
            foreach (var localizer in GetComponentsInChildren<LocalizeStringEvent>(true))
                localizer.enabled = false;
            confirmButton.onClick.RemoveAllListeners();
            cancelButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() => choice = 1);
            cancelButton.onClick.AddListener(() => choice = -1);
        }

        public IEnumerator CheckForUpdate()
        {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR
            if (checkedThisRun) yield break;
            checkedThisRun = true;
            korean = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale?.Identifier.Code.StartsWith("ko")
                     ?? Application.systemLanguage == SystemLanguage.Korean;
            string configPath = Path.Combine(Application.streamingAssetsPath, "desktop-update.json");
            if (!File.Exists(configPath)) yield break;
            UpdateConfig config = null;
            string error = Try(() =>
            {
                config = JsonCodec.Deserialize<UpdateConfig>(File.ReadAllText(configPath));
                Require(config != null && IsHttps(config.manifestUrl), "Invalid update configuration");
                Require(config.platform == Platform(), "Update platform mismatch");
            });
            if (error != null) yield break;

            // Lock the ordinary login/navigation controls while the modal owns startup.
            var controls = FindObjectsOfType<Selectable>()
                .Where(control => !control.transform.IsChildOf(transform))
                .Select(control => (control, control.interactable)).ToArray();
            foreach (var item in controls) item.control.interactable = false;
            try
            {
                yield return Run(config);
            }
            finally
            {
                root.SetActive(false);
                foreach (var item in controls)
                    if (item.control != null) item.control.interactable = item.interactable;
            }
#else
            yield break;
#endif
        }

#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR
        private IEnumerator Run(UpdateConfig config)
        {
            Show(Text("업데이트 확인 중…", "Checking for updates…"), null, Text("건너뛰기", "Skip"));
            UpdateManifest manifest = null;
            using (var request = UnityWebRequest.Get(config.manifestUrl))
            {
                request.timeout = 15;
                var operation = request.SendWebRequest();
                while (!operation.isDone && choice == 0)
                {
                    if (request.downloadedBytes > 64 * 1024) { request.Abort(); yield break; }
                    yield return null;
                }
                if (choice != 0) { request.Abort(); yield break; }
                if (request.result != UnityWebRequest.Result.Success) yield break;
                // Do not show remote parse details or raw URLs in the login UI.
                if (Try(() =>
                {
                    Require(IsHttps(request.url), "Insecure redirect");
                    Require(request.downloadedBytes <= 64 * 1024, "Manifest too large");
                    manifest = JsonCodec.Deserialize<UpdateManifest>(request.downloadHandler.text);
                    Require(manifest != null && manifest.schemaVersion == 1 && manifest.platform == config.platform,
                        "Invalid manifest");
                    Require(IsHttps(manifest.url) && Regex.IsMatch(manifest.sha256 ?? "", "^[0-9a-fA-F]{64}$")
                        && manifest.size > 0 && manifest.size <= 32L * 1024 * 1024 * 1024, "Invalid archive");
                    ParseVersion(manifest.version);
                    ParseVersion(Application.version);
                }) != null) yield break;
            }
            if (ParseVersion(manifest.version) <= ParseVersion(Application.version)) yield break;

            string installRoot = Directory.GetParent(Application.dataPath).FullName;
            string helperName = Application.platform == RuntimePlatform.WindowsPlayer ? "ArcaneUpdater.exe" : "ArcaneUpdater";
            string helperSource = Path.Combine(Application.streamingAssetsPath, "DesktopUpdater");
            InstallInfo install = null;
            string setupError = Try(() =>
            {
                string expectedFolder = config.platform.StartsWith("osx-") ? "ArcaneCasters.app" : "ArcaneCasters";
                Require(Path.GetFileName(installRoot).Equals(expectedFolder, StringComparison.OrdinalIgnoreCase),
                    "Not a dedicated game directory");
                string marker = config.platform.StartsWith("osx-") ? "Contents/Resources/arcane-install.json" : "arcane-install.json";
                install = JsonCodec.Deserialize<InstallInfo>(File.ReadAllText(Path.Combine(installRoot, marker)));
                Require(install != null && install.schemaVersion == 1 && install.appId == "arcane-casters"
                    && install.platform == config.platform && install.version == Application.version, "Invalid installation");
                Require(File.Exists(Path.Combine(helperSource, helperName)), "Updater missing");
                string probe = installRoot + ".write-probe-" + Guid.NewGuid().ToString("N");
                using (File.Create(probe)) { }
                File.Delete(probe);
            });
            if (setupError != null)
            {
                yield return Error(Text("이 설치본은 자동 업데이트를 지원하지 않습니다.", "This installation does not support automatic updates."));
                yield break;
            }
            Show(Text($"새 버전 {manifest.version} ({manifest.size / 1048576f:F0} MB)을 다운로드할까요?\n완료 후 게임이 재시작됩니다.",
                $"Download version {manifest.version} ({manifest.size / 1048576f:F0} MB)?\nThe game will restart after installation."),
                Text("업데이트", "Update"), Text("나중에", "Later"));
            while (choice == 0) yield return null;
            if (choice < 0) yield break;

            string work = Path.Combine(Application.persistentDataPath, "DesktopUpdates", Guid.NewGuid().ToString("N"));
            string archive = Path.Combine(work, "update.zip");
            if (Try(() => Directory.CreateDirectory(work)) != null)
            {
                yield return Error(Text("다운로드 폴더를 만들 수 없습니다.", "Cannot create the download folder."));
                yield break;
            }
            Show(Text("다운로드 준비 중…", "Preparing download…"), null, Text("취소", "Cancel"));
            UnityWebRequest download = null;
            string downloadError = Try(() =>
            {
                download = UnityWebRequest.Get(manifest.url);
                download.downloadHandler = new DownloadHandlerFile(archive) { removeFileOnAbort = true };
                download.timeout = 3600;
            });
            if (downloadError != null) download?.Dispose();
            if (downloadError == null)
            {
                using (download)
                {
                    var operation = download.SendWebRequest();
                    ulong lastBytes = 0;
                    float lastProgress = Time.realtimeSinceStartup;
                    while (!operation.isDone && choice == 0)
                    {
                        if (download.downloadedBytes != lastBytes)
                        {
                            lastBytes = download.downloadedBytes;
                            lastProgress = Time.realtimeSinceStartup;
                        }
                        if (lastBytes > (ulong)manifest.size || Time.realtimeSinceStartup - lastProgress > 60)
                        {
                            downloadError = "Download size/timeout";
                            download.Abort();
                            break;
                        }
                        float percent = Math.Min(100f, 100f * download.downloadedBytes / manifest.size);
                        messageText.text = Text($"전체 빌드 다운로드 중… {percent:F0}%", $"Downloading full build… {percent:F0}%");
                        yield return null;
                    }
                    if (choice != 0) download.Abort();
                    if (download.result != UnityWebRequest.Result.Success || !IsHttps(download.url))
                        downloadError = "Download failed";
                }
            }
            if (choice != 0 || downloadError != null)
            {
                Try(() => File.Delete(archive));
                if (choice == 0) yield return Error(Text("다운로드에 실패했습니다. 다음 실행 때 다시 시도할 수 있습니다.",
                    "Download failed. You can try again next time."));
                yield break;
            }
            Show(Text("다운로드 파일 확인 중…", "Verifying download…"), null, null);
            var verification = Task.Run(() => Try(() =>
            {
                using (var stream = File.OpenRead(archive))
                using (var sha = SHA256.Create())
                {
                    Require(stream.Length == manifest.size, "Size mismatch");
                    string hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
                    Require(hash.Equals(manifest.sha256, StringComparison.OrdinalIgnoreCase), "Checksum mismatch");
                }
            }));
            while (!verification.IsCompleted) yield return null;
            if (verification.Result != null)
            {
                Try(() => File.Delete(archive));
                yield return Error(Text("다운로드 파일 검증에 실패했습니다.", "Downloaded file verification failed."));
                yield break;
            }

            System.Diagnostics.Process helper = null;
            string launchError = Try(() =>
            {
                string helperDir = Path.Combine(work, "helper");
                CopyDirectory(helperSource, helperDir);
                string helperPath = Path.Combine(helperDir, helperName);
#if !UNITY_STANDALONE_WIN
                // File.Copy does not promise to preserve executable mode under Unity's Mono runtime.
                using (var chmod = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("/bin/chmod",
                    "+x " + Quote(helperPath)) { UseShellExecute = false, CreateNoWindow = true }))
                    Require(chmod != null && chmod.WaitForExit(5000) && chmod.ExitCode == 0, "Cannot enable updater");
#endif
                using (var current = System.Diagnostics.Process.GetCurrentProcess())
                {
                    var job = new UpdateJob {
                        installRoot = installRoot, archive = archive, sha256 = manifest.sha256, size = manifest.size,
                        platform = manifest.platform, version = manifest.version, currentVersion = Application.version,
                        executable = install.executable, parentPid = current.Id,
                        parentStartTicks = current.StartTime.ToUniversalTime().Ticks
                    };
                    string jobPath = Path.Combine(work, "job.json");
                    File.WriteAllText(jobPath, JsonCodec.Serialize(job));
                    helper = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(helperPath, Quote(jobPath)) {
                        UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = work
                    });
                    Require(helper != null, "Cannot start updater");
                }
            });
            if (launchError != null)
            {
                yield return Error(Text("업데이트 프로그램을 시작할 수 없습니다.", "Cannot start the updater."));
                yield break;
            }
            using (helper)
            {
                Show(Text("설치 준비 중… 잠시 기다려 주세요.", "Preparing installation… Please wait."), null, null);
                float started = Time.realtimeSinceStartup;
                string ready = Path.Combine(work, "ready");
                while (!File.Exists(ready) && !helper.HasExited && Time.realtimeSinceStartup - started < 600)
                    yield return null;
                if (!File.Exists(ready) || helper.HasExited)
                {
                    Try(() => File.WriteAllText(Path.Combine(work, "abort"), ""));
                    yield return Error(Text("설치를 준비하지 못했습니다. 기존 버전으로 계속합니다.",
                        "Could not prepare installation. Continuing with the current version."));
                    yield break;
                }
                if (Try(() => File.WriteAllText(Path.Combine(work, "commit"), "")) != null)
                {
                    Try(() => File.WriteAllText(Path.Combine(work, "abort"), ""));
                    yield return Error(Text("업데이트를 시작하지 못했습니다.", "Could not start installation."));
                    yield break;
                }
                messageText.text = Text("업데이트를 위해 게임을 재시작합니다…", "Restarting to install the update…");
                Application.Quit();
                // If another shutdown handler vetoes quit, never unlock login while a helper is pending.
                while (!helper.HasExited) yield return null;
                yield return Error(Text("게임 종료가 취소되어 업데이트하지 않았습니다.", "The game did not exit; the update was cancelled."));
            }
        }

        private static string Platform()
        {
#if UNITY_STANDALONE_WIN
            return "win-x64";
#elif UNITY_STANDALONE_LINUX
            return "linux-x64";
#else
            return System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture ==
                System.Runtime.InteropServices.Architecture.Arm64 ? "osx-arm64" : "osx-x64";
#endif
        }

        private static bool IsHttps(string value) =>
            Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            && string.IsNullOrEmpty(uri.UserInfo);

        private static Version ParseVersion(string value)
        {
            Require(Regex.IsMatch(value ?? "", @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$")
                && Version.TryParse(value, out _), "Expected X.Y.Z");
            return Version.Parse(value);
        }

        private static string Quote(string value)
        {
            Require(!value.Contains("\"") && !value.Contains("\n") && !value.Contains("\r"), "Unsupported path");
            return "\"" + value + "\"";
        }

        private static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (string file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
            foreach (string child in Directory.GetDirectories(source)) CopyDirectory(child, Path.Combine(target, Path.GetFileName(child)));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException(message);
        }

        private static string Try(Action action)
        {
            try { action(); return null; }
            catch (Exception exception) { return exception.GetType().Name; }
        }
#endif
        private string Text(string ko, string en) => korean ? ko : en;

        private void Show(string message, string confirm, string cancel)
        {
            choice = 0;
            messageText.text = message;
            SetButton(confirmButton, confirm);
            SetButton(cancelButton, cancel);
            root.SetActive(true);
        }

        private static void SetButton(UnityEngine.UI.Button button, string label)
        {
            button.gameObject.SetActive(label != null);
            button.interactable = label != null;
            if (label != null) button.GetComponentInChildren<TMP_Text>(true).text = label;
        }

        private IEnumerator Error(string message)
        {
            Show(message, Text("계속", "Continue"), null);
            while (choice == 0) yield return null;
        }

        [Serializable, UnityEngine.Scripting.Preserve]
        private sealed class UpdateConfig { public string manifestUrl; public string platform; }
        [Serializable, UnityEngine.Scripting.Preserve]
        private sealed class UpdateManifest
        {
            public int schemaVersion;
            public string version, platform, url, sha256;
            public long size;
        }
        [Serializable, UnityEngine.Scripting.Preserve]
        private sealed class InstallInfo
        {
            public int schemaVersion;
            public string appId, platform, version, executable;
        }
        [Serializable, UnityEngine.Scripting.Preserve]
        private sealed class UpdateJob
        {
            public string installRoot, archive, sha256, platform, version, currentVersion, executable;
            public long size, parentStartTicks;
            public int parentPid;
        }
    }
}
