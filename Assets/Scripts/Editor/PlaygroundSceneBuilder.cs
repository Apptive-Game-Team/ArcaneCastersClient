using System;
using System.IO;
using System.Linq;
using DevPlayground;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PlaygroundSceneBuilder {
    public const string ScenePath = "Assets/DevPlayground/Generated/Playground.unity";
    private const string PanelPrefab = "Assets/Prefabs/UI/Brown-UI-Base.prefab";
    private const string ButtonPrefab = "Assets/Prefabs/UI/Button Variant.prefab";
    private const string DropdownPrefab = "Assets/Prefabs/UI/Lobby/MatchingServerDropdown.prefab";
    private static readonly string[] MatchBehaviours = {
        "StompConnector", "PingSender", "FieldSelector", "GameSceneUIController",
        "CardInputSender", "BarController", "GameEndEventController", "PlayerFeedbackController",
        "DebugPanelController", "AdminViewModel", "UserMagicService", "GameCoachRuleProvider",
        "CoachDirector", "CoachHighlighter", "TutorialPanel", "FeverTimeController",
        "TimerController", "AdminOnly", "ModalActiveToggleButton"
    };

    [MenuItem("Tools/ArcaneCasters/Prepare Playground Scene")]
    public static void Generate() {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Prepare the playground before entering Play Mode.");
        var previous = SceneManager.GetActiveScene();
        var source = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Additive);
        try {
            SceneManager.SetActiveScene(source);
            foreach (var root in source.GetRootGameObjects()) {
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true)) canvas.enabled = false;
                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true)
                        .OrderBy(component => component == null ? -1 : Array.IndexOf(MatchBehaviours, component.GetType().Name))) {
                    if (behaviour == null || !MatchBehaviours.Contains(behaviour.GetType().Name)) continue;
                    // Disabled components still run Awake. Keep only the disabled raycast utility;
                    // ordinary network/match behaviours must not initialize in the playground.
                    if (behaviour.GetType().Name == "FieldSelector") behaviour.enabled = false;
                    else UnityEngine.Object.DestroyImmediate(behaviour);
                }
            }
            // Reserve sidebars while the existing aspect fitter keeps the whole normal world visible.
            var worldCamera = source.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)).First();
            worldCamera.rect = new Rect(0.2f, 0, 0.635f, 1);

            var controlRoot = new GameObject("Developer Playground");
            var controller = controlRoot.AddComponent<PlaygroundHost>();
            var canvasObject = new GameObject("Playground Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(controlRoot.transform, false);
            var canvasComponent = canvasObject.GetComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComponent.sortingOrder = 1000;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var left = InstantiateUi(PanelPrefab, canvasObject.transform, "Magic Panel");
            Rect(left.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(360, -32));
            var right = InstantiateUi(PanelPrefab, canvasObject.transform, "Control Panel");
            Rect(right.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(300, 530));

            var dropdown = InstantiateUi(DropdownPrefab, left.transform, "Caster Side");
            Rect(dropdown.GetComponent<RectTransform>(), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(310, 55));
            controller.sideDropdown = dropdown.GetComponent<TMP_Dropdown>();
            controller.sideDropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
            controller.sideDropdown.ClearOptions();
            controller.sideDropdown.AddOptions(new System.Collections.Generic.List<string> { "Left", "Right" });
            controller.sideDropdown.value = 0;

            var viewport = NewRect("Magic Viewport", left.transform);
            Rect(viewport, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(0, -38), new Vector2(-26, -118));
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            controller.iconContent = NewRect("Magic Icons", viewport);
            Rect(controller.iconContent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                Vector2.zero, Vector2.zero);
            var grid = controller.iconContent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(102, 100);
            grid.spacing = new Vector2(6, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.padding = new RectOffset(5, 5, 5, 5);
            var fitter = controller.iconContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = controller.iconContent;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30;

            var template = MakeButton(left.transform, "Magic Template", "");
            template.gameObject.SetActive(false);
            template.GetComponent<RectTransform>().sizeDelta = new Vector2(102, 100);
            var label = template.GetComponentInChildren<TMP_Text>(true);
            label.fontSize = 12;
            label.enableAutoSizing = true;
            label.fontSizeMin = 8;
            label.fontSizeMax = 12;
            Rect(label.rectTransform, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(-6, 28));
            var icon = NewRect("Icon", template.transform);
            Rect(icon, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(62, 62));
            var image = icon.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            controller.iconTemplate = template;

            controller.clearAllyButton = ControlButton(right.transform, "Clear ally units", -70);
            controller.clearEnemyButton = ControlButton(right.transform, "Clear enemy units", -135);
            controller.clearAllButton = ControlButton(right.transform, "Clear all units", -200);
            controller.immuneAllyButton = ControlButton(right.transform, "Ally invincible: OFF", -265);
            controller.immuneEnemyButton = ControlButton(right.transform, "Enemy invincible: OFF", -330);
            controller.closeButton = ControlButton(right.transform, "Close playground", -425);
            controller.timerText = LabelFrom(template, right.transform, "Remaining Time", "05:00",
                new Vector2(0, -18), new Vector2(270, 42), 26);
            controller.targetText = LabelFrom(template, left.transform, "Test Target", "Target: X 9, Z 5",
                new Vector2(0, -81), new Vector2(320, 25), 16);
            controller.statusText = LabelFrom(template, canvasObject.transform, "Status",
                "Click the field to set a target.", new Vector2(0, -15), new Vector2(1100, 54), 18);

            Directory.CreateDirectory("Assets/DevPlayground/Generated");
            if (!EditorSceneManager.SaveScene(source, ScenePath))
                throw new InvalidOperationException("Cannot save generated playground scene.");
            AssetDatabase.Refresh();
            PlaygroundBuildGuard.Validate();
            Debug.Log("Playground prepared. Enter from the Magic Playground button in AdminScene.");
        } finally {
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(source, true);
        }
    }

    public static void EnterFromAdmin() {
        if (!EditorApplication.isPlaying || Global.SceneContext.User == null ||
            string.IsNullOrEmpty(Global.SceneContext.JwtToken)) {
            EditorUtility.DisplayDialog("Developer playground",
                "Log in as an administrator before entering the playground.", "OK");
            return;
        }
        if (SceneManager.GetActiveScene().name != "AdminScene") {
            Debug.LogWarning("Enter the playground from AdminScene.");
            return;
        }
        if (!File.Exists(ScenePath)) {
            Debug.LogError("Playground preparation failed. Stop and restart Play Mode.");
            return;
        }
        EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
    }

    public static void ValidateGeneratedScene() {
        Generate();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try {
            var controller = scene.GetRootGameObjects().SelectMany(root =>
                root.GetComponentsInChildren<PlaygroundHost>(true)).Single();
            if (controller.sideDropdown == null || controller.iconContent == null ||
                controller.iconTemplate == null || controller.clearAllyButton == null ||
                controller.clearEnemyButton == null || controller.clearAllButton == null ||
                controller.immuneAllyButton == null || controller.immuneEnemyButton == null ||
                controller.closeButton == null || controller.statusText == null ||
                controller.timerText == null || controller.targetText == null)
                throw new InvalidOperationException("Playground UI is not wired.");
            foreach (var root in scene.GetRootGameObjects())
                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (behaviour != null && MatchBehaviours.Contains(behaviour.GetType().Name) &&
                        (behaviour.GetType().Name != "FieldSelector" || behaviour.enabled))
                        throw new InvalidOperationException("Match behaviour still enabled: " + behaviour.GetType().Name);
            Debug.Log("PLAYGROUND_VALIDATION_PASS");
        } finally { EditorSceneManager.CloseScene(scene, true); }
    }

    private static GameObject InstantiateUi(string path, Transform parent, string name) {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) throw new InvalidOperationException("Missing UI prefab: " + path);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true)) {
            if (behaviour == null) continue;
            string ns = behaviour.GetType().Namespace ?? "";
            if (!ns.StartsWith("UnityEngine.UI", StringComparison.Ordinal) &&
                !ns.StartsWith("TMPro", StringComparison.Ordinal)) UnityEngine.Object.DestroyImmediate(behaviour);
        }
        return instance;
    }

    internal static Button MakeButton(Transform parent, string name, string text) {
        var instance = InstantiateUi(ButtonPrefab, parent, name);
        var button = instance.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        var label = instance.GetComponentInChildren<TMP_Text>(true);
        label.gameObject.SetActive(true);
        label.text = text;
        label.fontSize = 20;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12;
        label.fontSizeMax = 20;
        return button;
    }
    private static Button ControlButton(Transform parent, string text, float y) {
        var button = MakeButton(parent, text, text);
        Rect(button.GetComponent<RectTransform>(), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(270, 54));
        return button;
    }
    private static TMP_Text LabelFrom(Button template, Transform parent, string name, string text,
            Vector2 position, Vector2 size, float fontSize) {
        var source = template.GetComponentInChildren<TMP_Text>(true);
        var label = UnityEngine.Object.Instantiate(source, parent);
        label.name = name;
        label.gameObject.SetActive(true);
        label.text = text;
        label.fontSize = fontSize;
        label.enableAutoSizing = false;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        Rect(label.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0.5f, 1), position, size);
        return label;
    }
    private static RectTransform NewRect(string name, Transform parent) {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }
    private static void Rect(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot,
            Vector2 position, Vector2 size) {
        rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot;
        rect.anchoredPosition = position; rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }
}
