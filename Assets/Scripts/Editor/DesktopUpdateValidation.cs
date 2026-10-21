using System;
using System.IO;
using Global;
using LoginScene;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DesktopUpdateValidation
{
    public static void Validate()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LoginScene.unity", OpenSceneMode.Single);
        var initializer = UnityEngine.Object.FindObjectOfType<LoginSceneInitializer>();
        var owner = new SerializedObject(initializer);
        var dialog = owner.FindProperty("updateDialog").objectReferenceValue as DesktopUpdateDialog;
        if (dialog == null || !PrefabUtility.IsPartOfPrefabInstance(dialog))
            throw new InvalidOperationException("Login scene update dialog must be a prefab instance");
        var serialized = new SerializedObject(dialog);
        foreach (string field in new[] { "root", "messageText", "confirmButton", "cancelButton" })
            if (serialized.FindProperty(field).objectReferenceValue == null)
                throw new InvalidOperationException("Missing update dialog reference: " + field);
        Debug.Log("Desktop update prefab/scene references validated");

        // A normal Editor compilation excludes the actual standalone update coroutine.
        // Compile player scripts explicitly, without building all textures/audio.
        string output = Path.GetFullPath(".utmp/desktop-update-player-scripts");
        Directory.CreateDirectory(output);
        var result = PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings {
            target = BuildTarget.StandaloneWindows64,
            group = BuildTargetGroup.Standalone
        }, output);
        if (result.assemblies == null || result.assemblies.Count == 0 ||
            !File.Exists(Path.Combine(output, "Assembly-CSharp.dll")))
            throw new InvalidOperationException("Standalone player script compilation failed");
        Debug.Log("Desktop update Windows player scripts compiled");
    }
}
