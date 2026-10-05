using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Global.Util;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace WordOnline.Tests
{
    [TestFixture]
    public class JwksServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            JwksService.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            JwksService.Reset();
        }

        [Test]
        public void FetchJwks_HandlesInvalidJson_CatchesExceptionAndLogsWarning()
        {
            // Simulate an invalid JSON payload returned by the web request
            JwksService.WebRequestOverride = url => "{ invalid json format: ";

            IEnumerator coroutine = JwksService.FetchJwks();
            while (coroutine.MoveNext()) { }

            Assert.IsFalse(JwksService.IsFetched);
            Assert.IsNull(JwksService.GetFirstKey());
            Assert.IsEmpty(JwksService.GetAllKeys());
        }

        [Test]
        public void FetchJwks_HandlesNullOrEmptyResponse()
        {
            JwksService.WebRequestOverride = url => "{}";

            IEnumerator coroutine = JwksService.FetchJwks();
            while (coroutine.MoveNext()) { }

            Assert.IsFalse(JwksService.IsFetched);
            Assert.IsNull(JwksService.GetFirstKey());
        }

        [Test]
        public void FetchJwks_HandlesNullKeysResponse()
        {
            JwksService.WebRequestOverride = url => "{\"keys\": null}";

            IEnumerator coroutine = JwksService.FetchJwks();
            while (coroutine.MoveNext()) { }

            Assert.IsFalse(JwksService.IsFetched);
            Assert.IsNull(JwksService.GetFirstKey());
        }

        [Test]
        public void FetchJwks_HandlesNetworkErrorResponse()
        {
            JwksService.WebRequestOverride = url => null;

            IEnumerator coroutine = JwksService.FetchJwks();
            while (coroutine.MoveNext()) { }

            Assert.IsFalse(JwksService.IsFetched);
            Assert.IsNull(JwksService.GetFirstKey());
        }

        [Test]
        public void FetchJwks_ValidJson_ParsesAndCachesKeys()
        {
            string validJson = @"{
                ""keys"": [
                    {
                        ""kid"": ""key-1"",
                        ""kty"": ""RSA"",
                        ""alg"": ""RS256"",
                        ""use"": ""sig"",
                        ""n"": ""sample-n-1"",
                        ""e"": ""AQAB""
                    },
                    {
                        ""kid"": ""key-2"",
                        ""kty"": ""RSA"",
                        ""alg"": ""RS256"",
                        ""use"": ""sig"",
                        ""n"": ""sample-n-2"",
                        ""e"": ""AQAB""
                    }
                ]
            }";

            JwksService.WebRequestOverride = url => validJson;

            IEnumerator coroutine = JwksService.FetchJwks();
            while (coroutine.MoveNext()) { }

            Assert.IsTrue(JwksService.IsFetched);

            JwksKey key1 = JwksService.GetKey("key-1");
            Assert.IsNotNull(key1);
            Assert.AreEqual("key-1", key1.kid);
            Assert.AreEqual("RSA", key1.kty);
            Assert.AreEqual("RS256", key1.alg);

            JwksKey key2 = JwksService.GetKey("key-2");
            Assert.IsNotNull(key2);
            Assert.AreEqual("key-2", key2.kid);

            JwksKey firstKey = JwksService.GetFirstKey();
            Assert.IsNotNull(firstKey);

            List<JwksKey> allKeys = JwksService.GetAllKeys().ToList();
            Assert.AreEqual(2, allKeys.Count);
        }

        [UnityTest]
        public IEnumerator FetchJwks_RequestFailure_LogsWarningAndStopsExecution()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"\[JwksService\] Failed to fetch JWKS:"));

            // No WebRequestOverride: an unreachable address makes the real UnityWebRequest fail
            JwksService.AccountServerUrl = "http://127.0.0.1:1";
            yield return JwksService.FetchJwks();

            Assert.IsFalse(JwksService.IsFetched);
            Assert.IsNull(JwksService.GetFirstKey());
            Assert.IsEmpty(JwksService.GetAllKeys());
        }

        [Test]
        public void ParseJwksResponse_InvalidJson_ReturnsFalseAndLeavesCacheEmpty()
        {
            bool success = JwksService.ParseJwksResponse("{ corrupted: true, ");

            Assert.IsFalse(success);
            Assert.IsFalse(JwksService.IsFetched);
            Assert.IsNull(JwksService.GetFirstKey());
        }

        [Test]
        public void ParseJwksResponse_ValidJson_PopulatesCache()
        {
            string validJson = @"{
                ""keys"": [
                    {
                        ""kid"": ""test-kid"",
                        ""kty"": ""RSA"",
                        ""alg"": ""RS256"",
                        ""use"": ""sig"",
                        ""n"": ""modulus"",
                        ""e"": ""exponent""
                    }
                ]
            }";

            bool success = JwksService.ParseJwksResponse(validJson);

            Assert.IsTrue(success);
            Assert.IsTrue(JwksService.IsFetched);
            Assert.IsNotNull(JwksService.GetKey("test-kid"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("non-existent-kid")]
        public void GetKey_InvalidOrMissingKid_ReturnsNull(string kid)
        {
            Assert.IsNull(JwksService.GetKey(kid));
        }
    }
}
