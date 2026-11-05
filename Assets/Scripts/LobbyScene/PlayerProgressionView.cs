using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data;
using Data.Localization;
using Data.Util;
using Global;
using RewardChest;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LobbyScene {
    public partial class PlayerProgressionView : MonoBehaviour {
        [SerializeField] private GameObject hud;
        [SerializeField] private GameObject page;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text xpText;
        [SerializeField] private Image xpFill;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text chestText;
        [SerializeField] private TMP_Text pagePlayerText;
        [SerializeField] private TMP_Text pageXpText;
        [SerializeField] private Image pageXpFill;
        [SerializeField] private TMP_Text pageGoldText;
        [SerializeField] private TMP_Text factionText;
        [SerializeField] private TMP_Text detailTitle;
        [SerializeField] private TMP_Text detailState;
        [SerializeField] private TMP_Text slotsText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private UnityEngine.UI.Button retryButton;
        [SerializeField] private ScrollRect levelScroll;
        [SerializeField] private RectTransform rowsRoot;
        [SerializeField] private ProgressionRewardRow rowPrefab;
        [SerializeField] private RectTransform rewardsRoot;
        [SerializeField] private RewardTileView tilePrefab;
        [SerializeField] private RewardTileView largeTilePrefab;
        [SerializeField] private ChestApiClient chestApi;
        [SerializeField] private GameObject xpNoticeRoot;
        [SerializeField] private TMP_Text xpNotice;
        private readonly List<ProgressionRewardRow> rows = new List<ProgressionRewardRow>();
        private PlayerProgressionDto state;
        private ProgressionRewardsDto catalog;
        private int selectedLevel;
        private int textVersion;
        private bool loading;
        private int? chestCount;
        private string paid = "", locked = "";
        private Coroutine xpAnimation;
        private ProgressionLevelRewardDto[] xpLevels;
        private bool hasPresentation;
        private long displayedXp;
        private long targetXp;
        private int targetLevel;
        private bool levelUpVisible;
        private int noticeLevel;
        private string levelUpFormat = "";

        private void OnEnable() { LocalizationSettings.SelectedLocaleChanged += LocaleChanged; }
        private void OnDisable() {
            LocalizationSettings.SelectedLocaleChanged -= LocaleChanged;
            CloseClaimPresentation();
            if (xpAnimation != null) { StopCoroutine(xpAnimation); xpAnimation = null; }
            if (state != null) RenderProgress(state.level, state.levelXp, state.nextLevelXp);
            displayedXp = targetXp;
            if (xpFill != null) xpFill.color = pageXpFill.color = new Color32(91, 208, 76, 255);
            if (xpNoticeRoot != null) xpNoticeRoot.SetActive(false);
        }
        private void LocaleChanged(Locale _) { Bind(state); if (catalog != null) SelectLevel(selectedLevel); }

        public void Bind(PlayerProgressionDto value) {
            if (value == null || string.IsNullOrEmpty(value.faction)) return;
            CheckClaimContext();
            if (state != null && state.faction != value.faction) {
                catalog = null; xpLevels = null; hasPresentation = false; ClearRows();
            }
            state = value;
            hud.SetActive(!page.activeSelf);
            goldText.text = pageGoldText.text = value.currency.ToString("N0");
            PresentExperience(value);
            _ = RefreshText(++textVersion);
            ScheduleClaimsRefresh();
            if (page.activeSelf && catalog == null && !loading) RetryRewards();
        }

        private void RenderProgress(int level, long xp, int cost) {
            levelText.text = $"Lv. {level}";
            xpText.text = cost <= 0 ? "MAX" : $"{xp} / {cost}";
            pageXpText.text = cost <= 0 ? "MAX" : xpText.text + " XP";
            float fill = cost <= 0 ? 1 : Mathf.Clamp01((float)xp / cost);
            xpFill.rectTransform.anchorMax = pageXpFill.rectTransform.anchorMax = new Vector2(fill, 1);
            pagePlayerText.text = $"{SceneContext.User?.name ?? ""} · Lv. {level}";
        }

        private void PresentExperience(PlayerProgressionDto value) {
            if (hasPresentation && targetXp == value.xp && targetLevel == value.level) return;
            long start = suppressContextExperience ? value.xp
                : hasPresentation ? displayedXp : Math.Max(0, value.xp - Math.Max(0, value.gainedXp));
            suppressContextExperience = false;
            bool animate = Application.isPlaying && isActiveAndEnabled && start < value.xp;
            if (xpAnimation != null) { StopCoroutine(xpAnimation); xpAnimation = null; }
            hasPresentation = true;
            targetXp = value.xp;
            targetLevel = value.level;
            levelUpVisible = false;
            if (!animate) {
                displayedXp = value.xp;
                RenderProgress(value.level, value.levelXp, value.nextLevelXp);
                if (xpNoticeRoot != null) xpNoticeRoot.SetActive(false);
                return;
            }
            // DTO는 지급 알림 뒤 재사용되므로 연출에 필요한 값은 시작할 때 복사한다.
            xpAnimation = StartCoroutine(AnimateExperience(start, value.xp, value.level,
                value.levelXp, value.nextLevelXp, value.faction));
        }

        private IEnumerator AnimateExperience(long from, long to, int finalLevel, long finalXp, int finalCost, string faction) {
            long gained = to - from;
            if (xpNoticeRoot != null) xpNoticeRoot.SetActive(true);
            if (xpNotice != null) xpNotice.text = $"+{gained:N0} XP";
            long finalThreshold = to - finalXp;
            var levels = xpLevels ?? catalog?.levels;
            if (from < finalThreshold && (levels == null || levels.Length == 0)) {
                // 레벨 경계는 서버의 지급표를 사용한다. 조회 실패도 다른 로비 조작을 막지 않는다.
                xpText.text = pageXpText.text = "…";
                yield return PlayerProgressionApi.Rewards(result => {
                    if (result != null && result.faction == faction) xpLevels = result.levels;
                });
                levels = xpLevels;
            }
            if (from >= finalThreshold) {
                levels = new[] { new ProgressionLevelRewardDto { level = finalLevel, requiredXp = finalThreshold } };
            }
            if (levels == null || levels.Length == 0) {
                RenderProgress(finalLevel, finalXp, finalCost);
                displayedXp = to;
            } else {
                levels = levels.OrderBy(level => level.requiredXp).ToArray();
                int index = Array.FindLastIndex(levels, level => level.requiredXp <= from);
                index = Mathf.Max(0, index);
                int crossings = Mathf.Max(0, finalLevel - levels[index].level);
                float duration = Mathf.Clamp(2.5f / (crossings + 1), 0.08f, 0.65f);
                displayedXp = from;
                while (displayedXp < to) {
                    var current = levels[index];
                    bool hasNext = index + 1 < levels.Length && current.level < finalLevel;
                    long next = hasNext ? levels[index + 1].requiredXp : to;
                    int cost = hasNext ? (int)(next - current.requiredXp) : finalCost;
                    long end = Math.Min(to, next);
                    long begin = displayedXp;
                    for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime) {
                        float progress = Mathf.SmoothStep(0, 1, elapsed / duration);
                        displayedXp = begin + (long)((end - begin) * (double)progress);
                        RenderProgress(current.level, displayedXp - current.requiredXp, cost);
                        yield return null;
                    }
                    displayedXp = end;
                    if (hasNext && end == next) {
                        index++;
                        levelUpVisible = true;
                        noticeLevel = levels[index].level;
                        if (xpNotice != null) xpNotice.text = string.IsNullOrEmpty(levelUpFormat)
                            ? $"Lv. {levels[index].level} ↑" : string.Format(levelUpFormat, levels[index].level);
                        xpFill.color = pageXpFill.color = new Color32(255, 210, 63, 255);
                    }
                    if (end <= begin) break;
                }
                RenderProgress(finalLevel, finalXp, finalCost);
            }
            displayedXp = to;
            yield return new WaitForSecondsRealtime(levelUpVisible ? 1.8f : 1.1f);
            xpFill.color = pageXpFill.color = new Color32(91, 208, 76, 255);
            if (xpNoticeRoot != null) xpNoticeRoot.SetActive(false);
            xpAnimation = null;
        }

        private async Task RefreshText(int version) {
            string selectedFaction = state?.faction;
            string completed = await LocaleUtils.GetStringAsync("LobbyUI", "ProgressionPaid");
            string unreached = await LocaleUtils.GetStringAsync("LobbyUI", "ProgressionLocked");
            string faction = await LocaleUtils.GetStringAsync("LobbyUI", selectedFaction == "HELL" ? "ProgressionHell" : "ProgressionNature");
            string chest = await LocaleUtils.GetStringAsync("LobbyUI", "ProgressionOwnedChests");
            string levelUp = await LocaleUtils.GetStringAsync("LobbyUI", "ProgressionLevelUp");
            if (this == null || version != textVersion) return;
            paid = completed; locked = unreached;
            levelUpFormat = levelUp;
            if (levelUpVisible && xpNotice != null) xpNotice.text = string.Format(levelUpFormat, noticeLevel);
            factionText.text = faction;
            chestText.text = string.Format(chest, chestCount.HasValue ? chestCount.Value.ToString() : "—");
            UpdateRowStates();
            _ = RefreshClaimText();
        }

        public IEnumerator RefreshChests() {
            var context = Data.Profile.RewardRequestContext.Capture();
            yield return chestApi.GetChests(chests => {
                if (!context.IsCurrent) return;
                chestCount = chests?.Length;
                if (state != null) _ = RefreshText(++textVersion);
            });
        }

        public void OpenRewards() {
            if (state == null) return;
            page.SetActive(true);
            hud.SetActive(false);
            if (catalog == null) RetryRewards();
            else { SelectLevel(selectedLevel); ScrollToSelection(); }
            StartClaimsRefresh();
        }

        public void CloseRewards() { CloseClaimPresentation(); page.SetActive(false); hud.SetActive(state != null); }
        public void OpenChests() { SceneManager.LoadScene("ChestScene"); }
        public void RetryRewards() {
            if (catalog == null && !loading) StartCoroutine(LoadRewards());
            StartClaimsRefresh();
        }

        private IEnumerator LoadRewards() {
            loading = true;
            retryButton.gameObject.SetActive(false);
            statusText.gameObject.SetActive(true);
            SetStatus("ProgressionLoading");
            yield return PlayerProgressionApi.Rewards(result => catalog = result);
            loading = false;
            if (catalog?.levels == null || catalog.levels.Length == 0 || catalog.faction != state.faction) {
                catalog = null;
                SetStatus("ProgressionLoadFailed");
                retryButton.gameObject.SetActive(true);
                yield break;
            }
            ClearRows();
            foreach (var reward in catalog.levels.OrderBy(x => x.level)) {
                var row = Instantiate(rowPrefab, rowsRoot);
                row.Bind(reward, SelectLevel);
                rows.Add(row);
            }
            statusText.gameObject.SetActive(false);
            if (showingPending) OpenPendingRewards();
            else SelectLevel(state.level);
            Canvas.ForceUpdateCanvases();
            ScrollToSelection();
        }

        private int statusVersion;
        private async void SetStatus(string key) {
            int version = ++statusVersion;
            string text = await LocaleUtils.GetStringAsync("LobbyUI", key);
            if (this != null && version == statusVersion) statusText.text = text;
        }

        private void ClearRows() {
            foreach (var row in rows) if (row != null) Remove(row.gameObject);
            rows.Clear();
        }

        private static void Remove(GameObject value) {
            value.SetActive(false);
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }

        private void ScrollToSelection() {
            Canvas.ForceUpdateCanvases();
            int index = rows.FindIndex(x => x.Level == selectedLevel);
            float distance = Mathf.Max(0, rowsRoot.rect.height - levelScroll.viewport.rect.height);
            levelScroll.verticalNormalizedPosition = distance > 0 ? 1 - Mathf.Clamp01(Mathf.Max(0, index - 2) * 54f / distance) : 1;
        }

        public void SelectLevel(int level) {
            var reward = catalog?.levels?.FirstOrDefault(x => x.level == level);
            if (reward == null) return;
            ShowLevelTab();
            slotsText.gameObject.SetActive(true);
            selectedLevel = level;
            for (int i = rewardsRoot.childCount - 1; i >= 0; i--) Remove(rewardsRoot.GetChild(i).gameObject);
            bool compact = (reward.cards?.Length ?? 0) > 2;
            var grid = rewardsRoot.GetComponent<GridLayoutGroup>();
            grid.cellSize = compact ? new Vector2(222, 124) : new Vector2(330, 260);
            grid.constraintCount = compact ? 3 : 2;
            var prefab = compact ? tilePrefab : largeTilePrefab;
            foreach (var card in reward.cards ?? Array.Empty<ProgressionFixedRewardDto>()) {
                var sprite = Resources.Load<Sprite>($"Game/sprites/{StringUtils.ToPascalCase(card.magicName)}");
                Instantiate(prefab, rewardsRoot).Render(new RewardView("MAGIC", card.magicName, card.magicId, card.amount),
                    new RewardTileContent(sprite, "Magic", StringUtils.ToCamelCase(card.magicName), card.magicName));
            }
            if (reward.chestCount > 0) {
                RewardSpriteResolver.TryResolveChestIcon("progression_chest", out var sprite);
                Instantiate(prefab, rewardsRoot).Render(new RewardView("CHEST", "progression_chest", 0, reward.chestCount),
                    new RewardTileContent(sprite, "LobbyUI", "ProgressionChest", ""));
            }
            _ = DetailText(reward, ++detailVersion);
            UpdateRowStates();
        }

        private int detailVersion;
        private async Task DetailText(ProgressionLevelRewardDto reward, int version) {
            string title = await LocaleUtils.GetStringAsync("LobbyUI", "ProgressionLevelTitle");
            string slots = await LocaleUtils.GetStringAsync("LobbyUI", "ProgressionChestSlots");
            if (this == null || version != detailVersion) return;
            detailTitle.text = string.Format(title, reward.level);
            slotsText.text = reward.chestCount > 0 ? string.Format(slots, reward.rewardSlots) : "";
        }

        private void UpdateRowStates() {
            if (state == null) return;
            RefreshClaimRowStates();
        }
    }
}
