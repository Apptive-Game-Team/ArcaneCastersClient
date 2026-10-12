using System.Collections.Generic;
using System.Text.RegularExpressions;
using LobbyScene;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace WordOnline.Tests
{
    [TestFixture]
    public class FriendEventStreamTests
    {
        private GameObject gameObject;
        private FriendEventStream stream;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("TestFriendEventStream");
            stream = gameObject.AddComponent<FriendEventStream>();
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
            var receivedEvents = new List<FriendEventPayload>();
            stream.EventReceived += payload => receivedEvents.Add(payload);

            string envelopeJson1 = @"{""data"":""{\""type\"":\""FRIEND_REQUEST_RECEIVED\"",\""request\"":{\""id\"":1,\""senderName\"":\""Alice\""}}""}";
            string envelopeJson2 = @"{""data"":""{\""type\"":\""FRIEND_INVITE_RECEIVED\"",\""invite\"":{\""inviteId\"":\""inv-1\"",\""inviterName\"":\""Bob\""}}""}";

            stream.OnFriendSseEvent(envelopeJson1);
            stream.OnFriendSseEvent(envelopeJson2);

            Assert.That(receivedEvents, Is.Empty, "Callbacks should not be executed prior to DrainCallbacks.");

            stream.DrainCallbacks();

            Assert.That(receivedEvents.Count, Is.EqualTo(2));
            Assert.That(receivedEvents[0].type, Is.EqualTo("FRIEND_REQUEST_RECEIVED"));
            Assert.That(receivedEvents[0].ResolveRequest().senderName, Is.EqualTo("Alice"));
            Assert.That(receivedEvents[1].type, Is.EqualTo("FRIEND_INVITE_RECEIVED"));
            Assert.That(receivedEvents[1].invite.inviteId, Is.EqualTo("inv-1"));
        }

        [Test]
        public void DrainCallbacks_ClearsQueueAfterExecution()
        {
            int invokeCount = 0;
            stream.EventReceived += _ => invokeCount++;

            string envelopeJson = @"{""data"":""{\""type\"":\""FRIEND_REQUEST_ACCEPTED\""}""}";
            stream.OnFriendSseEvent(envelopeJson);

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

            stream.OnFriendSseDisconnected("Network loss");
            Assert.That(disconnectedFired, Is.False, "Disconnected should not fire until DrainCallbacks is called.");

            stream.DrainCallbacks();
            Assert.That(disconnectedFired, Is.True);
            Assert.That(stream.IsConnected, Is.False);
        }

        [Test]
        public void DrainCallbacks_HandlesInvalidJsonGracefully()
        {
            int invokeCount = 0;
            stream.EventReceived += _ => invokeCount++;

            LogAssert.Expect(LogType.Error, new Regex(@"\[Friend SSE\] Invalid envelope"));
            LogAssert.Expect(LogType.Error, new Regex(@"\[Friend SSE\] Invalid event"));

            // Invalid envelope JSON
            stream.OnFriendSseEvent("invalid envelope json");

            // Valid envelope with invalid inner payload
            string invalidPayloadEnvelope = @"{""data"":""not a friend event json""}";
            stream.OnFriendSseEvent(invalidPayloadEnvelope);

            Assert.DoesNotThrow(() => stream.DrainCallbacks());
            Assert.That(invokeCount, Is.EqualTo(0));
        }
    }
}
