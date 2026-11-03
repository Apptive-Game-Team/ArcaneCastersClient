using TutorialScene;
using UnityEngine;

namespace LobbyScene.Button
{
    public class PracticeButton : LobbyButtonBase
    {
        [SerializeField] private PracticeDifficultyPanel difficultyPanel;

        protected override void OnClickButton()
        {
            if (!isActive) return;

            // 온보딩의 첫 전투는 기존 접대 봇 배정을 거친다.
            if (GlobalTutorialManager.GetCurrentProgress() == OnboardingProgress.HospitalityBattle)
            {
                LobbySceneViewModel.Instance.PlayPracticeMatch();
                return;
            }
            difficultyPanel.gameObject.SetActive(true);
        }
    }
}
