using Global;
using UnityEngine.SceneManagement;

namespace TutorialScene
{
    public class GlobalTutorialManager : SingletonObject<GlobalTutorialManager>
    {
        private const string LobbySceneName = "LobbyScene";
        private const string BattleTutorialSceneName = "InteractiveTutorialScene";

        private ITutorialState currentState;

        public OnboardingProgress CurrentProgress { get; private set; } = OnboardingProgress.Battle;

        private void Start()
        {
            if (CurrentProgress == OnboardingProgress.Battle)
            {
                CompleteTask(new BattleState());
            }
        }

        public void CompleteTask(ITutorialState nextState)
        {
            if (currentState != null)
            {
                currentState.Exit();
            }
            currentState = nextState;
            currentState.Enter();
        }

        public void StartOnboardingTutorial()
        {
            SetProgress(OnboardingProgress.Battle);
            LoadSceneIfNeeded(BattleTutorialSceneName);
            CompleteTask(new BattleState());
        }

        public void SkipOnboardingTutorial()
        {
            SetProgress(OnboardingProgress.Skipped);
            CompleteOnboarding();
            LoadSceneIfNeeded(LobbySceneName);
        }

        public void SetProgress(OnboardingProgress progress)
        {
            // 덱 만들기·저장 단계는 없어졌다. 옛 값이 들어오면 덱 단계의 마지막 단계로 보낸다.
            switch (progress)
            {
                case OnboardingProgress.Deck_SelectCreateDeck:
                case OnboardingProgress.Deck_CreateDeck:
                case OnboardingProgress.Deck_SaveDeck:
                    progress = OnboardingProgress.Deck_ReturnToLobby;
                    break;
            }

            CurrentProgress = progress;
        }

        public void Update()
        {
            if (currentState != null)
            {
                currentState.Update();
            }
        }

        internal void LoadSceneIfNeeded(string sceneName)
        {
            if (SceneManager.GetActiveScene().name == sceneName)
            {
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        private bool completed;

        // 두 번 불려도 안전해야 한다. 이 오브젝트는 스스로를 파괴하므로, 두 번째 호출은
        // 파괴된 오브젝트에 Destroy를 걸어 MissingReferenceException을 낸다. 진행이 이미
        // 끝났다는 사실 자체는 오류가 아니라 그냥 할 일이 없는 상태다.
        private void CompleteOnboarding()
        {
            if (completed)
            {
                return;
            }

            completed = true;

            if (currentState != null)
            {
                currentState.Exit();
                currentState = null;
            }

            Destroy(gameObject);
        }

        public void FinishOnboarding()
        {
            SetProgress(OnboardingProgress.Completed);
            CompleteOnboarding();
        }

        public static OnboardingProgress GetCurrentProgress()
        {
            return Instance != null ? Instance.CurrentProgress : OnboardingProgress.None;
        }

        public static bool IsDeckOnboardingActive()
        {
            switch (GetCurrentProgress())
            {
                case OnboardingProgress.Deck_EnterScene:
                case OnboardingProgress.Deck_ExplainCard:
                case OnboardingProgress.Deck_ExplainDeck:
                case OnboardingProgress.Deck_SelectCreateDeck:
                case OnboardingProgress.Deck_CreateDeck:
                case OnboardingProgress.Deck_SaveDeck:
                case OnboardingProgress.Deck_ReturnToLobby:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsMagicBookOnboardingActive()
        {
            switch (GetCurrentProgress())
            {
                case OnboardingProgress.MagicBook_EnterScene:
                case OnboardingProgress.MagicBook_ExplainMagicBook:
                case OnboardingProgress.MagicBook_SelectMagic:
                case OnboardingProgress.MagicBook_ExplainMagicInfo:
                case OnboardingProgress.MagicBook_OpenElementChart:
                case OnboardingProgress.MagicBook_ReturnToLobby:
                    return true;
                default:
                    return false;
            }
        }
    }
}
