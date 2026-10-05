using System.Collections.Generic;
using LobbyScene;
using NUnit.Framework;
using RewardChest;

namespace WordOnline.Tests
{
    /// <summary>
    /// Fixes the quest check body formats the lobby has sent: a bare array, an object with
    /// <c>rewards</c>, the old <c>type</c>/<c>id</c>/<c>value</c> names, and now <c>rewardKey</c>.
    /// </summary>
    public class QuestRewardPayloadTests
    {
        [Test]
        public void ObjectFormatCarriesRewardKey()
        {
            const string body = @"{ ""rewards"": [
                { ""rewardType"": ""APPEARANCE"", ""rewardId"": 3, ""rewardKey"": ""storm"", ""amount"": 1, ""questId"": 11 },
                { ""rewardType"": ""CHEST"", ""rewardId"": 2, ""rewardKey"": ""forest_chest"", ""amount"": 1, ""questId"": 12 },
                { ""rewardType"": ""MAGIC"", ""rewardId"": 40, ""rewardKey"": null, ""amount"": 2, ""questId"": 13 }
            ] }";

            QuestRewardDto[] rewards = QuestRewardPayload.Parse(body);

            Assert.AreEqual(3, rewards.Length);
            Assert.AreEqual("storm", rewards[0].rewardKey);
            Assert.AreEqual("forest_chest", rewards[1].rewardKey);
            Assert.IsNull(rewards[2].rewardKey);
            Assert.AreEqual(13L, rewards[2].questId);
        }

        [Test]
        public void BareArrayWithoutRewardKeyStillParses()
        {
            const string body = @"[ { ""rewardType"": ""MAGIC"", ""rewardId"": 40, ""amount"": 1, ""questId"": 5 } ]";

            QuestRewardDto[] rewards = QuestRewardPayload.Parse(body);

            Assert.AreEqual(1, rewards.Length);
            Assert.AreEqual(40L, rewards[0].rewardId);
            Assert.IsNull(rewards[0].rewardKey);
        }

        [Test]
        public void OldFieldNamesFillTheRewardView()
        {
            const string body = @"[ { ""type"": ""card"", ""id"": 7, ""value"": 3 } ]";

            List<RewardView> views = QuestRewardPayload.ToRewardViews(QuestRewardPayload.Parse(body));

            Assert.AreEqual(1, views.Count);
            Assert.AreEqual(RewardTypes.Card, views[0].Type);
            Assert.AreEqual(7L, views[0].Id);
            Assert.AreEqual(3, views[0].Amount);
        }

        [Test]
        public void UnknownTypeAndMissingAmountBecomeAView()
        {
            const string body = @"{ ""rewards"": [ { ""rewardType"": ""GOLD_COIN"", ""rewardId"": 0 }, { } ] }";

            List<RewardView> views = QuestRewardPayload.ToRewardViews(QuestRewardPayload.Parse(body));

            Assert.AreEqual(2, views.Count);
            Assert.AreEqual("GOLD_COIN", views[0].Type);
            Assert.AreEqual(1, views[0].Amount);
            Assert.AreEqual(RewardTypes.Unknown, views[1].Type);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("{}")]
        public void EmptyBodiesGiveNoRewards(string body)
        {
            Assert.AreEqual(0, QuestRewardPayload.Parse(body).Length);
        }
    }
}
