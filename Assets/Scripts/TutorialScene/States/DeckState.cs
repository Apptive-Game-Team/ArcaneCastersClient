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
                case OnboardingProgress.Deck_ExplainCard:
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

            GlobalTutorialManager.Instance.SetProgress(OnboardingProgress.Deck_ExplainCard);
            ShowDeckRules();
        }

        private void ShowDeckRules()
        {
            GlobalTutorialManager.Instance.SetProgress(OnboardingProgress.Deck_ExplainDeck);
            deckController.ShowDeckRules(ShowCreateDeckStep);
        }

        private void ShowCreateDeckStep()
        {
            GlobalTutorialManager.Instance.SetProgress(OnboardingProgress.Deck_SelectCreateDeck);
            deckController.CreateDeckSelected += OnCreateDeckSelected;
            deckController.ShowCreateDeck();
        }

        private void OnCreateDeckSelected()
        {
            deckController.CreateDeckSelected -= OnCreateDeckSelected;
            GlobalTutorialManager.Instance.SetProgress(OnboardingProgress.Deck_SaveDeck);
            deckController.DeckSaved += OnDeckSaved;
            deckController.ShowDeckEditor();
        }

        private void OnDeckSaved()
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
                deckController.CreateDeckSelected -= OnCreateDeckSelected;
                deckController.DeckSaved -= OnDeckSaved;
                deckController.ReturnToLobbySelected -= OnReturnToLobbySelected;
                deckController.Hide();
                deckController = null;
            }
        }
    }
}
