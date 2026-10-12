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

        public void ShowDeckList(System.Action onNext)
        {
            Show("onboarding.deck.deckList", deckListArea != null ? deckListArea.transform : null, onNext, true);
        }

        public void ShowDeckCards(System.Action onNext)
        {
            Show("onboarding.deck.deckCards", FindDeckCardsTarget(), onNext, true);
        }

        public void ShowDeckRules(System.Action onNext)
        {
            Show("onboarding.deck.rules", deckRuleArea != null ? deckRuleArea.transform : null, onNext, true);
        }

        public void ShowOwnedCards(System.Action onNext)
        {
            Show("onboarding.deck.ownedCards", ownedCardsArea != null ? ownedCardsArea.transform : null, TutorialPanelSide.Right, onNext, true);
        }

        public void ShowCardMana(System.Action onNext)
        {
            Show("onboarding.deck.cardMana", FindFirstOwnedCard(), TutorialPanelSide.Right, onNext, true);
        }

        public void ShowSaveButton(System.Action onNext)
        {
            Show("onboarding.deck.saveButton", saveButton != null ? saveButton.transform : null, onNext, true);
        }

        public void ShowReturnToLobby()
        {
            Show("onboarding.deck.returnToLobby", returnToLobbyButton != null ? returnToLobbyButton.transform : null, TutorialPanelSide.Right);
        }

        private void OnReturnToLobbySelected()
        {
            ReturnToLobbySelected?.Invoke();
        }

        /// <summary>
        /// 덱에 든 카드 목록의 ScrollRect 를 짚는다. currentDeckCardsArea 는 덱 목록과 저장 버튼까지
        /// 담은 오른쪽 판 전체라서, 카드 목록만 보이게 하려면 목록을 감싼 ScrollRect 를 올려야 한다.
        /// </summary>
        private Transform FindDeckCardsTarget()
        {
            Transform container = deckManagementController != null ? deckManagementController.deckCardsContainer : null;
            ScrollRect scrollRect = container != null ? container.GetComponentInParent<ScrollRect>() : null;
            if (scrollRect != null)
            {
                return scrollRect.transform;
            }

            return currentDeckCardsArea != null ? currentDeckCardsArea.transform : null;
        }

        /// <summary>
        /// 가진 카드 목록에서 맨 앞의 열린 카드를 짚는다. 잠긴 카드는 자물쇠와 여는 조건이 덮고 있으므로
        /// 건너뛴다. 목록을 아직 못 불러왔으면 가진 카드 영역 전체를 짚는다.
        /// </summary>
        private Transform FindFirstOwnedCard()
        {
            Transform container = deckManagementController != null ? deckManagementController.ownedCardsContainer : null;
            if (container != null)
            {
                foreach (Transform card in container)
                {
                    Button button = card.GetComponent<Button>();
                    if (card.gameObject.activeInHierarchy && button != null && button.interactable)
                    {
                        return card;
                    }
                }
            }

            return ownedCardsArea != null ? ownedCardsArea.transform : null;
        }
    }
}
