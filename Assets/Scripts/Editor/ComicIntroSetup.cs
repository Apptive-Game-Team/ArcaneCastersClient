using System;
using System.IO;
using System.Linq;
using TMPro;
using TutorialScene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Editor
{
    public static class ComicIntroSetup
    {
        public const string ScenePath = "Assets/Scenes/ComicIntroScene.unity";
        public const string PrefabPath = "Assets/Prefabs/UI/Tutorial/ComicIntroPlayer.prefab";
        private const string PanelPrefabPath = "Assets/Prefabs/UI/Tutorial/ComicPanelView.prefab";
        private const string ArtPath = "Assets/Art/Images/Tutorial/Intro";
        private const string BasePath = "Assets/Prefabs/UI/UI-Base.prefab";
        private const string ButtonPath = "Assets/Prefabs/UI/Button Variant.prefab";

        private static readonly Color Ink = new Color32(28, 26, 43, 255);
        private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Fonts/Jua SDF.asset");

        [MenuItem("Tools/Tutorial/Build Motion Comic Assets")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Stop Play Mode before rebuilding comic assets.");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            for (int i = 1; i <= 7; i++)
            {
                string path = $"{ArtPath}/intro_{i:00}.png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.mipmapEnabled = false;
                importer.isReadable = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }

            // 새 애셋만 임시 씬에서 만들고, 사용자가 열어 둔 씬은 유지한다.
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                GameObject panelTemplate = BuildPanelTemplate();
                PrefabUtility.SaveAsPrefabAsset(panelTemplate, PanelPrefabPath);
                Object.DestroyImmediate(panelTemplate);

                RectTransform root = Rect("ComicIntroPlayer", null);
                Canvas canvas = root.gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 3500;
                CanvasScaler scaler = root.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                root.gameObject.AddComponent<GraphicRaycaster>();
                ComicIntroPlayer player = root.gameObject.AddComponent<ComicIntroPlayer>();

                RectTransform background = Surface("Background", root, new Color32(10, 12, 18, 255));
                Stretch(background);
                background.GetComponent<Image>().sprite = null;
                background.GetComponent<Image>().raycastTarget = true;
                Button advance = background.gameObject.AddComponent<Button>();
                advance.transition = Selectable.Transition.None;
                advance.navigation = new Navigation { mode = Navigation.Mode.None };
                advance.targetGraphic = background.GetComponent<Image>();

                RectTransform stage = Rect("ComicPage", root);
                stage.sizeDelta = new Vector2(1280, 720);
                GameObject[] pages = new GameObject[1];
                for (int i = 0; i < pages.Length; i++)
                {
                    RectTransform page = Rect($"Page{i + 1}", stage);
                    Stretch(page);
                    pages[i] = page.gameObject;
                }

                ComicPanelView[] panels = {
                    Panel(pages[0].transform, 0, 1, new Rect(24, 44, 600, 218)),
                    Panel(pages[0].transform, 0, 2, new Rect(640, 44, 288, 192)),
                    Panel(pages[0].transform, 0, 3, new Rect(944, 44, 312, 208)),
                    Panel(pages[0].transform, 0, 4, new Rect(368, 250, 240, 135)),
                    Panel(pages[0].transform, 0, 5, new Rect(24, 354, 600, 294)),
                    Panel(pages[0].transform, 0, 6, new Rect(640, 270, 616, 348)),
                    Panel(pages[0].transform, 0, 7, new Rect(1016, 488, 240, 160))
                };

                string[] keys = { "mage", "tree", "invasion", "attack", "destroy", "defend", "war", "continue" };
                int[] panelIndices = { 0, 1, 2, 3, 4, 4, 5, 6 };
                var serialized = new SerializedObject(player);
                BindArray(serialized, "pages", pages);
                BindArray(serialized, "panels", panels);
                SerializedProperty beats = serialized.FindProperty("beats");
                beats.arraySize = keys.Length;
                for (int i = 0; i < keys.Length; i++)
                {
                    SerializedProperty beat = beats.GetArrayElementAtIndex(i);
                    beat.FindPropertyRelative("panel").objectReferenceValue = panels[panelIndices[i]];
                    beat.FindPropertyRelative("captionIndex").intValue = i == 5 ? 1 : 0;
                    beat.FindPropertyRelative("localizationKey").stringValue = "onboarding.comic." + keys[i];
                }
                Button skip = MakeButton("Skip", stage, new Vector2(1150, 22), new Vector2(192, 38));
                TMP_Text skipLabel = skip.GetComponentInChildren<TMP_Text>();
                skipLabel.text = "Skip";
                TMP_Text next = Label("NextHint", stage, 22);
                Position(next.rectTransform, new Rect(722, 664, 524, 40));
                next.alignment = TextAlignmentOptions.MidlineRight;
                next.color = Color.white;
                TMP_Text progress = Label("PageProgress", stage, 22);
                Position(progress.rectTransform, new Rect(34, 664, 160, 40));
                progress.color = new Color32(242, 225, 183, 255);

                RectTransform fadeRect = Surface("Fade", root, Color.black);
                Stretch(fadeRect);
                fadeRect.GetComponent<Image>().sprite = null;
                CanvasGroup fade = fadeRect.gameObject.AddComponent<CanvasGroup>();
                fade.alpha = 0;
                fade.blocksRaycasts = false;
                Set(serialized, "advanceButton", advance);
                Set(serialized, "skipButton", skip);
                Set(serialized, "skipLabel", skipLabel);
                Set(serialized, "nextLabel", next);
                Set(serialized, "pageLabel", progress);
                Set(serialized, "fade", fade);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
                Object.DestroyImmediate(root.gameObject);
                PrefabUtility.InstantiatePrefab(prefab, scene);
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                EditorSceneManager.SaveScene(scene, ScenePath);
                if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
                {
                    EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
                }
                Debug.Log("Motion comic assets saved: " + ScenePath);
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject BuildPanelTemplate()
        {
            RectTransform root = Rect("ComicPanelView", null);
            root.sizeDelta = new Vector2(800, 360);
            ComicPanelView view = root.gameObject.AddComponent<ComicPanelView>();
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            RectTransform shadow = Surface("Shadow", root, Ink);
            Stretch(shadow);
            shadow.GetComponent<Image>().sprite = null;
            shadow.anchoredPosition = new Vector2(0, -6);
            RectTransform frame = Surface("Frame", root, new Color32(249, 238, 213, 255));
            Stretch(frame);
            frame.GetComponent<Image>().sprite = null;
            RectTransform viewport = Rect("Viewport", root);
            Stretch(viewport, 5);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform art = Rect("Artwork", viewport);
            Stretch(art);
            Image image = art.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            AspectRatioFitter fit = art.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            Image backdrop = viewport.gameObject.AddComponent<Image>();
            backdrop.color = Ink;
            backdrop.raycastTarget = false;

            CanvasGroup[] groups = new CanvasGroup[2];
            TMP_Text[] labels = new TMP_Text[2];
            for (int i = 0; i < 2; i++)
            {
                RectTransform caption = Surface("Caption" + i, root, new Color32(249, 238, 213, 255));
                caption.anchorMin = caption.anchorMax = new Vector2(i == 0 ? 0 : 1, i == 0 ? 1 : 0);
                caption.pivot = new Vector2(i == 0 ? 0 : 1, i == 0 ? 1 : 0);
                caption.anchoredPosition = i == 0 ? new Vector2(20, -18) : new Vector2(-20, 18);
                caption.sizeDelta = new Vector2(400, 84);
                groups[i] = caption.gameObject.AddComponent<CanvasGroup>();
                groups[i].blocksRaycasts = false;
                labels[i] = Label("Dialogue", caption, 22);
                Stretch(labels[i].rectTransform, 10);
                labels[i].color = Ink;
            }
            var serialized = new SerializedObject(view);
            Set(serialized, "panelGroup", group);
            Set(serialized, "artwork", art);
            BindArray(serialized, "captionGroups", groups);
            BindArray(serialized, "captionTexts", labels);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return root.gameObject;
        }

        private static ComicPanelView Panel(Transform parent, int page, int number, Rect bounds)
        {
            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath), parent);
            root.name = "Panel" + number;
            Position((RectTransform)root.transform, bounds);
            ComicPanelView view = root.GetComponent<ComicPanelView>();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtPath}/intro_{number:00}.png");
            Image image = root.transform.Find("Viewport/Artwork").GetComponent<Image>();
            image.sprite = sprite;
            image.GetComponent<AspectRatioFitter>().aspectRatio = sprite.rect.width / sprite.rect.height;
            var serialized = new SerializedObject(view);
            serialized.FindProperty("page").intValue = page;
            serialized.FindProperty("drift").vector2Value = new Vector2(number % 2 == 0 ? 5 : -5, 2);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            RectTransform caption = (RectTransform)root.transform.Find("Caption0");
            caption.sizeDelta = new Vector2(Mathf.Min(300, bounds.width - 24), 74);
            caption.anchoredPosition = new Vector2(12, -12);
            if (number == 1)
            {
                caption.anchorMin = caption.anchorMax = Vector2.one;
                caption.pivot = Vector2.one;
                caption.anchoredPosition = new Vector2(-12, -12);
            }
            if (number == 3)
            {
                caption.sizeDelta = new Vector2(280, 90);
                caption.anchorMin = caption.anchorMax = Vector2.zero;
                caption.pivot = Vector2.zero;
                caption.anchoredPosition = new Vector2(12, 12);
            }
            if (number == 4 || number == 7)
            {
                caption.anchorMin = caption.anchorMax = number == 4 ? Vector2.up : Vector2.one;
                caption.pivot = number == 4 ? Vector2.up : Vector2.one;
                caption.anchoredPosition = new Vector2(number == 4 ? 12 : -12, -12);
                caption.sizeDelta = new Vector2(140, 44);
                EnemyCaption(caption);
            }
            if (number == 5)
            {
                caption.sizeDelta = new Vector2(200, 64);
                caption.anchorMin = caption.anchorMax = Vector2.zero;
                caption.pivot = Vector2.zero;
                caption.anchoredPosition = new Vector2(12, 12);
                EnemyCaption(caption);
                RectTransform defender = (RectTransform)root.transform.Find("Caption1");
                defender.sizeDelta = new Vector2(230, 64);
                defender.anchorMin = defender.anchorMax = Vector2.one;
                defender.pivot = Vector2.one;
                defender.anchoredPosition = new Vector2(-12, -12);
            }
            return view;
        }

        private static void EnemyCaption(RectTransform caption)
        {
            caption.GetComponent<Image>().color = new Color32(60, 12, 22, 255);
            TMP_Text text = caption.GetComponentInChildren<TMP_Text>();
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = text.fontSizeMax = 22;
            Outline outline = caption.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(240, 68, 58, 255);
            outline.effectDistance = new Vector2(2, -2);
        }

        private static RectTransform Surface(string name, Transform parent, Color color)
        {
            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BasePath), parent);
            root.name = name;
            root.transform.localScale = Vector3.one;
            Image image = root.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Images/UI/Flat/FlatCard.png");
            image.color = color;
            image.raycastTarget = false;
            image.pixelsPerUnitMultiplier = 2.5f;
            foreach (Shadow shadow in root.GetComponents<Shadow>()) shadow.enabled = false;
            return (RectTransform)root.transform;
        }

        private static Button MakeButton(string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath), parent);
            root.name = name;
            RectTransform rect = (RectTransform)root.transform;
            rect.localScale = Vector3.one;
            Position(rect, new Rect(position.x - size.x / 2, position.y - size.y / 2, size.x, size.y));
            Button button = root.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            TMP_Text label = root.GetComponentInChildren<TMP_Text>();
            label.fontSize = label.fontSizeMax = 20;
            return button;
        }

        private static TMP_Text Label(string name, Transform parent, float size)
        {
            RectTransform rect = Rect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = Font;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.enableWordWrapping = true;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16;
            label.fontSizeMax = size;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.layer = 5;
            RectTransform rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect, float inset = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void Position(RectTransform rect, Rect bounds)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(bounds.x, -bounds.y);
            rect.sizeDelta = bounds.size;
        }

        private static void Set(SerializedObject serialized, string name, Object value)
        {
            serialized.FindProperty(name).objectReferenceValue = value;
        }

        private static void BindArray(SerializedObject serialized, string name, Object[] values)
        {
            SerializedProperty array = serialized.FindProperty(name);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
