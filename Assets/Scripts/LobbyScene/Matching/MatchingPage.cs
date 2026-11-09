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
            viewModel.OnGameConnectionStateChanged += ApplyGameConnectionState;
            ApplyGameConnectionState();
        }

        private void OnDestroy()
        {
            if (viewModel == null) return;
            viewModel.CurrentState.OnStateChange -= ApplyLobbyState;
            viewModel.TicketState.OnStateChange -= ApplyTicketState;
            viewModel.OnGameConnectionStateChanged -= ApplyGameConnectionState;
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
            Render(state, viewModel.TicketState.Data);
        }

        private void ApplyTicketState(MatchTicketState state)
        {
            Render(viewModel.CurrentState.Data, state);
        }

        private void ApplyGameConnectionState()
        {
            Render(viewModel.CurrentState.Data, viewModel.TicketState.Data);
        }

        private void Render(LobbySceneViewModel.LobbyState lobbyState, MatchTicketState ticketState)
        {
            bool connecting = viewModel.PreparingGameConnection;
            bool matching = lobbyState == LobbySceneViewModel.LobbyState.Matching;
            bool preparing = !connecting &&
                (lobbyState == LobbySceneViewModel.LobbyState.PreparingPractice ||
                 (matching && ticketState == MatchTicketState.Allocating));
            if (preparing && !preparingSession)
            {
                preparationMessageIndex = 0;
                nextPreparationMessageAt = Time.realtimeSinceStartup + PreparationMessageSeconds;
            }
            preparingSession = preparing;

            titleLabel.StringReference = connecting ? connectingTitle :
                ticketState == MatchTicketState.CancelPending ? cancelingTitle :
                preparing ? preparationTitles[preparationMessageIndex] : matchingTitle;
            foreach (GameObject widget in matchmakingOnly) widget.SetActive(!preparing && !connecting);
            content.SetActive(connecting || preparing || matching);
        }
    }
}
