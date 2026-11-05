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
    public static class FactionTutorialSetup
    {
        public const string PanelPath = "Assets/Resources/UI/Tutorial/FactionTutorialPanel.prefab";
        public const string SelectionPath = "Assets/Scenes/FactionSelectionScene.unity";
        public const string BattlePath = "Assets/Scenes/FactionTutorialBattleScene.unity";
        private const string SurfacePath = "Assets/Prefabs/UI/UI-Base.prefab";
        private const string ButtonPath = "Assets/Prefabs/UI/Button Variant.prefab";
        private static readonly Color Ink = new Color32(28, 26, 43, 255);
        private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Fonts/LilitaOne SDF.asset");

        [MenuItem("Tools/Tutorial/Build Faction Tutorial")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first");
            Directory.CreateDirectory(Path.GetDirectoryName(PanelPath));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("FactionTutorialPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 3500;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var background = Surface("Background", root.transform, Vector2.zero, new Vector2(800.01f, 605.9091f));
            var backgroundImage = background.GetComponent<Image>();
            backgroundImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Images/Background/background.png");
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = true;
            var backdrop = Surface("Panel", root.transform, Vector2.zero, new Vector2(1180, 650));
            backdrop.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Images/UI/Flat/FlatCard.png");
            backdrop.GetComponent<Image>().color = Color.white;
            var panel = root.AddComponent<FactionTutorialPanel>();
            var title = Label("Title", backdrop.transform, new Vector2(0, 250), new Vector2(1080, 70), 40);
            var body = Label("Description", backdrop.transform, new Vector2(0, 120), new Vector2(1080, 180), 24);
            body.enableAutoSizing = true;
            body.fontSizeMin = 19;
            body.fontSizeMax = 24;
            var hell = Button("InfernoStone", backdrop.transform, new Vector2(-280, -160), new Vector2(520, 70), true);
            var nature = Button("NatureWater", backdrop.transform, new Vector2(280, -160), new Vector2(520, 70), false);
            var next = Button("Continue", backdrop.transform, new Vector2(0, -230), new Vector2(600, 70), true);
            var hellIcons = Icons("Inferno", backdrop.transform, -490);
            var natureIcons = Icons("Nature", backdrop.transform, 70);
            var serialized = new SerializedObject(panel);
            Set(serialized, "title", title);
            Set(serialized, "body", body);
            Set(serialized, "hell", hell);
            Set(serialized, "nature", nature);
            Set(serialized, "next", next);
            SetArray(serialized, "hellIcons", hellIcons);
            SetArray(serialized, "natureIcons", natureIcons);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PanelPath);
            Object.DestroyImmediate(root);

            var selectionPanel = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath));
            var selection = selectionPanel.AddComponent<FactionSelectionController>();
            var selectionData = new SerializedObject(selection);
            Set(selectionData, "panel", selectionPanel.GetComponent<FactionTutorialPanel>());
            selectionData.ApplyModifiedPropertiesWithoutUndo();
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            EditorSceneManager.SaveScene(scene, SelectionPath);

            scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
            // 전용 튜토리얼은 기존 표현·입력만 재사용하고 정상 경기 연결은 열지 않는다.
            foreach (var component in Object.FindObjectsOfType<MonoBehaviour>(true))
            {
                string type = component.GetType().Name;
                if (type == "StompConnector" || type == "PingSender" || type == "FeverTimeController") Object.DestroyImmediate(component);
            }
            foreach (var menu in Object.FindObjectsOfType<GameScene.GameMenu>(true)) menu.gameObject.SetActive(false);
            var battlePanel = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath));
            var driver = new GameObject("FactionTutorialBattle").AddComponent<FactionTutorialBattleDriver>();
            var driverData = new SerializedObject(driver);
            Set(driverData, "panel", battlePanel.GetComponent<FactionTutorialPanel>());
            driverData.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, BattlePath, true);

            var settings = EditorBuildSettings.scenes.ToList();
            foreach (string path in new[] { SelectionPath, BattlePath })
                if (!settings.Any(entry => entry.path == path)) settings.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = settings.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("FACTION_TUTORIAL_ASSETS_PASS");
        }

        private static RectTransform Surface(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SurfacePath), parent);
            go.name = name;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = Color.white;
            return rect;
        }

        private static TMP_Text Label(string name, Transform parent, Vector2 position, Vector2 size, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            label.font = Font;
            label.fontSize = fontSize;
            label.color = Ink;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.rectTransform.anchoredPosition = position;
            label.rectTransform.sizeDelta = size;
            label.text = "";
            return label;
        }

        private static UnityEngine.UI.Button Button(string name, Transform parent, Vector2 position, Vector2 size, bool primary)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath), parent);
            go.name = name;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Images/UI/Flat/FlatButton.png");
            image.type = Image.Type.Sliced;
            image.color = primary ? new Color32(255, 154, 31, 255) : Color.white;
            foreach (var localized in go.GetComponentsInChildren<UnityEngine.Localization.Components.LocalizeStringEvent>(true))
                Object.DestroyImmediate(localized);
            var label = go.GetComponentInChildren<TMP_Text>(true);
            label.font = Font;
            label.fontSize = 26;
            label.enableAutoSizing = true;
            label.fontSizeMin = 22;
            label.fontSizeMax = 26;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(18, 5);
            label.rectTransform.offsetMax = new Vector2(-18, -5);
            label.color = Ink;
            label.text = "";
            var button = go.GetComponent<UnityEngine.UI.Button>();
            button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            return button;
        }

        private static Image[] Icons(string prefix, Transform parent, float start)
        {
            var icons = new Image[5];
            for (int i = 0; i < icons.Length; i++)
            {
                var frame = Surface(prefix + "Card" + i, parent, new Vector2(start + i * 105, -40), new Vector2(90, 90));
                frame.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Images/UI/Flat/FlatTile.png");
                var go = new GameObject("Artwork", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(frame, false);
                var rect = go.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(76, 76);
                icons[i] = go.GetComponent<Image>();
                icons[i].preserveAspect = true;
                icons[i].raycastTarget = false;
            }
            return icons;
        }

        private static void Set(SerializedObject data, string name, Object value) => data.FindProperty(name).objectReferenceValue = value;
        private static void SetArray(SerializedObject data, string name, Object[] values)
        {
            var property = data.FindProperty(name);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
