using System.Collections.Generic;
using Global.Serialization;
using LobbyScene;
using NUnit.Framework;

namespace WordOnline.Tests
{
    [TestFixture]
    public class FriendDtoTests
    {
        [Test]
        public void Deserialize_FriendSummaryList_Succeeds()
        {
            string json = @"[
                {""userId"":10,""name"":""PlayerA"",""email"":""a@test.com"",""mmr"":1200,""status"":""Online""},
                {""userId"":11,""name"":""PlayerB"",""email"":""b@test.com"",""mmr"":1500,""status"":""Offline""}
            ]";

            bool success = JsonCodec.TryDeserialize(json, out List<FriendSummary> list, out string error);
            Assert.That(success, Is.True, error);
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0].userId, Is.EqualTo(10));
            Assert.That(list[0].name, Is.EqualTo("PlayerA"));
            Assert.That(list[0].status, Is.EqualTo("Online"));
            Assert.That(list[1].userId, Is.EqualTo(11));
        }

        [Test]
        public void Deserialize_FriendRequestItem_Succeeds()
        {
            string json = @"{
                ""id"": 101,
                ""senderId"": 10,
                ""senderName"": ""PlayerA"",
                ""receiverId"": 20,
                ""receiverName"": ""PlayerB"",
                ""status"": ""PENDING"",
                ""createdAt"": ""2026-10-06T12:00:00Z""
            }";

            bool success = JsonCodec.TryDeserialize(json, out FriendRequestItem req, out string error);
            Assert.That(success, Is.True, error);
            Assert.That(req.id, Is.EqualTo(101));
            Assert.That(req.senderName, Is.EqualTo("PlayerA"));
            Assert.That(req.status, Is.EqualTo("PENDING"));
        }

        [Test]
        public void Deserialize_FriendInviteItem_WithMatchInfo_Succeeds()
        {
            string json = @"{
                ""inviteId"": ""inv-123"",
                ""inviterId"": 10,
                ""inviterName"": ""PlayerA"",
                ""inviteeId"": 20,
                ""inviteeName"": ""PlayerB"",
                ""status"": ""ACCEPTED"",
                ""matchInfo"": {
                    ""sessionId"": ""sess-456"",
                    ""server"": ""ws://localhost:7777""
                }
            }";

            bool success = JsonCodec.TryDeserialize(json, out FriendInviteItem invite, out string error);
            Assert.That(success, Is.True, error);
            Assert.That(invite.inviteId, Is.EqualTo("inv-123"));
            Assert.That(invite.matchInfo, Is.Not.Null);
            Assert.That(invite.matchInfo.sessionId, Is.EqualTo("sess-456"));
            Assert.That(invite.matchInfo.server, Is.EqualTo("ws://localhost:7777"));
        }

        [Test]
        public void Deserialize_FriendSearchResult_Succeeds()
        {
            string json = @"[
                {""userId"":10,""name"":""PlayerA"",""email"":""test@domain.com"",""isFriend"":true,""hasPendingRequest"":false},
                {""userId"":20,""name"":""PlayerB"",""email"":""other@domain.com"",""isFriend"":false,""hasPendingRequest"":true}
            ]";

            bool success = JsonCodec.TryDeserialize(json, out List<FriendSearchResult> results, out string error);
            Assert.That(success, Is.True, error);
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].isFriend, Is.True);
            Assert.That(results[1].hasPendingRequest, Is.True);
        }

        [Test]
        public void FriendEventPayload_ResolvesRequestAndMatchInfo()
        {
            string json = @"{
                ""type"": ""FRIEND_INVITE_ACCEPTED"",
                ""invite"": {
                    ""inviteId"": ""inv-789"",
                    ""matchInfo"": {
                        ""sessionId"": ""sess-999""
                    }
                }
            }";

            bool success = JsonCodec.TryDeserialize(json, out FriendEventPayload payload, out string error);
            Assert.That(success, Is.True, error);
            Assert.That(payload.type, Is.EqualTo("FRIEND_INVITE_ACCEPTED"));
            Assert.That(payload.ResolveMatchInfo(), Is.Not.Null);
            Assert.That(payload.ResolveMatchInfo().sessionId, Is.EqualTo("sess-999"));
        }
    }
}
