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
        private const float PreparationMessageSeconds = 2.5f;

        [SerializeField] private GameObject content;
        [SerializeField] private LocalizeStringEvent titleLabel;
        [SerializeField] private GameObject[] matchmakingOnly;

        private LocalizedString matchingTitle;
        private readonly LocalizedString connectingTitle = new LocalizedString("LobbyUI", "GameServerConnecting");
        private readonly LocalizedString cancelingTitle = new LocalizedString("LobbyUI", "MatchingStateCancelPending");
        private readonly LocalizedString[] preparationTitles =
        {
            new LocalizedString("LobbyUI", "GameSessionPreparing"),
            new LocalizedString("LobbyUI", "PreparingTrees"),
            new LocalizedString("LobbyUI", "PreparingRiver"),
        };
        private LobbySceneViewModel viewModel;
        private bool preparingSession;
        private int preparationMessageIndex;
        private float nextPreparationMessageAt;

        private void Awake()
        {
            matchingTitle = titleLabel.StringReference;
        }

        private void Start()
        {
            viewModel = LobbySceneViewModel.Instance;
            viewModel.CurrentState.OnStateChange += ApplyLobbyState;
            viewModel.TicketState.OnStateChange += ApplyTicketState;
            viewModel.OnGameConnectionStateChanged += Render;
            Render();
        }

        private void OnDestroy()
        {
            if (viewModel == null) return;
            viewModel.CurrentState.OnStateChange -= ApplyLobbyState;
            viewModel.TicketState.OnStateChange -= ApplyTicketState;
            viewModel.OnGameConnectionStateChanged -= Render;
        }

        private void Update()
        {
            if (!preparingSession || Time.realtimeSinceStartup < nextPreparationMessageAt) return;
            preparationMessageIndex = (preparationMessageIndex + 1) % preparationTitles.Length;
            nextPreparationMessageAt = Time.realtimeSinceStartup + PreparationMessageSeconds;
            titleLabel.StringReference = preparationTitles[preparationMessageIndex];
        }

        private void ApplyLobbyState(LobbySceneViewModel.LobbyState state)
        {
            Render();
        }

        private void ApplyTicketState(MatchTicketState state)
        {
            Render();
        }

        private void Render()
        {
            bool connecting = viewModel.PreparingGameConnection;
            bool preparing = !connecting &&
                (viewModel.CurrentState.Data == LobbySceneViewModel.LobbyState.PreparingPractice ||
                 (viewModel.CurrentState.Data == LobbySceneViewModel.LobbyState.Matching &&
                  viewModel.TicketState.Data == MatchTicketState.Allocating));
            if (preparing && !preparingSession)
            {
                preparationMessageIndex = 0;
                nextPreparationMessageAt = Time.realtimeSinceStartup + PreparationMessageSeconds;
            }
            preparingSession = preparing;

            titleLabel.StringReference = connecting ? connectingTitle :
                viewModel.TicketState.Data == MatchTicketState.CancelPending ? cancelingTitle :
                preparing ? preparationTitles[preparationMessageIndex] : matchingTitle;
            foreach (GameObject widget in matchmakingOnly) widget.SetActive(!preparing && !connecting);
            content.SetActive(connecting || preparing ||
                viewModel.CurrentState.Data == LobbySceneViewModel.LobbyState.Matching);
        }
    }
}
