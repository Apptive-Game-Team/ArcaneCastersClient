using System;
using System.Collections;
using Data;
using Global;
using UnityEngine;

namespace LobbyScene
{
    public sealed class MatchmakingCoordinator : MonoBehaviour
    {
        private const float ReconnectDelaySeconds = 3f;
        private const float RecoveryPollIntervalSeconds = 15f;

        private readonly MatchTicketReducer reducer = new MatchTicketReducer();
        private MatchQueueApiService api;
        private MatchEventStream eventStream;
        private bool snapshotRequestInFlight;
        private bool stopping;
        private Coroutine reconnectCoroutine;
        private Coroutine fallbackCoroutine;
        private Coroutine snapshotRetryCoroutine;

        public MatchTicketState State { get; private set; } = MatchTicketState.Idle;
        public MatchTicket CurrentTicket => reducer.Current;
        public event Action<MatchTicketState, MatchTicket> StateChanged;
        public event Action<MatchedInfoDto> Matched;

        public void Initialize(MatchQueueApiService apiService)
        {
            if (api != null) return;
            api = apiService;
            eventStream = gameObject.AddComponent<MatchEventStream>();
            eventStream.TicketReceived += HandleStreamTicket;
            eventStream.Disconnected += HandleStreamDisconnected;
            ConnectAndRecover();
        }

        public void Enqueue(string deckMode)
        {
            StartCoroutine(api.CreateTicket(deckMode, ticket =>
            {
                if (ticket == null)
                {
                    PublishLocalState(MatchTicketState.Failed);
                    return;
                }
                ApplyTicket(ticket);
            }));
        }

        public void Cancel()
        {
            MatchTicket ticket = reducer.Current;
            if (ticket == null || (ticket.ParsedState != MatchTicketState.Queued &&
                                   ticket.ParsedState != MatchTicketState.Allocating))
                return;

            PublishLocalState(MatchTicketState.CancelPending);
            StartCoroutine(api.CancelTicket(ticket.ticketId, result =>
            {
                if (result?.ticket != null)
                {
                    if (!ApplyTicket(result.ticket)) RestoreServerStateFromTransientState();
                    return;
                }

                // Unknown outcome: never infer cancellation. Snapshot resolves race/failure.
                RecoverSnapshot();
            }));
        }

        public void RecoverSnapshot()
        {
            if (snapshotRequestInFlight || api == null) return;
            snapshotRequestInFlight = true;
            StartCoroutine(api.GetActiveTicket((requestSucceeded, ticket) =>
            {
                snapshotRequestInFlight = false;
                if (!requestSucceeded)
                {
                    PublishLocalState(MatchTicketState.Reconnecting);
                    if (snapshotRetryCoroutine == null)
                        snapshotRetryCoroutine = StartCoroutine(RetrySnapshot());
                    return;
                }
                if (ticket == null)
                {
                    if (reducer.Current == null) PublishLocalState(MatchTicketState.Idle);
                    return;
                }
                if (!ApplyTicket(ticket)) RestoreServerStateFromTransientState();
            }));
        }

        private IEnumerator RetrySnapshot()
        {
            yield return new WaitForSecondsRealtime(RecoveryPollIntervalSeconds);
            snapshotRetryCoroutine = null;
            if (!stopping) RecoverSnapshot();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) RecoverSnapshot();
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (!isPaused) RecoverSnapshot();
        }

        private void Update()
        {
            eventStream?.DrainCallbacks();
        }

        private void OnDestroy()
        {
            stopping = true;
            if (eventStream != null)
            {
                eventStream.TicketReceived -= HandleStreamTicket;
                eventStream.Disconnected -= HandleStreamDisconnected;
                eventStream.Dispose();
            }
        }

        private void ConnectAndRecover()
        {
            eventStream.Connect();
            RecoverSnapshot();
        }

        private void HandleStreamTicket(MatchTicket ticket)
        {
            ApplyTicket(ticket);
        }

        private void HandleStreamDisconnected()
        {
            if (stopping) return;
            PublishLocalState(MatchTicketState.Reconnecting);
            if (fallbackCoroutine == null) fallbackCoroutine = StartCoroutine(PollWhileDisconnected());
            if (reconnectCoroutine == null) reconnectCoroutine = StartCoroutine(Reconnect());
        }

        private IEnumerator Reconnect()
        {
            yield return new WaitForSecondsRealtime(ReconnectDelaySeconds);
            reconnectCoroutine = null;
            if (stopping) yield break;
            ConnectAndRecover();
        }

        private IEnumerator PollWhileDisconnected()
        {
            while (!stopping && !eventStream.IsConnected)
            {
                RecoverSnapshot();
                yield return new WaitForSecondsRealtime(RecoveryPollIntervalSeconds);
            }
            fallbackCoroutine = null;
        }

        private bool ApplyTicket(MatchTicket ticket)
        {
            if (!reducer.Apply(ticket)) return false;
            MatchTicketState parsedState = ticket.ParsedState;
            PublishLocalState(parsedState);
            if (parsedState == MatchTicketState.Matched && ticket.matchInfo != null)
                Matched?.Invoke(ticket.matchInfo);
            return true;
        }

        private void RestoreServerStateFromTransientState()
        {
            if (reducer.Current == null) return;
            if (State != MatchTicketState.Reconnecting && State != MatchTicketState.CancelPending) return;
            PublishLocalState(reducer.Current.ParsedState);
        }

        private void PublishLocalState(MatchTicketState state)
        {
            State = state;
            StateChanged?.Invoke(state, reducer.Current);
        }
    }
}
