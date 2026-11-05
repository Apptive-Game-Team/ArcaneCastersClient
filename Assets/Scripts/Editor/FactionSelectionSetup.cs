using System;
using System.IO;
using System.Linq;
using TMPro;
using TutorialScene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Editor
{
    public static class FactionSelectionSetup
    {
        public const string ViewPath = "Assets/Prefabs/UI/Tutorial/FactionSelectionView.prefab";
        private const string ArtPath = "Assets/Art/Images/UI/FactionSelection/Environments.png";
        private const string ButtonPath = "Assets/Prefabs/UI/Button Variant.prefab";
        private const string SurfacePath = "Assets/Prefabs/UI/UI-Base.prefab";

        public static void Run()
        {
            try { Build(); Debug.Log("FACTION_SELECTION_SETUP_PASS"); EditorApplication.Exit(0); }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }

        [MenuItem("Tools/Tutorial/Build Faction Selection")]
        public static void Build()
        {
            ImportBackgrounds();
            Directory.CreateDirectory(Path.GetDirectoryName(ViewPath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var root = new GameObject("FactionSelectionView", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(FactionSelectionView));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 3501;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var view = root.GetComponent<FactionSelectionView>();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(ArtPath).OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();
            var backgrounds = sprites.Select(sprite => {
                var image = Artwork(sprite.name, root.transform, sprite, Vector2.zero, new Vector2(1280, 720));
                var fitter = image.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = 16f / 9;
                return image;
            }).ToArray();
            var hell = Group("HellArmy", root.transform, new Vector2(-320, -190));
            var nature = Group("WorldTreeArmy", root.transform, new Vector2(320, -190));
            var hellUnits = new[] {
                Unit("CraterCarrier", hell, new Vector2(-195, 20), new Vector2(145, 185), false),
                Unit("MagmaSpirit", hell, new Vector2(-35, 0), new Vector2(310, 350), false),
                Unit("FireSpirit", hell, new Vector2(135, -15), new Vector2(210, 165), false)
            };
            var natureUnits = new[] {
                Unit("TreeGolem", nature, new Vector2(170, 20), new Vector2(255, 355), true),
                Unit("VineSpirit", nature, new Vector2(-100, 0), new Vector2(160, 225), true),
                Unit("WaterSlime", nature, new Vector2(30, -25), new Vector2(165, 155), true)
            };
            var title = Label("Title", root.transform, new Vector2(0, -70), new Vector2(900, 80), 48);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(.5f, 1);
            title.color = Color.white;
            title.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Fonts/LilitaOne SDF Outline.mat");
            var regions = new UnityEngine.UI.Button[2];
            var names = new TMP_Text[2];
            var plates = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath), root.transform);
                go.name = i == 0 ? "HellRegion" : "WorldTreeRegion";
                var rect = go.GetComponent<RectTransform>();
                rect.localScale = Vector3.one;
                rect.anchorMin = new Vector2(i * .5f, 0);
                rect.anchorMax = new Vector2((i + 1) * .5f, 1);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                go.GetComponent<Image>().color = Color.clear;
                foreach (var localize in go.GetComponentsInChildren<UnityEngine.Localization.Components.LocalizeStringEvent>(true))
                    Object.DestroyImmediate(localize);
                var text = go.GetComponentInChildren<TMP_Text>(true);
                text.gameObject.SetActive(false);
                var plate = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SurfacePath), go.transform);
                plate.name = "Nameplate";
                var plateRect = plate.GetComponent<RectTransform>();
                plateRect.anchorMin = plateRect.anchorMax = new Vector2(.5f, 0);
                plateRect.pivot = new Vector2(.5f, 0);
                plateRect.localScale = Vector3.one;
                plateRect.anchoredPosition = new Vector2(0, 45);
                plateRect.sizeDelta = new Vector2(380, 76);
                plates[i] = plate.GetComponent<Image>();
                foreach (var shadow in plate.GetComponents<Shadow>()) shadow.enabled = false;
                plates[i].sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Images/UI/Flat/FlatButton.png");
                plates[i].color = Color.white;
                plates[i].raycastTarget = false;
                plates[i].pixelsPerUnitMultiplier = 2.5f;
                names[i] = Label("FactionName", plate.transform, Vector2.zero, new Vector2(350, 64), 34);
                names[i].enableAutoSizing = true;
                names[i].fontSizeMin = 24;
                names[i].fontSizeMax = 34;
                regions[i] = go.GetComponent<UnityEngine.UI.Button>();
                regions[i].transition = Selectable.Transition.None;
                regions[i].onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                var trigger = go.AddComponent<FactionSelectionRegion>();
                var triggerData = new SerializedObject(trigger);
                Set(triggerData, "view", view);
                triggerData.FindProperty("faction").intValue = i + 1;
                triggerData.ApplyModifiedPropertiesWithoutUndo();
            }
            for (int i = 0; i < 2; i++)
                regions[i].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnLeft = regions[1 - i], selectOnRight = regions[1 - i] };
            var data = new SerializedObject(view);
            Array(data, "backgrounds", backgrounds);
            Array(data, "armies", new[] { hell, nature });
            Array(data, "hellUnits", hellUnits);
            Array(data, "natureUnits", natureUnits);
            Array(data, "regions", regions);
            Array(data, "names", names);
            Array(data, "nameplates", plates);
            Set(data, "title", title);
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, ViewPath);
            Object.DestroyImmediate(root);
            EditorSceneManager.OpenScene(FactionTutorialSetup.SelectionPath);
            foreach (var old in Object.FindObjectsOfType<FactionSelectionView>(true)) Object.DestroyImmediate(old.gameObject);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ViewPath));
            instance.SetActive(false);
            var controller = Object.FindObjectOfType<FactionSelectionController>(true);
            var controllerData = new SerializedObject(controller);
            Set(controllerData, "selectionView", instance.GetComponent<FactionSelectionView>());
            controllerData.ApplyModifiedPropertiesWithoutUndo();
            if (!EditorSceneManager.SaveScene(controller.gameObject.scene, FactionTutorialSetup.SelectionPath))
                throw new IOException("Failed to save faction selection scene");
            FactionTutorialSetup.EnsureUiSceneCamera(FactionTutorialSetup.SelectionPath);
        }

        private static void ImportBackgrounds()
        {
            AssetDatabase.ImportAsset(ArtPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(ArtPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 4096;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            float band = height / 3f;
            importer.spritesheet = new[] { "0Neutral", "1Hell", "2WorldTree" }.Select((name, i) =>
                new SpriteMetaData { name = name, rect = new Rect(0, height - (i + 1) * band + band * .2f,
                    width, width * 9f / 16), pivot = new Vector2(.5f, .5f) }).ToArray();
            importer.SaveAndReimport();
        }

        private static RectTransform Group(string name, Transform parent, Vector2 position)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            return rect;
        }

        private static Image Unit(string name, RectTransform parent, Vector2 position, Vector2 size, bool flip)
        {
            var image = Artwork(name, parent, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Game/sprites/" + name + ".png"), position, size);
            image.rectTransform.pivot = new Vector2(.5f, 0);
            image.rectTransform.localScale = new Vector3(flip ? -1 : 1, 1, 1);
            return image;
        }

        private static Image Artwork(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
        {
            if (sprite == null) throw new InvalidOperationException("Missing sprite: " + name);
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text Label(string name, Transform parent, Vector2 position, Vector2 size, float fontSize)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(parent, false);
            label.rectTransform.anchoredPosition = position;
            label.rectTransform.sizeDelta = size;
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Fonts/LilitaOne SDF.asset");
            label.fontSize = fontSize;
            label.color = new Color32(28, 26, 43, 255);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        private static void Set(SerializedObject data, string field, Object value) => data.FindProperty(field).objectReferenceValue = value;
        private static void Array(SerializedObject data, string field, Object[] values)
        {
            var property = data.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
