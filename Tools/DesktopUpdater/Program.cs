using System.Diagnostics;
using System.Text.Json;

namespace ArcaneUpdater;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length != 1) return 2;
        string jobPath = Path.GetFullPath(args[0]);
        string work = Path.GetDirectoryName(jobPath)!;
        string result = Path.Combine(work, "result.txt");
        string? stage = null;
        try
        {
            var job = JsonSerializer.Deserialize<UpdateJob>(File.ReadAllText(jobPath), Installer.Json)
                      ?? throw new InvalidDataException("Empty job");
            Installer.ValidateJob(job, work);
            using var installLock = new FileStream(job.InstallRoot + ".update.lock",
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            using var parent = Process.GetProcessById(job.ParentPid);
            // Mono and .NET derive Unix process start times with slightly different
            // clock precision. Still reject stale/reused process IDs.
            if (Math.Abs(parent.StartTime.ToUniversalTime().Ticks - job.ParentStartTicks) > TimeSpan.TicksPerSecond)
                throw new InvalidDataException("Parent process changed");
            stage = job.InstallRoot + ".staging-" + Guid.NewGuid().ToString("N");
            Installer.Prepare(job, stage);
            if (File.Exists(Path.Combine(work, "abort"))) throw new IOException("Update cancelled");
            File.WriteAllText(Path.Combine(work, "ready"), "");
            // A ready helper cannot install unless the client explicitly commits AND exits.
            Installer.WaitForParentExit(parent, work, TimeSpan.FromMinutes(2));
            Installer.Install(job, stage, executable =>
            {
                var info = new ProcessStartInfo(executable) {
                    UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = job.InstallRoot
                };
                using var child = Process.Start(info) ?? throw new IOException("Could not restart game");
            });
            stage = null;
            File.WriteAllText(result, "Updated successfully. Previous installation retained in a sibling .backup-* folder.");
            return 0;
        }
        catch (Exception exception)
        {
            // Paths stay in a local diagnostic file, never sent to a remote service.
            File.WriteAllText(result, "Update failed: " + exception.Message);
            return 1;
        }
        finally
        {
            // Only a directory this invocation generated, never a supplied installation path.
            if (stage != null && Directory.Exists(stage))
                try { Directory.Delete(stage, true); } catch { /* retain diagnostics */ }
        }
    }
}
