using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Data;
using Data.Net;
using Global.Serialization;
using LocalServer;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Global.LocalHosting
{
    /// <summary>설치된 서버 묶음 하나.</summary>
    public sealed class InstalledBundle
    {
        public string Version;

        /// <summary><c>local-server.jar</c> 와 <c>jre/</c> 가 있는 폴더. 로컬 서버의 작업 directory 다.</summary>
        public string Root;
    }

    /// <summary>
    /// lobby <c>GET /api/offline-pack</c> 응답을 들고, 정적 데이터 파일과 서버 묶음 설치를 맡는다.
    ///
    /// <list type="bullet">
    /// <item>정적 데이터는 작으므로 lobby 의 <c>staticVersion</c> 이 다르면 로비에 들어올 때 자동으로 받는다.</item>
    /// <item>서버 묶음(100MB 이상)은 설정 화면의 버튼으로만 받는다. zip 과 <c>.sha256</c> 을 받아 확인한 뒤
    /// <c>bundles/&lt;version&gt;/</c> 에 풀고, 새 묶음이 확인될 때까지 이전 묶음을 지우지 않는다.</item>
    /// </list>
    /// </summary>
    public class LocalServerPackStore : MonoBehaviour
    {
        private const int PackTimeoutSeconds = 10;
        private const string BundleJar = "local-server.jar";
        private const string BundleFolder = "local-server";

        /// <summary>마지막으로 받은 offline-pack. 아직 못 받았으면 null 이고, 그동안 로컬 경기는 하지 않는다.</summary>
        public OfflinePackResponse Latest { get; private set; }

        // Latest 를 준 lobby 주소. lobby 마다 정적 데이터와 로컬 경기 설정이 다르므로 주소가 바뀌면 버린다.
        private string latestSource;

        public bool Installing { get; private set; }

        /// <summary>0~1. 다운로드가 0.9 까지, 확인과 압축 풀기가 나머지다.</summary>
        public float InstallProgress { get; private set; }

        /// <summary>마지막 설치 실패 이유. 성공하거나 새로 시작하면 null.</summary>
        public string InstallError { get; private set; }

        /// <summary>offline-pack 이나 설치 상태가 바뀌었다.</summary>
        public event Action Changed;

        /// <summary>설치가 끝났다. 인자는 새 묶음이고, 실패하거나 취소되면 null.</summary>
        public event Action<InstalledBundle> InstallFinished;

        private bool cancelRequested;
        private UnityWebRequest activeRequest;

        // ─── offline-pack 과 정적 데이터 ──────────────────────────────────────

        /// <summary>
        /// lobby 에 offline-pack 을 묻는다. 가진 정적 데이터의 version 을 보내므로, 같으면 <c>staticData</c> 가
        /// null 로 오고, 다르면 새 내용을 <c>static.json</c> 에 저장한다. 실패하면 <see cref="Latest"/> 를 그대로 둔다.
        /// </summary>
        public IEnumerator RefreshOfflinePack()
        {
            StaticDataHeader downloaded = StaticDataFile.ReadHeader(LocalServerPaths.StaticDataPath);

            string source = ServerList.MatchingServer.url;
            if (latestSource != source)
            {
                Latest = null;
                latestSource = null;
            }

            ServerEndpoint endpoint = ServerList.MatchingServer.Api.Path("api", "offline-pack");
            if (downloaded.IsUsable) endpoint = endpoint.Query("staticVersion", downloaded.StaticVersion);

            using var request = UnityWebRequest.Get(endpoint);
            request.timeout = PackTimeoutSeconds;
            Server.SetAcceptLanguage(request);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                WDebug.LogWarning($"[LocalServer] offline-pack failed: {request.responseCode} / {request.error}");
                yield break;
            }

            string body = request.downloadHandler.text;
            if (!JsonCodec.TryDeserialize(body, out OfflinePackResponse pack, out string error) || pack == null)
            {
                WDebug.LogWarning($"[LocalServer] offline-pack parse error: {error} / {JsonCodec.Excerpt(body)}");
                yield break;
            }

            if (pack.staticData != null)
            {
                if (pack.schemaVersion == LocalHostingRules.SupportedSchemaVersion)
                {
                    SaveStaticData(pack.staticData.ToString(Formatting.None));
                }
                else
                {
                    WDebug.LogWarning($"[LocalServer] static data schema {pack.schemaVersion} is not supported");
                }

                // 수백 KB 를 메모리에 계속 들고 있을 이유가 없다.
                pack.staticData = null;
            }

            Latest = pack;
            latestSource = source;
            Changed?.Invoke();
        }

        /// <summary>
        /// 로컬 서버에 넘길 정적 데이터 파일을 고른다. 내려받은 것과 설치본에 든 것 중 lobby 가 요구하는
        /// version 과 같은 것을 먼저 쓰고, 그런 것이 없으면 형식만 맞는 것을 쓴다. 쓸 수 있는 파일이 없으면 null.
        /// </summary>
        public string SelectStaticDataPath(out StaticDataHeader header)
        {
            string wanted = Latest?.staticVersion;
            StaticDataHeader downloaded = StaticDataFile.ReadHeader(LocalServerPaths.StaticDataPath);
            StaticDataHeader bundled = StaticDataFile.ReadHeader(LocalServerPaths.BundledStaticDataPath);

            if (!string.IsNullOrEmpty(wanted))
            {
                if (downloaded.IsUsable && downloaded.StaticVersion == wanted)
                {
                    header = downloaded;
                    return LocalServerPaths.StaticDataPath;
                }

                if (bundled.IsUsable && bundled.StaticVersion == wanted)
                {
                    header = bundled;
                    return LocalServerPaths.BundledStaticDataPath;
                }
            }

            if (downloaded.IsUsable)
            {
                header = downloaded;
                return LocalServerPaths.StaticDataPath;
            }

            if (bundled.IsUsable)
            {
                header = bundled;
                return LocalServerPaths.BundledStaticDataPath;
            }

            header = default;
            return null;
        }

        private static void SaveStaticData(string json)
        {
            try
            {
                Directory.CreateDirectory(LocalServerPaths.Root);
                string temp = LocalServerPaths.StaticDataPath + ".tmp";
                File.WriteAllText(temp, json);
                if (File.Exists(LocalServerPaths.StaticDataPath)) File.Delete(LocalServerPaths.StaticDataPath);
                File.Move(temp, LocalServerPaths.StaticDataPath);
            }
            catch (Exception exception)
            {
                WDebug.LogWarning($"[LocalServer] could not save static data: {exception.Message}");
            }
        }

        // ─── 설치된 묶음 ─────────────────────────────────────────────────────

        /// <summary>
        /// <c>bundles/</c> 아래에서 가장 높은 version 의 온전한 묶음. 설치 중 임시 폴더(<c>.</c> 로 시작)는 보지 않는다.
        /// </summary>
        public InstalledBundle FindInstalledBundle()
        {
            if (!Directory.Exists(LocalServerPaths.BundlesDir)) return null;

            InstalledBundle best = null;
            foreach (string directory in Directory.GetDirectories(LocalServerPaths.BundlesDir))
            {
                string version = Path.GetFileName(directory);
                if (string.IsNullOrEmpty(version) || version.StartsWith(".")) continue;

                string root = ResolveBundleRoot(directory);
                if (root == null) continue;
                if (!File.Exists(Path.Combine(root, LocalServerPlatform.JavaRelativePath))) continue;

                if (best == null || LocalHostingRules.CompareVersions(version, best.Version) > 0)
                {
                    best = new InstalledBundle { Version = version, Root = root };
                }
            }

            return best;
        }

        /// <summary>lobby 가 알려 준 최신 version 이 설치된 것보다 높으면 true.</summary>
        public bool IsUpdateAvailable(InstalledBundle installed)
        {
            string latest = Latest?.localServer?.version;
            if (string.IsNullOrEmpty(latest)) return false;
            if (string.IsNullOrEmpty(LocalServerPlatform.PlatformKey)) return false;
            if (string.IsNullOrEmpty(Latest.localServer.AssetUrl(LocalServerPlatform.PlatformKey))) return false;
            return installed == null || LocalHostingRules.CompareVersions(latest, installed.Version) > 0;
        }

        /// <summary>
        /// <paramref name="keepVersion"/> 이외의 묶음을 지운다. 로컬 서버가 이전 묶음으로 돌고 있으면 Windows 에서는
        /// 파일이 잠겨 지워지지 않으므로, 서버를 띄우기 전에 부른다. 못 지운 폴더는 다음에 다시 시도한다.
        /// </summary>
        public void DeleteOtherBundles(string keepVersion)
        {
            if (!Directory.Exists(LocalServerPaths.BundlesDir)) return;

            foreach (string directory in Directory.GetDirectories(LocalServerPaths.BundlesDir))
            {
                string version = Path.GetFileName(directory);
                if (version == keepVersion) continue;

                try
                {
                    Directory.Delete(directory, true);
                    WDebug.Log($"[LocalServer] deleted old bundle {version}");
                }
                catch (Exception exception)
                {
                    WDebug.LogWarning($"[LocalServer] could not delete bundle {version}: {exception.Message}");
                }
            }
        }

        private static string ResolveBundleRoot(string directory)
        {
            if (File.Exists(Path.Combine(directory, BundleJar))) return directory;

            string nested = Path.Combine(directory, BundleFolder);
            return File.Exists(Path.Combine(nested, BundleJar)) ? nested : null;
        }

        // ─── 설치 ───────────────────────────────────────────────────────────

        /// <summary>설정 화면의 설치·업데이트 버튼. 이미 설치 중이면 무시한다.</summary>
        public void BeginInstall()
        {
            if (Installing) return;
            StartCoroutine(Install());
        }

        /// <summary>진행 중인 다운로드를 멈춘다. 확인이나 압축 풀기 중이면 그 단계가 끝나는 대로 버린다.</summary>
        public void CancelInstall()
        {
            if (!Installing) return;
            cancelRequested = true;
            activeRequest?.Abort();
        }

        private IEnumerator Install()
        {
            Installing = true;
            cancelRequested = false;
            InstallProgress = 0f;
            InstallError = null;
            Changed?.Invoke();

            InstalledBundle installed = null;
            string zipPath = null;
            string stagingPath = null;
            string failure = null;

            LocalServerRelease release = Latest?.localServer;
            string platform = LocalServerPlatform.PlatformKey;
            string assetUrl = release?.AssetUrl(platform);
            string version = release?.version;

            if (string.IsNullOrEmpty(assetUrl) || string.IsNullOrEmpty(version))
            {
                failure = "no bundle for this platform";
            }
            else if (!IsSafeFolderName(version))
            {
                failure = $"invalid version {version}";
            }

            // 1. zip
            if (failure == null)
            {
                Directory.CreateDirectory(LocalServerPaths.DownloadsDir);
                zipPath = Path.Combine(LocalServerPaths.DownloadsDir, $"local-server-{platform}-{version}.zip");

                using var download = new UnityWebRequest(assetUrl, UnityWebRequest.kHttpVerbGET)
                {
                    downloadHandler = new DownloadHandlerFile(zipPath) { removeFileOnAbort = true }
                };
                activeRequest = download;
                UnityWebRequestAsyncOperation operation = download.SendWebRequest();
                while (!operation.isDone)
                {
                    InstallProgress = download.downloadProgress * 0.9f;
                    yield return null;
                }

                activeRequest = null;
                if (cancelRequested) failure = "canceled";
                else if (download.result != UnityWebRequest.Result.Success)
                    failure = $"download failed: {download.responseCode} {download.error}";
            }

            // 2. sha256
            string expectedHash = null;
            if (failure == null)
            {
                using var hashRequest = UnityWebRequest.Get(assetUrl + ".sha256");
                hashRequest.timeout = PackTimeoutSeconds;
                activeRequest = hashRequest;
                yield return hashRequest.SendWebRequest();
                activeRequest = null;

                if (cancelRequested) failure = "canceled";
                else if (hashRequest.result != UnityWebRequest.Result.Success)
                    failure = $"checksum download failed: {hashRequest.responseCode} {hashRequest.error}";
                else
                {
                    expectedHash = LocalHostingRules.ParseSha256File(hashRequest.downloadHandler.text);
                    if (expectedHash == null) failure = "checksum file is malformed";
                }
            }

            // 3. 확인
            if (failure == null)
            {
                InstallProgress = 0.9f;
                Changed?.Invoke();
                string path = zipPath;
                Task<string> hashTask = Task.Run(() => ComputeSha256(path));
                while (!hashTask.IsCompleted) yield return null;

                if (cancelRequested) failure = "canceled";
                else if (hashTask.IsFaulted) failure = $"checksum failed: {hashTask.Exception?.GetBaseException().Message}";
                else if (!string.Equals(hashTask.Result, expectedHash, StringComparison.OrdinalIgnoreCase))
                    failure = "checksum mismatch";
            }

            // 4. 압축 풀기. 임시 폴더에 풀고 확인한 뒤에 이름을 바꾸므로, 실패해도 이전 묶음은 그대로다.
            if (failure == null)
            {
                InstallProgress = 0.95f;
                Changed?.Invoke();
                stagingPath = Path.Combine(LocalServerPaths.BundlesDir, "." + version + "-staging");
                string zip = zipPath;
                string staging = stagingPath;
                Task extractTask = Task.Run(() => Extract(zip, staging));
                while (!extractTask.IsCompleted) yield return null;

                if (cancelRequested) failure = "canceled";
                else if (extractTask.IsFaulted) failure = $"unzip failed: {extractTask.Exception?.GetBaseException().Message}";
            }

            if (failure == null)
            {
                string stagedRoot = ResolveBundleRoot(stagingPath);
                if (stagedRoot == null)
                {
                    failure = "bundle has no local-server.jar";
                }
                else
                {
                    string chmodFailure = null;
                    if (!LocalServerPlatform.IsWindows)
                    {
                        string root = stagedRoot;
                        Task<string> chmodTask = Task.Run(() => MakeExecutable(root));
                        while (!chmodTask.IsCompleted) yield return null;
                        chmodFailure = chmodTask.IsFaulted
                            ? chmodTask.Exception?.GetBaseException().Message
                            : chmodTask.Result;
                    }

                    if (chmodFailure != null)
                    {
                        failure = $"chmod failed: {chmodFailure}";
                    }
                    else if (!File.Exists(Path.Combine(stagedRoot, LocalServerPlatform.JavaRelativePath)))
                    {
                        failure = "bundle has no java for this platform";
                    }
                    else
                    {
                        failure = PromoteStaging(stagingPath, version);
                        if (failure == null)
                        {
                            string target = Path.Combine(LocalServerPaths.BundlesDir, version);
                            installed = new InstalledBundle { Version = version, Root = ResolveBundleRoot(target) };
                            stagingPath = null;
                        }
                    }
                }
            }

            TryDeleteFile(zipPath);
            TryDeleteDirectory(stagingPath);

            Installing = false;
            InstallProgress = installed != null ? 1f : 0f;
            InstallError = failure == "canceled" ? null : failure;
            if (failure != null) WDebug.LogWarning($"[LocalServer] install {version}: {failure}");
            else WDebug.Log($"[LocalServer] installed {version} at {installed.Root}");

            Changed?.Invoke();
            InstallFinished?.Invoke(installed);
        }

        private static string PromoteStaging(string stagingPath, string version)
        {
            string target = Path.Combine(LocalServerPaths.BundlesDir, version);
            try
            {
                // 같은 version 을 다시 설치하는 경우다. 그 묶음으로 서버가 돌고 있으면 Windows 에서 지우지 못하고 실패한다.
                if (Directory.Exists(target)) Directory.Delete(target, true);
                Directory.Move(stagingPath, target);
                return null;
            }
            catch (Exception exception)
            {
                return $"could not move bundle into place: {exception.Message}";
            }
        }

        private static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return LocalHostingRules.ToHex(sha.ComputeHash(stream));
        }

        /// <summary>
        /// 항목마다 경로를 확인하며 푼다. <c>../</c> 가 든 항목이 대상 폴더 밖에 파일을 쓰지 못하게 한다.
        /// </summary>
        private static void Extract(string zipPath, string destination)
        {
            if (Directory.Exists(destination)) Directory.Delete(destination, true);
            Directory.CreateDirectory(destination);

            string root = Path.GetFullPath(destination) + Path.DirectorySeparatorChar;
            using ZipArchive archive = ZipFile.OpenRead(zipPath);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string target = Path.GetFullPath(Path.Combine(destination, entry.FullName));
                if (!target.StartsWith(root, StringComparison.Ordinal))
                    throw new IOException($"zip entry escapes the bundle folder: {entry.FullName}");

                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(target);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(target));
                entry.ExtractToFile(target, true);
            }
        }

        /// <summary>
        /// macOS 와 Linux 에서는 zip 이 실행 권한을 싣고 와도 Mono 의 압축 풀기가 그것을 버린다. <c>jre/bin</c> 과
        /// <c>jre/lib/jspawnhelper</c> 에 실행 권한을 다시 준다. 실패하면 이유를 돌려준다.
        /// </summary>
        private static string MakeExecutable(string bundleRoot)
        {
            string bin = Path.Combine(bundleRoot, "jre", "bin");
            string spawnHelper = Path.Combine(bundleRoot, "jre", "lib", "jspawnhelper");
            string arguments = $"-R u+x \"{bin}\"";
            if (File.Exists(spawnHelper)) arguments += $" \"{spawnHelper}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = "/bin/chmod",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process chmod = Process.Start(startInfo);
            if (chmod == null) return "could not start /bin/chmod";
            if (!chmod.WaitForExit(10000)) return "/bin/chmod timed out";
            return chmod.ExitCode == 0 ? null : $"/bin/chmod exited {chmod.ExitCode}";
        }

        private static bool IsSafeFolderName(string version)
        {
            if (version.StartsWith(".")) return false;
            return version.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
                   version.IndexOf('/') < 0 && version.IndexOf('\\') < 0;
        }

        private static void TryDeleteFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception)
            {
                // 다음 설치 때 덮어쓴다.
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
            catch (Exception)
            {
                // 다음 설치 때 지운다.
            }
        }
    }
}
