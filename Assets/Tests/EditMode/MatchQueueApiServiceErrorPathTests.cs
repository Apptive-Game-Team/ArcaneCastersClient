using System.Collections;
using System.Text.RegularExpressions;
using Data;
using LobbyScene;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace WordOnline.Tests
{
    /// <summary>
    /// Every request goes to the Local option, where nothing listens on port 6209, so the web
    /// request fails with a connection error and each method reports it through its callback.
    /// </summary>
    public class MatchQueueApiServiceErrorPathTests
    {
        private GameObject _gameObject;
        private MatchQueueApiService _service;

        [SetUp]
        public void SetUp()
        {
            MatchingServerCatalog.Select(MatchingServerCatalog.Local);
            _gameObject = new GameObject("MatchQueueApiServiceErrorPathTestObject");
            _service = _gameObject.AddComponent<MatchQueueApiService>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_gameObject);
            }
        }

        [UnityTest]
        public IEnumerator MatchPractice_WhenWebRequestFails_InvokesCallbackWithNull()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"^MatchPractice error:"));
            bool callbackInvoked = false;
            MatchedInfoDto receivedDto = new MatchedInfoDto();

            yield return _service.MatchPractice(dto =>
            {
                callbackInvoked = true;
                receivedDto = dto;
            });

            Assert.IsTrue(callbackInvoked, "Callback should have been invoked on error.");
            Assert.IsNull(receivedDto, "Callback should receive null when web request fails.");
        }

        [UnityTest]
        public IEnumerator GetActiveTicket_WhenWebRequestFails_InvokesCallbackWithFalseAndNull()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"^GetActiveTicket error:"));
            bool callbackInvoked = false;
            bool successResult = true;
            MatchTicket receivedTicket = new MatchTicket();

            yield return _service.GetActiveTicket((success, ticket) =>
            {
                callbackInvoked = true;
                successResult = success;
                receivedTicket = ticket;
            });

            Assert.IsTrue(callbackInvoked);
            Assert.IsFalse(successResult);
            Assert.IsNull(receivedTicket);
        }

        [UnityTest]
        public IEnumerator CancelTicket_WhenWebRequestFails_InvokesCallbackWithNull()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"^CancelTicket error:"));
            bool callbackInvoked = false;
            MatchCancelResult receivedResult = new MatchCancelResult();

            yield return _service.CancelTicket("ticket-123", result =>
            {
                callbackInvoked = true;
                receivedResult = result;
            });

            Assert.IsTrue(callbackInvoked);
            Assert.IsNull(receivedResult);
        }

        [UnityTest]
        public IEnumerator CreateTicket_WhenWebRequestFails_InvokesCallbackWithNull()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"^CreateTicket error:"));
            bool callbackInvoked = false;
            MatchTicket receivedTicket = new MatchTicket();

            yield return _service.CreateTicket("standard", ticket =>
            {
                callbackInvoked = true;
                receivedTicket = ticket;
            });

            Assert.IsTrue(callbackInvoked);
            Assert.IsNull(receivedTicket);
        }
    }
}
