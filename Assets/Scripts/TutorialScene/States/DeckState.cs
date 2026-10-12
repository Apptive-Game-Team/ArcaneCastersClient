using UnityEngine.SceneManagement;

namespace TutorialScene
{
    public class DeckState : ITutorialState
    {
        private const string LobbySceneName = "LobbyScene";

        private LobbyTutorialController lobbyController;
        private DeckTutorialController deckController;

        public void Enter()
        {
            GlobalTutorialManager.Instance.SetProgress(OnboardingProgress.Deck_EnterScene);
            SceneManager.sceneLoaded += OnSceneLoaded;
            GlobalTutorialManager.Instance.LoadSceneIfNeeded(LobbySceneName);
            TryStartLobbyStep();
        }

        public void Update()
        {
        }

        public void Exit()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnbindLobby();
            UnbindDeck();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            switch (GlobalTutorialManager.Instance.CurrentProgress)
            {
                case OnboardingProgress.Deck_EnterScene:
                    TryStartLobbyStep();
                    break;
                // 설명 중에 저장 버튼을 누르면 덱 화면이 다시 열린다. 그때는 설명을 처음부터 다시 한다.
                case OnboardingProgress.Deck_ExplainCard:
                case OnboardingProgress.Deck_ExplainDeck:
                    TryStartDeckSceneSteps();
                    break;
                case OnboardingProgress.Deck_ReturnToLobby:
                    CompleteDeckState();
                    break;
            }
        }

        private void TryStartLobbyStep()
        {
            lobbyController = LobbyTutorialController.Instance;
            if (lobbyController == null)
            {
                return;
            }

            lobbyController.ShowDeckButton(OnDeckButtonClicked);
        }

        private void OnDeckButtonClicked()
        {
            UnbindLobby();
            GlobalTutorialManager.Instance.SetProgress(OnboardingProgress.Deck_ExplainCard);
        }

        private void TryStartDeckSceneSteps()
        {
            deckController = DeckTutorialController.Instance;
            if (deckController == null)
            {
                return;
            }

            ShowDeckListStep();
        }

        // 덱 화면을 한 부분씩 짚으며 다음 버튼으로 넘긴다.
        // 덱 목록, 덱에 든 카드, 덱 규칙, 가진 카드, 카드 한 장, 저장 버튼 순서다.
        private void ShowDeckListStep()
        {
            GlobalTutorialManager.Instance.SetProgress(OnboardingProgress.Deck_ExplainDeck);
            deckController.ShowDeckList(ShowDeckCardsStep);
        }

        private void ShowDeckCardsStep()
        {
            GlobalTutorialManager.Instance.SetProgress(OnboardingProgress.Deck_ExplainCard);
            deckController.ShowDeckCards(ShowDeckRulesStep);
        }

        private void ShowDeckRulesStep()
        {
            GlobalTutorialManager.Instance.SetProgress(OnboardingProgress.Deck_ExplainDeck);
            deckController.ShowDeckRules(ShowOwnedCardsStep);
        }

        private void ShowOwnedCardsStep()
        {
            GlobalTutorialManager.Instance.SetProgress(OnboardingProgress.Deck_ExplainCard);
            deckController.ShowOwnedCards(ShowCardManaStep);
        }

        private void ShowCardManaStep()
        {
            GlobalTutorialManager.Instance.SetProgress(OnboardingProgress.Deck_ExplainCard);
            deckController.ShowCardMana(ShowSaveButtonStep);
        }

        private void ShowSaveButtonStep()
        {
            GlobalTutorialManager.Instance.SetProgress(OnboardingProgress.Deck_ExplainDeck);
            deckController.ShowSaveButton(ShowReturnToLobbyStep);
        }

        private void ShowReturnToLobbyStep()
        {
            GlobalTutorialManager.Instance.SetProgress(OnboardingProgress.Deck_ReturnToLobby);
            deckController.ReturnToLobbySelected -= OnReturnToLobbySelected;
            deckController.ReturnToLobbySelected += OnReturnToLobbySelected;
            deckController.ShowReturnToLobby();
        }

        private void OnReturnToLobbySelected()
        {
            deckController.ReturnToLobbySelected -= OnReturnToLobbySelected;
        }

        private void CompleteDeckState()
        {
            GlobalTutorialManager.Instance.CompleteTask(new MagicBookState());
        }

        private void UnbindLobby()
        {
            if (lobbyController != null)
            {
                lobbyController.HideDeckButton(OnDeckButtonClicked);
                lobbyController = null;
            }
        }

        private void UnbindDeck()
        {
            if (deckController != null)
            {
                deckController.ReturnToLobbySelected -= OnReturnToLobbySelected;
                deckController.Hide();
                deckController = null;
            }
        }
    }
}
