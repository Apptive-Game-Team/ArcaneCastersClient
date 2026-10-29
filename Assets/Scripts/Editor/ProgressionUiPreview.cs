using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Data;
using Global;
using LobbyScene;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// 네트워크 없이 실제 프리팹에 예시 계정 상태를 바인딩하는 검토용 렌더와 회귀 검사다.
public static class ProgressionUiPreview {
    private static FieldInfo Field(object o, string name) => o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
    private static T Read<T>(object o, string name) => (T)Field(o, name).GetValue(o);
    private static void Set(object o, string name, object value) => Field(o, name).SetValue(o, value);
    private static void Check(bool ok, string description) { if (!ok) throw new Exception(description); Debug.Log("PROGRESSION_CHECK " + description); }

    public static async void Capture() {
        try {
            await LocalizationSettings.InitializationOperation.Task;
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("ko-KR");
            EditorSceneManager.OpenScene("Assets/Scenes/LobbyScene.unity");
            foreach (var behaviour in Object.FindObjectsOfType<MonoBehaviour>(true))
                if (!(behaviour is LocalizeStringEvent) && behaviour.GetType().Assembly.GetName().Name == "Assembly-CSharp") behaviour.enabled = false;
            foreach (var dialog in Object.FindObjectsOfType<FriendMatchInviteDialog>(true)) dialog.Close();
            foreach (var r in Object.FindObjectsOfType<RectTransform>(true))
                if (r.name == "TutorialPanel" || r.name == "AdminButton" || r.name == "AdminOnly") r.gameObject.SetActive(false);
            foreach (var admin in Object.FindObjectsOfType<MonoBehaviour>(true).Where(x => x.GetType().Name == "AdminOnly")) admin.gameObject.SetActive(false);
            var lobby = Object.FindObjectOfType<LobbyUIController>(true);
            Read<TMP_Dropdown>(lobby, "deckDropdown").captionText.text = "기본 덱";
            var userName = Object.FindObjectOfType<LobbyUserNameUI>(true);
            SceneContext.User = new User { name = "마법사" };
            userName.SetUserName("마법사");
            Check(userName.transform.parent.gameObject.activeSelf, "기존 프로필 객체 유지");
            var view = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Progression/PlayerProgressionView.prefab")).GetComponent<PlayerProgressionView>();
            foreach (var b in view.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++) b.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
            foreach (var loc in view.GetComponentsInChildren<LocalizeStringEvent>(true)) {
                for (int i = 0; i < loc.OnUpdateString.GetPersistentEventCount(); i++) loc.OnUpdateString.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
                loc.RefreshString();
            }
            var state = new PlayerProgressionDto { faction = "NATURE", level = 10, levelXp = 125, nextLevelXp = 250, currency = 240 };
            var catalog = new ProgressionRewardsDto { faction = "NATURE", levels = Enumerable.Range(1, 50).Select(level => new ProgressionLevelRewardDto {
                level = level, chestCount = level == 1 ? 0 : 1, rewardSlots = level == 1 ? 0 : level == 50 ? 7 : level % 5 == 0 ? 6 : 3,
                cards = level == 1 ? new[] { "chain_lightning", "water_slime_swarm", "vine_spirit", "tree_golem", "water_explosion" }.Select(name => new ProgressionFixedRewardDto { magicName = name, amount = 3 }).ToArray() :
                    level % 5 == 0 ? new[] { new ProgressionFixedRewardDto { magicName = new[] { "wind_spirit", "tornado_strike", "life_tree", "aqua_archer", "grass_generator", "tide_call", "overgrowth", "storm_rider", "vine_world", "sea_serpent" }[level / 5 - 1], amount = 2 } } : Array.Empty<ProgressionFixedRewardDto>()
            }).ToArray() };
            Set(view, "catalog", catalog); Set(view, "chestCount", (int?)3);
            var rows = Read<List<ProgressionRewardRow>>(view, "rows");
            foreach (var reward in catalog.levels) {
                var row = Object.Instantiate(Read<ProgressionRewardRow>(view, "rowPrefab"), Read<RectTransform>(view, "rowsRoot"));
                row.Bind(reward, view.SelectLevel); rows.Add(row);
            }
            Set(view, "selectedLevel", 10);
            Read<TMP_Text>(view, "statusText").gameObject.SetActive(false);
            Read<UnityEngine.UI.Button>(view, "retryButton").gameObject.SetActive(false);
            view.Bind(state);
            await Task.Delay(1000);
            Directory.CreateDirectory("docs/pr-media/393");
            Render("lobby-redesign");
            view.transform.Find("Hud/Rewards").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(Read<GameObject>(view, "page").activeSelf, "로비 버튼으로 레벨 페이지 열기");
            await Task.Delay(500); Render("level-rewards");
            rows.Single(x => x.Level == 15).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            await Task.Delay(100);
            Check(Read<int>(view, "selectedLevel") == 15, "미도달 레벨 선택");
            Check(Read<TMP_Text>(view, "detailState").text == "미도달", "미도달 지급 상태");
            view.Bind(state);
            Check(Read<int>(view, "selectedLevel") == 15, "동기화 중 선택 유지");
            view.SelectLevel(1); await Task.Delay(100);
            Check(Read<RectTransform>(view, "rewardsRoot").GetComponentsInChildren<RewardChest.RewardTileView>().Length == 5, "1레벨 시작 카드 5종 표시");
            Render("level-rewards-starter");
            state.level = 50; state.nextLevelXp = 0; state.levelXp = 0; view.Bind(state); view.SelectLevel(50); await Task.Delay(200);
            Check(Read<TMP_Text>(view, "xpText").text == "MAX", "최대 레벨 경험치 표시");
            Read<ScrollRect>(view, "levelScroll").verticalNormalizedPosition = 0;
            Render("level-rewards-max");
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
            view.Bind(state); view.SelectLevel(50); await Task.Delay(600); Render("level-rewards-en");
            Render("level-rewards-4x3", 1280, 960);
            Check(!Read<Image>(view, "xpFill").transform.parent.parent.GetComponent<Image>().raycastTarget, "경험치 막대가 기존 프로필 클릭을 가로막지 않음");
            view.transform.Find("LevelRewardsPage/Back").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(!Read<GameObject>(view, "page").activeSelf && Read<GameObject>(view, "hud").activeSelf, "뒤로 버튼으로 로비 복귀");
            // API 반환값이 없는 경우를 주입하되 요청 코루틴 자체는 실행하지 않는다.
            Set(view, "catalog", null);
            var failed = (System.Collections.IEnumerator)typeof(PlayerProgressionView).GetMethod("LoadRewards", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
            failed.MoveNext(); failed.MoveNext(); await Task.Delay(100);
            Check(Read<UnityEngine.UI.Button>(view, "retryButton").gameObject.activeSelf, "조회 실패 시 재시도 표시");
            Check(Read<TMP_Text>(view, "statusText").text == "Could not load rewards.", "조회 실패를 빈 보상으로 표시하지 않음");
            Debug.Log("PROGRESSION_UI_PREVIEW_COMPLETE");
            EditorApplication.Exit(0);
        } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    private static void Render(string name, int width = 1280, int height = 720) {
        var camera = Camera.main;
        var target = new RenderTexture(width, height, 24); target.Create(); camera.targetTexture = target;
        foreach (var canvas in Object.FindObjectsOfType<Canvas>(true).Where(x => x.isRootCanvas)) {
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        }
        Canvas.ForceUpdateCanvases();
        foreach (var text in Object.FindObjectsOfType<TMP_Text>()) text.ForceMeshUpdate();
        camera.Render(); var previous = RenderTexture.active; RenderTexture.active = target;
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
        File.WriteAllBytes($"docs/pr-media/393/{name}.jpg", texture.EncodeToJPG(88));
        RenderTexture.active = previous; camera.targetTexture = null;
        Object.DestroyImmediate(texture); target.Release(); Object.DestroyImmediate(target);
    }
}
