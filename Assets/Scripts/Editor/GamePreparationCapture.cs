using System;
using System.IO;
using System.Threading.Tasks;
using Data;
using GameScene;
using Global;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Settings;
using Object = UnityEngine.Object;

namespace Editor
{
    public static class GamePreparationCapture
    {
        public static async void Run()
        {
            int exit = 0;
            try
            {
                await LocalizationSettings.InitializationOperation.Task;
                Directory.CreateDirectory("docs/pr-media/465");
                foreach (string locale in new[] { "ko-KR", "en" })
                {
                    LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(locale);
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                    SceneContext.ClearAdventureMatch();
                    var match = new MatchedInfoDto {
                        sessionId = "preview-pvp", mapType = "RIVER",
                        leftUser = new User(1, "Arcane Caster", "", 0) { appearance = "default", matchMmr = 1234 },
                        rightUser = new User(-7, "Storm Mage", "", 0) { appearance = "storm", matchMmr = 1280 }
                    };
                    var overlay = GamePreparationOverlay.Show(match);
                    overlay.Render("loading");
                    await Settle();
                    Capture(overlay.GetComponent<Canvas>(), 1280, 720, $"docs/pr-media/465/{locale}-versus-unity.png");
                    overlay.Render("countdown", 3);
                    await Settle();
                    Capture(overlay.GetComponent<Canvas>(), 844, 390, $"docs/pr-media/465/{locale}-countdown-unity.png");
                    Object.DestroyImmediate(overlay.gameObject);
                    SceneContext.AdventureId = 1;
                    SceneContext.AdventureName = "Forest";
                    SceneContext.AdventureStageNumber = 2;
                    SceneContext.AdventureScenarioNumber = 1;
                    match.sessionId = "pve-preview";
                    match.mapType = "FOREST";
                    match.rightUser = new User(0, "dummy", "", 0);
                    overlay = GamePreparationOverlay.Show(match);
                    overlay.Render("applying");
                    await Settle();
                    Capture(overlay.GetComponent<Canvas>(), 1280, 720, $"docs/pr-media/465/{locale}-adventure-unity.png");
                    Object.DestroyImmediate(overlay.gameObject);
                }
                SceneContext.ClearAdventureMatch();
                Debug.Log("GAME_PREPARATION_CAPTURE_PASS");
            }
            catch (Exception error) { Debug.LogException(error); exit = 1; }
            EditorApplication.Exit(exit);
        }

        private static async Task Settle()
        {
            for (int i = 0; i < 12; i++)
            {
                var next = new TaskCompletionSource<bool>();
                EditorApplication.delayCall += () => next.SetResult(true);
                await next.Task;
            }
        }

        private static void Capture(Canvas canvas, int width, int height, string path)
        {
            var cameraObject = new GameObject("PreparationCaptureCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0, 0, -10);
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            foreach (var label in canvas.GetComponentsInChildren<TMP_Text>(true)) label.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var output = new Texture2D(width, height, TextureFormat.RGB24, false);
            output.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            output.Apply();
            File.WriteAllBytes(path, output.EncodeToPNG());
            RenderTexture.active = previous;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(output);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
        }
    }
}
