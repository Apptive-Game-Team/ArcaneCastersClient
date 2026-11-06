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
    public class PlayerProgressionView : MonoBehaviour {
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
        private readonly List<ProgressionRewardRow> rows = new List<ProgressionRewardRow>();
        private PlayerProgressionDto state;
        private ProgressionRewardsDto catalog;
        private int selectedLevel;
        private int textVersion;
        private bool loading;
        private int? chestCount;
        private string paid = "", locked = "";

        private void OnEnable() { LocalizationSettings.SelectedLocaleChanged += LocaleChanged; }
        private void OnDisable() { LocalizationSettings.SelectedLocaleChanged -= LocaleChanged; }
        private void LocaleChanged(Locale _) { Bind(state); if (catalog != null) SelectLevel(selectedLevel); }

        public void Bind(PlayerProgressionDto value) {
            if (value == null || string.IsNullOrEmpty(value.faction)) return;
            if (state != null && state.faction != value.faction) { catalog = null; ClearRows(); }
            state = value;
            hud.SetActive(!page.activeSelf);
            levelText.text = $"Lv. {value.level}";
            string xp = value.nextLevelXp <= 0 ? "MAX" : $"{value.levelXp} / {value.nextLevelXp}";
            xpText.text = xp;
            pageXpText.text = value.nextLevelXp <= 0 ? "MAX" : xp + " XP";
            float fill = value.nextLevelXp <= 0 ? 1 : Mathf.Clamp01((float)value.levelXp / value.nextLevelXp);
            xpFill.rectTransform.anchorMax = pageXpFill.rectTransform.anchorMax = new Vector2(fill, 1);
            goldText.text = pageGoldText.text = value.currency.ToString("N0");
            pagePlayerText.text = $"{SceneContext.User?.name ?? ""} · Lv. {value.level}";
            _ = RefreshText(++textVersion);
        }

        private async Task RefreshText(int version) {
            string completed = await LocaleUtils.GetStringAsync("LobbyUI", "ProgressionPaid");
            string unreached = await LocaleUtils.GetStringAsync("LobbyUI", "ProgressionLocked");
            string faction = await LocaleUtils.GetStringAsync("LobbyUI", state.faction == "HELL" ? "ProgressionHell" : "ProgressionNature");
            string chest = await LocaleUtils.GetStringAsync("LobbyUI", "ProgressionOwnedChests");
            if (this == null || version != textVersion) return;
            paid = completed; locked = unreached;
            factionText.text = faction;
            chestText.text = string.Format(chest, chestCount.HasValue ? chestCount.Value.ToString() : "—");
            UpdateRowStates();
        }

        public IEnumerator RefreshChests() {
            yield return chestApi.GetChests(chests => {
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
        }

        public void CloseRewards() { page.SetActive(false); hud.SetActive(state != null); }
        public void OpenChests() { SceneManager.LoadScene("ChestScene"); }
        public void RetryRewards() { if (!loading) StartCoroutine(LoadRewards()); }

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
            SelectLevel(state.level);
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
            foreach (var row in rows) row.Refresh(state.level, selectedLevel, paid, locked);
            detailState.text = selectedLevel <= state.level ? paid : locked;
            detailState.color = selectedLevel <= state.level ? new Color32(47, 133, 42, 255) : new Color32(91, 98, 117, 255);
        }
    }
}
