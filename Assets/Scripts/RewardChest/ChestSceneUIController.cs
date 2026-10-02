using System.Collections;
using System.Collections.Generic;
using Data;
using Data.Magic;
using RewardChest.Renderers;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace RewardChest
{
    /// <summary>
    /// <c>ChestScene</c>: lists the unopened chests, opens one, plays the reveal and lists again.
    /// </summary>
    public class ChestSceneUIController : MonoBehaviour
    {
        [SerializeField] private ChestApiClient apiClient;
        [SerializeField] private ChestItemFactory itemFactory;
        [SerializeField] private RewardChestPresenter presenter;
        [SerializeField] private GameObject emptyState;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private LocalizedString loadFailedMessage;
        [SerializeField] private LocalizedString openFailedMessage;

        private readonly List<ChestItem> items = new List<ChestItem>();
        private RewardTileRendererSelector selector;
        private bool busy;
        private int statusVersion;

        private void Awake()
        {
            if (apiClient == null)
            {
                apiClient = GetComponent<ChestApiClient>();
            }

            if (itemFactory == null)
            {
                itemFactory = GetComponent<ChestItemFactory>();
            }

            selector = RewardTileRenderers.CreateDefaultSelector();
            if (presenter != null)
            {
                presenter.SetRendererSelector(selector);
                presenter.gameObject.SetActive(false);
            }

            SetEmptyStateVisible(false);
            ShowStatus(null);
        }

        private IEnumerator Start()
        {
            // Magic tiles need the magic list. It is normally cached from the lobby already.
            if (LocalCombinedMagicData.GetEffectiveDataList().Count == 0)
            {
                yield return GameDataRefresh.Refresh();
            }

            yield return RefreshList();
        }

        private IEnumerator RefreshList()
        {
            busy = true;
            ChestDto[] chests = null;
            yield return apiClient.GetChests(result => chests = result);

            itemFactory.ClearItems();
            items.Clear();

            if (chests == null)
            {
                SetEmptyStateVisible(false);
                ShowStatus(loadFailedMessage, "Could not load chests.");
                busy = false;
                yield break;
            }

            ShowStatus(null);
            SetEmptyStateVisible(chests.Length == 0);
            foreach (ChestDto chest in chests)
            {
                ChestItem item = itemFactory.Create(chest, selector, OnOpenRequested);
                if (item != null)
                {
                    items.Add(item);
                }
            }

            busy = false;
        }

        private void OnOpenRequested(ChestItem item)
        {
            if (busy || item == null || item.Chest == null)
            {
                return;
            }

            StartCoroutine(OpenChest(item.Chest.id));
        }

        private IEnumerator OpenChest(long ownedChestId)
        {
            busy = true;
            SetItemsInteractable(false);
            ShowStatus(null);

            ChestOpenOutcome outcome = ChestOpenOutcome.Failed;
            List<RewardView> rewards = null;
            yield return apiClient.OpenChest(ownedChestId, (result, opened) =>
            {
                outcome = result;
                rewards = opened;
            });

            switch (outcome)
            {
                case ChestOpenOutcome.Opened:
                    if (presenter != null)
                    {
                        yield return presenter.Play(rewards);
                    }

                    yield return RefreshList();
                    break;
                case ChestOpenOutcome.AlreadyOpened:
                case ChestOpenOutcome.NotFound:
                    // Opened elsewhere or gone: the list is stale, so a refresh is the whole answer.
                    yield return RefreshList();
                    break;
                default:
                    ShowStatus(openFailedMessage, "Could not open the chest.");
                    SetItemsInteractable(true);
                    busy = false;
                    break;
            }
        }

        private void SetItemsInteractable(bool interactable)
        {
            foreach (ChestItem item in items)
            {
                if (item != null)
                {
                    item.SetInteractable(interactable);
                }
            }
        }

        private void SetEmptyStateVisible(bool visible)
        {
            if (emptyState != null)
            {
                emptyState.SetActive(visible);
            }
        }

        private void ShowStatus(LocalizedString message, string fallback = null)
        {
            if (statusText == null)
            {
                return;
            }

            int version = ++statusVersion;
            if (message == null || message.IsEmpty)
            {
                statusText.text = fallback ?? string.Empty;
                statusText.gameObject.SetActive(!string.IsNullOrEmpty(fallback));
                return;
            }

            statusText.text = fallback ?? string.Empty;
            statusText.gameObject.SetActive(true);
            message.GetLocalizedStringAsync().Completed += handle =>
            {
                // A later ShowStatus call has replaced this message while it loaded.
                if (statusText != null && version == statusVersion && !string.IsNullOrEmpty(handle.Result))
                {
                    statusText.text = handle.Result;
                }
            };
        }
    }
}
