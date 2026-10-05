using System.Collections.Generic;
using Data.Quests;
using NUnit.Framework;
using RewardChest;

namespace WordOnline.Tests
{
    /// <summary>
    /// <c>POST /api/users/mine/quests/{questId}/claim</c>: every status code maps to an outcome instead of
    /// throwing, and the 200 body parses into reward views.
    /// </summary>
    public class QuestClaimPayloadsTests
    {
        [TestCase(200, QuestClaimOutcome.Claimed)]
        [TestCase(201, QuestClaimOutcome.Claimed)]
        [TestCase(404, QuestClaimOutcome.NotFound)]
        [TestCase(409, QuestClaimOutcome.AlreadyClaimed)]
        [TestCase(422, QuestClaimOutcome.NotClaimableYet)]
        [TestCase(400, QuestClaimOutcome.Failed)]
        [TestCase(500, QuestClaimOutcome.Failed)]
        [TestCase(0, QuestClaimOutcome.Failed)]
        public void StatusCodeMapsToOutcome(long statusCode, QuestClaimOutcome expected)
        {
            Assert.AreEqual(expected, QuestClaimPayloads.OutcomeFromStatusCode(statusCode));
        }

        [Test]
        public void ParsesGrantedRewards()
        {
            const string json = @"{ ""rewards"": [ { ""rewardType"": ""CHEST"", ""rewardId"": 1, ""rewardKey"": ""forest_chest"", ""amount"": 1 },
                                                  { ""rewardType"": ""TROPHY"", ""rewardId"": 7, ""rewardKey"": null, ""amount"": 2 } ] }";

            Assert.IsTrue(QuestClaimPayloads.TryParseClaimResponse(json, out List<RewardView> rewards, out string error), error);

            Assert.AreEqual(2, rewards.Count);
            Assert.AreEqual(RewardTypes.Chest, rewards[0].Type);
            Assert.AreEqual("forest_chest", rewards[0].Key);
            Assert.AreEqual("TROPHY", rewards[1].Type);
            Assert.AreEqual(2, rewards[1].Amount);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("not json")]
        [TestCase("null")]
        public void UnreadableBodyGivesEmptyListAndFalse(string json)
        {
            Assert.IsFalse(QuestClaimPayloads.TryParseClaimResponse(json, out List<RewardView> rewards, out string error));

            Assert.IsNotNull(rewards);
            Assert.AreEqual(0, rewards.Count);
            Assert.IsFalse(string.IsNullOrEmpty(error));
        }

        [Test]
        public void MissingRewardsArrayGivesEmptyList()
        {
            Assert.IsTrue(QuestClaimPayloads.TryParseClaimResponse("{}", out List<RewardView> rewards, out _));

            Assert.AreEqual(0, rewards.Count);
        }

        [Test]
        public void FirstChestSkipsOtherTypes()
        {
            var rewards = new List<RewardView>
            {
                new RewardView("MAGIC", null, 83, 1),
                new RewardView("CHEST", "fortress_chest", 2, 1),
                new RewardView("CHEST", "forest_chest", 1, 1),
            };

            Assert.AreEqual("fortress_chest", QuestClaimPayloads.FirstChest(rewards).Key);
            Assert.IsNull(QuestClaimPayloads.FirstChest(new List<RewardView> { new RewardView("MAGIC", null, 83, 1) }));
            Assert.IsNull(QuestClaimPayloads.FirstChest(null));
        }
    }
}
