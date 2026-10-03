using Data.Quests;
using NUnit.Framework;

namespace WordOnline.Tests
{
    /// <summary>
    /// A chest claimed at the end of an adventure must not show up again in the lobby's
    /// <c>POST /api/users/mine/quests/check</c> popup.
    /// </summary>
    public class ClaimedQuestLedgerTests
    {
        private static LobbyScene.QuestRewardDto Reward(long questId, string type = "CHEST")
        {
            return new LobbyScene.QuestRewardDto { questId = questId, rewardType = type, rewardId = 1, amount = 1 };
        }

        [Test]
        public void DropsRewardsOfClaimedQuests()
        {
            var ledger = new ClaimedQuestLedger();
            ledger.Remember(31);

            LobbyScene.QuestRewardDto[] kept = ledger.WithoutClaimed(new[] { Reward(31), Reward(32, "MAGIC"), Reward(31, "MAGIC") });

            Assert.AreEqual(1, kept.Length);
            Assert.AreEqual(32L, kept[0].questId);
        }

        [Test]
        public void KeepsRewardsWithoutQuestId()
        {
            var ledger = new ClaimedQuestLedger();
            ledger.Remember(31);

            Assert.AreEqual(1, ledger.WithoutClaimed(new[] { Reward(0) }).Length);
        }

        [Test]
        public void IgnoresNonPositiveIdsAndNullLists()
        {
            var ledger = new ClaimedQuestLedger();
            ledger.Remember(0);
            ledger.Remember(-4);

            Assert.IsFalse(ledger.Contains(0));
            Assert.IsFalse(ledger.Contains(-4));
            Assert.AreEqual(0, ledger.WithoutClaimed(null).Length);
        }

        [Test]
        public void ClearForgetsEveryQuest()
        {
            var ledger = new ClaimedQuestLedger();
            ledger.Remember(31);
            ledger.Clear();

            Assert.IsFalse(ledger.Contains(31));
            Assert.AreEqual(1, ledger.WithoutClaimed(new[] { Reward(31) }).Length);
        }
    }
}
