using System;
using Data;
using Global.Serialization;
using LobbyScene;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class FriendInviteStateTests
    {
        [Test]
        public void OlderSnapshotCannotEraseDeliveryAcknowledgement()
        {
            var delivered = new FriendInviteItem { inviteId = "a", revision = 2, deliveredAt = "2026-10-26T00:00:01Z" };
            var older = new FriendInviteItem { inviteId = "a", revision = 1 };
            Assert.IsFalse(FriendInviteState.IsNewer(delivered, older));
        }

        [Test]
        public void RecoveredInviteUsesServerExpiry()
        {
            var invite = new FriendInviteItem { expiresAt = "2026-10-26T00:00:30Z" };
            Assert.AreEqual(5, FriendInviteState.RemainingSeconds(invite, DateTimeOffset.Parse("2026-10-26T00:00:25Z")));
            Assert.AreEqual(0, FriendInviteState.RemainingSeconds(invite, DateTimeOffset.Parse("2026-10-26T00:00:35Z")));
        }

        [Test]
        public void NewInviteWithinSameSecondReplacesOldInvite()
        {
            var old = new FriendInviteItem { inviteId = "a", createdAt = "2026-10-26T00:00:00Z" };
            var next = new FriendInviteItem { inviteId = "b", createdAt = "2026-10-26T00:00:00.1Z" };
            Assert.IsTrue(FriendInviteState.IsNewer(old, next));
        }

        [Test]
        public void DeliveryFieldsRoundTripAndOldServerPayloadStillParses()
        {
            var invite = new FriendInviteItem { revision = 3, deliveredAt = "2026-10-26T00:00:01Z" };
            var copy = JsonCodec.Deserialize<FriendInviteItem>(JsonCodec.Serialize(invite));
            Assert.AreEqual(invite.deliveredAt, copy.deliveredAt);
            Assert.AreEqual(3, copy.revision);
            Assert.IsNull(JsonCodec.Deserialize<FriendInviteItem>("{\"status\":\"PENDING\"}").deliveredAt);
        }
    }
}
