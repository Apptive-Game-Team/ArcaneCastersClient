using System.Collections;
using Data.Adventures.Domain;
using Data.Profile;
using Data.Quests;
using RewardChest;
using UnityEngine;

namespace Adventures
{
    /// <summary>
    /// Drives the chest at the end of the adventure map. Each time <see cref="AdventureMapController"/> shows an
    /// adventure it reads <c>GET /api/users/mine/quests</c>, takes that adventure's <c>ADVENTURE_CLEAR</c> quest
    /// and shows the chest hidden, claimable or claimed. A click hands the quest to <see cref="ChestClaimFlow"/>
    /// and reads the quests again when the flow ends.
    /// </summary>
    public class AdventureEndChestController : MonoBehaviour
    {
        [SerializeField] private AdventureMapController mapController;
        [SerializeField] private AdventureEndChest chest;
        [SerializeField] private QuestApiClient questApiClient;
        [SerializeField] private ChestClaimFlow claimFlow;

        private long? adventureId;
        private QuestDto adventureQuest;
        private int refreshVersion;

        private void Awake()
        {
            if (questApiClient == null)
            {
                questApiClient = GetComponent<QuestApiClient>();
            }

            if (claimFlow == null)
            {
                claimFlow = GetComponent<ChestClaimFlow>();
            }

            if (chest != null)
            {
                chest.SetState(AdventureChestState.Hidden);
                chest.Clicked += OnChestClicked;
            }

            if (mapController != null)
            {
                mapController.AdventureShown += OnAdventureShown;
            }
        }

        private void Start()
        {
            // The map may have shown its adventure before this Awake subscribed.
            if (mapController != null && mapController.HasShown)
            {
                OnAdventureShown(mapController.ShownAdventure);
            }
        }

        private void OnDestroy()
        {
            if (chest != null)
            {
                chest.Clicked -= OnChestClicked;
            }

            if (mapController != null)
            {
                mapController.AdventureShown -= OnAdventureShown;
            }
        }

        private void OnAdventureShown(Adventure adventure)
        {
            adventureId = adventure?.Id;
            Refresh();
        }

        private void Refresh()
        {
            int version = ++refreshVersion;
            adventureQuest = null;
            if (chest == null)
            {
                return;
            }

            if (!adventureId.HasValue || questApiClient == null)
            {
                chest.SetState(AdventureChestState.Hidden);
                return;
            }

            StartCoroutine(RefreshRoutine(adventureId.Value, version));
        }

        private IEnumerator RefreshRoutine(long id, int version)
        {
            QuestDto[] quests = null;
            yield return questApiClient.FetchQuests(result => quests = result);

            // Another adventure was shown, or a newer refresh started, while this one loaded.
            if (version != refreshVersion)
            {
                yield break;
            }

            adventureQuest = AdventureChestQuest.Select(quests, id);
            chest.SetChestKey(AdventureChestQuest.ChestRewardOf(adventureQuest)?.Key);
            chest.SetState(AdventureChestQuest.StateOf(adventureQuest));
        }

        private void OnChestClicked()
        {
            if (adventureQuest == null || claimFlow == null || claimFlow.IsRunning)
            {
                return;
            }

            chest.SetBusy(true);
            claimFlow.Run(
                adventureQuest.questId,
                AdventureChestQuest.ChestRewardOf(adventureQuest),
                () => chest.SetState(AdventureChestState.Claimed),
                OnClaimFinished);
        }

        private void OnClaimFinished(ChestClaimResult result)
        {
            chest.SetBusy(false);

            // Every outcome reads the quests again: 409 and 422 mean this screen was stale, and a claim
            // should come back as COMPLETED.
            Refresh();
        }
    }
}
