using System;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using TutorialScene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Settings;
using Object = UnityEngine.Object;

namespace Editor
{
    public static class FactionSelectionCapture
    {
        public static async void Run()
        {
            int exit = 0;
            try
            {
                FactionSelectionSetup.Build();
                await LocalizationSettings.InitializationOperation.Task;
                Directory.CreateDirectory(".work/captures");
                foreach (var locale in new[] { "ko-KR", "en" })
                {
                    LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(locale);
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                    var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FactionSelectionSetup.ViewPath));
                    var view = root.GetComponent<FactionSelectionView>();
                    view.Show(() => { }, () => { });
                    for (int i = 0; i < 20; i++) await Next();
                    for (int state = 0; state < 3; state++)
                    {
                        view.Preview(state);
                        typeof(FactionSelectionView).GetMethod("Apply", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            .Invoke(view, new object[] { 1f });
                        Capture(root.GetComponent<Canvas>(), 1280, 720, ".work/captures/" + locale + "-" + state + ".png");
                    }
                    Capture(root.GetComponent<Canvas>(), 1024, 768, ".work/captures/" + locale + "-4x3.png");
                    if (locale == "ko-KR")
                    {
                        Directory.CreateDirectory(".work/frames");
                        int frame = 0;
                        foreach (int state in new[] { 0, 1, 2, 0 })
                        {
                            view.Preview(state);
                            for (int i = 0; i < 16; i++)
                            {
                                typeof(FactionSelectionView).GetMethod("Apply", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                    .Invoke(view, new object[] { 1 - Mathf.Exp(-12f / 20) });
                                Capture(root.GetComponent<Canvas>(), 640, 360, ".work/frames/frame-" + frame.ToString("D3") + ".png");
                                frame++;
                            }
                        }
                    }
                    Object.DestroyImmediate(root);
                }
                Debug.Log("FACTION_SELECTION_CAPTURE_PASS");
            }
            catch (Exception error) { Debug.LogException(error); exit = 1; }
            EditorApplication.Exit(exit);
        }

        private static Task Next()
        {
            var completion = new TaskCompletionSource<bool>();
            EditorApplication.delayCall += () => completion.SetResult(true);
            return completion.Task;
        }

        private static void Capture(Canvas canvas, int width, int height, string path)
        {
            var go = new GameObject("CaptureCamera", typeof(Camera));
            var camera = go.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            go.transform.position = new Vector3(0, 0, -10);
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
            Object.DestroyImmediate(go);
        }
    }
}
