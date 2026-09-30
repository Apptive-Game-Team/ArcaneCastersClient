using Global.Udp;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class UdpEndpointTests
    {
        [Test]
        public void BuildAndParse_RoundTrip()
        {
            string url = UdpEndpoint.Build("game.example.com", 7777, "a1b2-c3d4", 42L);
            string host;
            int port;
            string sessionId;
            long userId;

            Assert.IsTrue(UdpEndpoint.TryParse(url, out host, out port, out sessionId, out userId));

            Assert.AreEqual("game.example.com", host);
            Assert.AreEqual(7777, port);
            Assert.AreEqual("a1b2-c3d4", sessionId);
            Assert.AreEqual(42L, userId);
        }

        [Test]
        public void SessionIdWithReservedCharactersSurvives()
        {
            string url = UdpEndpoint.Build("127.0.0.1", 9000, "a&b=c d", 1L);
            string host;
            int port;
            string sessionId;
            long userId;

            Assert.IsTrue(UdpEndpoint.TryParse(url, out host, out port, out sessionId, out userId));

            Assert.AreEqual("a&b=c d", sessionId);
        }

        [Test]
        public void TryParse_RejectsWhatIsNotAUdpEndpoint()
        {
            string host;
            int port;
            string sessionId;
            long userId;

            Assert.IsFalse(UdpEndpoint.TryParse(null, out host, out port, out sessionId, out userId));
            Assert.IsFalse(UdpEndpoint.TryParse("wss://game.example.com/ws", out host, out port, out sessionId, out userId));
            Assert.IsFalse(UdpEndpoint.TryParse("udp://game.example.com:7777", out host, out port, out sessionId, out userId));
            Assert.IsFalse(UdpEndpoint.TryParse("udp://game.example.com:7777?session=s&user=0", out host, out port, out sessionId, out userId));
            Assert.IsFalse(UdpEndpoint.TryParse("udp://game.example.com:7777?user=3", out host, out port, out sessionId, out userId));
        }
    }
}
