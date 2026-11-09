using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data;
using Data.Localization;
using Data.Profile;
using Data.Quests;
using Global;
using RewardChest;
using RewardChest.Renderers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LobbyScene
{
    public partial class PlayerProgressionView
    {
        [SerializeField] private UnityEngine.UI.Button levelTab;
        [SerializeField] private UnityEngine.UI.Button pendingTab;
        [SerializeField] private UnityEngine.UI.Button claimButton;
        [SerializeField] private GameObject pendingDot;
        [SerializeField] private TMP_Text claimMessage;
        [SerializeField] private GameObject rewardUiPrefab;
        [SerializeField] private RewardTileView claimTilePrefab;
        private readonly List<ProgressionRewardRow> claimRows = new List<ProgressionRewardRow>();
        private readonly Dictionary<string, string> claimText = new Dictionary<string, string>();
        private readonly List<ClaimItem> claimItems = new List<ClaimItem>();
        private ProgressionPendingRewardsDto pendingRewards;
        private QuestDto[] pendingQuests;
        private RewardRequestContext claimContext;
        private ClaimItem selectedClaim;
        private readonly Dictionary<string, ClaimItem> unconfirmedClaims = new Dictionary<string, ClaimItem>();
        private bool showingPending;
        private bool refreshingClaims;
        private bool claiming;
        private bool claimsKnown;
        private bool claimsFailed;
        private float nextClaimsRefresh;
        private int claimTextVersion;
        private int claimPresentationVersion;
        private GameObject claimPresentation;
        private string claimMessageKey;
        private bool suppressContextExperience;

        private sealed class ClaimItem
        {
            public long Id;
            public bool Quest;
            public string Title;
            public int? Level;
            public List<RewardView> Rewards;
            public string Key => (Quest ? "quest:" : "progression:") + Id;
        }

        public bool HasUnclaimedRewards => claimItems.Count > 0;
        public bool ClaimsKnown => claimsKnown;
        private void Update() { ScheduleClaimsRefresh(); }
        private string ClaimText(string key) => claimText.TryGetValue(key, out string value) ? value : "…";

        private void CheckClaimContext()
        {
            var current = RewardRequestContext.Capture();
            if (claimContext.Owner == current.Owner && claimContext.Server == current.Server) return;
            bool changedOwner = claimContext.Owner > 0 && state != null;
            claimContext = current;
            if (changedOwner) {
                if (xpAnimation != null) { StopCoroutine(xpAnimation); xpAnimation = null; }
                hasPresentation = false; suppressContextExperience = true;
                displayedXp = targetXp = 0; targetLevel = selectedLevel = 0;
                state = null; catalog = null; xpLevels = null;
                levelUpVisible = false; textVersion++; detailVersion++;
                if (xpNoticeRoot != null) xpNoticeRoot.SetActive(false);
                ClearRows();
                if (rewardsRoot != null) foreach (Transform child in rewardsRoot) Remove(child.gameObject);
                detailTitle.text = detailState.text = slotsText.text = "";
                hud.SetActive(false);
            }
            pendingRewards = null; pendingQuests = null; selectedClaim = null; unconfirmedClaims.Clear();
            claimItems.Clear(); claimsKnown = false; claimsFailed = false;
            nextClaimsRefresh = 0;
            CloseClaimPresentation();
            if (pendingDot != null) pendingDot.SetActive(false);
            ClearClaimRows();
            RefreshClaimControls();
        }

        private void ScheduleClaimsRefresh()
        {
            CheckClaimContext();
            if (Application.isPlaying && isActiveAndEnabled && Time.realtimeSinceStartup >= nextClaimsRefresh)
                StartClaimsRefresh();
        }

        public void StartClaimsRefresh()
        {
            CheckClaimContext();
            if (!Application.isPlaying || !isActiveAndEnabled || refreshingClaims || claiming) return;
            if (!claimContext.IsCurrent) return;
            StartCoroutine(RefreshClaims());
        }

        public IEnumerator RefreshClaims()
        {
            if (refreshingClaims) yield break;
            var context = RewardRequestContext.Capture();
            if (!context.IsCurrent) yield break;
            refreshingClaims = true;
            ProgressionPendingRewardsDto result = null;
            long code = 0;
            yield return PlayerProgressionApi.Pending((value, status) => { result = value; code = status; });
            if (this == null) yield break;
            refreshingClaims = false;
            if (!context.IsCurrent) yield break;
            nextClaimsRefresh = Time.realtimeSinceStartup + 30;
            if (result == null && (code == 404 || code == 405)) result = new ProgressionPendingRewardsDto { enabled = false };
            if (result == null)
            {
                claimsFailed = true;
                SetClaimMessage("ProgressionPendingUnknown");
                RefreshClaimControls();
                yield break;
            }
            pendingRewards = result;
            claimsKnown = true; claimsFailed = false;
            if (result.enabled)
            {
                var questApi = GetComponent<QuestApiClient>() ?? gameObject.AddComponent<QuestApiClient>();
                QuestDto[] quests = null;
                refreshingClaims = true;
                yield return questApi.FetchQuests(value => quests = value);
                if (this == null) yield break;
                refreshingClaims = false;
                if (!context.IsCurrent) yield break;
                if (quests != null) pendingQuests = quests;
                else claimsFailed = true;
            }
            else pendingQuests = Array.Empty<QuestDto>();
            if (!result.enabled && showingPending) OpenLevelRewards();
            yield return RefreshClaimTextRoutine();
            if (!context.IsCurrent) yield break;
            RebuildClaimItems();
            RefreshClaimRowStates();
            RefreshClaimControls();
        }

        private IEnumerator RefreshClaimTextRoutine()
        {
            var task = RefreshClaimText();
            while (!task.IsCompleted) yield return null;
        }

        private async Task RefreshClaimText()
        {
            int version = ++claimTextVersion;
            var values = new Dictionary<string, string>();
            foreach (string key in new[] { "ProgressionPending", "ProgressionClaim", "ProgressionDaily", "ProgressionQuest",
                "ProgressionClaimFailed", "ProgressionClaimedElsewhere", "ProgressionNoPending", "ProgressionPendingUnknown", "ProgressionClaimReceived", "ProgressionReceiptEmpty",
                "QuestStageClearTitle", "QuestTotalWinTitle" })
                values[key] = await LocaleUtils.GetStringAsync("LobbyUI", key);
            if (this == null || version != claimTextVersion) return;
            foreach (var entry in values) claimText[entry.Key] = entry.Value;
            if (claimMessage != null && claimMessageKey != null) claimMessage.text = ClaimText(claimMessageKey);
            RebuildClaimItems();
            RefreshClaimRowStates();
            RefreshClaimControls();
        }

        private void RebuildClaimItems()
        {
            string previous = selectedClaim?.Key;
            claimItems.Clear();
            if (pendingRewards?.enabled == true)
            {
                foreach (var reward in pendingRewards.rewards ?? Array.Empty<ProgressionPendingRewardDto>())
                    if (reward != null) claimItems.Add(new ClaimItem { Id = reward.id, Level = reward.level,
                        Title = reward.level.HasValue ? $"Lv. {reward.level.Value}" : ClaimText("ProgressionDaily"),
                        Rewards = ProgressionClaimPresentation.PositiveRewards(reward.rewards) });
                foreach (var quest in pendingQuests ?? Array.Empty<QuestDto>())
                    if (quest != null && quest.IsManualClaim && quest.claimable && !quest.IsCompleted)
                        claimItems.Add(new ClaimItem { Id = quest.questId, Quest = true,
                            Title = quest.conditionType == QuestConditionTypes.StageClear
                                ? string.Format(ClaimText("QuestStageClearTitle"), quest.DisplayRequireValue)
                                : quest.conditionType == QuestConditionTypes.TotalWin
                                    ? string.Format(ClaimText("QuestTotalWinTitle"), quest.DisplayRequireValue)
                                    : ClaimText("ProgressionQuest"),
                            Rewards = (quest.rewards ?? Array.Empty<Data.Quests.QuestRewardDto>()).Where(reward => reward != null && reward.amount > 0)
                                .Select(reward => new RewardView(reward.rewardType, reward.rewardKey, reward.rewardId, reward.amount)).ToList() });
            }
            foreach (var item in unconfirmedClaims.Values)
                if (claimItems.All(existing => existing.Key != item.Key)) claimItems.Add(item);
            selectedClaim = claimItems.FirstOrDefault(item => item.Key == previous) ?? claimItems.FirstOrDefault();
            ClearClaimRows();
            if (rowPrefab == null || rowsRoot == null) return;
            foreach (var item in claimItems)
            {
                var captured = item;
                var row = Instantiate(rowPrefab, rowsRoot);
                row.BindPending(item.Title, ClaimText("ProgressionClaim"), () => SelectClaim(captured));
                row.gameObject.SetActive(showingPending);
                claimRows.Add(row);
            }
            if (showingPending) {
                if (selectedClaim != null) SelectClaim(selectedClaim);
                else RenderEmptyPending();
            }
        }

        private void ClearClaimRows()
        {
            foreach (var row in claimRows) if (row != null) Remove(row.gameObject);
            claimRows.Clear();
        }

        public void ShowLevelTab()
        {
            showingPending = false;
            foreach (var row in rows) if (row != null) row.gameObject.SetActive(true);
            foreach (var row in claimRows) if (row != null) row.gameObject.SetActive(false);
            if (claimMessage != null) claimMessage.gameObject.SetActive(false);
            RefreshClaimControls();
        }

        public void OpenLevelRewards() {
            SelectLevel(selectedLevel > 0 ? selectedLevel : state?.level ?? 1);
            ScrollToSelection();
        }

        public void OpenPendingRewards()
        {
            showingPending = true;
            foreach (var row in rows) if (row != null) row.gameObject.SetActive(false);
            foreach (var row in claimRows) if (row != null) row.gameObject.SetActive(true);
            if (selectedClaim != null) SelectClaim(selectedClaim);
            else RenderEmptyPending();
            RefreshClaimControls();
            StartClaimsRefresh();
        }

        private void RenderEmptyPending() {
            detailVersion++;
            foreach (Transform child in rewardsRoot) Remove(child.gameObject);
            detailTitle.text = ClaimText("ProgressionPending"); detailState.text = ""; slotsText.text = "";
            SetClaimMessage(claimsFailed || !claimsKnown ? "ProgressionPendingUnknown" : "ProgressionNoPending");
        }

        private void SelectClaim(ClaimItem item)
        {
            selectedClaim = item;
            for (int i = 0; i < claimRows.Count; i++)
                claimRows[i].RefreshStatus(claimItems[i].Key == item.Key ? 0 : -1, ClaimText("ProgressionClaim"), true);
            detailVersion++;
            detailTitle.text = item.Title;
            detailState.text = ClaimText("ProgressionClaim");
            detailState.color = new Color32(47, 133, 42, 255);
            slotsText.text = "";
            foreach (Transform child in rewardsRoot) Remove(child.gameObject);
            var selector = RewardTileRenderers.CreateDefaultSelector();
            var grid = rewardsRoot.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(222, 124); grid.constraintCount = 3;
            foreach (var reward in item.Rewards) Instantiate(tilePrefab, rewardsRoot).Render(reward, selector.Build(reward));
            if (claimMessage != null) claimMessage.gameObject.SetActive(false);
            RefreshClaimControls();
        }

        private void RefreshClaimRowStates()
        {
            if (state == null) return;
            foreach (var row in rows)
            {
                bool ready = pendingRewards?.rewards?.Any(item => item.level == row.Level) == true;
                bool received = claimsKnown && ProgressionClaimPresentation.IsClaimed(row.Level, state.level, pendingRewards);
                row.RefreshStatus(selectedLevel, ready ? ClaimText("ProgressionClaim") : received ? paid : row.Level > state.level ? locked : "…", received || ready);
            }
            if (showingPending) return;
            bool selectedReady = pendingRewards?.rewards?.Any(item => item.level == selectedLevel) == true;
            bool selectedPaid = claimsKnown && ProgressionClaimPresentation.IsClaimed(selectedLevel, state.level, pendingRewards);
            detailState.text = selectedReady ? ClaimText("ProgressionClaim") : selectedPaid ? paid : selectedLevel > state.level ? locked : "…";
            detailState.color = selectedPaid || selectedReady ? new Color32(47, 133, 42, 255) : new Color32(91, 98, 117, 255);
            RefreshClaimControls();
        }

        private void RefreshClaimControls()
        {
            if (retryButton != null) {
                retryButton.gameObject.SetActive(claimsFailed || (catalog == null && !loading));
                retryButton.interactable = !refreshingClaims && !claiming && !loading;
            }
            if (pendingDot != null) pendingDot.SetActive(HasUnclaimedRewards);
            if (pendingTab != null) pendingTab.gameObject.SetActive(!claimsKnown || pendingRewards?.enabled == true);
            if (claimButton != null)
            {
                bool ready = showingPending ? selectedClaim != null : pendingRewards?.rewards?.Any(item => item.level == selectedLevel) == true;
                claimButton.gameObject.SetActive(pendingRewards?.enabled == true && ready);
                claimButton.interactable = !claiming && !refreshingClaims;
            }
            if (levelTab != null) levelTab.interactable = showingPending;
            if (pendingTab != null) pendingTab.interactable = !showingPending;
        }

        private void SetClaimMessage(string key)
        {
            claimMessageKey = key;
            if (claimMessage == null) return;
            claimMessage.text = ClaimText(key);
            claimMessage.gameObject.SetActive(page.activeSelf);
            if (page.activeSelf && slotsText != null) slotsText.gameObject.SetActive(false);
        }

        public void ClaimSelectedReward()
        {
            if (claiming || refreshingClaims || !claimContext.IsCurrent || pendingRewards?.enabled != true) return;
            var item = showingPending ? selectedClaim : claimItems.FirstOrDefault(value => !value.Quest && value.Level == selectedLevel);
            if (item != null) StartCoroutine(ClaimReward(item));
        }

        private IEnumerator ClaimReward(ClaimItem item)
        {
            claiming = true;
            var context = RewardRequestContext.Capture();
            int presentation = claimPresentationVersion;
            RefreshClaimControls();
            bool success = false, replay = false;
            List<RewardView> granted = new List<RewardView>();
            long status = 0;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                if (item.Quest) {
                    bool parsed = false;
                    var api = GetComponent<QuestApiClient>() ?? gameObject.AddComponent<QuestApiClient>();
                    yield return api.ClaimQuest(item.Id, (outcome, rewards) => {
                        replay = outcome == QuestClaimOutcome.AlreadyClaimed;
                        success = replay || (outcome == QuestClaimOutcome.Claimed && parsed);
                        if (success && !replay) granted = rewards ?? new List<RewardView>();
                    }, (code, valid) => { status = code; parsed = valid; });
                } else {
                    yield return PlayerProgressionApi.Claim(item.Id, (receipt, code) => {
                        status = code;
                        success = receipt != null && receipt.id == item.Id;
                        if (success) { replay = receipt.alreadyClaimed; granted = ProgressionClaimPresentation.PositiveRewards(receipt.rewards); }
                    });
                }
                if (!ProgressionClaimPresentation.ShouldRetry(attempt, status, success, context.IsCurrent)) break;
            }
            if (this == null) yield break;
            claiming = false;
            if (!context.IsCurrent) { RefreshClaimControls(); yield break; }
            bool uncertain = ProgressionClaimPresentation.IsAmbiguousFailure(status, success);
            RecordClaimOutcome(item, success, status);
            // 응답이 없어도 서버에서 지급됐을 수 있으므로 소유 캐시는 다음 조회에서 갱신한다.
            if (success || uncertain) {
                DeckScene.DeckManagementViewModel.ClearCachedUserData(); LobbyUIController.ClearCachedDecks(); SceneContext.OwnedCards = null;
            }
            string resultMessage;
            if (success)
            {
                string shownKey = "manual-reward-shown:" + context.Server + ":" + context.Owner + ":" + item.Key;
                bool seen = PlayerPrefs.GetInt(shownKey, 0) == 1;
                if ((!item.Quest || !replay) && !seen)
                {
                    PlayerPrefs.SetInt(shownKey, 1); PlayerPrefs.Save();
                    if (page.activeSelf && presentation == claimPresentationVersion && granted.Count > 0 && rewardUiPrefab != null)
                    {
                        claimPresentation = Instantiate(rewardUiPrefab);
                        claimPresentation.SetActive(true);
                        QuestRewardTracker.PopulateRewardUI(claimPresentation, granted, claimTilePrefab, RewardTileRenderers.CreateDefaultSelector());
                    }
                }
                resultMessage = replay ? "ProgressionClaimedElsewhere" : granted.Count == 0 ? "ProgressionReceiptEmpty" : "ProgressionClaimReceived";
            }
            else resultMessage = "ProgressionClaimFailed";
            yield return RefreshClaims();
            if (!context.IsCurrent) yield break;
            yield return PlayerProgressionApi.Sync(value => { if (context.IsCurrent && value != null) Bind(value); });
            if (!context.IsCurrent) yield break;
            yield return RefreshChests();
            if (!context.IsCurrent) yield break;
            SetClaimMessage(resultMessage);
            RefreshClaimControls();
        }

        private void CloseClaimPresentation()
        {
            claimPresentationVersion++;
            if (claimPresentation != null) { Remove(claimPresentation); claimPresentation = null; }
        }

        private void RecordClaimOutcome(ClaimItem item, bool success, long status) {
            if (success) unconfirmedClaims.Remove(item.Key);
            else if (ProgressionClaimPresentation.IsAmbiguousFailure(status, false)) unconfirmedClaims[item.Key] = item;
        }
    }
}
