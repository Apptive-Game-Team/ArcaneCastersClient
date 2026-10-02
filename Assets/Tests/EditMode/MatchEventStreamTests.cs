using System.Collections.Generic;
using LobbyScene;
using NUnit.Framework;
using UnityEngine;

namespace WordOnline.Tests
{
    [TestFixture]
    public class MatchEventStreamTests
    {
        private GameObject gameObject;
        private MatchEventStream stream;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("TestMatchEventStream");
            stream = gameObject.AddComponent<MatchEventStream>();
        }

        [TearDown]
        public void TearDown()
        {
            if (stream != null)
            {
                stream.Dispose();
            }

            if (gameObject != null)
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void DrainCallbacks_WhenQueueIsEmpty_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => stream.DrainCallbacks());
        }

        [Test]
        public void DrainCallbacks_ExecutesQueuedEventsInOrder()
        {
            var receivedTickets = new List<MatchTicket>();
            stream.TicketReceived += ticket => receivedTickets.Add(ticket);

            string envelopeJson1 = @"{""data"":""{\""ticketId\"":\""ticket-1\"",\""version\"":1,\""state\"":\""QUEUED\""}""}";
            string envelopeJson2 = @"{""data"":""{\""ticketId\"":\""ticket-2\"",\""version\"":2,\""state\"":\""MATCHED\""}""}";

            stream.OnMatchSseEvent(envelopeJson1);
            stream.OnMatchSseEvent(envelopeJson2);

            Assert.That(receivedTickets, Is.Empty, "Callbacks should not be executed prior to DrainCallbacks.");

            stream.DrainCallbacks();

            Assert.That(receivedTickets.Count, Is.EqualTo(2));
            Assert.That(receivedTickets[0].ticketId, Is.EqualTo("ticket-1"));
            Assert.That(receivedTickets[1].ticketId, Is.EqualTo("ticket-2"));
        }

        [Test]
        public void DrainCallbacks_ClearsQueueAfterExecution()
        {
            int invokeCount = 0;
            stream.TicketReceived += _ => invokeCount++;

            string envelopeJson = @"{""data"":""{\""ticketId\"":\""ticket-1\"",\""version\"":1,\""state\"":\""QUEUED\""}""}";
            stream.OnMatchSseEvent(envelopeJson);

            stream.DrainCallbacks();
            Assert.That(invokeCount, Is.EqualTo(1));

            stream.DrainCallbacks();
            Assert.That(invokeCount, Is.EqualTo(1), "Subsequent DrainCallbacks should not re-execute drained callbacks.");
        }

        [Test]
        public void DrainCallbacks_InvokesDisconnectedCallback_WhenConnected()
        {
            bool disconnectedFired = false;
            stream.Disconnected += () => disconnectedFired = true;

            stream.SetConnectedForTesting(true);
            Assert.That(stream.IsConnected, Is.True);

            stream.OnMatchSseDisconnected("Network loss");
            Assert.That(disconnectedFired, Is.False, "Disconnected should not fire until DrainCallbacks is called.");

            stream.DrainCallbacks();
            Assert.That(disconnectedFired, Is.True);
            Assert.That(stream.IsConnected, Is.False);
        }

        [Test]
        public void DrainCallbacks_OnMatchSseDisconnected_DoesNotInvokeDisconnected_WhenNotConnected()
        {
            bool disconnectedFired = false;
            stream.Disconnected += () => disconnectedFired = true;

            Assert.That(stream.IsConnected, Is.False);

            stream.OnMatchSseDisconnected("Disconnect while not connected");
            stream.DrainCallbacks();

            Assert.That(disconnectedFired, Is.False);
        }

        [Test]
        public void DrainCallbacks_HandlesInvalidJsonGracefully()
        {
            int invokeCount = 0;
            stream.TicketReceived += _ => invokeCount++;

            // Invalid envelope JSON (will log error and not queue)
            stream.OnMatchSseEvent("invalid envelope json");

            // Valid envelope with invalid inner payload (will queue callback, but deserialization inside callback will fail safely)
            string invalidTicketEnvelope = @"{""data"":""not a match ticket json""}";
            stream.OnMatchSseEvent(invalidTicketEnvelope);

            Assert.DoesNotThrow(() => stream.DrainCallbacks());
            Assert.That(invokeCount, Is.EqualTo(0));
        }
    }
}
