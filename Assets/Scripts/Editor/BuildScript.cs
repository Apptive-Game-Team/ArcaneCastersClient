using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    private const string DevDefine = "DEV_BUILD";

    public static void BuildDevWebGL() => Build(devBuild: true);
    public static void BuildWebGL() => Build(devBuild: false);

    private static void Build(bool devBuild)
    {
        PlaygroundBuildGuard.Validate();
        SetDefine(DevDefine, devBuild);

        // game-ci passes -customBuildPath; fall back to convention
        var outputPath = GetArg("-customBuildPath")
            ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "build/WebGL/WebGL"));

        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[BuildScript] Build failed: {report.summary.result}");
            EditorApplication.Exit(1);
        }
    }

    private static void SetDefine(string define, bool enable)
    {
        const BuildTargetGroup group = BuildTargetGroup.WebGL;
        var list = new List<string>(
            PlayerSettings.GetScriptingDefineSymbolsForGroup(group)
                .Split(';')
                .Where(d => !string.IsNullOrWhiteSpace(d))
        );

        if (enable && !list.Contains(define))
            list.Add(define);
        else if (!enable)
            list.Remove(define);

        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", list));
        Debug.Log($"[BuildScript] DEV_BUILD={enable}, defines: {string.Join(";", list)}");
    }

    private static string GetArg(string name)
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }
}
