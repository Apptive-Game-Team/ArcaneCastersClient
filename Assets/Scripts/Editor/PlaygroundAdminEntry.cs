using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PlaygroundAdminEntry {
    public const string ButtonName = "Magic Playground";
    static PlaygroundAdminEntry() {
        EditorApplication.playModeStateChanged += PrepareForPlay;
        SceneManager.sceneLoaded += Bind;
    }

    private static void PrepareForPlay(PlayModeStateChange state) {
        if (state == PlayModeStateChange.ExitingEditMode &&
            SceneManager.GetActiveScene().name != "Playground") PlaygroundSceneBuilder.Generate();
    }

    private static void Bind(Scene scene, LoadSceneMode mode) {
        if (!EditorApplication.isPlaying || scene.name != "AdminScene") return;
        var button = FindButton(scene);
        if (button == null) { Debug.LogError("AdminScene is missing the playground button."); return; }
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(PlaygroundSceneBuilder.EnterFromAdmin);
    }

    public static Button FindButton(Scene scene) => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<Button>(true))
        .SingleOrDefault(button => button.name == ButtonName);

    // Author the prefab instance once; the committed scene owns the layout.
    public static void AuthorButton() {
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/AdminScene.unity", OpenSceneMode.Additive);
        try {
            SceneManager.SetActiveScene(scene);
            var button = FindButton(scene);
            if (button == null) {
                var reload = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Button>(true))
                    .Single(button => button.name == "Reload Rooms");
                button = PlaygroundSceneBuilder.MakeButton(reload.transform.parent, ButtonName, "Magic\nPlayground");
            }
            button.GetComponentInChildren<TMPro.TMP_Text>(true).text = "Magic\nPlayground";
            button.gameObject.tag = "EditorOnly";
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-12, -8);
            rect.sizeDelta = new Vector2(132, 48);
            EditorSceneManager.SaveScene(scene);
        } finally {
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
