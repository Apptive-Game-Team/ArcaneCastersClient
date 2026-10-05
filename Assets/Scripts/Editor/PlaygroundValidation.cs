using System;
using System.IO;
using System.Linq;
using DevPlayground;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Batch verification and a render of the generated editor layout, without a server.</summary>
public static class PlaygroundValidation {
    private const string PlayModeCheck = "PlaygroundValidation.PlayMode";
    public static void ValidatePlayMode() {
        PlaygroundSceneBuilder.Generate();
        EditorSceneManager.OpenScene(PlaygroundSceneBuilder.ScenePath, OpenSceneMode.Single);
        SessionState.SetString(PlayModeCheck, DateTime.UtcNow.AddSeconds(60).ToString("O"));
        EditorApplication.update += CheckPlayMode;
        EditorApplication.isPlaying = true;
    }
    [InitializeOnLoadMethod]
    private static void ResumePlayModeCheck() {
        if (!string.IsNullOrEmpty(SessionState.GetString(PlayModeCheck, "")))
            EditorApplication.update += CheckPlayMode;
    }
    private static void CheckPlayMode() {
        var deadline = SessionState.GetString(PlayModeCheck, "");
        if (string.IsNullOrEmpty(deadline)) return;
        if (DateTime.UtcNow > DateTime.Parse(deadline)) {
            Debug.LogError("Playground Play Mode bootstrap did not start.");
            SessionState.EraseString(PlayModeCheck);
            EditorApplication.Exit(1);
            return;
        }
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        var host = UnityEngine.Object.FindObjectOfType<PlaygroundHost>();
        if (host == null || host.OnStart == null || host.OnUpdate == null || host.OnDestroyed == null ||
            !host.statusText.text.StartsWith("Log in as a developer")) return;
        Debug.Log("PLAYGROUND_PLAY_MODE_PASS: host callbacks and unauthenticated start verified.");
        SessionState.EraseString(PlayModeCheck);
        EditorApplication.update -= CheckPlayMode;
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () => EditorApplication.Exit(0);
    }

    public static void Validate() {
        PlaygroundSceneBuilder.ValidateGeneratedScene();
        var original = EditorBuildSettings.scenes;
        const string texturePath = "Assets/DevPlayground/Generated/BuildGuardFixture.asset";
        const string materialPath = "Assets/Resources/PlaygroundBuildGuardFixture.mat";
        if (File.Exists(texturePath) || File.Exists(materialPath))
            throw new InvalidOperationException("Validation fixture already exists.");
        try {
            EditorBuildSettings.scenes = original.Concat(new[] {
                new EditorBuildSettingsScene(PlaygroundSceneBuilder.ScenePath, true)
            }).ToArray();
            ExpectRejected("build scene");
            EditorBuildSettings.scenes = original;
            AssetDatabase.CreateAsset(new Texture2D(2, 2), texturePath);
            var material = new Material(Shader.Find("Unlit/Texture"));
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            AssetDatabase.CreateAsset(material, materialPath);
            AssetDatabase.SaveAssets();
            ExpectRejected("Resources dependency");
        } finally {
            EditorBuildSettings.scenes = original;
            AssetDatabase.DeleteAsset(materialPath);
            AssetDatabase.DeleteAsset(texturePath);
        }
        PlaygroundBuildGuard.Validate();
        Debug.Log("PLAYGROUND_BUILD_GUARD_PASS: ordinary roots accepted, scene and Resources leaks rejected.");
    }

    private static void ExpectRejected(string fixture) {
        try { PlaygroundBuildGuard.Validate(); }
        catch (BuildFailedException) { return; }
        throw new InvalidOperationException("Build guard accepted " + fixture);
    }

    public static void CaptureLayout() {
        Validate();
        var scene = EditorSceneManager.OpenScene(PlaygroundSceneBuilder.ScenePath, OpenSceneMode.Additive);
        RenderTexture render = null;
        Texture2D pixels = null;
        GameObject uiCameraObject = null;
        try {
            var roots = scene.GetRootGameObjects();
            var controller = roots.SelectMany(root => root.GetComponentsInChildren<PlaygroundHost>(true)).Single();
            controller.statusText.text = "Editor layout preview — no server connection";
            foreach (var sprite in Resources.LoadAll<Sprite>("Game/sprites").OrderBy(sprite => sprite.name).Take(30)) {
                var button = UnityEngine.Object.Instantiate(controller.iconTemplate, controller.iconContent);
                button.gameObject.SetActive(true);
                var icon = button.transform.Find("Icon").GetComponent<Image>();
                icon.sprite = sprite;
                button.GetComponentInChildren<TMP_Text>(true).text = sprite.name;
            }
            var camera = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).First();
            render = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = render;
            camera.fieldOfView = 2 * Mathf.Atan(Mathf.Tan(12.5f * Mathf.Deg2Rad) * (16f / 9f) / camera.aspect) * Mathf.Rad2Deg;
            var canvas = controller.GetComponentInChildren<Canvas>();
            foreach (var child in canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 5;
            uiCameraObject = new GameObject("Layout UI Camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(uiCameraObject, scene);
            var uiCamera = uiCameraObject.GetComponent<Camera>();
            uiCamera.targetTexture = render;
            uiCamera.clearFlags = CameraClearFlags.Depth;
            uiCamera.cullingMask = 1 << 5;
            camera.cullingMask &= ~(1 << 5);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = uiCamera.nearClipPlane + 1;
            Canvas.ForceUpdateCanvases();
            RenderTexture.active = render;
            GL.Clear(true, true, new Color(.13f, .16f, .12f));
            camera.Render();
            uiCamera.Render();
            RenderTexture.active = render;
            pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            pixels.Apply();
            Directory.CreateDirectory("docs/pr-media/252");
            File.WriteAllBytes("docs/pr-media/252/playground-editor-layout.png", pixels.EncodeToPNG());
            camera.targetTexture = null;
            Debug.Log("PLAYGROUND_LAYOUT_CAPTURED");
        } finally {
            RenderTexture.active = null;
            if (uiCameraObject != null) UnityEngine.Object.DestroyImmediate(uiCameraObject);
            if (render != null) UnityEngine.Object.DestroyImmediate(render);
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
