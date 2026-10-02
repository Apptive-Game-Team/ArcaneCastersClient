using System.Collections;
using Data;
using Global.Serialization;
using LobbyScene;
using NUnit.Framework;
using UnityEngine;

namespace WordOnline.Tests
{
    public class MatchQueueApiServiceTests
    {
        private GameObject gameObject;
        private MatchQueueApiService apiService;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("MatchQueueApiServiceTestObject");
            apiService = gameObject.AddComponent<MatchQueueApiService>();
        }

        [TearDown]
        public void TearDown()
        {
            if (gameObject != null)
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CreateTicket_ReturnsNonNilEnumeratorAndDoesNotInvokeCallbackImmediately()
        {
            bool callbackInvoked = false;

            IEnumerator coroutine = apiService.CreateTicket(MatchDeckMode.Selected, ticket =>
            {
                callbackInvoked = true;
            });

            Assert.IsNotNull(coroutine, "CreateTicket should return a valid IEnumerator instance");
            Assert.IsFalse(callbackInvoked, "Callback should not be invoked synchronously upon calling CreateTicket");

            // Advance outer coroutine to yield SendTicketRequest inner enumerator
            bool hasNext = coroutine.MoveNext();
            Assert.IsTrue(hasNext, "CreateTicket coroutine should yield inner request enumerator before completing");
            Assert.IsNotNull(coroutine.Current, "Current yield object should be non-null (nested SendTicketRequest enumerator)");
            Assert.IsInstanceOf<IEnumerator>(coroutine.Current, "Current yield object should be SendTicketRequest enumerator");
        }

        [Test]
        public void CreateTicket_WithRandomDeckMode_ReturnsValidEnumerator()
        {
            bool callbackInvoked = false;

            IEnumerator coroutine = apiService.CreateTicket(MatchDeckMode.Random, ticket =>
            {
                callbackInvoked = true;
            });

            Assert.IsNotNull(coroutine, "CreateTicket should return a valid IEnumerator instance for random deck mode");
            Assert.IsFalse(callbackInvoked, "Callback should not be invoked synchronously");
        }
    }
}
