using System;
using System.Collections;
using System.Collections.Generic;
using Data;
using Data.Localization;
using Data.Magic;
using Data.Profile;
using Data.Quests;
using Global;
using RewardChest.Renderers;
using UnityEngine;

namespace RewardChest
{
    /// <summary>How a <see cref="ChestClaimFlow"/> run ended.</summary>
    public enum ChestClaimResult
    {
        /// <summary>Claimed and opened here; the presenter showed every reward.</summary>
        Opened,

        /// <summary>Claimed, not opened. The chest waits in ChestScene.</summary>
        KeptForLater,

        /// <summary>409: claimed earlier. Nothing was shown; the caller refreshes.</summary>
        AlreadyClaimed,

        /// <summary>422: the condition is not met yet. A system message said so.</summary>
        NotClaimableYet,

        /// <summary>404: the server does not know the quest. A system message said so.</summary>
        NotFound,

        /// <summary>The claim request failed. A system message said so.</summary>
        Failed
    }

    /// <summary>
    /// Claims a quest whose reward is a chest and lets the player open it on the spot:
    /// claim → "you got a chest" card (<see cref="ChestClaimChoicePanel"/>) → "Open now" opens it through
    /// <see cref="ChestApiClient"/> and plays <see cref="RewardChestPresenter"/>, "Later" leaves it in ChestScene.
    /// It knows nothing about adventures; the caller passes a quest id and gets callbacks.
    /// </summary>
    public class ChestClaimFlow : MonoBehaviour
    {
        [SerializeField] private QuestApiClient questApiClient;
        [SerializeField] private ChestApiClient chestApiClient;
        [SerializeField] private ChestClaimChoicePanel choicePanel;
        [SerializeField] private RewardChestPresenter presenter;

        private RewardTileRendererSelector selector;

        public bool IsRunning { get; private set; }

        private void Awake()
        {
            if (questApiClient == null)
            {
                questApiClient = GetComponent<QuestApiClient>();
            }

            if (chestApiClient == null)
            {
                chestApiClient = GetComponent<ChestApiClient>();
            }

            selector = RewardTileRenderers.CreateDefaultSelector();
            if (presenter != null)
            {
                presenter.SetRendererSelector(selector);
            }
        }

        /// <summary>
        /// Starts one run. Ignored while another run is in progress.
        /// </summary>
        /// <param name="questId">The quest to claim.</param>
        /// <param name="expectedChest">
        /// The chest the quest list promised, used when the claim response does not name one. May be null.
        /// </param>
        /// <param name="onClaimed">Called as soon as the server granted the rewards, before any choice.</param>
        /// <param name="onFinished">Called once at the end, whatever happened.</param>
        public void Run(long questId, RewardView expectedChest, Action onClaimed, Action<ChestClaimResult> onFinished)
        {
            if (IsRunning)
            {
                return;
            }

            if (questApiClient == null || chestApiClient == null)
            {
                WDebug.LogError("ChestClaimFlow is missing its QuestApiClient or ChestApiClient.");
                onFinished?.Invoke(ChestClaimResult.Failed);
                return;
            }

            StartCoroutine(RunRoutine(questId, expectedChest, onClaimed, onFinished));
        }

        private IEnumerator RunRoutine(long questId, RewardView expectedChest, Action onClaimed,
            Action<ChestClaimResult> onFinished)
        {
            IsRunning = true;
            ChestClaimResult result = ChestClaimResult.Failed;

            QuestClaimOutcome outcome = QuestClaimOutcome.Failed;
            List<RewardView> granted = new List<RewardView>();
            yield return questApiClient.ClaimQuest(questId, (claimOutcome, rewards) =>
            {
                outcome = claimOutcome;
                granted = rewards ?? new List<RewardView>();
            });

            switch (outcome)
            {
                case QuestClaimOutcome.Claimed:
                    ClaimedQuestLedger.Session.Remember(questId);
                    onClaimed?.Invoke();
                    yield return PresentClaimed(granted, expectedChest, chosen => result = chosen);
                    break;
                case QuestClaimOutcome.AlreadyClaimed:
                    result = ChestClaimResult.AlreadyClaimed;
                    break;
                case QuestClaimOutcome.NotClaimableYet:
                    ShowMessage(ChestClaimTextKeys.NotClaimableYet, "Clear every stage to take this chest.");
                    result = ChestClaimResult.NotClaimableYet;
                    break;
                case QuestClaimOutcome.NotFound:
                    ShowMessage(ChestClaimTextKeys.NotFound, "This reward is no longer available.");
                    result = ChestClaimResult.NotFound;
                    break;
                default:
                    ShowMessage(ChestClaimTextKeys.ClaimFailed, "Could not take the chest. Try again.");
                    result = ChestClaimResult.Failed;
                    break;
            }

            IsRunning = false;
            onFinished?.Invoke(result);
        }

        private IEnumerator PresentClaimed(List<RewardView> granted, RewardView expectedChest,
            Action<ChestClaimResult> onResult)
        {
            RewardView chest = QuestClaimPayloads.FirstChest(granted) ?? expectedChest;
            if (chest == null)
            {
                // The quest paid no chest: show what it did pay with the same reveal and stop there.
                if (presenter != null && granted.Count > 0)
                {
                    yield return presenter.Play(granted);
                }

                onResult(ChestClaimResult.Opened);
                yield break;
            }

            bool openNow = false;
            if (choicePanel != null)
            {
                yield return choicePanel.Ask(chest, selector, chosen => openNow = chosen);
            }

            if (!openNow)
            {
                onResult(ChestClaimResult.KeptForLater);
                yield break;
            }

            bool opened = false;
            yield return OpenClaimedChest(chest.Key, success => opened = success);
            onResult(opened ? ChestClaimResult.Opened : ChestClaimResult.KeptForLater);
        }

        /// <summary>
        /// Opens the newest unopened chest of <paramref name="chestKey"/>. On any failure the chest stays in
        /// ChestScene and a system message says so.
        /// </summary>
        private IEnumerator OpenClaimedChest(string chestKey, Action<bool> onDone)
        {
            // Magic tiles need the magic list. It is normally cached from the lobby already.
            if (LocalCombinedMagicData.GetEffectiveDataList().Count == 0)
            {
                yield return GameDataRefresh.Refresh();
            }

            ChestDto[] chests = null;
            yield return chestApiClient.GetChests(result => chests = result);
            ChestDto target = ChestSelection.FindNewestUnopened(chests, chestKey);
            if (target == null)
            {
                WDebug.LogWarning($"No unopened chest with key '{chestKey}' after the claim.");
                ShowMessage(ChestClaimTextKeys.OpenFailed, "Could not open it now. It is waiting in your chests.");
                onDone(false);
                yield break;
            }

            ChestOpenOutcome outcome = ChestOpenOutcome.Failed;
            List<RewardView> rewards = null;
            yield return chestApiClient.OpenChest(target.id, (result, opened) =>
            {
                outcome = result;
                rewards = opened;
            });

            if (outcome != ChestOpenOutcome.Opened)
            {
                ShowMessage(ChestClaimTextKeys.OpenFailed, "Could not open it now. It is waiting in your chests.");
                onDone(false);
                yield break;
            }

            if (presenter != null)
            {
                yield return presenter.Play(rewards, null, chestKey);
            }

            onDone(true);
        }

        private static async void ShowMessage(string key, string fallback)
        {
            string localized = await LocaleUtils.GetStringAsync(RewardNameKeys.Table, key);
            string message = string.IsNullOrEmpty(localized) ? fallback : localized;
            if (SystemMessageUI.Instance == null)
            {
                WDebug.LogWarning($"No SystemMessage in this scene: {message}");
                return;
            }

            SystemMessageUI.Instance.ShowMessage(message);
        }
    }

    /// <summary>Keys of the claim flow's strings in the <c>LobbyUI</c> string table.</summary>
    public static class ChestClaimTextKeys
    {
        public const string NotClaimableYet = "AdventureChestNotClaimableYet";
        public const string NotFound = "AdventureChestNotFound";
        public const string ClaimFailed = "AdventureChestClaimFailed";
        public const string OpenFailed = "AdventureChestOpenFailed";
    }
}
