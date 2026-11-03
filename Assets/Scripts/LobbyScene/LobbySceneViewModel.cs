using System;
using System.Collections;
using Data;
using Global;
using Global.Serialization;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace LobbyScene
{
    public class LobbySceneViewModel : LocalSingletonObject<LobbySceneViewModel>
    {
        [SerializeField] private MatchQueueApiService _matchQueueApi;
        private MatchmakingCoordinator coordinator;
        private bool enteringGameScene;
        private MatchedInfoDto failedEntry;
        public bool PreparingGameConnection => enteringGameScene;
        public event Action OnGameConnectionStateChanged;
        public string DeckMode { get; set; } = MatchDeckMode.Selected;

        public enum LobbyState
        {
            Idle,
            Matching,
            // 연습경기는 티켓 없이 준비하며, 전용 안내를 게임씬 전환까지 유지한다.
            PreparingPractice,
        }

        public StateEvent<LobbyState> CurrentState = new (LobbyState.Idle);

        // LobbyState only says "matching or not". The matching page needs the ticket
        // state behind it to tell waiting apart from allocating or reconnecting.
        public StateEvent<MatchTicketState> TicketState = new (MatchTicketState.Idle);

        public event Action OnMatchingStart;
        public event Action OnMatchingStop;
        public event Action OnMatchingFailed;
        public event Action OnMatchingCanceled;
        public event Action OnPracticeStart;
        public event Action OnPracticeFailed;
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
            if (RetryEntry(StartEnqueue)) return;
            StartEnqueue();
        }

        private void StartEnqueue()
        {
            Debug.Log("Enqueue button clicked: Enqueueing player.");
            StartMatching();
            coordinator.Enqueue(DeckMode);
        }

        public IEnumerator LoadPracticeDifficulties(Action<PracticeDifficultiesDto> callback)
        {
            return _matchQueueApi.PracticeDifficulties(callback);
        }

        public void PlayPracticeMatch() => PlayPracticeMatch(null);

        public void PlayPracticeMatch(string difficultyId)
        {
            if (CurrentState.Data != LobbyState.Idle) return;
            if (RetryEntry(() => StartPracticeMatch(difficultyId))) return;
            StartPracticeMatch(difficultyId);
        }

        private void StartPracticeMatch(string difficultyId)
        {
            CurrentState.UpdateData(LobbyState.PreparingPractice);
            OnPracticeStart?.Invoke();
            StartCoroutine(difficultyId == null
                ? _matchQueueApi.MatchPractice(OnPracticeMatched)
                : _matchQueueApi.MatchDifficulty(difficultyId, OnPracticeMatched));
        }

        private void OnPracticeMatched(MatchedInfoDto dto)
        {
            if (dto != null)
            {
                OnMatched(dto);
                return;
            }

            CurrentState.UpdateData(LobbyState.Idle);
            OnPracticeFailed?.Invoke();
        }

        public void EnterFriendMatch(MatchedInfoDto matchedInfoDto)
        {
            if (matchedInfoDto == null) return;
            OnMatched(matchedInfoDto);
        }

        private void OnMatched(MatchedInfoDto matchedInfoDto)
        {
            if (enteringGameScene || failedEntry?.sessionId == matchedInfoDto.sessionId) return;
            SceneContext.MatchInfo = matchedInfoDto;
            // PVP/practice matches from the lobby are never an adventure match; guarantee the
            // forest default in case a previous adventure match left a theme set.
            SceneContext.ClearAdventureMatch();
            const string targetSceneName = "GameScene";
            if (SceneManager.GetActiveScene().name.Contains(targetSceneName)) return;

            // Taken from the server's mapType; no mapType keeps the scene default.
            SceneContext.PrepareMap(matchedInfoDto.mapType, null);

            // Matched can fire more than once for the same reconnect (snapshot poll and
            // event stream both feed it), so only the first call may start the transition.
            enteringGameScene = true;
            CurrentState.UpdateData(LobbyState.Matching);
            OnGameConnectionStateChanged?.Invoke();
            StartCoroutine(EnterGameScene(targetSceneName));
        }

        private IEnumerator EnterGameScene(string targetSceneName)
        {
            if (SceneContext.User == null)
            {
                yield return UserInfoGetter.GetUserInfo();

                // GetUserInfo already redirected to LoginScene and left User null on failure.
                if (SceneContext.User == null) { EntryFailed(); yield break; }
            }

            yield return GameDataRefresh.Refresh();
            yield return GameSceneEntry.PrepareAndLoad(this, EntryFailed, targetSceneName);
        }

        private bool RetryEntry(Action startNewMatch)
        {
            if (enteringGameScene) return true;
            if (failedEntry == null) return false;
            var retry = failedEntry;
            enteringGameScene = true;
            CurrentState.UpdateData(LobbyState.Matching);
            OnGameConnectionStateChanged?.Invoke();
            StartCoroutine(RevalidateEntry(retry, startNewMatch));
            return true;
        }

        private IEnumerator RevalidateEntry(MatchedInfoDto retry, Action startNewMatch)
        {
            // 연결 실패만으로 세션 소실을 단정하지 않는다. 로비가 호스트의 생존을 확인한 응답을 쓴다.
            using var request = UnityWebRequest.Get(
                $"{ServerList.MatchingServer.url}/api/users/mine/match-info");
            request.timeout = 15;
            Server.SetAcceptLanguage(request);
            Server.SetAuthorization(request);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success &&
                JsonCodec.TryDeserialize(request.downloadHandler.text, out MatchedInfoDto activeMatch, out _) &&
                activeMatch != null && !string.IsNullOrWhiteSpace(activeMatch.sessionId))
            {
                failedEntry = null;
                enteringGameScene = false;
                OnMatched(activeMatch);
                yield break;
            }

            if (request.result == UnityWebRequest.Result.ProtocolError &&
                (request.responseCode == 400 || request.responseCode == 404 || request.responseCode == 410))
            {
                // match-info 조회만으로 MATCHED 티켓은 해제되지 않는다. 서버의 정리 판정을 기다린다.
                SceneContext.MatchInfo = retry;
                SessionLossVerdict verdict = SessionLossVerdict.NotReported;
                yield return SessionLossReporter.Report(SessionLossReason.SessionRecoveryFailed, result => verdict = result);
                if (!verdict.ClearsMatchState())
                {
                    EntryFailed();
                    yield break;
                }
                failedEntry = null;
                enteringGameScene = false;
                CurrentState.UpdateData(LobbyState.Idle);
                OnGameConnectionStateChanged?.Invoke();
                startNewMatch();
                yield break;
            }

            // 인증 오류·서버 오류·전송 실패는 판정 보류다. 중복 생성 없이 수동 재시도를 남긴다.
            SceneContext.MatchInfo = retry;
            EntryFailed();
        }

        public void EntryFailed()
        {
            failedEntry = SceneContext.MatchInfo;
            enteringGameScene = false;
            CurrentState.UpdateData(LobbyState.Idle);
            OnGameConnectionStateChanged?.Invoke();
            OnMatchingFailed?.Invoke();
            OnMatchingStop?.Invoke();
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
            // 연습 요청 중에는 일반 매칭 복구 응답이 대기 화면을 덮어쓰지 않게 한다.
            if (CurrentState.Data == LobbyState.PreparingPractice) return;
            if (failedEntry != null && !enteringGameScene)
            {
                if (state == MatchTicketState.Reconnecting ||
                    (state == MatchTicketState.Matched && ticket?.matchInfo?.sessionId == failedEntry.sessionId))
                {
                    TicketState.UpdateData(state);
                    CurrentState.UpdateData(LobbyState.Idle);
                    return;
                }
                if (state != MatchTicketState.Idle) failedEntry = null;
            }
            MatchTicketState previousState = TicketState.Data;
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
                    // Idle right after CancelPending is a cancel the server resolved with no
                    // active ticket. Any other Idle (a practice or friend match holds no ticket)
                    // is not a cancel and must not show the cancel message.
                    if (previousState == MatchTicketState.CancelPending)
                        StopMatchingAsCanceled();
                    else
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
