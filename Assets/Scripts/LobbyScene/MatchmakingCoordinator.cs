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
        private bool enqueueInFlight;
        private bool cancelRequestedDuringEnqueue;
        private bool stopping;
        private Coroutine reconnectCoroutine;
        private Coroutine fallbackCoroutine;
        private Coroutine snapshotRetryCoroutine;

        public MatchTicketState State { get; private set; } = MatchTicketState.Idle;
        public MatchTicket CurrentTicket => reducer.Current;
        public event Action<MatchTicketState, MatchTicket> StateChanged;
        public event Action<MatchedInfoDto> Matched;

        public void Initialize(MatchQueueApiService apiService, bool connectStream = true)
        {
            if (api != null) return;
            api = apiService;
            if (connectStream)
            {
                eventStream = gameObject.AddComponent<MatchEventStream>();
                eventStream.TicketReceived += HandleStreamTicket;
                eventStream.Disconnected += HandleStreamDisconnected;
                ConnectAndRecover();
                return;
            }
            RecoverSnapshot();
        }

        public void Enqueue(string deckMode)
        {
            enqueueInFlight = true;
            cancelRequestedDuringEnqueue = false;
            StartCoroutine(api.CreateTicket(deckMode, ticket =>
            {
                enqueueInFlight = false;
                if (ticket == null)
                {
                    bool cancelRequested = cancelRequestedDuringEnqueue;
                    cancelRequestedDuringEnqueue = false;
                    // No ticket was created, so a pending cancel has nothing left to cancel.
                    PublishLocalState(cancelRequested ? MatchTicketState.Canceled : MatchTicketState.Failed);
                    return;
                }
                // ApplyTicket starts the pending cancel itself when the ticket is Queued or Allocating,
                // so the flag is cleared only after it ran.
                ApplyTicket(ticket);
                cancelRequestedDuringEnqueue = false;
            }));
        }

        public void Cancel()
        {
            MatchTicket ticket = reducer.Current;
            if (IsCancelable(ticket))
            {
                RequestCancel(ticket);
                return;
            }

            // The create request has not returned yet, so there is no ticket id to cancel.
            // Remember the request; the ticket is canceled as soon as it is applied.
            if (enqueueInFlight)
            {
                cancelRequestedDuringEnqueue = true;
                PublishLocalState(MatchTicketState.CancelPending);
            }
        }

        private static bool IsCancelable(MatchTicket ticket)
        {
            return ticket != null && (ticket.ParsedState == MatchTicketState.Queued ||
                                      ticket.ParsedState == MatchTicketState.Allocating);
        }

        private static bool IsSameTicket(MatchTicket left, MatchTicket right)
        {
            return left != null && right != null && left.ticketId == right.ticketId &&
                   left.version == right.version;
        }

        private void RequestCancel(MatchTicket ticket)
        {
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
            // A 404 only describes the ticket held when the request went out. A ticket applied
            // while it was in flight (the create response arriving first) must not be cleared by it.
            MatchTicket requestedFor = reducer.Current;
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
                    // The create request may not have reached the server yet.
                    if (enqueueInFlight) return;
                    MatchTicket local = reducer.Current;
                    if (local == null)
                    {
                        PublishLocalState(MatchTicketState.Idle);
                    }
                    else if (IsSameTicket(local, requestedFor) &&
                             (IsCancelable(local) || State == MatchTicketState.CancelPending))
                    {
                        // The server has no active ticket, so the player is not matching.
                        // A local Matched ticket is left alone for the game scene reconnect.
                        reducer.Clear();
                        PublishLocalState(MatchTicketState.Idle);
                    }
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
            eventStream.Connect(ServerList.MatchingServer.Api, SceneContext.JwtToken);
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
            if (cancelRequestedDuringEnqueue && IsCancelable(ticket))
            {
                // Whichever path (event stream or create response) applies the ticket first
                // starts the cancel; the flag makes sure it happens once.
                cancelRequestedDuringEnqueue = false;
                RequestCancel(ticket);
                return true;
            }
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
