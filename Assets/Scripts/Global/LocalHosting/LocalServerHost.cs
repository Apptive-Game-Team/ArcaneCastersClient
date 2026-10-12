using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Global.Serialization;
using LocalServer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Global.LocalHosting
{
    public enum LocalServerState
    {
        /// <summary>설치된 서버 묶음이 없다.</summary>
        NotInstalled,

        /// <summary>설치돼 있지만 돌고 있지 않다(설정이 꺼졌거나 아직 띄우지 않음).</summary>
        Stopped,

        /// <summary>프로세스를 띄웠고 <c>ready</c> 를 기다린다.</summary>
        Starting,

        /// <summary><c>ready</c> 를 받았다. 경기를 만들 수 있다.</summary>
        Ready,

        /// <summary>기동에 실패했거나 도중에 끝났다. 이유는 <see cref="LocalServerHost.FailureReason"/>.</summary>
        Failed
    }

    /// <summary>
    /// 로컬 game server 프로세스 하나를 띄우고, stdin/stdout 한 줄 JSON 프로토콜(계약 문서 4번)로 다룬다.
    ///
    /// <list type="bullet">
    /// <item>stdin 첫 줄은 <c>init</c>(무작위 token, dataDir, staticDataPath)다. token 을 실행 인자로 넘기지 않으므로
    /// 같은 PC 의 다른 사용자가 프로세스 목록에서 볼 수 없다.</item>
    /// <item>stdout 은 background thread 가 읽어 queue 에 넣고, <see cref="Update"/> 가 main thread 에서 처리한다.</item>
    /// <item>stderr 는 읽어서 <c>dataDir/logs/client-stderr.log</c> 에 쓴다. 읽지 않으면 pipe buffer 가 차서 서버가 멈춘다.</item>
    /// <item>30초 안에 <c>ready</c> 가 없거나 <c>error</c> 가 오면 <see cref="LocalServerState.Failed"/> 로 두고, 경기는 공유
    /// 서버에서 한다.</item>
    /// <item>앱이 끝날 때 <c>shutdown</c> 을 보내고 stdin 을 닫는다. client 가 죽어도 OS 가 pipe 를 닫으므로 서버는
    /// stdin EOF 로 함께 끝난다.</item>
    /// </list>
    /// </summary>
    public class LocalServerHost : MonoBehaviour
    {
        public const float ReadyTimeoutSeconds = 30f;
        private const int TokenBytes = 32;
        private const int StopGraceMilliseconds = 5000;

        private readonly struct OutputLine
        {
            public readonly int Generation;
            public readonly string Text;
            public readonly bool Exited;
            public readonly int ExitCode;

            public OutputLine(int generation, string text, bool exited, int exitCode)
            {
                Generation = generation;
                Text = text;
                Exited = exited;
                ExitCode = exitCode;
            }
        }

        private readonly ConcurrentQueue<OutputLine> output = new ConcurrentQueue<OutputLine>();

        private Process process;
        private StreamWriter stdin;
        private int generation;
        private float startedAt;

        public LocalServerState State { get; private set; } = LocalServerState.NotInstalled;
        public string FailureReason { get; private set; }

        /// <summary>지금 띄운 묶음의 version(폴더 이름). 돌고 있지 않으면 null.</summary>
        public string BundleVersion { get; private set; }

        /// <summary><c>ready</c> 가 알린 값들. <see cref="LocalServerState.Ready"/> 일 때만 의미가 있다.</summary>
        public int Port { get; private set; }
        public string ServerVersion { get; private set; }
        public int SchemaVersion { get; private set; }
        public string StaticVersion { get; private set; }

        /// <summary><c>init</c> 으로 넘긴 token. 로컬 서버 API 와 경기 WebSocket 인증에 쓴다.</summary>
        public string Token { get; private set; }

        public bool IsRunning => State == LocalServerState.Starting || State == LocalServerState.Ready;

        public event Action StateChanged;

        /// <summary>경기가 끝날 때 오는 <c>matchStats</c>.</summary>
        public event Action<LocalServerMessage> MatchStatsReceived;

        /// <summary>경기가 끝날 때 오는 <c>matchResult</c>. 두 번째 인자는 <c>sessionId</c> 를 뺀 본문이다.</summary>
        public event Action<string, JObject> MatchResultReceived;

        private void OnEnable()
        {
            Application.quitting += HandleQuitting;
        }

        private void OnDisable()
        {
            Application.quitting -= HandleQuitting;
        }

        private void OnDestroy()
        {
            StopServer();
        }

        /// <summary>돌고 있지 않을 때 설치 여부만 상태에 반영한다.</summary>
        public void SetInstalled(bool installed)
        {
            if (IsRunning) return;
            if (State == LocalServerState.Failed && installed) return; // 실패 이유를 설정 화면에 남겨 둔다.
            SetState(installed ? LocalServerState.Stopped : LocalServerState.NotInstalled);
        }

        /// <summary>
        /// 묶음 root 의 java 로 로컬 서버를 띄운다. 이미 돌고 있으면 먼저 멈춘다.
        /// </summary>
        public void StartServer(string bundleRoot, string bundleVersion, string staticDataPath)
        {
            StopServer();

            string java = Path.Combine(bundleRoot, LocalServerPlatform.JavaRelativePath);
            string jar = Path.Combine(bundleRoot, "local-server.jar");
            if (!File.Exists(java) || !File.Exists(jar))
            {
                Fail($"bundle is incomplete: {bundleRoot}");
                return;
            }

            Token = CreateToken();
            BundleVersion = bundleVersion;
            Port = 0;
            ServerVersion = null;
            SchemaVersion = 0;
            StaticVersion = null;
            FailureReason = null;

            int processGeneration = ++generation;
            Directory.CreateDirectory(LocalServerPaths.LogsDir);
            string stderrLogPath = Path.Combine(LocalServerPaths.LogsDir, "client-stderr.log");

            var startInfo = new ProcessStartInfo
            {
                FileName = java,
                Arguments = "-Xmx512m -jar local-server.jar --spring.profiles.active=local",
                WorkingDirectory = bundleRoot,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = new UTF8Encoding(false),
                StandardErrorEncoding = new UTF8Encoding(false)
            };

            try
            {
                process = Process.Start(startInfo);
            }
            catch (Exception exception)
            {
                process = null;
                Fail($"could not start java: {exception.Message}");
                return;
            }

            if (process == null)
            {
                Fail("could not start java");
                return;
            }

            // stdin encoding 을 ProcessStartInfo 로 정하는 API 가 Mono 에 없을 수 있어 BaseStream 에 직접 UTF-8 로 쓴다.
            stdin = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };

            Process started = process;
            StartReader($"LocalServer stdout {processGeneration}", () => ReadStdout(started, processGeneration));
            StartReader($"LocalServer stderr {processGeneration}", () => DrainStderr(started, stderrLogPath));

            startedAt = Time.realtimeSinceStartup;
            SetState(LocalServerState.Starting);

            var init = new LocalServerInitCommand
            {
                token = Token,
                dataDir = LocalServerPaths.DataDir,
                staticDataPath = staticDataPath
            };

            // 경로에 한글 사용자 이름이 들어갈 수 있다. ASCII 로 escape 해 두면 서버의 stdin encoding 과 무관하게 읽힌다.
            string initLine = JsonConvert.SerializeObject(init, new JsonSerializerSettings
            {
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
            });

            if (!WriteLine(initLine))
            {
                Fail("could not send init");
                return;
            }

            WDebug.Log($"[LocalServer] started {bundleVersion} (pid {SafePid(process)}), waiting for ready");
        }

        /// <summary>
        /// <c>shutdown</c> 을 보내고 stdin 을 닫는다. 서버는 진행 중인 경기를 끝내고 스스로 종료한다.
        /// 몇 초 안에 끝나지 않으면 background thread 가 프로세스를 끝낸다.
        /// </summary>
        public void StopServer()
        {
            if (process == null)
            {
                if (IsRunning) SetState(LocalServerState.Stopped);
                return;
            }

            Process stopping = process;
            process = null;
            generation++; // 이전 프로세스가 남긴 stdout 줄은 무시한다.

            WriteLine("{\"type\":\"shutdown\"}");
            CloseStdin();

            var killer = new Thread(() =>
            {
                try
                {
                    if (!stopping.WaitForExit(StopGraceMilliseconds)) stopping.Kill();
                }
                catch (Exception)
                {
                    // 이미 끝났다.
                }
                finally
                {
                    stopping.Dispose();
                }
            }) { IsBackground = true, Name = "LocalServer stop" };
            killer.Start();

            Token = null;
            BundleVersion = null;
            Port = 0;
            if (State != LocalServerState.Failed) SetState(LocalServerState.Stopped);
        }

        private void Update()
        {
            while (output.TryDequeue(out OutputLine line))
            {
                if (line.Generation != generation) continue;

                if (line.Exited)
                {
                    HandleExit(line.ExitCode);
                    continue;
                }

                HandleLine(line.Text);
            }

            if (State == LocalServerState.Starting && Time.realtimeSinceStartup - startedAt > ReadyTimeoutSeconds)
            {
                WDebug.LogWarning($"[LocalServer] no ready within {ReadyTimeoutSeconds}s");
                KillProcess();
                Fail($"no ready within {ReadyTimeoutSeconds:0}s");
            }
        }

        private void HandleLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            JObject json;
            try
            {
                json = JObject.Parse(text);
            }
            catch (JsonException)
            {
                // 계약상 stdout 에는 프로토콜 줄만 온다. 다른 출력은 서버 설정 문제라 log 만 남긴다.
                WDebug.LogWarning($"[LocalServer] non-protocol stdout: {JsonCodec.Excerpt(text)}");
                return;
            }

            string type = (string)json["type"];
            switch (type)
            {
                case LocalServerMessage.Ready:
                {
                    LocalServerMessage ready = json.ToObject<LocalServerMessage>();
                    if (ready == null || ready.port <= 0)
                    {
                        KillProcess();
                        Fail("ready without port");
                        return;
                    }

                    Port = ready.port;
                    ServerVersion = ready.serverVersion;
                    SchemaVersion = ready.schemaVersion;
                    StaticVersion = ready.staticVersion;
                    WDebug.Log($"[LocalServer] ready on 127.0.0.1:{Port}, server {ServerVersion}, static {StaticVersion}");
                    SetState(LocalServerState.Ready);
                    break;
                }
                case LocalServerMessage.Error:
                {
                    string reason = (string)json["reason"];
                    WDebug.LogWarning($"[LocalServer] error: {reason}");
                    Fail(string.IsNullOrEmpty(reason) ? "error" : reason);
                    break;
                }
                case LocalServerMessage.MatchStats:
                {
                    LocalServerMessage stats = json.ToObject<LocalServerMessage>();
                    if (stats != null) MatchStatsReceived?.Invoke(stats);
                    break;
                }
                case LocalServerMessage.MatchResult:
                {
                    string sessionId = (string)json["sessionId"];
                    if (string.IsNullOrEmpty(sessionId))
                    {
                        WDebug.LogWarning("[LocalServer] matchResult without sessionId");
                        return;
                    }

                    json.Remove("type");
                    json.Remove("sessionId");
                    MatchResultReceived?.Invoke(sessionId, json);
                    break;
                }
                default:
                    WDebug.LogWarning($"[LocalServer] unknown stdout type: {type}");
                    break;
            }
        }

        private void HandleExit(int exitCode)
        {
            WDebug.Log($"[LocalServer] process exited ({exitCode})");
            CloseStdin();
            if (process != null)
            {
                process.Dispose();
                process = null;
            }

            Token = null;
            Port = 0;
            if (IsRunning) Fail($"exited ({exitCode})");
        }

        private void HandleQuitting()
        {
            StopServer();
        }

        private void Fail(string reason)
        {
            FailureReason = reason;
            SetState(LocalServerState.Failed);
        }

        private void SetState(LocalServerState state)
        {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke();
        }

        private bool WriteLine(string line)
        {
            try
            {
                if (stdin == null) return false;
                stdin.WriteLine(line);
                return true;
            }
            catch (Exception exception)
            {
                WDebug.LogWarning($"[LocalServer] stdin write failed: {exception.Message}");
                return false;
            }
        }

        private void CloseStdin()
        {
            try
            {
                stdin?.Dispose();
            }
            catch (Exception)
            {
                // pipe 가 이미 닫혔다.
            }

            stdin = null;
        }

        private void KillProcess()
        {
            Process target = process;
            process = null;
            generation++;
            CloseStdin();
            if (target == null) return;

            try
            {
                if (!target.HasExited) target.Kill();
            }
            catch (Exception)
            {
                // 이미 끝났다.
            }

            target.Dispose();
            Token = null;
            Port = 0;
        }

        private void ReadStdout(Process source, int processGeneration)
        {
            try
            {
                string line;
                while ((line = source.StandardOutput.ReadLine()) != null)
                {
                    output.Enqueue(new OutputLine(processGeneration, line, false, 0));
                }
            }
            catch (Exception)
            {
                // 프로세스가 정리되며 stream 이 닫혔다.
            }

            int exitCode = -1;
            try
            {
                source.WaitForExit(StopGraceMilliseconds);
                if (source.HasExited) exitCode = source.ExitCode;
            }
            catch (Exception)
            {
                // Dispose 된 뒤라 exit code 를 못 읽는다.
            }

            output.Enqueue(new OutputLine(processGeneration, null, true, exitCode));
        }

        private static void DrainStderr(Process source, string logPath)
        {
            StreamWriter log = null;
            try
            {
                log = new StreamWriter(logPath, append: true, new UTF8Encoding(false)) { AutoFlush = true };
                log.WriteLine($"--- {DateTime.UtcNow:O} start");
            }
            catch (Exception)
            {
                log = null; // 파일을 못 열어도 pipe 는 계속 비운다.
            }

            try
            {
                string line;
                while ((line = source.StandardError.ReadLine()) != null)
                {
                    log?.WriteLine(line);
                }
            }
            catch (Exception)
            {
                // 프로세스가 정리되며 stream 이 닫혔다.
            }
            finally
            {
                log?.Dispose();
            }
        }

        private static void StartReader(string name, ThreadStart body)
        {
            new Thread(body) { IsBackground = true, Name = name }.Start();
        }

        private static string CreateToken()
        {
            var bytes = new byte[TokenBytes];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(bytes);
            }

            return LocalHostingRules.ToHex(bytes);
        }

        private static string SafePid(Process target)
        {
            try
            {
                return target.Id.ToString();
            }
            catch (Exception)
            {
                return "?";
            }
        }
    }
}
