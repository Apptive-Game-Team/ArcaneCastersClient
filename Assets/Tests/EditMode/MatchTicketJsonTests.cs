using Data;
using Global.Serialization;
using LobbyScene;
using NUnit.Framework;

namespace WordOnline.Tests
{
    /// <summary>
    /// 매칭 티켓 응답이 실제 서버 페이로드 형태로 역직렬화되는지 고정한다.
    /// <see cref="User"/> 가 기본 생성자를 잃으면 <c>matchInfo.leftUser</c> 에서 Json.NET 이 예외를 던진다.
    /// </summary>
    public class MatchTicketJsonTests
    {
        private const string MatchedTicketJson = @"{
            ""ticketId"": ""ticket-1"",
            ""state"": ""MATCHED"",
            ""version"": 4,
            ""matchInfo"": {
                ""message"": ""matched"",
                ""server"": ""http://localhost:7777"",
                ""sessionId"": ""session-1"",
                ""leftUser"": { ""id"": 11, ""name"": ""left"", ""email"": ""left@team6515.com"", ""selectedDeckId"": 3, ""mmr"": 1200 },
                ""rightUser"": { ""id"": 22, ""name"": ""right"", ""email"": ""right@team6515.com"", ""selectedDeckId"": 4, ""mmr"": 1150 }
            }
        }";

        [Test]
        public void DeserializesMatchedTicketWithUsers()
        {
            MatchTicket ticket = JsonCodec.Deserialize<MatchTicket>(MatchedTicketJson);

            Assert.AreEqual(MatchTicketState.Matched, ticket.ParsedState);
            Assert.AreEqual(11L, ticket.matchInfo.leftUser.id);
            Assert.AreEqual("left", ticket.matchInfo.leftUser.name);
            Assert.AreEqual(3L, ticket.matchInfo.leftUser.selectedDeckId);
            Assert.AreEqual(1200, ticket.matchInfo.leftUser.mmr);
            Assert.AreEqual(22L, ticket.matchInfo.rightUser.id);
        }

        [Test]
        public void DeserializesUserWithMissingAndNullFields()
        {
            User user = JsonCodec.Deserialize<User>(@"{""id"":11,""name"":null,""mmr"":null}");

            Assert.AreEqual(11L, user.id);
            Assert.IsNull(user.name);
            Assert.AreEqual(0, user.mmr);
            Assert.AreEqual(0L, user.selectedDeckId);
        }

        [Test]
        public void DeserializesQueuedTicketWithoutMatchInfo()
        {
            MatchTicket ticket = JsonCodec.Deserialize<MatchTicket>(
                @"{""ticketId"":""ticket-1"",""state"":""QUEUED"",""version"":1,""matchInfo"":null}");

            Assert.AreEqual(MatchTicketState.Queued, ticket.ParsedState);
            Assert.IsNull(ticket.matchInfo);
        }

        [Test]
        public void SerializesRandomDeckTicketRequestAsStringMode()
        {
            string json = JsonCodec.Serialize(new MatchTicketRequest
            {
                deckMode = MatchDeckMode.Random
            });

            StringAssert.Contains("\"deckMode\":\"RANDOM\"", json);
        }
    }
}
