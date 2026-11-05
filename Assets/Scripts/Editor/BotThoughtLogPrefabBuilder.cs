using System;
using GameScene;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BotThoughtLogPrefabBuilder {
    public const string PrefabPath = "Assets/Prefabs/UI/Debug/BotThoughtLog.prefab";
    private const string ButtonPath = "Assets/Prefabs/UI/Button Variant.prefab";
    private const string PanelPath = "Assets/Prefabs/UI/Brown-UI-Base.prefab";
    private static readonly Color Ink = new Color32(28, 26, 43, 255);

    // Editor render approximation for checking the authored layout; not a live-game capture.
    [MenuItem("Tools/Bot Thoughts/Capture Preview")]
    public static void CapturePreview() {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before capturing the bot log preview.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) throw new InvalidOperationException("Build the bot log prefab first.");
        Scene preview = EditorSceneManager.NewPreviewScene();
        RenderTexture target = null;
        RenderTexture previous = RenderTexture.active;
        try {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
            var controller = instance.GetComponent<BotThoughtLogUI>();
            controller.enabled = false;
            var fields = new SerializedObject(controller);
            var visible = (GameObject)fields.FindProperty("visibilityRoot").objectReferenceValue;
            var panel = (GameObject)fields.FindProperty("panel").objectReferenceValue;
            var toggle = (TMP_Text)fields.FindProperty("toggleLabel").objectReferenceValue;
            var title = (TMP_Text)fields.FindProperty("titleLabel").objectReferenceValue;
            var latest = (TMP_Text)fields.FindProperty("latestLabel").objectReferenceValue;
            var status = (TMP_Text)fields.FindProperty("statusLabel").objectReferenceValue;
            var log = (TMP_Text)fields.FindProperty("logText").objectReferenceValue;
            var scroll = (ScrollRect)fields.FindProperty("scrollRect").objectReferenceValue;
            visible.SetActive(true);
            panel.SetActive(true);
            toggle.text = "봇 생각 (100) 접기";
            title.text = "봇의 판단 기록";
            latest.text = "최신으로";
            status.text = "최근 100개 · 실시간";
            var samples = new[] {
                "상대가 중앙으로 접근하고 있어 방어 마법을 준비합니다.",
                "마나를 모아 다음 공격을 준비합니다. 아군과 거리를 유지합니다.",
                "적 유닛이 모여 있는 위치에 광역 마법을 사용합니다.",
                "현재 위치를 지키며 다음 판단을 기다립니다."
            };
            var text = new System.Text.StringBuilder();
            for (int i = 0; i < 100; i++) {
                if (i > 0) text.Append("\n\n");
                text.AppendFormat("[{0:00}:{1:00}] {2}\n{3}", i / 60, i % 60,
                    i % 2 == 0 ? "아군 봇" : "상대 봇", samples[i % samples.Length]);
            }
            log.richText = false;
            log.text = text.ToString();

            var cameraObject = new GameObject("BotThoughtLogPreviewCamera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, preview);
            var camera = cameraObject.GetComponent<Camera>();
            camera.scene = preview;
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 360;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(15, 36, 56, 255);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100;
            target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            target.Create();
            camera.targetTexture = target;
            var canvas = instance.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            // A target texture is independent of the Editor Game View's current resolution.
            var scaler = instance.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1;
            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text label in instance.GetComponentsInChildren<TMP_Text>(true))
                label.ForceMeshUpdate(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(log.rectTransform);
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0;
            Canvas.ForceUpdateCanvases();
            WritePreview(camera, target, "BotThoughtLog-preview.png");
            panel.SetActive(false);
            toggle.text = "봇 생각 (100) 펼치기";
            Canvas.ForceUpdateCanvases();
            toggle.ForceMeshUpdate();
            WritePreview(camera, target, "BotThoughtLog-preview-collapsed.png");
        } finally {
            RenderTexture.active = previous;
            EditorSceneManager.ClosePreviewScene(preview);
            if (target != null) {
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }

    private static void WritePreview(Camera camera, RenderTexture target, string fileName) {
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try {
            image.ReadPixels(new UnityEngine.Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", fileName));
            System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
            Debug.Log("Bot thought log editor preview: " + path);
        } finally {
            UnityEngine.Object.DestroyImmediate(image);
        }
    }

    [MenuItem("Tools/Bot Thoughts/Build Prefab")]
    public static void BuildPrefab() {
        var root = new GameObject("BotThoughtLog", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.layer = 5;
        try {
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var visible = Rect("Visibility", root.transform);
            Stretch(visible, Vector2.zero, Vector2.zero);

            Button toggle = MakeButton("Toggle", visible, "봇 생각", out TMP_Text toggleLabel);
            Position((RectTransform)toggle.transform, new Vector2(16, -70), new Vector2(180, 38));
            var panel = Instance(PanelPath, visible, "Panel");
            panel.AddComponent<BotThoughtLogPointerCapture>();
            Position((RectTransform)panel.transform, new Vector2(16, -114), new Vector2(440, 340));
            StyleSurface(panel.GetComponent<Image>(), "5fa9efed53cb40869b9ade226431f66c");
            foreach (var shadow in panel.GetComponents<Shadow>()) shadow.enabled = false;

            TMP_FontAsset bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Art/Fonts/Pretendard-Regular SDF.asset");
            if (bodyFont == null) throw new InvalidOperationException("Bot log body font is missing.");
            TMP_Text title = Label("Title", panel.transform, bodyFont, "봇의 판단 기록", 18);
            Position(title.rectTransform, new Vector2(16, -14), new Vector2(408, 26));

            var viewport = Rect("Viewport", panel.transform);
            Stretch(viewport, new Vector2(16, 58), new Vector2(-16, -48));
            var hitArea = viewport.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            hitArea.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            var capture = viewport.gameObject.AddComponent<BotThoughtLogPointerCapture>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 25;
            scroll.inertia = false;

            TMP_Text log = Label("Log", viewport, bodyFont, string.Empty, 17);
            var content = log.rectTransform;
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            log.enableWordWrapping = true;
            log.richText = false;
            log.overflowMode = TextOverflowModes.Overflow;
            log.paragraphSpacing = 10;
            var fitter = log.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;

            TMP_Text status = Label("Status", panel.transform, bodyFont, "생각을 기다리는 중", 15);
            status.rectTransform.anchorMin = Vector2.zero;
            status.rectTransform.anchorMax = Vector2.zero;
            status.rectTransform.pivot = Vector2.zero;
            status.rectTransform.anchoredPosition = new Vector2(16, 19);
            status.rectTransform.sizeDelta = new Vector2(240, 24);
            status.enableWordWrapping = false;
            status.overflowMode = TextOverflowModes.Ellipsis;
            Button latest = MakeButton("Latest", panel.transform, "최신으로", out TMP_Text latestLabel);
            var latestRect = (RectTransform)latest.transform;
            latestRect.anchorMin = new Vector2(1, 0);
            latestRect.anchorMax = new Vector2(1, 0);
            latestRect.pivot = new Vector2(1, 0);
            latestRect.anchoredPosition = new Vector2(-16, 14);
            latestRect.sizeDelta = new Vector2(142, 34);

            var controller = root.AddComponent<BotThoughtLogUI>();
            var serialized = new SerializedObject(controller);
            Bind(serialized, "visibilityRoot", visible.gameObject);
            Bind(serialized, "panel", panel);
            Bind(serialized, "toggleButton", toggle);
            Bind(serialized, "toggleLabel", toggleLabel);
            Bind(serialized, "titleLabel", title);
            Bind(serialized, "latestButton", latest);
            Bind(serialized, "latestLabel", latestLabel);
            Bind(serialized, "statusLabel", status);
            Bind(serialized, "logText", log);
            Bind(serialized, "scrollRect", scroll);
            Bind(serialized, "scrollCapture", capture);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            panel.SetActive(false);
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) {
                if (!PrefabUtility.IsPartOfPrefabInstance(child)) continue;
                PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
                foreach (Component component in child.GetComponents<Component>()) {
                    if (component != null) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        } finally {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [MenuItem("Tools/Bot Thoughts/Build Prefab And Wire Scenes")]
    public static void Build() {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before building the bot log UI.");
        for (int i = 0; i < SceneManager.sceneCount; i++) {
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save open scenes before wiring the bot log UI.");
        }
        BuildPrefab();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        foreach (string name in new[] { "GameScene", "SpectatingScene", "ResultScene" }) {
            string path = "Assets/Scenes/" + name + ".unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try {
                bool exists = false;
                foreach (GameObject root in scene.GetRootGameObjects()) {
                    if (root.GetComponentInChildren<BotThoughtLogUI>(true) != null) exists = true;
                }
                if (exists) continue;
                PrefabUtility.InstantiatePrefab(prefab, scene);
                EditorSceneManager.SaveScene(scene);
            } finally {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static GameObject Instance(string path, Transform parent, string name) {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) throw new InvalidOperationException("Missing UI prefab: " + path);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        instance.name = name;
        return instance;
    }

    private static Button MakeButton(string name, Transform parent, string text, out TMP_Text label) {
        var instance = Instance(ButtonPath, parent, name);
        var button = instance.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        instance.AddComponent<BotThoughtLogPointerCapture>();
        StyleSurface(instance.GetComponent<Image>(), "479a46a33fab413d8d1eaf3fd2081950");
        label = instance.GetComponentInChildren<TMP_Text>(true);
        label.text = text;
        label.fontSize = 20;
        label.fontSizeMin = 15;
        label.fontSizeMax = 20;
        label.enableAutoSizing = true;
        label.color = Ink;
        label.raycastTarget = false;
        return button;
    }

    private static void StyleSurface(Image image, string spriteGuid) {
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(spriteGuid));
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.pixelsPerUnitMultiplier = 2.5f;
    }

    private static RectTransform Rect(string name, Transform parent) {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.layer = 5;
        obj.transform.SetParent(parent, false);
        return (RectTransform)obj.transform;
    }

    private static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, string text, float size) {
        var rect = Rect(name, parent);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = Ink;
        label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.TopLeft;
        return label;
    }

    private static void Position(RectTransform rect, Vector2 position, Vector2 size) {
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect, Vector2 minimum, Vector2 maximum) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = minimum;
        rect.offsetMax = maximum;
    }

    private static void Bind(SerializedObject target, string name, UnityEngine.Object value) {
        var property = target.FindProperty(name);
        if (property == null) throw new InvalidOperationException("Missing bot log serialized field: " + name);
        property.objectReferenceValue = value;
    }
}
