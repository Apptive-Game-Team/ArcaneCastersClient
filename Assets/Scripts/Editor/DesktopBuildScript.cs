using System;
using System.IO;
using System.Linq;
using System.Xml;
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
            if (target == BuildTarget.StandaloneOSX)
                NormalizeMacExecutable(root);
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

    internal static void NormalizeMacExecutable(string root)
    {
        string plistPath = Path.Combine(root, "Contents/Info.plist");
        var plist = new XmlDocument { XmlResolver = null };
        plist.Load(plistPath);
        var executable = plist.SelectSingleNode("/plist/dict/key[.='CFBundleExecutable']/following-sibling::*[1][self::string]");
        string name = executable?.InnerText;
        if (string.IsNullOrWhiteSpace(name) || name == "." || name == ".." || name.IndexOfAny(new[] { '/', '\\' }) >= 0)
            throw new InvalidDataException("macOS bundle has no valid CFBundleExecutable");
        string directory = Path.Combine(root, "Contents/MacOS");
        if (!File.Exists(Path.Combine(directory, name)))
            throw new FileNotFoundException("macOS bundle executable is missing", name);
        if (name == "ArcaneCasters") return;

        // productName은 저장 경로에도 쓰이므로 빌드 결과만 updater 규약에 맞춘다.
        // 변경된 번들은 updater 삽입 후 릴리스 패키징 단계에서 다시 서명한다.
        File.Move(Path.Combine(directory, name), Path.Combine(directory, "ArcaneCasters"));
        executable.InnerText = "ArcaneCasters";
        plist.Save(plistPath);
    }

    private static string Argument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, name);
        if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Missing " + name);
        return args[index + 1];
    }
}
