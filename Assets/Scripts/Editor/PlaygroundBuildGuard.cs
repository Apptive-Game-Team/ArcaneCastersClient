using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PlaygroundBuildGuard : IPreprocessBuildWithReport, IPostprocessBuildWithReport, IProcessSceneWithReport {
    public const string Root = "Assets/DevPlayground/";
    public int callbackOrder => 1000;
    public void OnPreprocessBuild(BuildReport report) => Validate();
    public void OnProcessScene(Scene scene, BuildReport report) {
        // Unity also invokes this callback during Editor Play Mode with no build report.
        if (report == null) return;
        StripAdminEntry(scene);
    }
    internal static void StripAdminEntry(Scene scene) {
        if (scene.name != "AdminScene") return;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) {
                if (child == null || child.name != PlaygroundAdminEntry.ButtonName) continue;
                if (!child.CompareTag("EditorOnly"))
                    throw new BuildFailedException("The admin playground button must remain EditorOnly.");
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
    }
    public void OnPostprocessBuild(BuildReport report) {
        foreach (var packed in report.packedAssets)
            foreach (var item in packed.contents)
                Reject(item.sourceAssetPath);
    }
    public static void Validate() {
        var roots = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToList();
        roots.AddRange(AssetDatabase.GetAllAssetPaths().Where(path =>
            path.Contains("/Resources/") || path.StartsWith("Assets/StreamingAssets/", StringComparison.Ordinal)));
        foreach (string guid in AssetDatabase.FindAssets("t:AddressableAssetSettings")) {
            var settings = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(AssetDatabase.GUIDToAssetPath(guid));
            if (settings == null) continue;
            foreach (var group in settings.groups.Where(group => group != null))
                foreach (var entry in group.entries) {
                    roots.Add(entry.AssetPath);
                    if (AssetDatabase.IsValidFolder(entry.AssetPath))
                        roots.AddRange(AssetDatabase.GetAllAssetPaths().Where(path =>
                            path.StartsWith(entry.AssetPath + "/", StringComparison.Ordinal)));
                }
        }
        foreach (string path in roots.Distinct()) {
            Reject(path);
            foreach (string dependency in AssetDatabase.GetDependencies(path, true)) Reject(dependency);
        }
        if (EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path.StartsWith(Root, StringComparison.Ordinal)))
            throw new BuildFailedException("The playground cannot be a player build scene.");
    }
    private static void Reject(string path) {
        if (path != null && path.StartsWith(Root, StringComparison.Ordinal))
            throw new BuildFailedException("Editor playground asset would enter a player build: " + path);
    }
}
