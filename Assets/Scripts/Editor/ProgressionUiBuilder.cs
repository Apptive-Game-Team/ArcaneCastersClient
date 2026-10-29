using System;
using System.IO;
using System.Linq;
using LobbyScene;
using RewardChest;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// 승인된 로비/레벨 보상 배치를 기존 UI 프리팹 인스턴스로 작성한다.
public static class ProgressionUiBuilder {
    private const string Folder = "Assets/Resources/UI/Progression/";
    private const string Base = "Assets/Prefabs/UI/UI-Base.prefab";
    private const string ButtonPath = "Assets/Prefabs/UI/Button Variant.prefab";
    private static readonly Color Ink = new Color32(28, 26, 43, 255);
    private static readonly Color Green = new Color32(91, 208, 76, 255);
    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Fonts/LilitaOne SDF.asset");
    private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Images/UI/Flat/{name}.png");

    [MenuItem("Tools/Progression/Build UI")]
    public static void Build() {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + "Gold.png");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 256;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        BuildRow();
        BuildTile(false);
        BuildTile(true);
        var root = new GameObject("PlayerProgressionView", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.layer = 5;
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 18;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var view = root.AddComponent<PlayerProgressionView>();
        var fields = new SerializedObject(view);
        var hud = Rect("Hud", root.transform); Stretch(hud);
        Ref(fields, "hud", hud.gameObject);
        // 프로필 이름과 아바타는 로비의 기존 객체가 계속 담당한다.
        Ref(fields, "levelText", Text("Level", hud, "", 18, 210, 27, 95, 25));
        var xp = Progress(hud, "Xp", 88, 56, 205, 13);
        Ref(fields, "xpFill", xp);
        Ref(fields, "xpText", Text("XpValue", hud, "", 13, 88, 72, 205, 20));
        var gold = Panel("Gold", hud, 1024, 20, 165, 52, "FlatPill");
        Icon("Coin", gold, AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "Gold.png"), 2, 0, 60, 50);
        Ref(fields, "goldText", Text("Value", gold, "", 23, 62, 7, 85, 32));
        Button("Rewards", hud, "ProgressionRewards", 24, 106, 245, 46, view.OpenRewards);
        var chests = Button("Chests", hud, null, 115, 622, 360, 76, view.OpenChests);
        chests.GetComponentInChildren<TMP_Text>(true).gameObject.SetActive(false);
        RewardSpriteResolver.TryResolveChestIcon("progression_chest", out var chestSprite);
        Icon("Chest", chests.transform, chestSprite, 12, 4, 76, 64);
        Ref(fields, "chestText", Text("Count", chests.transform, "", 23, 100, 20, 240, 36));

        var page = Rect("LevelRewardsPage", root.transform); Stretch(page);
        var bg = page.gameObject.AddComponent<Image>(); bg.color = new Color32(15, 36, 56, 255);
        Ref(fields, "page", page.gameObject);
        var back = Button("Back", page, null, 30, 20, 54, 50, view.CloseRewards);
        back.GetComponent<Image>().color = new Color32(255, 154, 31, 255);
        back.GetComponentInChildren<TMP_Text>(true).gameObject.SetActive(false);
        Icon("Arrow", back.transform, Sprite("IconBack"), 10, 9, 32, 30);
        var title = Text("Title", page, "", 40, 108, 22, 550, 50); title.color = Color.white; Localize(title, "ProgressionRewards");
        var pageGold = Panel("Gold", page, 1080, 22, 165, 48, "FlatPill");
        Icon("Coin", pageGold, AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "Gold.png"), 2, 0, 56, 44);
        Ref(fields, "pageGoldText", Text("Value", pageGold, "", 23, 60, 7, 90, 30));
        var summary = Panel("Summary", page, 30, 90, 1220, 76);
        Ref(fields, "pagePlayerText", Text("Player", summary, "", 23, 24, 17, 300, 35));
        Ref(fields, "pageXpFill", Progress(summary, "Xp", 338, 25, 340, 22));
        Ref(fields, "pageXpText", Text("XpValue", summary, "", 20, 698, 20, 200, 30));
        Ref(fields, "factionText", Text("Faction", summary, "", 18, 926, 18, 270, 36));
        var list = Panel("Levels", page, 30, 182, 405, 508);
        Localize(Text("Title", list, "", 25, 24, 18, 355, 38), "ProgressionRewards");
        var viewport = Rect("Viewport", list); Position(viewport, 16, 64, 373, 423);
        viewport.gameObject.AddComponent<RectMask2D>();
        var hit = viewport.gameObject.AddComponent<Image>(); hit.color = Color.clear;
        var content = Rect("Content", viewport); Position(content, 0, 0, 373, 0);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 4;
        layout.childControlWidth = true; layout.childControlHeight = false; layout.childForceExpandHeight = false;
        var fit = content.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.content = content; scroll.viewport = viewport;
        scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
        Ref(fields, "levelScroll", scroll); Ref(fields, "rowsRoot", content);
        Ref(fields, "rowPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "ProgressionRewardRow.prefab").GetComponent<ProgressionRewardRow>());
        var detail = Panel("Detail", page, 450, 182, 800, 508);
        Ref(fields, "detailTitle", Text("Title", detail, "", 32, 30, 22, 420, 45));
        Ref(fields, "detailState", Text("State", detail, "", 23, 490, 28, 275, 35));
        Localize(Text("Fixed", detail, "", 23, 30, 83, 650, 35), "ProgressionFixed");
        var rewards = Rect("Rewards", detail); Position(rewards, 30, 132, 740, 270);
        var grid = rewards.gameObject.AddComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(222, 124);
        grid.spacing = new Vector2(24, 15); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
        Ref(fields, "rewardsRoot", rewards);
        Ref(fields, "tilePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "ProgressionRewardTile.prefab").GetComponent<RewardTileView>());
        Ref(fields, "largeTilePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "ProgressionRewardTileLarge.prefab").GetComponent<RewardTileView>());
        Ref(fields, "slotsText", Text("Slots", detail, "", 18, 30, 410, 460, 60));
        Button("Chests", detail, "ProgressionViewChests", 535, 429, 235, 48, view.OpenChests);
        var status = Text("Status", detail, "", 21, 30, 123, 720, 55);
        Ref(fields, "statusText", status);
        Ref(fields, "retryButton", Button("Retry", detail, "ProgressionRetryButton", 30, 190, 180, 44, view.RetryRewards));
        Ref(fields, "chestApi", root.AddComponent<ChestApiClient>());
        fields.ApplyModifiedPropertiesWithoutUndo();
        hud.gameObject.SetActive(false); page.gameObject.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(root, Folder + "PlayerProgressionView.prefab");
        Object.DestroyImmediate(root);
        UpdateLobby();
        AssetDatabase.SaveAssets();
        Debug.Log("PROGRESSION_UI_BUILD_COMPLETE");
    }

    private static void UpdateLobby() {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LobbyScene.unity");
        var pill = Object.FindObjectsOfType<RectTransform>(true).First(x => x.name == "UserNamePill");
        pill.GetComponent<HorizontalLayoutGroup>().enabled = false;
        pill.GetComponent<ContentSizeFitter>().enabled = false;
        // 로비의 1920x1080 기준으로 배치하며 기존 프로필과 친구 컴포넌트를 유지한다.
        Position(pill, 30, 30, 450, 120);
        var name = pill.GetComponentsInChildren<TMP_Text>(true).First();
        Position(name.rectTransform, 100, 8, 180, 38); name.fontSize = 28;
        name.enableAutoSizing = true; name.fontSizeMin = 18; name.fontSizeMax = 28;
        Position((RectTransform)pill.Find("Avatar"), 10, 18, 78, 78);
        var server = Object.FindObjectsOfType<MonoBehaviour>(true).FirstOrDefault(x => x.GetType().Name == "MatchingServerDropdown");
        if (server != null) ((RectTransform)server.transform).anchoredPosition = new Vector2(-744, 255);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
    }

    private static void BuildRow() {
        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath));
        root.name = "ProgressionRewardRow"; Position((RectTransform)root.transform, 0, 0, 373, 50);
        Clean(root); Style(root.GetComponent<Image>(), "FlatButton");
        root.GetComponent<Image>().sprite = null;
        root.GetComponentInChildren<TMP_Text>(true).gameObject.SetActive(false);
        var row = root.AddComponent<ProgressionRewardRow>(); var so = new SerializedObject(row);
        Ref(so, "levelText", Text("Level", root.transform, "", 20, 12, 9, 90, 30));
        Ref(so, "stateText", Text("State", root.transform, "", 15, 260, 11, 100, 27));
        RewardSpriteResolver.TryResolveChestIcon("progression_chest", out var sprite);
        Ref(so, "chestIcon", Icon("Chest", root.transform, sprite, 115, 4, 45, 40));
        Ref(so, "magicIcon", Icon("Magic", root.transform, null, 178, 4, 45, 40));
        Ref(so, "background", root.GetComponent<Image>()); Ref(so, "button", root.GetComponent<UnityEngine.UI.Button>());
        so.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(root, Folder + "ProgressionRewardRow.prefab"); Object.DestroyImmediate(root);
    }

    private static void BuildTile(bool large) {
        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/RewardChest/RewardTile.prefab"));
        root.name = large ? "ProgressionRewardTileLarge" : "ProgressionRewardTile";
        Position((RectTransform)root.transform, 0, 0, large ? 330 : 222, large ? 260 : 124);
        var tile = root.GetComponent<RewardTileView>(); var so = new SerializedObject(tile);
        var icon = (Image)so.FindProperty("iconImage").objectReferenceValue;
        Position(icon.rectTransform, 8, 2, 115, 94); icon.color = Color.white;
        var name = (TMP_Text)so.FindProperty("nameText").objectReferenceValue;
        Position(name.rectTransform, 5, 94, 212, 28); SetFont(name, 18); name.enableAutoSizing = true; name.fontSizeMin = 12; name.fontSizeMax = 18;
        var amount = (TMP_Text)so.FindProperty("amountText").objectReferenceValue;
        Position(amount.rectTransform, 143, 30, 70, 42); SetFont(amount, 28);
        if (large) {
            Position(icon.rectTransform, 40, 10, 250, 190);
            Position(name.rectTransform, 14, 207, 240, 42); name.fontSizeMax = 25; name.fontSize = 25;
            Position(amount.rectTransform, 264, 207, 60, 42); amount.fontSize = 28;
        }
        foreach (var image in root.GetComponentsInChildren<Image>(true)) if (image != icon) { image.sprite = null; image.color = new Color32(238, 243, 248, 255); }
        PrefabUtility.SaveAsPrefabAsset(root, Folder + root.name + ".prefab"); Object.DestroyImmediate(root);
    }

    private static RectTransform Rect(string name, Transform parent) {
        var go = new GameObject(name, typeof(RectTransform)); go.layer = 5; go.transform.SetParent(parent, false); return (RectTransform)go.transform;
    }
    private static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
    private static void Position(RectTransform r, float x, float y, float w, float h) {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); r.localScale = Vector3.one;
    }
    private static RectTransform Panel(string name, Transform parent, float x, float y, float w, float h, string sprite = "FlatCard") {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Base), parent);
        go.name = name; Clean(go); var r = (RectTransform)go.transform; Position(r, x, y, w, h); Style(go.GetComponent<Image>(), sprite); return r;
    }
    private static void Clean(GameObject go) { foreach (var shadow in go.GetComponentsInChildren<Shadow>(true)) shadow.enabled = false; foreach (var loc in go.GetComponentsInChildren<LocalizeStringEvent>(true)) Object.DestroyImmediate(loc); }
    private static void Style(Image image, string sprite) { image.sprite = Sprite(sprite); image.color = Color.white; image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 2.5f; }
    private static TMP_Text Text(string name, Transform parent, string value, float size, float x, float y, float w, float h) {
        var r = Rect(name, parent); Position(r, x, y, w, h); var t = r.gameObject.AddComponent<TextMeshProUGUI>(); SetFont(t, size); t.text = value; return t;
    }
    private static void SetFont(TMP_Text t, float size) { t.font = Font; t.fontSharedMaterial = Font.material; t.fontSize = size; t.color = Ink; t.alignment = TextAlignmentOptions.MidlineLeft; t.enableWordWrapping = false; t.raycastTarget = false; t.overflowMode = TextOverflowModes.Ellipsis; }
    private static Image Icon(string name, Transform parent, Sprite sprite, float x, float y, float w, float h) {
        var r = Rect(name, parent); Position(r, x, y, w, h); var i = r.gameObject.AddComponent<Image>(); i.sprite = sprite; i.preserveAspect = true; i.raycastTarget = false; return i;
    }
    private static Image Progress(Transform parent, string name, float x, float y, float w, float h) {
        var track = Panel(name, parent, x, y, w, h, "FlatChip");
        track.GetComponent<Image>().raycastTarget = false;
        var area = Rect("FillArea", track); Position(area, 4, 3, w - 8, h - 8);
        var fillRect = Rect("Fill", area); Stretch(fillRect);
        var fill = fillRect.gameObject.AddComponent<Image>(); fill.color = Green; fill.raycastTarget = false; return fill;
    }
    private static UnityEngine.UI.Button Button(string name, Transform parent, string key, float x, float y, float w, float h, UnityAction action) {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath), parent);
        go.name = name; Clean(go); Position((RectTransform)go.transform, x, y, w, h); Style(go.GetComponent<Image>(), "FlatButton");
        var label = go.GetComponentInChildren<TMP_Text>(true); SetFont(label, 22); label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true; label.fontSizeMin = 14; label.fontSizeMax = 22;
        if (key != null) Localize(label, key);
        var b = go.GetComponent<UnityEngine.UI.Button>(); b.onClick = new UnityEngine.UI.Button.ButtonClickedEvent(); UnityEventTools.AddPersistentListener(b.onClick, action); return b;
    }
    private static void Localize(TMP_Text text, string key) {
        var loc = text.gameObject.AddComponent<LocalizeStringEvent>(); loc.StringReference.SetReference("LobbyUI", key);
        var setter = (UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), text, typeof(TMP_Text).GetProperty("text").GetSetMethod());
        UnityEventTools.AddPersistentListener<string>(loc.OnUpdateString, setter);
    }
    private static void Ref(SerializedObject so, string name, Object value) { so.FindProperty(name).objectReferenceValue = value; }
}
