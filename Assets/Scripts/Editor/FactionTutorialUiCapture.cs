using System;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using TutorialScene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Editor
{
    public static class FactionTutorialUiCapture
    {
        public static async void RunCompact()
        {
            int exit = 0;
            try
            {
                await LocalizationSettings.InitializationOperation.Task;
                Directory.CreateDirectory("docs/pr-media/413");
                foreach (string locale in new[] { "ko-KR", "en" })
                {
                    LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(locale);
                    foreach (var size in new[] { (1280, 720), (862, 478), (1024, 768) })
                    {
                        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FactionTutorialSetup.PanelPath));
                        var panel = go.GetComponent<FactionTutorialPanel>();
                        foreach (string stage in new[] { "shot", "meteor", "tornado", "anti_air", "formation", "explosion" })
                        {
                            panel.Show("FactionTutorial.success", "FactionTutorial.success." + stage, "FactionTutorial.next", () => { });
                            for (int i = 0; i < 15; i++) await NextUpdate();
                            string path = $"docs/pr-media/413/tutorial-{stage}-{locale}-{size.Item1}x{size.Item2}.jpg";
                            Capture(go.GetComponent<Canvas>(), size.Item1, size.Item2, path);
                            foreach (var text in go.GetComponentsInChildren<TMP_Text>())
                                if (string.IsNullOrWhiteSpace(text.text) || text.isTextOverflowing)
                                    throw new Exception($"Tutorial text does not fit: {locale}/{stage}/{text.name}");
                        }
                        Object.DestroyImmediate(go);
                    }
                }
                Debug.Log("FACTION_TUTORIAL_COMPACT_CAPTURE_PASS");
            }
            catch (Exception failure) { Debug.LogException(failure); exit = 1; }
            EditorApplication.Exit(exit);
        }

        public static async void Run()
        {
            int exit = 0;
            try
            {
                await LocalizationSettings.InitializationOperation.Task;
                Directory.CreateDirectory("docs/pr-media/397");
                foreach (var entry in new[] { ("ko-KR", 1280, 720), ("en", 1024, 768) })
                {
                    LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(entry.Item1);
                    string title = await LocalizationSettings.StringDatabase.GetLocalizedStringAsync("Onboarding", "FactionTutorial.title").Task;
                    if (string.IsNullOrWhiteSpace(title) || title.Contains("No translation")) throw new Exception("Tutorial localization missing");
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FactionTutorialSetup.PanelPath));
                    var panel = go.GetComponent<FactionTutorialPanel>();
                    panel.Show("FactionTutorial.title", "FactionTutorial.choose", null, null, () => { }, () => { });
                    var data = new SerializedObject(panel);
                    foreach (var faction in new[] { ("hellIcons", FactionTutorialCards.Hell), ("natureIcons", FactionTutorialCards.Nature) })
                    {
                        var icons = data.FindProperty(faction.Item1);
                        for (int i = 0; i < icons.arraySize; i++)
                            ((Image)icons.GetArrayElementAtIndex(i).objectReferenceValue).sprite = Resources.Load<Sprite>(
                                "Game/sprites/" + Data.Util.StringUtils.ToPascalCase(faction.Item2[i]));
                    }
                    for (int i = 0; i < 15; i++) await NextUpdate();
                    Capture(go.GetComponent<Canvas>(), entry.Item2, entry.Item3,
                        $"docs/pr-media/397/faction-selection-{entry.Item1}.jpg");
                    panel.Show("FactionTutorial.complete", "FactionTutorial.reward.HELL", "FactionTutorial.lobby", () => { }, rewardFaction: "HELL");
                    var rewardIcons = data.FindProperty("hellIcons");
                    for (int i = 0; i < rewardIcons.arraySize; i++)
                        ((Image)rewardIcons.GetArrayElementAtIndex(i).objectReferenceValue).sprite = Resources.Load<Sprite>(
                            "Game/sprites/" + Data.Util.StringUtils.ToPascalCase(FactionTutorialCards.Hell[i]));
                    for (int i = 0; i < 15; i++) await NextUpdate();
                    Capture(go.GetComponent<Canvas>(), entry.Item2, entry.Item3,
                        $"docs/pr-media/397/tutorial-complete-{entry.Item1}.jpg");
                    Object.DestroyImmediate(go);
                }
                Debug.Log("FACTION_TUTORIAL_UI_CAPTURE_PASS");
            }
            catch (Exception failure) { Debug.LogException(failure); exit = 1; }
            EditorApplication.Exit(exit);
        }

        private static Task NextUpdate()
        {
            var result = new TaskCompletionSource<bool>();
            EditorApplication.delayCall += () => result.SetResult(true);
            return result.Task;
        }

        private static void Capture(Canvas canvas, int width, int height, string path)
        {
            var cameraObject = new GameObject("TutorialCaptureCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(28, 26, 43, 255);
            camera.orthographic = true;
            camera.orthographicSize = 5;
            cameraObject.transform.position = new Vector3(0, 0, -10);
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            foreach (var text in canvas.GetComponentsInChildren<TMP_Text>(true)) text.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToJPG(85));
            RenderTexture.active = previous;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
        }
    }
}
