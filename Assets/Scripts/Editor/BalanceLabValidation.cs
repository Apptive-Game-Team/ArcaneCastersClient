using System;
using System.IO;
using System.Linq;
using DevPlayground;
using GameScene.Object;
using GameScene.ServedObjectComponent;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BalanceLabValidation {
    private const string Key = "BalanceLabValidation.Deadline";
    private static int phase;
    private static double phaseTime;
    private static int initialBodies;

    public static void Validate() {
        BalanceLabWindow.Prepare();
        Directory.CreateDirectory("Logs");
        SessionState.SetString(Key + ".replay", "Logs/balance-live-" + DateTime.UtcNow.Ticks + ".json");
        EditorSceneManager.OpenScene(BalanceLabWindow.ScenePath);
        SessionState.SetString(Key, DateTime.UtcNow.AddSeconds(90).ToString("O"));
        EditorApplication.update += Check;
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    private static void Resume() {
        if (!string.IsNullOrEmpty(SessionState.GetString(Key, ""))) EditorApplication.update += Check;
    }

    private static void Require(bool condition, string message) {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Check() {
        string deadline = SessionState.GetString(Key, "");
        if (string.IsNullOrEmpty(deadline)) return;
        try {
            Require(DateTime.UtcNow < DateTime.Parse(deadline).ToUniversalTime(), "Balance Play Mode validation timed out.");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            var controller = BalanceController.Active;
            var host = UnityEngine.Object.FindObjectOfType<PlaygroundHost>();
            if (controller == null || host == null) return;
            switch (phase) {
                case 0:
                    if (host.sideDropdown.options.Count < 2 || host.iconContent.GetComponentsInChildren<Button>().Length < 2) return;
                    Require(host.balanceMode, "Balance mode was not serialized.");
                    initialBodies = UnityEngine.Object.FindObjectsOfType<ServedObject>().Length;
                    Require(initialBodies >= 5, "Initial production prefabs were not rendered.");
                    host.immuneAllyButton.onClick.Invoke();
                    phase = 1; phaseTime = EditorApplication.timeSinceStartup;
                    break;
                case 1:
                    if (EditorApplication.timeSinceStartup - phaseTime < 1.0) return;
                    Require(host.timerText.text.Contains("마나"), "Mana is not shown.");
                    var icon = host.iconContent.GetComponentsInChildren<Button>().Single(b => b.GetComponentInChildren<TMP_Text>(true).text == "tree_golem");
                    icon.onClick.Invoke();
                    controller.ProcessPointer(new Vector3(-20, -20), true);
                    controller.ProcessPointer(Camera.main.WorldToScreenPoint(new Vector3(6, 0, 7)), true);
                    phase = 2; phaseTime = EditorApplication.timeSinceStartup;
                    break;
                case 2:
                    if (EditorApplication.timeSinceStartup - phaseTime < 1.0) return;
                    Require(!host.targetText.isTextTruncated, "Selected magic name is truncated.");
                    Require(UnityEngine.Object.FindObjectsOfType<ServedObject>().Length > initialBodies,
                        "Field click did not create a production unit through HTTP.");
                    host.immuneAllyButton.onClick.Invoke();
                    phase = 3; phaseTime = EditorApplication.timeSinceStartup;
                    break;
                case 3:
                    if (EditorApplication.timeSinceStartup - phaseTime < 0.5) return;
                    Require(host.immuneAllyButton.GetComponentInChildren<TMP_Text>(true).text.Contains("계속"), "Server pause was not acknowledged.");
                    host.clearAllyButton.onClick.Invoke();
                    phase = 4; phaseTime = EditorApplication.timeSinceStartup;
                    break;
                case 4:
                    if (EditorApplication.timeSinceStartup - phaseTime < 1.0) return;
                    Require(host.clearAllyButton.GetComponentInChildren<TMP_Text>(true).text.Contains("수정안"), "Candidate was not selected.");
                    PlaygroundBuildGuard.Validate();
                    Capture(host, "Logs/balance-live.jpg");
                    host.immuneAllyButton.onClick.Invoke();
                    phase = 5; phaseTime = EditorApplication.timeSinceStartup;
                    break;
                case 5:
                    if (EditorApplication.timeSinceStartup - phaseTime < 1.0) return;
                    controller.ExportTo(SessionState.GetString(Key + ".replay", ""));
                    phase = 6;
                    break;
                case 6:
                    string replayPath = SessionState.GetString(Key + ".replay", "");
                    if (!File.Exists(replayPath)) return;
                    controller.Replay(File.ReadAllText(replayPath));
                    phase = 7; phaseTime = EditorApplication.timeSinceStartup;
                    break;
                case 7:
                    if (EditorApplication.timeSinceStartup - phaseTime < 1.0) return;
                    Require(host.timerText.text.Contains("재생"), "Exported recording did not replay.");
                    Require(UnityEngine.Object.FindObjectsOfType<ServedObject>().Length >= 5, "Replay prefabs were not rendered.");
                    Capture(host, "Logs/balance-replay.jpg");
                    Debug.Log("BALANCE_LAB_PLAYMODE_PASS: loopback HTTP, production rendering, field cast, mana, pause, candidate restart, export and replay.");
                    SessionState.EraseString(Key);
                    EditorApplication.update -= Check;
                    EditorApplication.isPlaying = false;
                    EditorApplication.delayCall += () => EditorApplication.Exit(0);
                    break;
            }
        } catch (Exception e) {
            Debug.LogException(e);
            SessionState.EraseString(Key);
            EditorApplication.Exit(1);
        }
    }

    private static void Capture(PlaygroundHost host, string path) {
        var camera = Camera.main;
        var canvas = host.magicPanel.GetComponentInParent<Canvas>();
        var previousMode = canvas.renderMode;
        var previousCamera = canvas.worldCamera;
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var render = new RenderTexture(1920, 1080, 24);
        var pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        try {
            camera.targetTexture = render;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = camera.nearClipPlane + 1;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = render;
            pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToJPG(85));
        } finally {
            canvas.renderMode = previousMode;
            canvas.worldCamera = previousCamera;
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(render);
            UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
}
