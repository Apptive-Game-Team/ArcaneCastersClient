using System.Collections;
using System.Text.RegularExpressions;
using Global.Util;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace WordOnline.Tests
{
    public class JwksServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            JwksService.ResetForTest();
        }

        [TearDown]
        public void TearDown()
        {
            JwksService.ResetForTest();
        }

        [Test]
        public void InitialState_IsNotFetchedAndKeysAreEmpty()
        {
            Assert.IsFalse(JwksService.IsFetched);
            Assert.IsNull(JwksService.GetFirstKey());
            Assert.IsNull(JwksService.GetKey("any-kid"));
            Assert.IsEmpty(JwksService.GetAllKeys());
        }

        [UnityTest]
        public IEnumerator FetchJwks_RequestFailure_LogsWarningAndStopsExecution()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"\[JwksService\] Failed to fetch JWKS:"));

            // Point to an unreachable address to trigger UnityWebRequest failure
            yield return JwksService.FetchJwks("http://127.0.0.1:1");

            Assert.IsFalse(JwksService.IsFetched);
            Assert.IsNull(JwksService.GetFirstKey());
            Assert.IsNull(JwksService.GetKey("some-key-id"));
            Assert.IsEmpty(JwksService.GetAllKeys());
        }

        [Test]
        public void GetKey_WithNullOrEmptyKid_ReturnsNull()
        {
            Assert.IsNull(JwksService.GetKey(null));
            Assert.IsNull(JwksService.GetKey(string.Empty));
        }

        [Test]
        public void ResetForTest_ClearsState()
        {
            JwksService.ResetForTest();

            Assert.IsFalse(JwksService.IsFetched);
            Assert.IsNull(JwksService.GetFirstKey());
        }
    }
}
