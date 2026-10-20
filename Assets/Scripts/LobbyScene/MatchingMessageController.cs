using Global;
using UnityEngine;
using UnityEngine.Localization;

namespace LobbyScene
{
    public class MatchingMessageController : MonoBehaviour
    {
        [SerializeField] private LobbySceneViewModel viewModel;
        [SerializeField] private LocalizedString matchingStartMessage;
        [SerializeField] private LocalizedString matchingFailMessage;
        [SerializeField] private LocalizedString matchingCancelMessage;
        [SerializeField] private LocalizedString practicePreparingMessage;
        [SerializeField] private LocalizedString practiceFailMessage;
        
        private void OnEnable()
        {
            viewModel.OnMatchingStart += ShowMatchingStartMessage;
            viewModel.OnMatchingFailed += ShowMatchingFailMessage;
            viewModel.OnMatchingCanceled += ShowMatchingCancelMessage;
            viewModel.OnPracticeStart += ShowPracticePreparingMessage;
            viewModel.OnPracticeFailed += ShowPracticeFailMessage;
        }

        private void OnDisable()
        {
            viewModel.OnMatchingStart -= ShowMatchingStartMessage;
            viewModel.OnMatchingFailed -= ShowMatchingFailMessage;
            viewModel.OnMatchingCanceled -= ShowMatchingCancelMessage;
            viewModel.OnPracticeStart -= ShowPracticePreparingMessage;
            viewModel.OnPracticeFailed -= ShowPracticeFailMessage;
        }

        private void ShowMatchingStartMessage()
        {
            SystemMessageUI.Instance.ShowMessage(matchingStartMessage);
        }

        private void ShowMatchingFailMessage()
        {
            SystemMessageUI.Instance.ShowMessage(matchingFailMessage);
        }

        private void ShowMatchingCancelMessage()
        {
            SystemMessageUI.Instance.ShowMessage(matchingCancelMessage);
        }

        private void ShowPracticePreparingMessage()
        {
            SystemMessageUI.Instance.ShowMessage(practicePreparingMessage);
        }

        private void ShowPracticeFailMessage()
        {
            SystemMessageUI.Instance.ShowMessage(practiceFailMessage);
        }
    }
}
