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
                Object.DestroyImmediate(testObject);
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
                matchInfo = new MatchedInfoDto { matchId = "match-777" }
            };

            coordinator.Enqueue("normal_deck");

            Assert.That(coordinator.State, Is.EqualTo(MatchTicketState.Matched));
            Assert.That(matchedDto, Is.Not.Null);
            Assert.That(matchedDto.matchId, Is.EqualTo("match-777"));
        }

        private class FakeMatchQueueApiService : MatchQueueApiService
        {
            public string LastRequestedDeckMode { get; private set; }
            public MatchTicket TicketToReturnOnCreate { get; set; }

            public override IEnumerator CreateTicket(string deckMode, Action<MatchTicket> callback)
            {
                LastRequestedDeckMode = deckMode;
                callback?.Invoke(TicketToReturnOnCreate);
                yield break;
            }

            public override IEnumerator GetActiveTicket(Action<bool, MatchTicket> callback)
            {
                callback?.Invoke(true, null);
                yield break;
            }

            public override IEnumerator CancelTicket(string ticketId, Action<MatchCancelResult> callback)
            {
                callback?.Invoke(null);
                yield break;
            }
        }
    }
}
