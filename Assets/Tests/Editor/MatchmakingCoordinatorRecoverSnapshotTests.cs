using System;
using System.Collections;
using Data;
using LobbyScene;
using NUnit.Framework;
using UnityEngine;

namespace WordOnline.Tests
{
    public class MatchmakingCoordinatorRecoverSnapshotTests
    {
        private GameObject testObject;
        private MatchmakingCoordinator coordinator;
        private RecoverSnapshotFakeMatchQueueApiService fakeApi;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestMatchmakingCoordinatorRecoverSnapshot");
            coordinator = testObject.AddComponent<MatchmakingCoordinator>();
            fakeApi = testObject.AddComponent<RecoverSnapshotFakeMatchQueueApiService>();
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
        public void RecoverSnapshot_WhenApiIsNull_DoesNothing()
        {
            // Act
            coordinator.RecoverSnapshot();

            // Assert
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Idle));
            Assert.That(coordinator.CurrentTicket, Is.Null);
        }

        [Test]
        public void RecoverSnapshot_WhenRequestInFlight_PreventsDuplicateRequest()
        {
            // Arrange
            fakeApi.ImmediateCallback = false;
            coordinator.Initialize(fakeApi, connectStream: false);
            Assert.That(fakeApi.GetActiveTicketCallCount, Is.EqualTo(1));

            // Act - second call while snapshot request is in flight
            coordinator.RecoverSnapshot();

            // Assert
            Assert.That(fakeApi.GetActiveTicketCallCount, Is.EqualTo(1));

            // Complete first request
            fakeApi.CompleteLastCallback(true, null);

            // Act - subsequent call after request completed
            coordinator.RecoverSnapshot();

            // Assert
            Assert.That(fakeApi.GetActiveTicketCallCount, Is.EqualTo(2));
        }

        [Test]
        public void RecoverSnapshot_WhenRequestFails_SetsStateToReconnecting()
        {
            // Arrange
            fakeApi.RequestSucceeded = false;
            MatchTicketState? reportedState = null;
            coordinator.StateChanged += (state, ticket) => reportedState = state;

            // Act
            coordinator.Initialize(fakeApi, connectStream: false);

            // Assert
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Reconnecting));
            Assert.That(reportedState, Is.EqualTo(MatchTicketState.Reconnecting));
        }

        [Test]
        public void RecoverSnapshot_WhenRequestSucceeds_WithNoTicket_SetsStateToIdle()
        {
            // Arrange
            fakeApi.RequestSucceeded = true;
            fakeApi.ReturnTicket = null;

            // Act
            coordinator.Initialize(fakeApi, connectStream: false);

            // Assert
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Idle));
            Assert.That(coordinator.CurrentTicket, Is.Null);
        }

        [Test]
        public void RecoverSnapshot_WhenRequestSucceeds_WithNewActiveTicket_AppliesTicketAndSetsState()
        {
            // Arrange
            fakeApi.RequestSucceeded = true;
            fakeApi.ReturnTicket = new MatchTicket { ticketId = "ticket-1", version = 1, state = "QUEUED" };
            MatchTicketState? reportedState = null;
            MatchTicket reportedTicket = null;
            coordinator.StateChanged += (state, ticket) =>
            {
                reportedState = state;
                reportedTicket = ticket;
            };

            // Act
            coordinator.Initialize(fakeApi, connectStream: false);

            // Assert
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Queued));
            Assert.That(coordinator.CurrentTicket, Is.Not.Null);
            Assert.That(coordinator.CurrentTicket.ticketId, Is.EqualTo("ticket-1"));
            Assert.That(reportedState, Is.EqualTo(MatchTicketState.Queued));
            Assert.That(reportedTicket, Is.Not.Null);
            Assert.That(reportedTicket.ticketId, Is.EqualTo("ticket-1"));
        }

        [Test]
        public void RecoverSnapshot_WhenRequestSucceeds_WithMatchedTicket_FiresMatchedEvent()
        {
            // Arrange
            var matchInfo = new MatchedInfoDto { sessionId = "room-abc-123" };
            fakeApi.RequestSucceeded = true;
            fakeApi.ReturnTicket = new MatchTicket
            {
                ticketId = "ticket-2",
                version = 2,
                state = "MATCHED",
                matchInfo = matchInfo
            };

            MatchedInfoDto receivedMatchInfo = null;
            coordinator.Matched += info => receivedMatchInfo = info;

            // Act
            coordinator.Initialize(fakeApi, connectStream: false);

            // Assert
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Matched));
            Assert.That(receivedMatchInfo, Is.Not.Null);
            Assert.That(receivedMatchInfo.sessionId, Is.EqualTo("room-abc-123"));
        }

        [Test]
        public void RecoverSnapshot_WhenRequestSucceeds_WithOutdatedTicketInReconnectingState_RestoresServerState()
        {
            // Arrange
            // 1) Initialize with ticket version 5 in ALLOCATING state
            fakeApi.RequestSucceeded = true;
            fakeApi.ReturnTicket = new MatchTicket { ticketId = "ticket-3", version = 5, state = "ALLOCATING" };
            coordinator.Initialize(fakeApi, connectStream: false);
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Allocating));

            // 2) Simulate snapshot failure -> moves state to Reconnecting
            fakeApi.RequestSucceeded = false;
            coordinator.RecoverSnapshot();
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Reconnecting));

            // 3) Snapshot succeeds but returns an outdated ticket version 3
            fakeApi.RequestSucceeded = true;
            fakeApi.ReturnTicket = new MatchTicket { ticketId = "ticket-3", version = 3, state = "QUEUED" };

            // Act
            coordinator.RecoverSnapshot();

            // Assert: Outdated ticket rejected by reducer, restoring state back to ALLOCATING (current ticket parsed state)
            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Allocating));
            Assert.That(coordinator.CurrentTicket.version, Is.EqualTo(5));
        }

        private class RecoverSnapshotFakeMatchQueueApiService : MatchQueueApiService
        {
            public bool ImmediateCallback = true;
            public bool RequestSucceeded = true;
            public MatchTicket ReturnTicket = null;
            public int GetActiveTicketCallCount = 0;
            private Action<bool, MatchTicket> pendingCallback;

            public override IEnumerator GetActiveTicket(Action<bool, MatchTicket> callback)
            {
                GetActiveTicketCallCount++;
                pendingCallback = callback;
                if (ImmediateCallback)
                {
                    callback?.Invoke(RequestSucceeded, ReturnTicket);
                }
                yield break;
            }

            public void CompleteLastCallback(bool success, MatchTicket ticket)
            {
                pendingCallback?.Invoke(success, ticket);
                pendingCallback = null;
            }
        }
    }
}
