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
    private const string AdminCheck = "PlaygroundValidation.Admin";
    public static void ValidateAdminEntry() {
        Validate();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/AdminScene.unity", OpenSceneMode.Single);
        if (PlaygroundAdminEntry.FindButton(scene) == null) throw new InvalidOperationException("Missing admin button.");
        SuppressAdminRequests(scene, LoadSceneMode.Single);
        SessionState.SetString(AdminCheck, "entry");
        SessionState.SetString(AdminCheck + ".deadline", DateTime.UtcNow.AddSeconds(60).ToString("O"));
        InstallAdminCheck();
        EditorApplication.isPlaying = true;
    }
    [InitializeOnLoadMethod]
    private static void ResumeAdminCheck() {
        if (!string.IsNullOrEmpty(SessionState.GetString(AdminCheck, ""))) InstallAdminCheck();
    }
    private static void InstallAdminCheck() {
        EditorApplication.update -= CheckAdminEntry;
        EditorApplication.update += CheckAdminEntry;
        SceneManager.sceneLoaded -= SuppressAdminRequests;
        SceneManager.sceneLoaded += SuppressAdminRequests;
    }
    private static void SuppressAdminRequests(Scene scene, LoadSceneMode mode) {
        if (scene.name == "Playground") {
            // The entry gate gets a local identity fixture, cleared before Start can send HTTP.
            Global.SceneContext.User = null;
            Global.SceneContext.JwtToken = null;
        }
        if (scene.name != "AdminScene") return;
        var cancelTogglers = scene.GetRootGameObjects().SelectMany(root =>
            root.GetComponentsInChildren<LobbyScene.Button.CancelMatchingButtonActiveToggler>(true)).ToArray();
        GameObject stateFixture = null;
        if (EditorApplication.isPlaying && cancelTogglers.Length > 0 && LobbyScene.LobbySceneViewModel.Instance == null)
            stateFixture = new GameObject("Admin validation state", typeof(LobbyScene.LobbySceneViewModel));
        // This isolated Admin test has no lobby singleton. Remove its unrelated subscription teardown.
        foreach (var component in cancelTogglers) UnityEngine.Object.DestroyImmediate(component);
        if (stateFixture != null) UnityEngine.Object.DestroyImmediate(stateFixture);
        foreach (var root in scene.GetRootGameObjects())
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true)) {
                var ns = behaviour == null ? "" : behaviour.GetType().Namespace ?? "";
                if (behaviour != null && !ns.StartsWith("UnityEngine.UI") && !ns.StartsWith("TMPro")) behaviour.enabled = false;
            }
    }
    private static void CheckAdminEntry() {
        if (DateTimeOffset.UtcNow > DateTimeOffset.Parse(SessionState.GetString(AdminCheck + ".deadline", ""))) {
            Debug.LogError("Admin playground entry/return timed out.");
            SessionState.EraseString(AdminCheck);
            EditorApplication.Exit(1);
            return;
        }
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        var scene = SceneManager.GetActiveScene();
        var phase = SessionState.GetString(AdminCheck, "");
        if (phase == "entry" && scene.name == "AdminScene") {
            var button = PlaygroundAdminEntry.FindButton(scene);
            if (button == null || !button.CompareTag("EditorOnly") || !button.isActiveAndEnabled) return;
            Global.SceneContext.User = new Data.User(501, "Editor fixture", "", -1);
            Global.SceneContext.JwtToken = "editor-validation-fixture";
            SessionState.SetString(AdminCheck, "playground");
            button.onClick.Invoke();
        } else if (phase == "playground" && scene.name == "Playground") {
            var host = UnityEngine.Object.FindObjectOfType<PlaygroundHost>();
            if (host == null || !host.statusText.text.StartsWith("Log in as a developer")) return;
            SessionState.SetString(AdminCheck, "return");
            host.closeButton.onClick.Invoke();
        } else if (phase == "return" && scene.name == "AdminScene") {
            if (PlaygroundAdminEntry.FindButton(scene) == null) throw new InvalidOperationException("Missing return button.");
            Debug.Log("PLAYGROUND_ADMIN_ENTRY_PASS: clicked Admin button, entered playground and returned to Admin.");
            SessionState.EraseString(AdminCheck);
            EditorApplication.update -= CheckAdminEntry;
            SceneManager.sceneLoaded -= SuppressAdminRequests;
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }
    }
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
        if (DateTimeOffset.UtcNow > DateTimeOffset.Parse(deadline)) {
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
        var admin = EditorSceneManager.OpenScene("Assets/Scenes/AdminScene.unity", OpenSceneMode.Additive);
        try {
            var button = PlaygroundAdminEntry.FindButton(admin);
            if (button == null || !button.CompareTag("EditorOnly")) throw new InvalidOperationException("Missing EditorOnly admin entry.");
            new PlaygroundBuildGuard().OnProcessScene(admin, null);
            if (PlaygroundAdminEntry.FindButton(admin) == null) throw new InvalidOperationException("Admin entry stripped in Play Mode.");
            PlaygroundBuildGuard.StripAdminEntry(admin);
            if (PlaygroundAdminEntry.FindButton(admin) != null) throw new InvalidOperationException("Admin entry survived build stripping.");
        } finally { EditorSceneManager.CloseScene(admin, true); }
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

    public static void CaptureAdminLayout() {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/AdminScene.unity", OpenSceneMode.Additive);
        RenderTexture render = null;
        Texture2D pixels = null;
        try {
            var button = PlaygroundAdminEntry.FindButton(scene);
            if (button == null || !button.CompareTag("EditorOnly")) throw new InvalidOperationException("Missing admin entry.");
            var camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)).First();
            render = new RenderTexture(1280, 720, 24);
            camera.targetTexture = render;
            foreach (var canvas in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Canvas>(true))) {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + 1;
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = render;
            pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            pixels.Apply();
            Directory.CreateDirectory("docs/pr-media/252");
            File.WriteAllBytes("docs/pr-media/252/admin-playground-entry.png", pixels.EncodeToPNG());
            camera.targetTexture = null;
            Debug.Log("PLAYGROUND_ADMIN_LAYOUT_CAPTURED");
        } finally {
            RenderTexture.active = null;
            if (render != null) UnityEngine.Object.DestroyImmediate(render);
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
