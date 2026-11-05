using Data;
using Global.Serialization;
using NUnit.Framework;

namespace WordOnline.Tests {
    public class ProgressionClaimContractTests {
        [TestCase(0, false, true)]
        [TestCase(503, false, true)]
        [TestCase(200, false, true)]
        [TestCase(404, false, false)]
        [TestCase(409, true, false)]
        [TestCase(200, true, false)]
        public void AmbiguousWritesRetryOnceOnlyWithinOriginalContext(long status, bool success, bool expected) {
            Assert.That(ProgressionClaimPresentation.ShouldRetry(0, status, success, true), Is.EqualTo(expected));
            Assert.That(ProgressionClaimPresentation.ShouldRetry(1, status, success, true), Is.False);
            Assert.That(ProgressionClaimPresentation.ShouldRetry(0, status, success, false), Is.False);
        }
        [Test]
        public void PendingKeepsNullableLevelAndFutureRewardTypes() {
            const string json = "{\"enabled\":true,\"baselineLevel\":3,\"claimedLevels\":[4],\"rewards\":[{\"id\":9,\"source\":\"DAILY\",\"level\":null,\"createdAt\":\"2026-11-05T00:00:00Z\",\"rewards\":[{\"rewardType\":\"FUTURE_TYPE\",\"rewardId\":17,\"rewardKey\":\"future\",\"amount\":2,\"rewardSlots\":3}]}]}";
            Assert.That(JsonCodec.TryDeserialize(json, out ProgressionPendingRewardsDto pending, out _), Is.True);
            Assert.That(pending.rewards[0].level, Is.Null);
            Assert.That(pending.rewards[0].rewards[0].rewardType, Is.EqualTo("FUTURE_TYPE"));
            Assert.That(pending.rewards[0].rewards[0].rewardSlots, Is.EqualTo(3));
        }

        [Test]
        public void ReceiptFiltersZeroBeforeRewardViewCanClampIt() {
            const string json = "{\"id\":9,\"alreadyClaimed\":true,\"rewards\":[{\"rewardType\":\"MAGIC\",\"rewardId\":17,\"amount\":0},{\"rewardType\":\"CHEST\",\"rewardKey\":\"progression_chest\",\"amount\":2}]}";
            Assert.That(JsonCodec.TryDeserialize(json, out ProgressionClaimReceiptDto receipt, out _), Is.True);
            Assert.That(receipt.alreadyClaimed, Is.True);
            var actual = ProgressionClaimPresentation.PositiveRewards(receipt.rewards);
            Assert.That(actual.Count, Is.EqualTo(1));
            Assert.That(actual[0].Amount, Is.EqualTo(2));
        }

        [Test]
        public void ReachedLevelAloneDoesNotMeanClaimed() {
            var pending = new ProgressionPendingRewardsDto { enabled = true, baselineLevel = 3, claimedLevels = new[] { 4 } };
            Assert.That(ProgressionClaimPresentation.IsClaimed(3, 8, pending), Is.True);
            Assert.That(ProgressionClaimPresentation.IsClaimed(4, 8, pending), Is.True);
            Assert.That(ProgressionClaimPresentation.IsClaimed(5, 8, pending), Is.False);
            Assert.That(ProgressionClaimPresentation.IsClaimed(4, 3, pending), Is.False);
            Assert.That(ProgressionClaimPresentation.IsClaimed(3, 8, null), Is.False);
            pending.enabled = false;
            Assert.That(ProgressionClaimPresentation.IsClaimed(5, 8, pending), Is.True);
        }
    }
}
