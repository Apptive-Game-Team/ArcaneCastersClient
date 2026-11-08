using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using TutorialScene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Editor
{
    public static class FactionTutorialUiCapture
    {
        [MenuItem("Tools/Tutorial/Capture Reward Review")]
        public static void CaptureRewardReview()
        {
            var previousScene = SceneManager.GetActiveScene();
            var previousLocale = LocalizationSettings.SelectedLocale;
            Scene temporary = default;
            GameObject root = null;
            Data.Magic.MagicInfoDataSource catalog = null;
            object previousMagics = null;
            var magicsField = typeof(Data.Magic.MagicInfoDataSource).GetField("magics", BindingFlags.Instance | BindingFlags.NonPublic);
            try
            {
                if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first");
                LocalizationSettings.InitializationOperation.WaitForCompletion();
                LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("ko-KR");
                foreach (string table in new[] { "Onboarding", "Magic", "MagicBook" })
                    LocalizationSettings.StringDatabase.GetTableAsync(table).WaitForCompletion();
                Directory.CreateDirectory("docs/pr-media/397");
                temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(temporary);
                catalog = Data.Magic.MagicInfoDataSource.Instance;
                previousMagics = magicsField.GetValue(catalog);
                // EditMode에서는 Awake가 캐시를 읽지 않으므로 저장된 카탈로그를 명시적으로 읽는다.
                typeof(Data.Magic.MagicInfoDataSource).GetMethod("LoadFromPlayerPrefs", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(catalog, null);
                root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FactionTutorialSetup.PanelPath));
                var panel = root.GetComponent<FactionTutorialPanel>();
                foreach (string faction in new[] { "HELL", "NATURE" })
                {
                    panel.Show("FactionTutorial.complete", "FactionTutorial.reward." + faction,
                        "FactionTutorial.lobby", () => { }, rewardFaction: faction);
                    Canvas.ForceUpdateCanvases();
                    if (faction == "NATURE")
                    {
                        var data = new SerializedObject(panel);
                        var icon = (Image)data.FindProperty("natureIcons").GetArrayElementAtIndex(4).objectReferenceValue;
                        var magic = Data.Magic.LocalCombinedMagicData.GetCombinedMagicData(FactionTutorialCards.Nature[4]);
                        if (magic == null) throw new InvalidOperationException("Reward magic catalog unavailable");
                        icon.transform.parent.GetComponent<TutorialRewardCard>().OnPointerClick(null);
                        Canvas.ForceUpdateCanvases();
                    }
                    Capture(root.GetComponent<Canvas>(), 1280, 720,
                        "docs/pr-media/397/tutorial-reward-" + faction.ToLowerInvariant() + ".jpg");
                }
                Debug.Log("FACTION_TUTORIAL_REWARD_CAPTURE_PASS");
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally
            {
                if (catalog != null) magicsField.SetValue(catalog, previousMagics);
                if (root != null) Object.DestroyImmediate(root);
                if (temporary.IsValid() && temporary.isLoaded) EditorSceneManager.CloseScene(temporary, true);
                if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
                LocalizationSettings.SelectedLocale = previousLocale;
            }
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
