using System.IO.Compression;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ArcaneUpdater;

internal sealed class UpdateJob
{
    public string InstallRoot { get; set; } = "";
    public string Archive { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public long Size { get; set; }
    public string Platform { get; set; } = "";
    public string Version { get; set; } = "";
    public string CurrentVersion { get; set; } = "";
    public string Executable { get; set; } = "";
    public int ParentPid { get; set; }
    public long ParentStartTicks { get; set; }
}

internal sealed class InstallMarker
{
    public int SchemaVersion { get; set; }
    public string AppId { get; set; } = "";
    public string Platform { get; set; } = "";
    public string Version { get; set; } = "";
    public string Executable { get; set; } = "";
}

internal static class Installer
{
    internal static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    internal const long MaxExpandedBytes = 32L * 1024 * 1024 * 1024;

    internal static void WaitForParentExit(Process parent, string work, TimeSpan timeout)
    {
        var timer = Stopwatch.StartNew();
        while (!parent.HasExited)
        {
            if (File.Exists(Path.Combine(work, "abort")) || timer.Elapsed > timeout)
                throw new IOException("Client did not exit; update cancelled");
            Thread.Sleep(100);
        }
        if (!File.Exists(Path.Combine(work, "commit")) || File.Exists(Path.Combine(work, "abort")))
            throw new IOException("Update was not committed");
    }

    internal static void ValidateJob(UpdateJob job, string work)
    {
        job.InstallRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(job.InstallRoot));
        job.Archive = Path.GetFullPath(job.Archive);
        if (!Directory.Exists(job.InstallRoot) || Directory.GetParent(job.InstallRoot) == null)
            throw new InvalidDataException("Invalid installation directory");
        string expectedFolder = job.Platform.StartsWith("osx-") ? "ArcaneCasters.app" : "ArcaneCasters";
        if (!Path.GetFileName(job.InstallRoot).Equals(expectedFolder, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Install into a dedicated " + expectedFolder + " folder");
        RejectLinkAncestors(job.InstallRoot);
        if (IsWithin(job.InstallRoot, work) || IsWithin(job.InstallRoot, AppContext.BaseDirectory)
            || !IsWithin(work, job.Archive))
            throw new InvalidDataException("Updater and download must be outside the installation");
        if (job.Size <= 0 || job.Size > MaxExpandedBytes
            || !Regex.IsMatch(job.Sha256, "^[a-fA-F0-9]{64}$"))
            throw new InvalidDataException("Invalid archive metadata");
        if (StableVersion(job.Version) <= StableVersion(job.CurrentVersion))
            throw new InvalidDataException("Update must be newer");
        if (job.Platform != HostPlatform())
            throw new InvalidDataException("Update platform does not match updater");
        ValidateMarker(job.InstallRoot, job, job.CurrentVersion);
    }

    internal static string HostPlatform()
    {
        string os = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";
        string arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        return os + "-" + arch;
    }

    internal static Version StableVersion(string value)
    {
        if (!Regex.IsMatch(value, @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$")
            || !Version.TryParse(value, out var version))
            throw new InvalidDataException("Expected stable X.Y.Z version");
        return version;
    }

    internal static bool IsWithin(string root, string path)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        path = Path.GetFullPath(path);
        return path.Equals(root, comparison) || path.StartsWith(root + Path.DirectorySeparatorChar, comparison);
    }

    internal static string SafePath(string root, string relative)
    {
        // Reject Windows-special names on every platform so the same package cannot have aliases.
        if (string.IsNullOrEmpty(relative) || relative.Contains('\\') || relative.Contains(':')
            || relative.StartsWith('/') || relative.Split('/').Any(p => p is "." or ".."
                || p.EndsWith(' ') || p.EndsWith('.') || p.Any(c => c < 32)
                || Regex.IsMatch(p, @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(\.|$)", RegexOptions.IgnoreCase)))
            throw new InvalidDataException("Unsafe archive path");
        string path = Path.GetFullPath(Path.Combine(root, relative));
        if (!IsWithin(root, path) || path == Path.GetFullPath(root))
            throw new InvalidDataException("Archive path escapes installation");
        return path;
    }

    private static void RejectLinkAncestors(string path)
    {
        for (var directory = new DirectoryInfo(path); directory != null; directory = directory.Parent)
            if (directory.Exists && directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
                throw new InvalidDataException("Installation must not be inside a symbolic link");
    }

    internal static void ValidateMarker(string root, UpdateJob job, string version)
    {
        var markerPath = SafePath(root, MarkerPath(job.Platform));
        if (File.GetAttributes(markerPath).HasFlag(FileAttributes.ReparsePoint))
            throw new InvalidDataException("Installation marker cannot be a link");
        var marker = JsonSerializer.Deserialize<InstallMarker>(File.ReadAllText(markerPath), Json)
                     ?? throw new InvalidDataException("Missing installation marker");
        if (marker.SchemaVersion != 1 || marker.AppId != "arcane-casters" ||
            marker.Platform != job.Platform || marker.Version != version ||
            marker.Executable != job.Executable)
            throw new InvalidDataException("Installation identity mismatch");
        string executable = SafePath(root, marker.Executable);
        if (!File.Exists(executable) || File.GetAttributes(executable).HasFlag(FileAttributes.ReparsePoint))
            throw new InvalidDataException("Missing or linked game executable");
    }

    internal static string MarkerPath(string platform) => platform.StartsWith("osx-")
        ? "Contents/Resources/arcane-install.json" : "arcane-install.json";

    internal static void Prepare(UpdateJob job, string stage)
    {
        using (var file = File.OpenRead(job.Archive))
        {
            if (file.Length != job.Size ||
                !Convert.ToHexString(SHA256.HashData(file)).Equals(job.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Archive checksum/size mismatch");
        }
        if (Directory.Exists(stage)) throw new IOException("Staging directory already exists");
        Directory.CreateDirectory(stage);
        using var zip = ZipFile.OpenRead(job.Archive);
        if (zip.Entries.Count > 200000) throw new InvalidDataException("Too many files");
        long expanded = 0;
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var links = new Dictionary<string, string>(StringComparer.Ordinal);
        var modes = new List<(string Path, int Mode)>();
        foreach (var entry in zip.Entries)
        {
            bool directory = entry.FullName.EndsWith('/');
            string name = entry.FullName.TrimEnd('/');
            string destination = SafePath(stage, name);
            if (!paths.Add(name)) throw new InvalidDataException("Duplicate archive path");
            expanded = checked(expanded + entry.Length);
            if (expanded > MaxExpandedBytes) throw new InvalidDataException("Archive too large");
            int mode = (entry.ExternalAttributes >> 16) & 0xffff;
            int kind = mode & 0xf000;
            if (kind == 0xa000)
            {
                if (OperatingSystem.IsWindows() || entry.Length > 4096)
                    throw new InvalidDataException("Unsupported symbolic link");
                using var reader = new StreamReader(entry.Open());
                string target = reader.ReadToEnd();
                if (Path.IsPathRooted(target) || target.Contains('\\') || target.Contains('\0')
                    || !IsWithin(stage, Path.GetFullPath(Path.Combine(Path.GetDirectoryName(destination)!, target))))
                    throw new InvalidDataException("Link escapes installation");
                links.Add(destination, target);
                continue;
            }
            if (kind != 0 && kind != 0x8000 && kind != 0x4000)
                throw new InvalidDataException("Unsupported archive entry type");
            Directory.CreateDirectory(directory ? destination : Path.GetDirectoryName(destination)!);
            if (!directory)
            {
                using var input = entry.Open();
                using var output = new FileStream(destination, FileMode.CreateNew);
                input.CopyTo(output);
            }
            if (!OperatingSystem.IsWindows() && mode != 0)
                modes.Add((destination, mode & 0x1ff)); // never preserve setuid/setgid bits
        }
        // Create links last: extraction never traverses an archive-supplied symlink.
        foreach (var link in links)
        {
            if (paths.Any(name => SafePath(stage, name).StartsWith(link.Key + Path.DirectorySeparatorChar, StringComparison.Ordinal)))
                throw new InvalidDataException("Archive has entries beneath a symbolic link");
            // macOS frameworks commonly link through Versions/Current. Resolve the whole
            // chain before creating anything, bounding every hop and rejecting cycles.
            string resolved = ResolveLink(stage, link.Key, links);
            Directory.CreateDirectory(Path.GetDirectoryName(link.Key)!);
            if (Directory.Exists(resolved)) Directory.CreateSymbolicLink(link.Key, link.Value);
            else if (File.Exists(resolved)) File.CreateSymbolicLink(link.Key, link.Value);
            else throw new InvalidDataException("Broken symbolic link");
        }
        if (!OperatingSystem.IsWindows())
            foreach (var (path, mode) in modes.OrderByDescending(x => x.Path.Length))
                File.SetUnixFileMode(path, (UnixFileMode)mode);
        ValidateMarker(stage, job, job.Version);
        // Probe the required rename permission before asking the client to exit.
        string probe = stage + ".probe";
        Directory.Move(stage, probe);
        Directory.Move(probe, stage);
    }

    private static string ResolveLink(string root, string path, Dictionary<string, string> links)
    {
        for (int hop = 0; hop < 40; hop++)
        {
            var key = links.Keys.OrderByDescending(candidate => candidate.Length)
                .FirstOrDefault(candidate => IsWithin(candidate, path));
            if (key == null) return path;
            string suffix = Path.GetRelativePath(key, path);
            path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(key)!, links[key], suffix));
            if (!IsWithin(root, path)) throw new InvalidDataException("Symbolic link chain escapes installation");
        }
        throw new InvalidDataException("Symbolic link cycle");
    }

    internal static string Install(UpdateJob job, string stage, Action<string> launch)
    {
        string backup = job.InstallRoot + ".backup-" + Guid.NewGuid().ToString("N");
        Directory.Move(job.InstallRoot, backup);
        bool installed = false;
        try
        {
            Directory.Move(stage, job.InstallRoot);
            installed = true;
            launch(SafePath(job.InstallRoot, job.Executable));
            return backup;
        }
        catch
        {
            if (installed) Directory.Move(job.InstallRoot, stage);
            Directory.Move(backup, job.InstallRoot);
            throw;
        }
    }
}
