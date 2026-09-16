using System;
using System.Collections;
using Data;
using Global;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LobbyScene
{
    public class LobbySceneViewModel : LocalSingletonObject<LobbySceneViewModel>
    {
        public const string SelectedDeckMode = "SELECTED";
        public const string RandomDeckMode = "RANDOM";

        [SerializeField] private MatchQueueApiService _matchQueueApi;
        private MatchmakingCoordinator coordinator;
        private bool enteringGameScene;
        public string DeckMode { get; set; } = SelectedDeckMode;

        public enum LobbyState
        {
            Idle,
            Matching,
        }

        public StateEvent<LobbyState> CurrentState = new (LobbyState.Idle);

        // LobbyState only says "matching or not". The matching page needs the ticket
        // state behind it to tell waiting apart from allocating or reconnecting.
        public StateEvent<MatchTicketState> TicketState = new (MatchTicketState.Idle);

        public event Action OnMatchingStart;
        public event Action OnMatchingStop;
        public event Action OnMatchingFailed;
        public event Action OnMatchingCanceled;
        protected override void Awake()
        {
            base.Awake();
            coordinator = gameObject.GetComponent<MatchmakingCoordinator>();
            if (coordinator == null) coordinator = gameObject.AddComponent<MatchmakingCoordinator>();
            coordinator.StateChanged += HandleMatchmakingState;
            coordinator.Matched += OnMatched;
            coordinator.Initialize(_matchQueueApi);
        }

        private void OnDestroy()
        {
            if (coordinator == null) return;
            coordinator.StateChanged -= HandleMatchmakingState;
            coordinator.Matched -= OnMatched;
        }

        public void Enqueue()
        {
            Debug.Log("Enqueue button clicked: Enqueueing player.");
            StartMatching();
            coordinator.Enqueue(DeckMode);
        }

        public void PlayPracticeMatch()
        {
            Debug.Log("Practice button clicked: Starting practice match.");
            StartMatching();
            StartCoroutine(_matchQueueApi.MatchPractice(dto =>
            {
                if (dto != null)
                    OnMatched(dto);
                else
                    StopMatchingAsFailed();
            }));
        }

        private void OnMatched(MatchedInfoDto matchedInfoDto)
        {
            SceneContext.MatchInfo = matchedInfoDto;
            const string targetSceneName = "GameScene";
            if (SceneManager.GetActiveScene().name.Contains(targetSceneName)) return;

            // Matched can fire more than once for the same reconnect (snapshot poll and
            // event stream both feed it), so only the first call may start the transition.
            if (enteringGameScene) return;
            enteringGameScene = true;
            StartCoroutine(EnterGameScene(targetSceneName));
        }

        private IEnumerator EnterGameScene(string targetSceneName)
        {
            if (SceneContext.User == null)
            {
                yield return UserInfoGetter.GetUserInfo();

                // GetUserInfo already redirected to LoginScene and left User null on failure.
                if (SceneContext.User == null) yield break;
            }

            yield return GameDataRefresh.Refresh(() => SceneManager.LoadScene(targetSceneName));
        }

        public void RemoveFromQueue()
        {
            Debug.Log("Remove button clicked: Removing player.");
            coordinator.Cancel();
        }

        public void CheckIfInQueue()
        {
            coordinator.RecoverSnapshot();
        }

        private void StartMatching()
        {
            CurrentState.UpdateData(LobbyState.Matching);
            OnMatchingStart?.Invoke();
        }

        private void HandleMatchmakingState(MatchTicketState state, MatchTicket ticket)
        {
            TicketState.UpdateData(state);

            switch (state)
            {
                case MatchTicketState.Queued:
                case MatchTicketState.CancelPending:
                case MatchTicketState.Allocating:
                case MatchTicketState.Reconnecting:
                    CurrentState.UpdateData(LobbyState.Matching);
                    break;
                case MatchTicketState.Canceled:
                    StopMatchingAsCanceled();
                    break;
                case MatchTicketState.Failed:
                case MatchTicketState.Expired:
                    StopMatchingAsFailed();
                    break;
                case MatchTicketState.Idle:
                    CurrentState.UpdateData(LobbyState.Idle);
                    break;
            }
        }

        private void StopMatchingAsFailed()
        {
            CurrentState.UpdateData(LobbyState.Idle);
            OnMatchingFailed?.Invoke();
            OnMatchingStop?.Invoke();
        }

        private void StopMatchingAsCanceled()
        {
            CurrentState.UpdateData(LobbyState.Idle);
            OnMatchingCanceled?.Invoke();
            OnMatchingStop?.Invoke();
        }
    }
}
