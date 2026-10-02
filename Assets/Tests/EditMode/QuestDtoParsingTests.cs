using Data.Appearances;
using Data.Quests;
using Global.Serialization;
using NUnit.Framework;

namespace WordOnline.Tests
{
    /// <summary>
    /// <c>GET /api/users/mine/quests</c> and the appearance endpoints must parse whatever value the server
    /// adds later. A type the client does not know must survive as a string instead of throwing, because
    /// <c>JsonCodec.TryDeserialize</c> discards the whole response on a throw.
    /// </summary>
    public class QuestDtoParsingTests
    {
        private const string QuestListJson = @"[
            { ""questId"": 1, ""conditionType"": ""STAGE_CLEAR"", ""conditionTargetId"": null, ""state"": ""IN_PROGRESS"",
              ""progress"": 2, ""requireValue"": 3,
              ""rewards"": [ { ""rewardType"": ""MAGIC"", ""rewardId"": 83, ""rewardKey"": null, ""amount"": 2 } ] },
            { ""questId"": 2, ""conditionType"": ""WEEKLY_PVP_WIN"", ""conditionTargetId"": 7, ""state"": ""SOMETHING_NEW"",
              ""progress"": 0, ""requireValue"": 5,
              ""rewards"": [ { ""rewardType"": ""TROPHY"", ""rewardId"": 1, ""rewardKey"": ""gold_cup"", ""amount"": 1 } ] },
            { ""questId"": 3, ""conditionType"": ""ADVENTURE_CLEAR"", ""conditionTargetId"": 2, ""state"": ""COMPLETED"",
              ""progress"": 4, ""requireValue"": 4, ""rewards"": [] }
        ]";

        [Test]
        public void ParsesQuestListWithUnknownConditionTypeAndRewardType()
        {
            Assert.IsTrue(JsonCodec.TryDeserialize(QuestListJson, out QuestDto[] quests, out string error), error);

            Assert.AreEqual(3, quests.Length);
            Assert.AreEqual("WEEKLY_PVP_WIN", quests[1].conditionType);
            Assert.AreEqual("SOMETHING_NEW", quests[1].state);
            Assert.AreEqual("TROPHY", quests[1].rewards[0].rewardType);
            Assert.AreEqual("gold_cup", quests[1].rewards[0].rewardKey);
        }

        [Test]
        public void NullConditionTargetIdStaysNull()
        {
            QuestDto[] quests = JsonCodec.Deserialize<QuestDto[]>(QuestListJson);

            Assert.IsFalse(quests[0].conditionTargetId.HasValue);
            Assert.AreEqual(7L, quests[1].conditionTargetId);
        }

        [Test]
        public void NullRewardKeyStaysNull()
        {
            QuestDto[] quests = JsonCodec.Deserialize<QuestDto[]>(QuestListJson);

            QuestRewardDto reward = quests[0].rewards[0];
            Assert.IsNull(reward.rewardKey);
            Assert.AreEqual("MAGIC", reward.rewardType);
            Assert.AreEqual(83L, reward.rewardId);
            Assert.AreEqual(2, reward.amount);
        }

        [Test]
        public void EmptyAndMissingRewardsReadAsEmpty()
        {
            QuestDto[] quests = JsonCodec.Deserialize<QuestDto[]>(QuestListJson);
            QuestDto withoutRewards = JsonCodec.Deserialize<QuestDto>(@"{ ""questId"": 9, ""conditionType"": ""TOTAL_WIN"" }");

            Assert.AreEqual(0, quests[2].Rewards.Length);
            Assert.IsNull(withoutRewards.rewards);
            Assert.AreEqual(0, withoutRewards.Rewards.Length);
        }

        [Test]
        public void StateIsMatchedCaseInsensitively()
        {
            Assert.IsTrue(new QuestDto { state = "completed" }.IsCompleted);
            Assert.IsFalse(new QuestDto { state = "IN_PROGRESS" }.IsCompleted);
            Assert.IsFalse(new QuestDto { state = null }.IsCompleted);
        }

        [Test]
        public void DisplayProgressIsClampedAndCompletedReadsFull()
        {
            Assert.AreEqual(3, new QuestDto { progress = 7, requireValue = 3 }.DisplayProgress);
            Assert.AreEqual(0, new QuestDto { progress = -1, requireValue = 3 }.DisplayProgress);
            Assert.AreEqual(1, new QuestDto { progress = 0, requireValue = 0 }.DisplayRequireValue);
            Assert.AreEqual(5, new QuestDto { progress = 1, requireValue = 5, state = QuestStates.Completed }.DisplayProgress);
        }

        [Test]
        public void ComputedPropertiesStayOutOfTheSerializedPayload()
        {
            string json = JsonCodec.Serialize(new QuestDto { questId = 1, state = QuestStates.Completed, requireValue = 2 });

            StringAssert.DoesNotContain("IsCompleted", json);
            StringAssert.DoesNotContain("DisplayProgress", json);
            StringAssert.DoesNotContain("Rewards\"", json);
        }

        [Test]
        public void ParsesAppearanceCatalogAndSelectionResponse()
        {
            AppearanceDto[] catalog = JsonCodec.Deserialize<AppearanceDto[]>(
                @"[ { ""key"": ""default"", ""sortOrder"": 0, ""owned"": true, ""selected"": true },
                    { ""key"": ""storm"", ""sortOrder"": 1, ""owned"": false, ""selected"": false } ]");
            AppearanceSelectionDto selection = JsonCodec.Deserialize<AppearanceSelectionDto>(@"{ ""appearance"": ""storm"" }");

            Assert.AreEqual(2, catalog.Length);
            Assert.IsTrue(catalog[0].selected);
            Assert.IsFalse(catalog[1].owned);
            Assert.AreEqual("storm", selection.appearance);
            Assert.AreEqual(@"{""appearance"":""storm""}", JsonCodec.Serialize(new AppearanceSelectionDto { appearance = "storm" }));
        }
    }
}
