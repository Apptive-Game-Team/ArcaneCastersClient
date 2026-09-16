using System;
using Data;
using Data.Deck;
using Global;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeckScene
{

    public class DeckManagementController : MonoBehaviour
    {
        [Header("UI")]
        public Transform deckListContainer;      // 덱 리스트
        public GameObject deckPrefab; // 덱 
        public Transform deckCardsContainer;     // 선택된 덱 카드
        public Transform ownedCardsContainer;    // 보유 카드
        public GameObject cardItemPrefab;        // 카드 UI 프리팹
        public GameObject cardInDeckItemPrefab;        // 덱 카드 UI 프리팹
        public GameObject createDeckPrefab;        // 덱 생성 UI 프리팹
        public Button submitDeckButton;        // 덱 제출 버튼
        public Button removeDeckButton;        // 덱 삭제 버튼
        public InputField deckNameInputField; // 덱 이름 입력 필드
        [SerializeField] private HaveCardMagicPopup ownedCardMagicPopup;
        [SerializeField] private DeckMagicInfoButton deckMagicInfoButton;
        [SerializeField] private DeckRequirementStatusText deckRequirementStatusText;
        [SerializeField] private ConfirmationDialogController deleteConfirmDialogBehaviour;

        private DeckManagementViewModel viewModel;
        private DeckManagementView view;
        private IConfirmationDialog deleteConfirmDialog;
        private LoadingHandle loadingHandle;
        
        public LocalizedString newDeck;
        public LocalizedString deckCreationFailed;
        public LocalizedString deckCreationSuccess;
        public LocalizedString deckUpdateFailed;
        public LocalizedString deckUpdateSuccess;

        public LocalizedString errorCardCount;

        [Header("Delete")]
        public LocalizedString deckDeletionConfirmMessage;
        public LocalizedString deckDeletionFailedMessage;
        public LocalizedString deckDeletionSuccessMessage;

        public event Action NewDeckSelected;
        public event Action<int> DeckCardCountChanged;
        public event Func<bool> DeckSaved;
        
        private void Awake()
        {
            viewModel = new DeckManagementViewModel();
            ownedCardMagicPopup ??= FindObjectOfType<HaveCardMagicPopup>(true);
            if (ownedCardMagicPopup == null)
            {
                WDebug.LogWarning("HaveCardMagicPopup is not assigned in ManageDeckScene.");
            }

            deleteConfirmDialog = deleteConfirmDialogBehaviour as IConfirmationDialog;
            if (deleteConfirmDialogBehaviour != null && deleteConfirmDialog == null)
            {
                WDebug.LogWarning("Deck delete confirmation dialog does not implement IConfirmationDialog.");
            }

            view = new DeckManagementView(
                deckListContainer,
                deckPrefab,
                deckCardsContainer,
                ownedCardsContainer,
                cardItemPrefab,
                cardInDeckItemPrefab,
                createDeckPrefab,
                submitDeckButton,
                removeDeckButton,
                deckNameInputField,
                OnDeckSelected,
                OnNewDeckSelected,
                OnOwnedCardSelected,
                OnCardInDeckSelected,
                ownedCardMagicPopup,
                viewModel.GetOwnedCardMagicSuggestions
            );
            view.BindSubmit(OnDeckSubmit);
            view.BindRemove(OnDeckRemove);
            deckMagicInfoButton?.Init(viewModel.GetCurrentDeckAvailableMagics);
            UpdateRemoveButton();
            UpdateDeckRequirements();
        }

        private void Start()
        {
            loadingHandle = LoadingPage.Begin(this);
            if (viewModel.HasCachedData)
            {
                PopulateDeckList();
                PopulateOwnedCardsList();
            }

            StartCoroutine(viewModel.LoadAll(OnLoadCompleted));
        }

        private void OnLoadCompleted(bool isSuccess)
        {
            if (!isSuccess)
            {
                loadingHandle?.Dispose();
                return;
            }

            PopulateDeckList();
            PopulateOwnedCardsList();
            UpdateDeckRequirements();
            loadingHandle?.Dispose();
        }
        
        private void PopulateOwnedCardsList()
        {
            view.RenderOwnedCards(viewModel.OwnedCards);
        }

        private void PopulateDeckList()
        {
            view.RenderDecks(viewModel.UserDecks);
        }
        
        private void ReloadDeckList()
        {
            view.RenderDeckCards(viewModel.CurrentDeck);
        }
        
        private void OnDeckSelected(DeckResponseDto deck)
        {
            WDebug.Log($"OnDeckSelected: {deck.name} (ID: {deck.id})");
            viewModel.SelectDeck(deck);
            view.SetDeckName(viewModel.CurrentDeck.name);
            UpdateRemoveButton();

            ReloadDeckList();
            UpdateDeckRequirements();
        }

        private async void OnNewDeckSelected()
        {
            WDebug.Log("OnNewDeckSelected");

            string newDeckString = await newDeck.GetLocalizedStringAsync().Task;
            viewModel.SelectNewDeck(newDeckString);
            view.SetDeckName(viewModel.CurrentDeck.name);
            UpdateRemoveButton();
            ReloadDeckList();
            UpdateDeckRequirements();
            NewDeckSelected?.Invoke();
        }

        private void OnOwnedCardSelected(CardDto card)
        {
            WDebug.Log($"OnOwnedCardSelected: {card.name} (ID: {card.id})");
            if (viewModel.TryAddOwnedCard(card))
            {
                ReloadDeckList();
                UpdateDeckRequirements();
                NotifyDeckCardCountChanged();
            }
        }

        private void OnCardInDeckSelected(CardDto card)
        {
            if (viewModel.TryRemoveCard(card))
            {
                ReloadDeckList();
                UpdateDeckRequirements();
                NotifyDeckCardCountChanged();
            }
        }

        private void OnDeckSubmit()
        {
            WDebug.Log("OnDeckSubmit");
            if (!viewModel.CanSubmitCurrentDeck)
            {
                SystemMessageUI.Instance.ShowMessage(errorCardCount);
                return;
            }

            StartCoroutine(viewModel.SubmitCurrentDeck(view.DeckName, OnDeckSubmitted));
        }

        private void OnDeckRemove()
        {
            WDebug.Log("OnDeckRemove");
            if (!viewModel.CanDeleteCurrentDeck)
            {
				WDebug.Log(viewModel.CurrentMode);
				WDebug.Log(viewModel.CurrentDeck);
                return;
            }

            if (deleteConfirmDialog == null)
            {
                WDebug.LogWarning("Deck delete confirmation dialog is not assigned.");
                return;
            }
            WDebug.Log("OnDeckRemove");
            deleteConfirmDialog.Show(deckDeletionConfirmMessage, DeleteCurrentDeck);
        }

        private void DeleteCurrentDeck()
        {
            StartCoroutine(viewModel.DeleteCurrentDeck(OnDeckDeleted));
        }

        private void OnDeckDeleted(bool isSuccess)
        {
            SystemMessageUI.Instance.ShowMessage(isSuccess ? deckDeletionSuccessMessage : deckDeletionFailedMessage);
            if (isSuccess)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        private void OnDeckSubmitted(DeckEditMode mode, bool isSuccess)
        {
            if (mode == DeckEditMode.Create)
            {
                SystemMessageUI.Instance.ShowMessage(isSuccess ? deckCreationSuccess : deckCreationFailed);
            }
            else
            {
                SystemMessageUI.Instance.ShowMessage(isSuccess ? deckUpdateSuccess : deckUpdateFailed);
            }

            if (isSuccess)
            {
                if (NotifyDeckSaved())
                {
                    return;
                }

                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        private void UpdateRemoveButton()
        {
            view.SetRemoveButtonActive(viewModel.CanDeleteCurrentDeck);
        }

        private void UpdateDeckRequirements()
        {
            DeckRequirementSummary summary = viewModel.GetCurrentDeckSummary();
            bool canSubmit = viewModel.CanSubmitCurrentDeck;

            if (deckRequirementStatusText != null)
            {
                deckRequirementStatusText.Render(summary, canSubmit);
            }
            else if (submitDeckButton != null)
            {
                submitDeckButton.interactable = canSubmit;
            }
        }

        private void NotifyDeckCardCountChanged()
        {
            DeckCardCountChanged?.Invoke(viewModel.CurrentDeck?.cards?.Length ?? 0);
        }

        private bool NotifyDeckSaved()
        {
            if (DeckSaved == null)
            {
                return false;
            }

            bool isHandled = false;
            foreach (Func<bool> handler in DeckSaved.GetInvocationList())
            {
                isHandled |= handler();
            }

            return isHandled;
        }
    }
}
