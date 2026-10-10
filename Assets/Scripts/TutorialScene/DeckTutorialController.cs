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

        public event System.Action ReturnToLobbySelected;

        private bool isSubscribedToReturnButton;

        private void OnEnable()
        {
            // This controller lives in the regular deck scene, so it must stay inert
            // unless the deck onboarding step is the one currently running.
            if (!GlobalTutorialManager.IsDeckOnboardingActive())
            {
                return;
            }

            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.OnClick += OnReturnToLobbySelected;
            }

            isSubscribedToReturnButton = true;
        }

        private void OnDisable()
        {
            if (!isSubscribedToReturnButton)
            {
                return;
            }

            isSubscribedToReturnButton = false;

            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.OnClick -= OnReturnToLobbySelected;
            }
        }

        public void ShowDeckRules(System.Action onNext)
        {
            Show("onboarding.deck.rules", deckRuleArea != null ? deckRuleArea.transform : null, onNext);
        }

        public void ShowReturnToLobby()
        {
            Show("onboarding.deck.returnToLobby", returnToLobbyButton != null ? returnToLobbyButton.transform : null, TutorialPanelSide.Right);
        }

        private void OnReturnToLobbySelected()
        {
            ReturnToLobbySelected?.Invoke();
        }
    }
}
