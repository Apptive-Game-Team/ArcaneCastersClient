using System;
using System.Collections;
using Data;
using LobbyScene;
using NUnit.Framework;
using UnityEngine;

namespace WordOnline.Tests
{
    public class MatchmakingCoordinatorTests
    {
        private GameObject testObject;
        private FakeMatchQueueApiService apiStub;
        private MatchmakingCoordinator coordinator;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("MatchmakingCoordinatorTests_GameObject");
            apiStub = testObject.AddComponent<FakeMatchQueueApiService>();
            coordinator = testObject.AddComponent<MatchmakingCoordinator>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                UnityEngine.Object.DestroyImmediate(testObject);
            }
        }

        [Test]
        public void Enqueue_WhenApiReturnsTicket_AppliesTicketAndSetsQueuedState()
        {
            coordinator.Initialize(apiStub);
            var expectedTicket = new MatchTicket
            {
                ticketId = "ticket-101",
                version = 1,
                state = "QUEUED"
            };
            apiStub.TicketToReturnOnCreate = expectedTicket;

            MatchTicketState? reportedState = null;
            MatchTicket reportedTicket = null;
            coordinator.StateChanged += (state, ticket) =>
            {
                reportedState = state;
                reportedTicket = ticket;
            };

            coordinator.Enqueue("normal_deck");

            Assert.That(apiStub.LastRequestedDeckMode, Is.EqualTo("normal_deck"));
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Queued));
            Assert.That(coordinator.CurrentTicket, Is.Not.Null);
            Assert.That(coordinator.CurrentTicket.ticketId, Is.EqualTo("ticket-101"));
            Assert.That(reportedState, Is.EqualTo(MatchTicketState.Queued));
            Assert.That(reportedTicket, Is.Not.Null);
            Assert.That(reportedTicket.ticketId, Is.EqualTo("ticket-101"));
        }

        [Test]
        public void Enqueue_WhenApiReturnsNull_SetsFailedState()
        {
            coordinator.Initialize(apiStub);
            apiStub.TicketToReturnOnCreate = null;

            MatchTicketState? reportedState = null;
            coordinator.StateChanged += (state, _) => reportedState = state;

            coordinator.Enqueue("normal_deck");

            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Failed));
            Assert.That(reportedState, Is.EqualTo(MatchTicketState.Failed));
        }

        [Test]
        public void Enqueue_PassesDeckModeToApiService()
        {
            coordinator.Initialize(apiStub);
            apiStub.TicketToReturnOnCreate = new MatchTicket
            {
                ticketId = "ticket-102",
                version = 1,
                state = "QUEUED"
            };

            coordinator.Enqueue("special_deck_mode");

            Assert.That(apiStub.LastRequestedDeckMode, Is.EqualTo("special_deck_mode"));
        }

        [Test]
        public void Enqueue_WhenApiReturnsMatchedTicket_TriggersMatchedEvent()
        {
            coordinator.Initialize(apiStub);
            MatchedInfoDto matchedDto = null;
            coordinator.Matched += info => matchedDto = info;

            apiStub.TicketToReturnOnCreate = new MatchTicket
            {
                ticketId = "ticket-103",
                version = 1,
                state = "MATCHED",
                matchInfo = new MatchedInfoDto { sessionId = "session-777" }
            };

            coordinator.Enqueue("normal_deck");

            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Matched));
            Assert.That(matchedDto, Is.Not.Null);
            Assert.That(matchedDto.sessionId, Is.EqualTo("session-777"));
        }

        [Test]
        public void Cancel_WhenCreateRequestInFlight_CancelsTicketOnceCreateReturns()
        {
            coordinator.Initialize(apiStub);
            apiStub.HoldCreate = true;
            apiStub.CancelResultToReturn = new MatchCancelResult
            {
                ticket = new MatchTicket { ticketId = "ticket-201", version = 2, state = "CANCELED" }
            };
            coordinator.Enqueue("normal_deck");

            coordinator.Cancel();

            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.CancelPending));
            Assert.That(apiStub.CancelCallCount, Is.EqualTo(0));

            apiStub.CompleteCreate(new MatchTicket { ticketId = "ticket-201", version = 1, state = "QUEUED" });

            Assert.That(apiStub.CancelCallCount, Is.EqualTo(1));
            Assert.That(apiStub.LastCanceledTicketId, Is.EqualTo("ticket-201"));
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Canceled));
        }

        [Test]
        public void Cancel_WhenStreamTicketArrivesBeforeCreateResponse_CancelsExactlyOnce()
        {
            coordinator.Initialize(apiStub, connectStream: false);
            apiStub.HoldCreate = true;
            apiStub.CancelResultToReturn = new MatchCancelResult
            {
                ticket = new MatchTicket { ticketId = "ticket-202", version = 2, state = "CANCELED" }
            };
            coordinator.Enqueue("normal_deck");
            coordinator.Cancel();

            // Same ticket arrives through the snapshot path first, then through the create response.
            apiStub.SnapshotTicket = new MatchTicket { ticketId = "ticket-202", version = 1, state = "QUEUED" };
            coordinator.RecoverSnapshot();
            apiStub.CompleteCreate(new MatchTicket { ticketId = "ticket-202", version = 1, state = "QUEUED" });

            Assert.That(apiStub.CancelCallCount, Is.EqualTo(1));
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Canceled));
        }

        [Test]
        public void Cancel_WhenCreateRequestFails_PublishesCanceled()
        {
            coordinator.Initialize(apiStub);
            apiStub.HoldCreate = true;
            coordinator.Enqueue("normal_deck");
            coordinator.Cancel();

            apiStub.CompleteCreate(null);

            Assert.That(apiStub.CancelCallCount, Is.EqualTo(0));
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Canceled));
        }

        [Test]
        public void Cancel_WhenServerReportsNoActiveTicket_ClearsStaleTicketAndGoesIdle()
        {
            coordinator.Initialize(apiStub);
            apiStub.TicketToReturnOnCreate = new MatchTicket { ticketId = "ticket-203", version = 1, state = "QUEUED" };
            coordinator.Enqueue("normal_deck");
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Queued));

            // CancelTicket returns null (NOT_FOUND) and the snapshot has no ticket.
            coordinator.Cancel();

            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Idle));
            Assert.That(coordinator.CurrentTicket, Is.Null);
        }

        [Test]
        public void RecoverSnapshot_WhenStale404ArrivesAfterCreateResponse_KeepsQueuedTicket()
        {
            // The snapshot goes out before the create request reaches the server, so the server
            // answers 404, but the create response reaches the client first.
            apiStub.HoldSnapshot = true;
            coordinator.Initialize(apiStub, connectStream: false);
            apiStub.TicketToReturnOnCreate = new MatchTicket { ticketId = "ticket-204", version = 1, state = "QUEUED" };
            coordinator.Enqueue("normal_deck");
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Queued));

            apiStub.CompleteSnapshot(true, null);

            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Queued));
            Assert.That(coordinator.CurrentTicket?.ticketId, Is.EqualTo("ticket-204"));
        }

        [Test]
        public void Cancel_WhileRequestIsPending_SendsOnlyOneRequest()
        {
            coordinator.Initialize(apiStub, connectStream: false);
            apiStub.TicketToReturnOnCreate = new MatchTicket { ticketId = "pending-cancel", version = 1, state = "QUEUED" };
            coordinator.Enqueue("normal_deck");
            apiStub.HoldCancel = true;

            coordinator.Cancel();
            coordinator.Cancel();

            Assert.That(apiStub.CancelCallCount, Is.EqualTo(1));
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.CancelPending));
            apiStub.CompleteCancel(new MatchCancelResult {
                ticket = new MatchTicket { ticketId = "pending-cancel", version = 2, state = "CANCELED" }
            });
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Canceled));
        }

        [Test]
        public void SnapshotFailure_AfterCancellation_KeepsCancellationConfirmed()
        {
            apiStub.SnapshotTicket = new MatchTicket { ticketId = "finished", version = 2, state = "CANCELED" };
            coordinator.Initialize(apiStub, connectStream: false);
            apiStub.SnapshotSucceeded = false;

            coordinator.RecoverSnapshot();

            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Canceled));
        }

        [Test]
        public void Cancel_ConfirmedBeforeCreateResponse_DoesNotReopenOnConnectionFailure()
        {
            coordinator.Initialize(apiStub, connectStream: false);
            apiStub.HoldCreate = true;
            coordinator.Enqueue("normal_deck");
            coordinator.Cancel();
            var queued = new MatchTicket { ticketId = "canceled-before-create", version = 1, state = "QUEUED" };
            apiStub.SnapshotTicket = queued;
            apiStub.CancelResultToReturn = new MatchCancelResult {
                ticket = new MatchTicket { ticketId = "canceled-before-create", version = 2, state = "CANCELED" }
            };
            coordinator.RecoverSnapshot();
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Canceled));

            apiStub.SnapshotSucceeded = false;
            coordinator.RecoverSnapshot();
            apiStub.CompleteCreate(queued);

            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Canceled));
        }

        [Test]
        public void Cancel_DuringUnknownCreateOutcome_RemembersIntentUntilRecovery()
        {
            coordinator.Initialize(apiStub, connectStream: false);
            apiStub.HoldCreate = true;
            coordinator.Enqueue("normal_deck");
            apiStub.SnapshotSucceeded = false;
            apiStub.CompleteCreate(null);
            coordinator.Cancel();
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.CancelPending));
            apiStub.SnapshotSucceeded = true;
            apiStub.SnapshotTicket = new MatchTicket { ticketId = "unknown-create", version = 1, state = "QUEUED" };
            apiStub.CancelResultToReturn = new MatchCancelResult {
                ticket = new MatchTicket { ticketId = "unknown-create", version = 2, state = "CANCELED" }
            };

            coordinator.RecoverSnapshot();

            Assert.That(apiStub.CancelCallCount, Is.EqualTo(1));
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Canceled));
        }

        [Test]
        public void CreateFailure_AfterPreviousTerminalTicket_SettlesNewOutcome()
        {
            apiStub.SnapshotTicket = new MatchTicket { ticketId = "previous", version = 2, state = "CANCELED" };
            coordinator.Initialize(apiStub, connectStream: false);
            apiStub.SnapshotTicket = null;
            apiStub.TicketToReturnOnCreate = null;

            coordinator.Enqueue("normal_deck");

            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Failed));
            Assert.That(coordinator.CurrentTicket, Is.Null);
        }

        [Test]
        public void CreateFailure_DoesNotUseSnapshotStartedBeforeCreation()
        {
            apiStub.HoldSnapshot = true;
            coordinator.Initialize(apiStub, connectStream: false);
            apiStub.HoldCreate = true;
            coordinator.Enqueue("normal_deck");
            apiStub.CompleteCreate(null);
            Assert.That(apiStub.SnapshotCallCount, Is.EqualTo(1));

            apiStub.CompleteSnapshot(true, null);

            Assert.That(apiStub.SnapshotCallCount, Is.EqualTo(2));
            Assert.That(coordinator.State, Is.Not.EqualTo(MatchTicketState.Failed));
            apiStub.CompleteSnapshot(true, new MatchTicket { ticketId = "created-after-snapshot", version = 1, state = "QUEUED" });
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Queued));
        }

        [Test]
        public void Cancel_WhenCreateResponseIsLost_CancelsTicketRecoveredFromServer()
        {
            coordinator.Initialize(apiStub, connectStream: false);
            apiStub.HoldCreate = true;
            coordinator.Enqueue("normal_deck");
            coordinator.Cancel();
            apiStub.SnapshotTicket = new MatchTicket { ticketId = "lost-create", version = 1, state = "QUEUED" };
            apiStub.CancelResultToReturn = new MatchCancelResult {
                ticket = new MatchTicket { ticketId = "lost-create", version = 2, state = "CANCELED" }
            };

            apiStub.CompleteCreate(null);

            Assert.That(apiStub.CancelCallCount, Is.EqualTo(1));
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Canceled));
        }

        [Test]
        public void Cancel_WhenCreateAndSnapshotFail_WaitsForConfirmationBeforeCanceling()
        {
            coordinator.Initialize(apiStub, connectStream: false);
            apiStub.HoldCreate = true;
            coordinator.Enqueue("normal_deck");
            coordinator.Cancel();
            apiStub.SnapshotSucceeded = false;

            apiStub.CompleteCreate(null);

            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Reconnecting));
            Assert.That(apiStub.CancelCallCount, Is.Zero);
            apiStub.SnapshotSucceeded = true;
            apiStub.SnapshotTicket = new MatchTicket { ticketId = "recovered-create", version = 1, state = "QUEUED" };
            apiStub.CancelResultToReturn = new MatchCancelResult {
                ticket = new MatchTicket { ticketId = "recovered-create", version = 2, state = "CANCELED" }
            };
            coordinator.RecoverSnapshot();
            Assert.That(apiStub.CancelCallCount, Is.EqualTo(1));
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Canceled));
        }

        [Test]
        public void Cancel_LateFailureAfterConfirmedCancellation_DoesNotRestartMatching()
        {
            coordinator.Initialize(apiStub, connectStream: false);
            apiStub.TicketToReturnOnCreate = new MatchTicket { ticketId = "confirmed-cancel", version = 1, state = "QUEUED" };
            coordinator.Enqueue("normal_deck");
            apiStub.HoldCancel = true;
            coordinator.Cancel();
            apiStub.SnapshotTicket = new MatchTicket { ticketId = "confirmed-cancel", version = 2, state = "CANCELED" };
            coordinator.RecoverSnapshot();
            int snapshots = apiStub.SnapshotCallCount;

            apiStub.CompleteCancel(null);

            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Canceled));
            Assert.That(apiStub.SnapshotCallCount, Is.EqualTo(snapshots));
        }

        [Test]
        public void Cancel_OldResponseAfterNewEnqueue_DoesNotReplaceNewTicket()
        {
            coordinator.Initialize(apiStub, connectStream: false);
            apiStub.TicketToReturnOnCreate = new MatchTicket { ticketId = "old-cancel", version = 1, state = "QUEUED" };
            coordinator.Enqueue("normal_deck");
            apiStub.HoldCancel = true;
            coordinator.Cancel();
            apiStub.TicketToReturnOnCreate = new MatchTicket { ticketId = "new-queue", version = 1, state = "QUEUED" };
            coordinator.Enqueue("normal_deck");

            apiStub.CompleteCancel(new MatchCancelResult {
                ticket = new MatchTicket { ticketId = "old-cancel", version = 2, state = "CANCELED" }
            });

            Assert.That(coordinator.CurrentTicket.ticketId, Is.EqualTo("new-queue"));
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Queued));
        }

        [Test]
        public void Cancel_UnconfirmedResponseAndQueuedSnapshot_AllowsRetry()
        {
            coordinator.Initialize(apiStub, connectStream: false);
            apiStub.TicketToReturnOnCreate = new MatchTicket { ticketId = "retry-cancel", version = 1, state = "QUEUED" };
            coordinator.Enqueue("normal_deck");
            apiStub.SnapshotTicket = apiStub.TicketToReturnOnCreate;

            coordinator.Cancel();
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Queued));
            coordinator.Cancel();

            Assert.That(apiStub.CancelCallCount, Is.EqualTo(2));
        }

        private class FakeMatchQueueApiService : MatchQueueApiService
        {
            public string LastRequestedDeckMode { get; private set; }
            public MatchTicket TicketToReturnOnCreate { get; set; }
            public bool HoldCreate { get; set; }
            public MatchTicket SnapshotTicket { get; set; }
            public MatchCancelResult CancelResultToReturn { get; set; }
            public int CancelCallCount { get; private set; }
            public string LastCanceledTicketId { get; private set; }
            public bool HoldSnapshot { get; set; }
            public bool SnapshotSucceeded { get; set; } = true;
            public bool HoldCancel { get; set; }
            public int SnapshotCallCount { get; private set; }
            private Action<MatchTicket> pendingCreateCallback;
            private Action<bool, MatchTicket> pendingSnapshotCallback;
            private Action<MatchCancelResult> pendingCancelCallback;

            public void CompleteCancel(MatchCancelResult result)
            {
                var callback = pendingCancelCallback;
                pendingCancelCallback = null;
                callback?.Invoke(result);
            }

            public void CompleteSnapshot(bool requestSucceeded, MatchTicket ticket)
            {
                Action<bool, MatchTicket> callback = pendingSnapshotCallback;
                pendingSnapshotCallback = null;
                callback?.Invoke(requestSucceeded, ticket);
            }

            public void CompleteCreate(MatchTicket ticket)
            {
                Action<MatchTicket> callback = pendingCreateCallback;
                pendingCreateCallback = null;
                callback?.Invoke(ticket);
            }

            public override IEnumerator CreateTicket(string deckMode, Action<MatchTicket> callback)
            {
                LastRequestedDeckMode = deckMode;
                if (HoldCreate)
                {
                    pendingCreateCallback = callback;
                    yield break;
                }
                callback?.Invoke(TicketToReturnOnCreate);
                yield break;
            }

            public override IEnumerator GetActiveTicket(Action<bool, MatchTicket> callback)
            {
                SnapshotCallCount++;
                if (HoldSnapshot)
                {
                    pendingSnapshotCallback = callback;
                    yield break;
                }
                callback?.Invoke(SnapshotSucceeded, SnapshotTicket);
                yield break;
            }

            public override IEnumerator CancelTicket(string ticketId, Action<MatchCancelResult> callback)
            {
                CancelCallCount++;
                LastCanceledTicketId = ticketId;
                if (HoldCancel)
                {
                    pendingCancelCallback = callback;
                    yield break;
                }
                callback?.Invoke(CancelResultToReturn);
                yield break;
            }
        }
    }
}
