using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace LobbyScene.Matching
{
    /// <summary>
    /// 일반 매칭과 연습경기 준비가 끝날 때까지 대기 화면을 표시한다.
    /// </summary>
    public sealed class MatchingPage : MonoBehaviour
    {
        [SerializeField] private GameObject content;
        [SerializeField] private LocalizeStringEvent titleLabel;
        [SerializeField] private GameObject[] matchmakingOnly;

        private LocalizedString matchingTitle;
        private readonly LocalizedString practiceTitle = new LocalizedString("LobbyUI", "PracticePreparing");

        private void Awake()
        {
            matchingTitle = titleLabel.StringReference;
        }

        private void Start()
        {
            LobbySceneViewModel.Instance.CurrentState.OnStateChange += ApplyLobbyState;
            ApplyLobbyState(LobbySceneViewModel.Instance.CurrentState.Data);
        }

        private void OnDestroy()
        {
            if (LobbySceneViewModel.Instance == null) return;
            LobbySceneViewModel.Instance.CurrentState.OnStateChange -= ApplyLobbyState;
        }

        private void ApplyLobbyState(LobbySceneViewModel.LobbyState state)
        {
            bool practice = state == LobbySceneViewModel.LobbyState.PreparingPractice;
            titleLabel.StringReference = practice ? practiceTitle : matchingTitle;
            foreach (GameObject widget in matchmakingOnly) widget.SetActive(!practice);
            content.SetActive(practice || state == LobbySceneViewModel.LobbyState.Matching);
        }
    }
}
