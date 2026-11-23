using UnityEngine.SceneManagement;

namespace TutorialScene
{
    public class MagicBookState : ITutorialState
    {
        private const string LobbySceneName = "LobbyScene";

        private LobbyTutorialController lobbyController;
        private MagicBookTutorialController magicBookController;

        public void Enter()
        {
            GlobalTutorialManager.Instance.SetProgress(OnboardingProgress.MagicBook_EnterScene);
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
            UnbindMagicBook();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            switch (GlobalTutorialManager.Instance.CurrentProgress)
            {
                case OnboardingProgress.MagicBook_EnterScene:
                    TryStartLobbyStep();
                    break;
                case OnboardingProgress.MagicBook_ExplainMagicBook:
                    TryStartMagicBookSceneSteps();
                    break;
                case OnboardingProgress.MagicBook_ReturnToLobby:
                    CompleteMagicBookState();
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

            lobbyController.ShowMagicBookButton(OnMagicBookButtonClicked);
        }

        private void OnMagicBookButtonClicked()
        {
            UnbindLobby();
            GlobalTutorialManager.Instance.SetProgress(OnboardingProgress.MagicBook_ExplainMagicBook);
        }

        private void TryStartMagicBookSceneSteps()
        {
            magicBookController = MagicBookTutorialController.Instance;
            if (magicBookController == null)
            {
                return;
            }

            magicBookController.ShowMagicBook(ShowMagicSelection);
        }

        private void ShowMagicSelection()
        {
            GlobalTutorialManager.Instance.SetProgress(OnboardingProgress.MagicBook_SelectMagic);
            magicBookController.MagicSelected += OnMagicSelected;
            magicBookController.ShowMagicSelection();
        }

        private void OnMagicSelected()
        {
            magicBookController.MagicSelected -= OnMagicSelected;
            GlobalTutorialManager.Instance.SetProgress(OnboardingProgress.MagicBook_ExplainMagicInfo);
            magicBookController.ShowMagicInfo(ShowReturnToLobbyStep);
        }

        private void ShowReturnToLobbyStep()
        {
            GlobalTutorialManager.Instance.SetProgress(OnboardingProgress.MagicBook_ReturnToLobby);
            magicBookController.ReturnToLobbySelected += OnReturnToLobbySelected;
            magicBookController.ShowReturnToLobby();
        }

        private void OnReturnToLobbySelected()
        {
            magicBookController.ReturnToLobbySelected -= OnReturnToLobbySelected;
        }

        private void CompleteMagicBookState()
        {
            GlobalTutorialManager.Instance.CompleteTask(new LobbyCommonState());
        }

        private void UnbindLobby()
        {
            if (lobbyController != null)
            {
                lobbyController.HideMagicBookButton(OnMagicBookButtonClicked);
                lobbyController = null;
            }
        }

        private void UnbindMagicBook()
        {
            if (magicBookController != null)
            {
                magicBookController.MagicSelected -= OnMagicSelected;
                magicBookController.ReturnToLobbySelected -= OnReturnToLobbySelected;
                magicBookController.Hide();
                magicBookController = null;
            }
        }
    }
}
