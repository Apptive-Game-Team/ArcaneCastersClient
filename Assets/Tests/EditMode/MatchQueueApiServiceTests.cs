using System.Collections;
using Data;
using LobbyScene;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;

namespace WordOnline.Tests
{
    public class MatchQueueApiServiceTests
    {
        private GameObject _gameObject;
        private MatchQueueApiService _service;

        [SetUp]
        public void SetUp()
        {
            MatchingServerCatalog.Select(MatchingServerCatalog.Local);
            _gameObject = new GameObject("MatchQueueApiServiceTestObject");
            _service = _gameObject.AddComponent<MatchQueueApiService>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }
        }

        [Test]
        public void MatchPractice_WhenWebRequestFails_InvokesCallbackWithNull()
        {
            bool callbackInvoked = false;
            MatchedInfoDto receivedDto = new MatchedInfoDto(); // Non-null sentinel value

            IEnumerator coroutine = _service.MatchPractice(dto =>
            {
                callbackInvoked = true;
                receivedDto = dto;
            });

            // First MoveNext creates webRequest and yields webRequest.SendWebRequest()
            Assert.IsTrue(coroutine.MoveNext());
            var asyncOp = coroutine.Current as UnityWebRequestAsyncOperation;
            Assert.IsNotNull(asyncOp, "Expected coroutine to yield a UnityWebRequestAsyncOperation.");

            // Instantly simulate request failure without network delays or thread sleeping
            asyncOp.webRequest.Abort();
            Assert.IsTrue(asyncOp.isDone, "Async operation should be completed after abort.");

            // Second MoveNext executes the error branch after web request failure
            bool hasNext = coroutine.MoveNext();

            Assert.IsFalse(hasNext, "Coroutine should yield break after handling request error.");
            Assert.IsTrue(callbackInvoked, "Callback should have been invoked on error.");
            Assert.IsNull(receivedDto, "Callback should receive null when web request fails.");
        }

        [Test]
        public void GetActiveTicket_WhenWebRequestFails_InvokesCallbackWithFalseAndNull()
        {
            bool callbackInvoked = false;
            bool successResult = true;
            MatchTicket receivedTicket = new MatchTicket();

            IEnumerator coroutine = _service.GetActiveTicket((success, ticket) =>
            {
                callbackInvoked = true;
                successResult = success;
                receivedTicket = ticket;
            });

            Assert.IsTrue(coroutine.MoveNext());
            var asyncOp = coroutine.Current as UnityWebRequestAsyncOperation;
            Assert.IsNotNull(asyncOp);

            asyncOp.webRequest.Abort();
            Assert.IsTrue(asyncOp.isDone);

            bool hasNext = coroutine.MoveNext();

            Assert.IsFalse(hasNext);
            Assert.IsTrue(callbackInvoked);
            Assert.IsFalse(successResult);
            Assert.IsNull(receivedTicket);
        }

        [Test]
        public void CancelTicket_WhenWebRequestFails_InvokesCallbackWithNull()
        {
            bool callbackInvoked = false;
            MatchCancelResult receivedResult = new MatchCancelResult();

            IEnumerator coroutine = _service.CancelTicket("ticket-123", result =>
            {
                callbackInvoked = true;
                receivedResult = result;
            });

            Assert.IsTrue(coroutine.MoveNext());
            var asyncOp = coroutine.Current as UnityWebRequestAsyncOperation;
            Assert.IsNotNull(asyncOp);

            asyncOp.webRequest.Abort();
            Assert.IsTrue(asyncOp.isDone);

            bool hasNext = coroutine.MoveNext();

            Assert.IsFalse(hasNext);
            Assert.IsTrue(callbackInvoked);
            Assert.IsNull(receivedResult);
        }

        [Test]
        public void CreateTicket_WhenWebRequestFails_InvokesCallbackWithNull()
        {
            bool callbackInvoked = false;
            MatchTicket receivedTicket = new MatchTicket();

            IEnumerator coroutine = _service.CreateTicket("standard", ticket =>
            {
                callbackInvoked = true;
                receivedTicket = ticket;
            });

            Assert.IsTrue(coroutine.MoveNext());
            var asyncOp = coroutine.Current as UnityWebRequestAsyncOperation;
            Assert.IsNotNull(asyncOp);

            asyncOp.webRequest.Abort();
            Assert.IsTrue(asyncOp.isDone);

            bool hasNext = coroutine.MoveNext();

            Assert.IsFalse(hasNext);
            Assert.IsTrue(callbackInvoked);
            Assert.IsNull(receivedTicket);
        }
    }
}
