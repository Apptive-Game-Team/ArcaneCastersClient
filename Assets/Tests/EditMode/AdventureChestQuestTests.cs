using Data.Quests;
using Global.Serialization;
using NUnit.Framework;
using RewardChest;

namespace WordOnline.Tests
{
    /// <summary>
    /// The chest at the end of an adventure reads its state from the adventure's <c>ADVENTURE_CLEAR</c> quest in
    /// <c>GET /api/users/mine/quests</c>. A server that predates manual claims must leave the chest hidden.
    /// </summary>
    public class AdventureChestQuestTests
    {
        private static QuestDto Quest(long questId, string conditionType, long? targetId, string state = QuestStates.InProgress,
            bool claimable = false)
        {
            return new QuestDto
            {
                questId = questId,
                conditionType = conditionType,
                conditionTargetId = targetId,
                state = state,
                claimable = claimable,
            };
        }

        [Test]
        public void ParsesClaimModeAndClaimable()
        {
            const string json = @"[{ ""questId"": 9, ""conditionType"": ""ADVENTURE_CLEAR"", ""conditionTargetId"": 1,
                ""state"": ""IN_PROGRESS"", ""progress"": 1, ""requireValue"": 1, ""claimMode"": ""MANUAL"", ""claimable"": true,
                ""rewards"": [ { ""rewardType"": ""CHEST"", ""rewardId"": 1, ""rewardKey"": ""forest_chest"", ""amount"": 1 } ] }]";

            Assert.IsTrue(JsonCodec.TryDeserialize(json, out QuestDto[] quests, out string error), error);

            Assert.AreEqual("MANUAL", quests[0].claimMode);
            Assert.IsTrue(quests[0].claimable);
            Assert.IsTrue(quests[0].IsManualClaim);
        }

        [Test]
        public void MissingClaimFieldsDefaultToNotClaimable()
        {
            const string json = @"[{ ""questId"": 9, ""conditionType"": ""ADVENTURE_CLEAR"", ""conditionTargetId"": 1,
                ""state"": ""IN_PROGRESS"", ""progress"": 1, ""requireValue"": 1, ""rewards"": [] }]";

            Assert.IsTrue(JsonCodec.TryDeserialize(json, out QuestDto[] quests, out string error), error);

            Assert.IsNull(quests[0].claimMode);
            Assert.IsFalse(quests[0].claimable);
            Assert.IsFalse(quests[0].IsManualClaim);
            Assert.AreEqual(AdventureChestState.Hidden, AdventureChestQuest.StateOf(quests[0]));
        }

        [Test]
        public void UnknownClaimModeStaysAString()
        {
            const string json = @"[{ ""questId"": 9, ""conditionType"": ""ADVENTURE_CLEAR"", ""claimMode"": ""SCHEDULED"" }]";

            Assert.IsTrue(JsonCodec.TryDeserialize(json, out QuestDto[] quests, out string error), error);

            Assert.AreEqual("SCHEDULED", quests[0].claimMode);
            Assert.IsFalse(quests[0].IsManualClaim);
        }

        [Test]
        public void ClaimFieldsDoNotAddComputedPropertiesToTheCache()
        {
            string json = JsonCodec.Serialize(Quest(1, QuestConditionTypes.AdventureClear, 1, claimable: true));

            StringAssert.Contains("\"claimable\":true", json);
            StringAssert.DoesNotContain("IsManualClaim", json);
        }

        [Test]
        public void SelectsTheQuestByConditionTypeAndTargetId()
        {
            QuestDto[] quests =
            {
                Quest(1, QuestConditionTypes.StageClear, 1),
                Quest(2, QuestConditionTypes.AdventureClear, 2),
                Quest(3, QuestConditionTypes.AdventureClear, 1),
                Quest(4, QuestConditionTypes.TotalWin, null),
            };

            Assert.AreEqual(3L, AdventureChestQuest.Select(quests, 1).questId);
            Assert.AreEqual(2L, AdventureChestQuest.Select(quests, 2).questId);
        }

        [Test]
        public void ConditionTypeMatchIgnoresCaseAndSpaces()
        {
            QuestDto[] quests = { Quest(5, " adventure_clear ", 1) };

            Assert.AreEqual(5L, AdventureChestQuest.Select(quests, 1).questId);
        }

        [Test]
        public void NoMatchingQuestGivesNull()
        {
            QuestDto[] quests =
            {
                Quest(1, QuestConditionTypes.StageClear, 1),
                Quest(2, QuestConditionTypes.AdventureClear, 2),
                Quest(3, QuestConditionTypes.AdventureClear, null),
                null,
            };

            Assert.IsNull(AdventureChestQuest.Select(quests, 1));
            Assert.IsNull(AdventureChestQuest.Select(null, 1));
            Assert.IsNull(AdventureChestQuest.Select(new QuestDto[0], 1));
        }

        [Test]
        public void SeveralMatchesPreferClaimableThenCompletedThenLowestId()
        {
            QuestDto inProgress = Quest(10, QuestConditionTypes.AdventureClear, 1);
            QuestDto completed = Quest(11, QuestConditionTypes.AdventureClear, 1, QuestStates.Completed);
            QuestDto claimable = Quest(12, QuestConditionTypes.AdventureClear, 1, claimable: true);

            Assert.AreSame(claimable, AdventureChestQuest.Select(new[] { inProgress, completed, claimable }, 1));
            Assert.AreSame(completed, AdventureChestQuest.Select(new[] { inProgress, completed }, 1));

            QuestDto laterInProgress = Quest(20, QuestConditionTypes.AdventureClear, 1);
            Assert.AreSame(inProgress, AdventureChestQuest.Select(new[] { laterInProgress, inProgress }, 1));
        }

        [Test]
        public void StateMapsClaimableCompletedAndEverythingElse()
        {
            Assert.AreEqual(AdventureChestState.Hidden, AdventureChestQuest.StateOf(null));
            Assert.AreEqual(AdventureChestState.Hidden,
                AdventureChestQuest.StateOf(Quest(1, QuestConditionTypes.AdventureClear, 1, QuestStates.Pending)));
            Assert.AreEqual(AdventureChestState.Hidden,
                AdventureChestQuest.StateOf(Quest(1, QuestConditionTypes.AdventureClear, 1, QuestStates.InProgress)));
            Assert.AreEqual(AdventureChestState.Claimable,
                AdventureChestQuest.StateOf(Quest(1, QuestConditionTypes.AdventureClear, 1, QuestStates.InProgress, true)));
            Assert.AreEqual(AdventureChestState.Claimed,
                AdventureChestQuest.StateOf(Quest(1, QuestConditionTypes.AdventureClear, 1, QuestStates.Completed)));
            Assert.AreEqual(AdventureChestState.Claimed,
                AdventureChestQuest.StateOf(Quest(1, QuestConditionTypes.AdventureClear, 1, "completed")));
        }

        [Test]
        public void ChestRewardIsTheFirstChestEntry()
        {
            QuestDto quest = Quest(1, QuestConditionTypes.AdventureClear, 1);
            quest.rewards = new[]
            {
                new QuestRewardDto { rewardType = "MAGIC", rewardId = 83, amount = 1 },
                new QuestRewardDto { rewardType = "chest", rewardId = 1, rewardKey = "forest_chest", amount = 0 },
            };

            RewardView chest = AdventureChestQuest.ChestRewardOf(quest);

            Assert.AreEqual(RewardTypes.Chest, chest.Type);
            Assert.AreEqual("forest_chest", chest.Key);
            Assert.AreEqual(1, chest.Amount);
        }

        [Test]
        public void QuestWithoutChestRewardGivesNull()
        {
            QuestDto quest = Quest(1, QuestConditionTypes.AdventureClear, 1);
            Assert.IsNull(AdventureChestQuest.ChestRewardOf(quest));

            quest.rewards = new[] { new QuestRewardDto { rewardType = "MAGIC", rewardId = 83, amount = 1 } };
            Assert.IsNull(AdventureChestQuest.ChestRewardOf(quest));
            Assert.IsNull(AdventureChestQuest.ChestRewardOf(null));
        }
    }
}
