using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Diagnostics;
using System.Reflection;
using ArcaneUpdater;

if (args.Contains("--parent"))
{
    Thread.Sleep(TimeSpan.FromMinutes(2));
    return;
}

int passed = 0;
void Test(string name, Action action)
{
    action();
    Console.WriteLine("PASS " + name);
    passed++;
}
void Reject(Action action)
{
    try { action(); }
    catch (Exception) { return; }
    throw new Exception("Expected rejection");
}

string sandbox = Path.Combine(Environment.GetEnvironmentVariable("ARCANE_TEST_ROOT") ?? Path.GetTempPath(),
    "arcane-updater-test-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox);
int index = 0;
(UpdateJob Job, string Stage) Fixture(Action<ZipArchive>? extra = null)
{
    string test = Path.Combine(sandbox, (++index).ToString());
    string root = Path.Combine(test, OperatingSystem.IsMacOS() ? "ArcaneCasters.app" : "ArcaneCasters");
    Directory.CreateDirectory(root);
    var job = new UpdateJob {
        InstallRoot = root, Archive = Path.Combine(test, "update.zip"), Platform = Installer.HostPlatform(),
        CurrentVersion = "1.0.0", Version = "1.1.0", Executable = "Game.exe"
    };
    string Marker(string version) => JsonSerializer.Serialize(new InstallMarker {
        SchemaVersion = 1, AppId = "arcane-casters", Platform = job.Platform, Version = version, Executable = job.Executable
    });
    string markerPath = Installer.MarkerPath(job.Platform);
    Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(root, markerPath))!);
    File.WriteAllText(Path.Combine(root, markerPath), Marker(job.CurrentVersion));
    File.WriteAllText(Path.Combine(root, "Game.exe"), "old");
    using (var zip = ZipFile.Open(job.Archive, ZipArchiveMode.Create))
    {
        using (var writer = new StreamWriter(zip.CreateEntry(markerPath).Open())) writer.Write(Marker(job.Version));
        using (var writer = new StreamWriter(zip.CreateEntry("Game.exe").Open())) writer.Write("new");
        extra?.Invoke(zip);
    }
    void Digest()
    {
        job.Size = new FileInfo(job.Archive).Length;
        using var input = File.OpenRead(job.Archive);
        job.Sha256 = Convert.ToHexString(SHA256.HashData(input));
    }
    Digest();
    return (job, root + ".stage");
}
try
{
    Test("numeric stable versions", () => {
        if (!(Installer.StableVersion("1.10.0") > Installer.StableVersion("1.9.9"))) throw new Exception();
        Reject(() => Installer.StableVersion("1.2.3-beta"));
        Reject(() => Installer.StableVersion("01.2.3"));
    });
    Test("successful full replacement retains backup", () => {
        var (job, stage) = Fixture();
        Installer.Prepare(job, stage);
        string backup = Installer.Install(job, stage, exe => {
            if (File.ReadAllText(exe) != "new") throw new Exception();
        });
        if (File.ReadAllText(Path.Combine(backup, job.Executable)) != "old") throw new Exception();
    });
    Test("launch failure restores old installation", () => {
        var (job, stage) = Fixture();
        Installer.Prepare(job, stage);
        Reject(() => Installer.Install(job, stage, _ => throw new IOException("Simulated start failure")));
        if (File.ReadAllText(Path.Combine(job.InstallRoot, job.Executable)) != "old") throw new Exception();
    });
    Test("swap failure restores old installation", () => {
        var (job, stage) = Fixture();
        Reject(() => Installer.Install(job, stage, _ => throw new Exception("Should not launch")));
        if (!Directory.Exists(job.InstallRoot)) throw new Exception();
    });
    Test("bad checksum cannot touch installed files", () => {
        var (job, stage) = Fixture();
        job.Sha256 = new string('0', 64);
        Reject(() => Installer.Prepare(job, stage));
        if (Directory.Exists(stage) || !Directory.Exists(job.InstallRoot)) throw new Exception();
    });
    Test("size mismatch", () => {
        var (job, stage) = Fixture(); job.Size++;
        Reject(() => Installer.Prepare(job, stage));
    });
    Test("corrupt ZIP with matching checksum is rejected", () => {
        var (job, stage) = Fixture();
        File.WriteAllText(job.Archive, "not a zip");
        job.Size = new FileInfo(job.Archive).Length;
        using (var stream = File.OpenRead(job.Archive)) job.Sha256 = Convert.ToHexString(SHA256.HashData(stream));
        Reject(() => Installer.Prepare(job, stage));
        if (File.ReadAllText(Path.Combine(job.InstallRoot, job.Executable)) != "old") throw new Exception();
    });
    Test("wrong staged identity", () => {
        var (job, stage) = Fixture(); job.Version = "9.0.0";
        Reject(() => Installer.Prepare(job, stage));
    });
    Test("missing executable", () => {
        var (job, stage) = Fixture(); job.Executable = "missing.exe";
        Reject(() => Installer.Prepare(job, stage));
    });
    Test("zip traversal and platform aliases", () => {
        foreach (var name in new[] { "../escape", "/absolute", "C:/escape", @"..\escape", "dir/../escape",
                     "NUL", "file:stream", "trailing.", "trailing " })
        {
            var (job, stage) = Fixture(zip => zip.CreateEntry(name));
            Reject(() => Installer.Prepare(job, stage));
        }
        if (File.Exists(Path.Combine(sandbox, "escape"))) throw new Exception();
    });
    Test("case-colliding entries", () => {
        var (job, stage) = Fixture(zip => zip.CreateEntry("game.EXE"));
        Reject(() => Installer.Prepare(job, stage));
    });
    Test("escaping symbolic link", () => {
        var (job, stage) = Fixture(zip => {
            var link = zip.CreateEntry("escape");
            link.ExternalAttributes = (0xa000 | 0x1ff) << 16;
            using var writer = new StreamWriter(link.Open()); writer.Write("../outside");
        });
        Reject(() => Installer.Prepare(job, stage));
    });
    Test("dedicated install ownership and helper location", () => {
        var (job, _) = Fixture();
        Installer.ValidateJob(job, Path.GetDirectoryName(job.Archive)!);
        Reject(() => Installer.ValidateJob(job, job.InstallRoot));
        File.Delete(Path.Combine(job.InstallRoot, Installer.MarkerPath(job.Platform)));
        Reject(() => Installer.ValidateJob(job, Path.GetDirectoryName(job.Archive)!));
    });
    Test("downgrade and architecture mismatch", () => {
        var (job, _) = Fixture();
        job.Version = job.CurrentVersion;
        Reject(() => Installer.ValidateJob(job, Path.GetDirectoryName(job.Archive)!));
        job.Version = "1.2.0"; job.Platform = "wrong";
        Reject(() => Installer.ValidateJob(job, Path.GetDirectoryName(job.Archive)!));
    });
    Test("exclusive lock rejects concurrent updater", () => {
        string path = Path.Combine(sandbox, "lock");
        using var first = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        Reject(() => { using var second = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); });
    });
    Test("parent timeout and abort prevent handoff", () => {
        using var parent = Process.GetCurrentProcess();
        Reject(() => Installer.WaitForParentExit(parent, sandbox, TimeSpan.FromMilliseconds(1)));
        File.WriteAllText(Path.Combine(sandbox, "abort"), "");
        Reject(() => Installer.WaitForParentExit(parent, sandbox, TimeSpan.FromMinutes(1)));
        File.Delete(Path.Combine(sandbox, "abort"));
    });
    if (OperatingSystem.IsWindows())
    {
        Test("locked game files preserve original installation", () => {
            var (job, stage) = Fixture();
            Installer.Prepare(job, stage);
            using var held = new FileStream(Path.Combine(job.InstallRoot, job.Executable),
                FileMode.Open, FileAccess.Read, FileShare.Read);
            Reject(() => Installer.Install(job, stage, _ => throw new Exception("Must not launch")));
            if (!File.Exists(Path.Combine(job.InstallRoot, job.Executable))) throw new Exception();
        });
    }
    string? helperExe = Environment.GetEnvironmentVariable("ARCANE_UPDATER_EXECUTABLE");
    if (!string.IsNullOrEmpty(helperExe))
    {
        foreach (string scenario in new[] { "commit", "abort", "no-commit" })
        Test("real helper handshake/restart: " + scenario, () => {
            var (job, _) = Fixture();
            string work = Path.GetDirectoryName(job.Archive)!;
            using (var zip = ZipFile.Open(job.Archive, ZipArchiveMode.Update))
            {
                zip.GetEntry(job.Executable)!.Delete();
                string binary = OperatingSystem.IsWindows()
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "whoami.exe")
                    : OperatingSystem.IsMacOS() ? "/usr/bin/true" : "/bin/true";
                zip.CreateEntryFromFile(binary, job.Executable);
            }
            job.Size = new FileInfo(job.Archive).Length;
            using (var stream = File.OpenRead(job.Archive)) job.Sha256 = Convert.ToHexString(SHA256.HashData(stream));
            string currentHost = Environment.ProcessPath!;
            var parentInfo = new ProcessStartInfo(currentHost) { UseShellExecute = false, CreateNoWindow = true };
            if (Path.GetFileNameWithoutExtension(currentHost).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                parentInfo.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
            parentInfo.ArgumentList.Add("--parent");
            using var parent = Process.Start(parentInfo)!;
            job.ParentPid = parent.Id;
            job.ParentStartTicks = parent.StartTime.ToUniversalTime().Ticks;
            string jobPath = Path.Combine(work, "job.json");
            File.WriteAllText(jobPath, JsonSerializer.Serialize(job));
            var info = new ProcessStartInfo(helperExe) { UseShellExecute = false, CreateNoWindow = true };
            info.ArgumentList.Add(jobPath);
            using var helper = Process.Start(info)!;
            try
            {
                var timeout = Stopwatch.StartNew();
                while (!File.Exists(Path.Combine(work, "ready")) && !helper.HasExited && timeout.Elapsed.TotalSeconds < 30)
                    Thread.Sleep(50);
                if (!File.Exists(Path.Combine(work, "ready"))) throw new Exception("Helper failed to prepare");
                if (scenario != "no-commit") File.WriteAllText(Path.Combine(work, scenario), "");
                parent.Kill(); parent.WaitForExit();
                if (!helper.WaitForExit(30000)) throw new Exception("Helper timeout");
                bool success = scenario == "commit";
                if ((helper.ExitCode == 0) != success) throw new Exception(File.ReadAllText(Path.Combine(work, "result.txt")));
                if (!success && File.ReadAllText(Path.Combine(job.InstallRoot, job.Executable)) != "old")
                    throw new Exception("Uncommitted update changed installation");
                if (success && !Directory.EnumerateDirectories(work, "ArcaneCasters*.backup-*").Any())
                    throw new Exception("Backup not retained");
            }
            finally
            {
                if (!parent.HasExited) { parent.Kill(); parent.WaitForExit(); }
                if (!helper.HasExited) { helper.Kill(); helper.WaitForExit(); }
            }
        });
    }
    if (!OperatingSystem.IsWindows())
    {
        Test("Unix executable permissions survive extraction", () => {
            var (job, stage) = Fixture(zip => {
                var executable = zip.CreateEntry("helper");
                executable.ExternalAttributes = (0x8000 | 0x1ed) << 16;
            });
            Installer.Prepare(job, stage);
            if (!File.GetUnixFileMode(Path.Combine(stage, "helper")).HasFlag(UnixFileMode.UserExecute))
                throw new Exception();
        });
        Test("macOS-style framework symlink chains", () => {
            var (job, stage) = Fixture(zip => {
                using (var writer = new StreamWriter(zip.CreateEntry("Framework/Versions/A/library").Open())) writer.Write("library");
                foreach (var (path, target) in new[] {
                    ("Framework/Versions/Current", "A"), ("Framework/library", "Versions/Current/library") })
                {
                    var link = zip.CreateEntry(path);
                    link.ExternalAttributes = (0xa000 | 0x1ff) << 16;
                    using var writer = new StreamWriter(link.Open()); writer.Write(target);
                }
            });
            Installer.Prepare(job, stage);
            if (File.ReadAllText(Path.Combine(stage, "Framework/library")) != "library") throw new Exception();
        });
    }
    Console.WriteLine($"{passed} updater tests passed");
}
finally
{
    // sandbox is a freshly created, absolute temporary directory owned by this test.
    if (Path.GetFileName(sandbox).StartsWith("arcane-updater-test-")) Directory.Delete(sandbox, true);
}
