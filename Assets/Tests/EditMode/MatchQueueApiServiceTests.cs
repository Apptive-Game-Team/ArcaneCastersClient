using System.Collections.Generic;
using Global.Serialization;
using LobbyScene;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class MatchQueueApiServiceTests
    {
        [Test]
        public void GameServerPinger_AppendQuery_JoinsPingsAsServerIdAndRtt()
        {
            var pings = new List<ServerPing>
            {
                new ServerPing { serverId = 1, rttMs = 42 },
                new ServerPing { serverId = 2, rttMs = 80 }
            };

            Assert.AreEqual("http://lobby/api/x?pings=1%3A42%2C2%3A80", GameServerPinger.AppendQuery("http://lobby/api/x", pings));
        }

        [Test]
        public void GameServerPinger_AppendQuery_WithoutPings_LeavesPathUntouched()
        {
            Assert.AreEqual("http://lobby/api/x", GameServerPinger.AppendQuery("http://lobby/api/x", null));
            Assert.AreEqual("http://lobby/api/x", GameServerPinger.AppendQuery("http://lobby/api/x", new List<ServerPing>()));
        }

        [Test]
        public void CreateTicket_ServerPings_SerializesServerIdAndRttMs()
        {
            var request = new MatchTicketRequest
            {
                deckMode = MatchDeckMode.Selected,
                serverPings = new List<ServerPing> { new ServerPing { serverId = 3, rttMs = 42 } }
            };

            string json = JsonCodec.Serialize(request);

            StringAssert.Contains("\"serverPings\":[{\"serverId\":3,\"rttMs\":42}]", json);
        }

        [Test]
        public void CreateTicket_NullServerPings_OmitsServerPingsField()
        {
            string json = JsonCodec.Serialize(new MatchTicketRequest { deckMode = MatchDeckMode.Selected });

            StringAssert.DoesNotContain("serverPings", json);
        }

        [Test]
        public void CreateTicket_SelectedDeckMode_SerializesWithSelectedDeckModeValue()
        {
            var request = new MatchTicketRequest
            {
                deckMode = MatchDeckMode.Selected
            };

            string json = JsonCodec.Serialize(request);

            Assert.IsNotNull(json, "Serialized JSON should not be null");
            StringAssert.Contains("\"deckMode\":\"SELECTED\"", json);
        }

        [Test]
        public void CreateTicket_RandomDeckMode_SerializesWithRandomDeckModeValue()
        {
            var request = new MatchTicketRequest
            {
                deckMode = MatchDeckMode.Random
            };

            string json = JsonCodec.Serialize(request);

            Assert.IsNotNull(json, "Serialized JSON should not be null");
            StringAssert.Contains("\"deckMode\":\"RANDOM\"", json);
        }

        [Test]
        public void CreateTicket_NullDeckMode_OmitsDeckModeField()
        {
            var request = new MatchTicketRequest
            {
                deckMode = null
            };

            string json = JsonCodec.Serialize(request);

            Assert.IsNotNull(json, "Serialized JSON should not be null");
            StringAssert.DoesNotContain("deckMode", json);
        }

        [Test]
        public void CreateTicket_CustomDeckMode_SerializesExactString()
        {
            var request = new MatchTicketRequest
            {
                deckMode = "CUSTOM_MODE"
            };

            string json = JsonCodec.Serialize(request);

            Assert.IsNotNull(json, "Serialized JSON should not be null");
            StringAssert.Contains("\"deckMode\":\"CUSTOM_MODE\"", json);
        }
    }
}
