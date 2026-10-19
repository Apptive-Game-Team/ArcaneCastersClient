using System;
using Data.Net;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class ServerEndpointTests
    {
        [Test]
        public void Path_EscapesEachSegmentAndKeepsSingleSlash()
        {
            string url = ServerEndpoint.Of("https://example.com:8443").Path("api", "match", "tickets");

            Assert.AreEqual("https://example.com:8443/api/match/tickets", url);
        }

        [Test]
        public void Path_EscapesReservedCharactersInsideSegment()
        {
            string url = ServerEndpoint.Of("https://example.com:8443").Path("api", "tickets", "a/b c");

            Assert.AreEqual("https://example.com:8443/api/tickets/a%2Fb%20c", url);
        }

        [Test]
        public void Query_ChoosesSeparatorByExistingQuery()
        {
            ServerEndpoint endpoint = ServerEndpoint.Of("https://example.com:8443")
                .Path("api", "games")
                .Query("page", "0")
                .Query("size", "20");

            Assert.AreEqual("https://example.com:8443/api/games?page=0&size=20", endpoint.ToString());
        }

        [Test]
        public void AsWebSocket_MapsHttpsToWssAndKeepsPort()
        {
            string url = ServerEndpoint.Of("https://example.com:8443").AsWebSocket().Path("ws").Query("token", "abc");

            Assert.AreEqual("wss://example.com:8443/ws?token=abc", url);
        }

        [Test]
        public void AsWebSocket_MapsHttpToWs()
        {
            string url = ServerEndpoint.Of("http://localhost:6209").AsWebSocket();

            Assert.AreEqual("ws://localhost:6209/", url);
        }

        [Test]
        public void AsWebSocket_IsIdempotentForWebSocketUrls()
        {
            ServerEndpoint alreadyWebSocket = ServerEndpoint.Of("wss://example.com:8443/game/ws");

            Assert.AreEqual(alreadyWebSocket.ToString(), alreadyWebSocket.AsWebSocket().ToString());
        }

        [Test]
        public void Query_AppendsToServerSuppliedQuery()
        {
            string url = ServerEndpoint.Of("wss://example.com:8443/ws?session=42").Query("token", "abc");

            Assert.AreEqual("wss://example.com:8443/ws?session=42&token=abc", url);
        }

        [Test]
        public void Query_EscapesValue()
        {
            string url = ServerEndpoint.Of("https://example.com:8443").Query("q", "a b&c");

            Assert.AreEqual("https://example.com:8443/?q=a%20b%26c", url);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("example.com/api")]
        [TestCase("file:///etc/passwd")]
        public void TryOf_RejectsUnusableUrls(string url)
        {
            Assert.IsFalse(ServerEndpoint.TryOf(url, out ServerEndpoint endpoint));
            Assert.IsFalse(endpoint.HasValue);
        }

        [Test]
        public void Of_ThrowsOnUnsupportedUrl()
        {
            Assert.Throws<ArgumentException>(() => ServerEndpoint.Of("example.com/api"));
        }

        [Test]
        public void Uninitialized_ThrowsOnBuild()
        {
            ServerEndpoint endpoint = default;

            Assert.Throws<InvalidOperationException>(() => endpoint.Path("api"));
            Assert.AreEqual(string.Empty, endpoint.ToString());
        }
    }
}
