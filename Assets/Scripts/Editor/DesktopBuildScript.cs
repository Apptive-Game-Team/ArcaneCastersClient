using System;
using System.IO;
using System.Linq;
using Global.Serialization;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Produces a dedicated installation directory for the full-build updater.</summary>
public static class DesktopBuildScript
{
    public static void Build()
    {
        string platform = Argument("-desktopPlatform");
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", Argument("-desktopBuildPath")));
        BuildTarget target;
        string executable;
        switch (platform)
        {
            case "win-x64": target = BuildTarget.StandaloneWindows64; executable = "ArcaneCasters.exe"; break;
            case "linux-x64": target = BuildTarget.StandaloneLinux64; executable = "ArcaneCasters"; break;
            case "osx-x64":
            case "osx-arm64":
                target = BuildTarget.StandaloneOSX;
                executable = "Contents/MacOS/ArcaneCasters";
                if (!root.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("macOS installation path must end with .app");
                break;
            default: throw new ArgumentException("Use win-x64, linux-x64, osx-x64 or osx-arm64");
        }
        string folder = target == BuildTarget.StandaloneOSX ? "ArcaneCasters.app" : "ArcaneCasters";
        if (!Path.GetFileName(root).Equals(folder, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Build into a dedicated directory named " + folder);
        if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())
            throw new IOException("Use an empty dedicated build directory");
        PlaygroundBuildGuard.Validate();
        int previousArchitecture = PlayerSettings.GetArchitecture(BuildTargetGroup.Standalone);
        try
        {
            if (target == BuildTarget.StandaloneOSX)
                PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone, platform == "osx-arm64" ? 1 : 0);
            string path = target == BuildTarget.StandaloneOSX ? root : Path.Combine(root, executable);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                target = target, locationPathName = path, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Desktop build failed: " + report.summary.result);
            if (!File.Exists(Path.Combine(root, executable)))
                throw new IOException("Build executable does not match the updater layout");
            string marker = target == BuildTarget.StandaloneOSX ? "Contents/Resources/arcane-install.json" : "arcane-install.json";
            File.WriteAllText(Path.Combine(root, marker), JsonCodec.Serialize(new {
                schemaVersion = 1, appId = "arcane-casters", platform,
                version = PlayerSettings.bundleVersion, executable
            }));
        }
        finally
        {
            PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone, previousArchitecture);
        }
    }

    private static string Argument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, name);
        if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Missing " + name);
        return args[index + 1];
    }
}
