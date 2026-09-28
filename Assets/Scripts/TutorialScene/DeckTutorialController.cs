using DeckScene;
using Global.Button;
using UnityEngine;
using UnityEngine.UI;

namespace TutorialScene
{
    public class DeckTutorialController : SceneTutorialController<DeckTutorialController>
    {
        [Header("Deck Targets")]
        [SerializeField] private DeckManagementController deckManagementController;
        [SerializeField] private GameObject deckRuleArea;
        [SerializeField] private GameObject deckListArea;
        [SerializeField] private GameObject ownedCardsArea;
        [SerializeField] private GameObject currentDeckCardsArea;
        [SerializeField] private Button saveButton;
        [SerializeField] private ButtonBase returnToLobbyButton;

        public event System.Action CreateDeckSelected;
        public event System.Action DeckSaved;
        public event System.Action ReturnToLobbySelected;

        private bool isSubscribedToDeckManagement;

        private void OnEnable()
        {
            // This controller lives in the regular deck scene, so it must stay inert
            // unless the deck onboarding step is the one currently running.
            if (!GlobalTutorialManager.IsDeckOnboardingActive())
            {
                return;
            }

            if (deckManagementController == null)
            {
                deckManagementController = GetComponent<DeckManagementController>();
            }

            if (deckManagementController == null)
            {
                return;
            }

            deckManagementController.NewDeckSelected += OnNewDeckSelected;
            deckManagementController.DeckCardCountChanged += NotifyDeckCardCountChanged;
            deckManagementController.DeckSaved += OnDeckSaved;

            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.OnClick += OnReturnToLobbySelected;
            }

            isSubscribedToDeckManagement = true;
        }

        private void OnDisable()
        {
            if (!isSubscribedToDeckManagement)
            {
                return;
            }

            isSubscribedToDeckManagement = false;

            deckManagementController.NewDeckSelected -= OnNewDeckSelected;
            deckManagementController.DeckCardCountChanged -= NotifyDeckCardCountChanged;
            deckManagementController.DeckSaved -= OnDeckSaved;

            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.OnClick -= OnReturnToLobbySelected;
            }
        }

        public void ShowDeckRules(System.Action onNext)
        {
            Show("onboarding.deck.rules", deckRuleArea != null ? deckRuleArea.transform : null, onNext);
        }

        public void ShowCreateDeck()
        {
            Show("onboarding.deck.createDeck", deckListArea != null ? deckListArea.transform : null);
        }

        public void ShowDeckEditor()
        {
            Show(
                "onboarding.deck.selectCards",
                new[]
                {
                    currentDeckCardsArea != null ? currentDeckCardsArea.transform : null,
                    ownedCardsArea != null ? ownedCardsArea.transform : null
                },
                null,
                TutorialPanelSide.Right);
        }

        public void ShowSaveDeck()
        {
            Show("onboarding.deck.saveEnabled",
                new[]
                {
                    currentDeckCardsArea != null ? currentDeckCardsArea.transform : null,
                    ownedCardsArea != null ? ownedCardsArea.transform : null
                },
                null, TutorialPanelSide.Right);
        }

        public void ShowReturnToLobby()
        {
            Show("onboarding.deck.returnToLobby", returnToLobbyButton != null ? returnToLobbyButton.transform : null, TutorialPanelSide.Right);
        }

        private void OnNewDeckSelected()
        {
            CreateDeckSelected?.Invoke();
        }

        private void NotifyDeckCardCountChanged(int cardCount)
        {
            if (cardCount >= 15)
            {
                ShowSaveDeck();
            }
        }

        private bool OnDeckSaved()
        {
            if (DeckSaved == null)
            {
                return false;
            }

            DeckSaved?.Invoke();
            return true;
        }

        private void OnReturnToLobbySelected()
        {
            ReturnToLobbySelected?.Invoke();
        }
    }
}
