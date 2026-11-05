using System;
using System.IO;
using Admin;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using Global;
using Data;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CustomBattleValidation {
    private static GameObject livePage;
    private static double started;
    static CustomBattleValidation() {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-customBattleUiTest") >= 0) {
            EditorApplication.playModeStateChanged += state => {
                if (state != PlayModeStateChange.EnteredPlayMode) return;
                SceneContext.User = new User(1,"local-test","",-1);
                livePage = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("CustomBotBattlePage"));
                started = EditorApplication.timeSinceStartup;
                EditorApplication.update += Poll;
            };
        }
    }
    public static void RunUi() {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }
    private static void Poll() {
        if (livePage==null) return;
        var page=livePage.GetComponent<CustomBotBattlePage>();
        var serialized=new SerializedObject(page);
        var button=(Button)serialized.FindProperty("confirmButton").objectReferenceValue;
        if (button.interactable) {
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/custom-battle-ui-validation.txt","PASS: headless play-mode page detected the actual local service/server/model and enabled Start. No game scene or remote service was opened.");
            EditorApplication.Exit(0);
        } else if (EditorApplication.timeSinceStartup-started>20) {
            File.WriteAllText("Logs/custom-battle-ui-validation.txt","FAIL: page did not enable Start within 20 seconds.");
            EditorApplication.Exit(1);
        }
    }
    public static void Run() {
        var path="Assets/Resources/CustomBotBattlePage.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {
            var page=root.GetComponent<CustomBotBattlePage>();
            if (page==null) throw new InvalidOperationException("Custom battle page component missing");
            var serialized=new SerializedObject(page);
            foreach(var name in new[]{"root","messageText","confirmButton","cancelButton"}) {
                if(serialized.FindProperty(name).objectReferenceValue==null)
                    throw new InvalidOperationException("Missing page reference: "+name);
            }
            if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root)!=0)
                throw new InvalidOperationException("Missing page script");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/custom-battle-validation.txt","PASS: project compiled; prefab loaded; page references resolved. Play-mode battle not tested.");
        } finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
