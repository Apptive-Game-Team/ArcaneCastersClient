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
        
        private void OnEnable()
        {
            viewModel.OnMatchingStart += ShowMatchingStartMessage;
            viewModel.OnMatchingFailed += ShowMatchingFailMessage;
            viewModel.OnMatchingCanceled += ShowMatchingCancelMessage;
        }

        private void OnDisable()
        {
            viewModel.OnMatchingStart -= ShowMatchingStartMessage;
            viewModel.OnMatchingFailed -= ShowMatchingFailMessage;
            viewModel.OnMatchingCanceled -= ShowMatchingCancelMessage;
        }

        private void ShowMatchingStartMessage()
        {
            SystemMessageUI.Instance.ShowSceneMessage(matchingStartMessage);
        }

        private void ShowMatchingFailMessage()
        {
            SystemMessageUI.Instance.ShowSceneMessage(matchingFailMessage);
        }

        private void ShowMatchingCancelMessage()
        {
            SystemMessageUI.Instance.ShowSceneMessage(matchingCancelMessage);
        }
    }
}
