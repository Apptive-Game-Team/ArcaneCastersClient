using Global;
using UnityEngine;
using UnityEngine.Localization;

namespace LobbyScene
{
    public class MatchingMessageController : MonoBehaviour
    {
        [SerializeField] private LobbySceneViewModel viewModel;
        [SerializeField] private LocalizedString matchingFailMessage;
        [SerializeField] private LocalizedString matchingCancelMessage;
        [SerializeField] private LocalizedString practiceFailMessage;
        
        private void OnEnable()
        {
            viewModel.OnMatchingFailed += ShowMatchingFailMessage;
            viewModel.OnMatchingCanceled += ShowMatchingCancelMessage;
            viewModel.OnPracticeFailed += ShowPracticeFailMessage;
        }

        private void OnDisable()
        {
            viewModel.OnMatchingFailed -= ShowMatchingFailMessage;
            viewModel.OnMatchingCanceled -= ShowMatchingCancelMessage;
            viewModel.OnPracticeFailed -= ShowPracticeFailMessage;
        }

        private void ShowMatchingFailMessage()
        {
            SystemMessageUI.Instance.ShowSceneMessage(matchingFailMessage);
        }

        private void ShowMatchingCancelMessage()
        {
            SystemMessageUI.Instance.ShowSceneMessage(matchingCancelMessage);
        }

        private void ShowPracticeFailMessage()
        {
            SystemMessageUI.Instance.ShowMessage(practiceFailMessage);
        }
    }
}
