using Global.Serialization;
using LobbyScene;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class MatchQueueApiServiceTests
    {
        [Test]
        public void CreateTicket_SelectedDeckMode_SerializesWithSelectedDeckModeValue()
        {
            var request = new MatchTicketRequest
            {
                deckMode = MatchDeckMode.Selected
            };

            string json = JsonCodec.Serialize(request);

            Assert.IsNotNull(json, "Serialized JSON should not be null");
            Assert.That(json, Does.Contain("\"deckMode\":\"SELECTED\""));
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
            Assert.That(json, Does.Contain("\"deckMode\":\"RANDOM\""));
        }

        [Test]
        public void CreateTicket_NullDeckMode_SerializesWithoutDeckModeError()
        {
            var request = new MatchTicketRequest
            {
                deckMode = null
            };

            string json = JsonCodec.Serialize(request);

            Assert.IsNotNull(json, "Serialized JSON should not be null");
            Assert.That(json, Does.Contain("\"deckMode\":null"));
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
            Assert.That(json, Does.Contain("\"deckMode\":\"CUSTOM_MODE\""));
        }
    }
}
