using System.Collections.Generic;
using NUnit.Framework;
using RewardChest;

namespace WordOnline.Tests
{
    /// <summary>
    /// Fixes how the chest endpoints' bodies are read. A reward type the client does not know and a null
    /// <c>rewardKey</c> must parse like any other entry, because the whole list is discarded on a throw.
    /// </summary>
    public class ChestPayloadsTests
    {
        [Test]
        public void LevelRewardsPreserveCardNamesAndChestSlotsWithoutAnUnlockPool()
        {
            const string json = "{\"faction\":\"NATURE\",\"levels\":[{\"level\":1,\"requiredXp\":0,\"chestCount\":0,\"rewardSlots\":0,\"cards\":[{\"magicId\":5,\"magicName\":\"chain_lightning\",\"amount\":3}]},{\"level\":50,\"requiredXp\":12500,\"chestCount\":1,\"rewardSlots\":7,\"cards\":[{\"magicId\":90,\"magicName\":\"sea_serpent\",\"amount\":2}]}]}";
            Assert.IsTrue(Global.Serialization.JsonCodec.TryDeserialize(json, out Data.ProgressionRewardsDto catalog, out string error), error);
            Assert.AreEqual("NATURE", catalog.faction);
            Assert.AreEqual(0, catalog.levels[0].chestCount);
            Assert.AreEqual("chain_lightning", catalog.levels[0].cards[0].magicName);
            Assert.AreEqual(3, catalog.levels[0].cards[0].amount);
            Assert.AreEqual(12500L, catalog.levels[1].requiredXp);
            Assert.AreEqual(1, catalog.levels[1].chestCount);
            Assert.AreEqual(7, catalog.levels[1].rewardSlots);
            Assert.AreEqual(2, catalog.levels[1].cards[0].amount);
        }

        [Test]
        public void ProgressionChestKeepsSlotCountAndCurrencyOnlyResult()
        {
            Assert.IsTrue(ChestPayloads.TryParseChestList(
                "[{\"id\":9,\"chestKey\":\"progression_chest\",\"rewardSlots\":7,\"rewards\":[]}]",
                out ChestDto[] chests, out string error), error);
            Assert.AreEqual(7, chests[0].rewardSlots);
            Assert.IsEmpty(chests[0].rewards);
            Assert.IsTrue(ChestPayloads.TryParseOpenResponse(
                "{\"rewards\":[{\"rewardType\":\"CURRENCY\",\"rewardId\":0,\"rewardKey\":\"gold\",\"amount\":90}]}",
                out List<RewardView> rewards, out error), error);
            Assert.AreEqual("CURRENCY", rewards[0].Type);
            Assert.AreEqual("gold", rewards[0].Key);
            Assert.AreEqual(90, rewards[0].Amount);
        }

        [Test]
        public void ProgressionPayloadPreservesLargeCurrencyAndMaxLevel()
        {
            Assert.IsTrue(Global.Serialization.JsonCodec.TryDeserialize(
                "{\"faction\":\"NATURE\",\"level\":50,\"xp\":12500,\"levelXp\":0,\"nextLevelXp\":0,\"currency\":3000000000,\"gainedXp\":100,\"grantedChests\":2}",
                out Data.PlayerProgressionDto state, out string error), error);
            Assert.AreEqual(3000000000L, state.currency);
            Assert.AreEqual(50, state.level);
            Assert.AreEqual(0, state.nextLevelXp);
            Assert.AreEqual(2, state.grantedChests);
        }

        private const string ChestListJson = @"[
            {
                ""id"": 7,
                ""chestId"": 2,
                ""chestKey"": ""forest_chest"",
                ""acquiredAt"": ""2026-10-01T09:30:00Z"",
                ""rewards"": [
                    { ""rewardType"": ""MAGIC"", ""rewardId"": 12, ""rewardKey"": null, ""amount"": 1 },
                    { ""rewardType"": ""APPEARANCE"", ""rewardId"": 3, ""rewardKey"": ""storm"", ""amount"": 1 },
                    { ""rewardType"": ""GOLD_COIN"", ""rewardId"": 0, ""rewardKey"": null, ""amount"": 250 }
                ]
            },
            null,
            { ""id"": 8, ""chestId"": 2, ""chestKey"": ""forest_chest"", ""acquiredAt"": ""2026-10-02T09:30:00Z"" }
        ]";

        [Test]
        public void ChestListKeepsUnknownTypeAndNullKey()
        {
            bool parsed = ChestPayloads.TryParseChestList(ChestListJson, out ChestDto[] chests, out string error);

            Assert.IsTrue(parsed, error);
            Assert.AreEqual(2, chests.Length, "the null entry is dropped");
            Assert.AreEqual(7L, chests[0].id);
            Assert.AreEqual("forest_chest", chests[0].chestKey);
            Assert.AreEqual(3, chests[0].rewards.Length);
            Assert.IsNull(chests[0].rewards[0].rewardKey);
            Assert.AreEqual("storm", chests[0].rewards[1].rewardKey);
            Assert.AreEqual("GOLD_COIN", chests[0].rewards[2].rewardType);
            Assert.AreEqual(250, chests[0].rewards[2].amount);
        }

        [Test]
        public void ChestWithoutRewardsGetsEmptyList()
        {
            ChestPayloads.TryParseChestList(ChestListJson, out ChestDto[] chests, out _);

            Assert.IsNotNull(chests[1].rewards);
            Assert.AreEqual(0, chests[1].rewards.Length);
        }

        [TestCase("")]
        [TestCase("not json")]
        [TestCase("{\"rewards\": []}")]
        public void UnreadableChestListReturnsFalseAndEmptyArray(string body)
        {
            bool parsed = ChestPayloads.TryParseChestList(body, out ChestDto[] chests, out string error);

            Assert.IsFalse(parsed);
            Assert.IsNotNull(chests);
            Assert.AreEqual(0, chests.Length);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void OpenResponseBecomesNormalizedRewardViews()
        {
            const string body = @"{ ""rewards"": [
                { ""rewardType"": ""chest"", ""rewardId"": 4, ""rewardKey"": ""ice_chest"", ""amount"": 1 },
                { ""rewardType"": ""SOMETHING_NEW"", ""rewardId"": 9, ""amount"": 0 },
                null
            ] }";

            bool parsed = ChestPayloads.TryParseOpenResponse(body, out List<RewardView> rewards, out string error);

            Assert.IsTrue(parsed, error);
            Assert.AreEqual(2, rewards.Count);
            Assert.AreEqual(RewardTypes.Chest, rewards[0].Type);
            Assert.AreEqual("ice_chest", rewards[0].Key);
            Assert.AreEqual("SOMETHING_NEW", rewards[1].Type);
            Assert.IsNull(rewards[1].Key);
            Assert.AreEqual(1, rewards[1].Amount, "a missing amount still counts as one");
        }

        [Test]
        public void OpenResponseWithoutRewardsIsEmpty()
        {
            bool parsed = ChestPayloads.TryParseOpenResponse("{}", out List<RewardView> rewards, out _);

            Assert.IsTrue(parsed);
            Assert.AreEqual(0, rewards.Count);
        }

        [TestCase(200, ChestOpenOutcome.Opened)]
        [TestCase(204, ChestOpenOutcome.Opened)]
        [TestCase(404, ChestOpenOutcome.NotFound)]
        [TestCase(409, ChestOpenOutcome.AlreadyOpened)]
        [TestCase(500, ChestOpenOutcome.Failed)]
        [TestCase(0, ChestOpenOutcome.Failed)]
        public void StatusCodeMapsToOutcome(long statusCode, ChestOpenOutcome expected)
        {
            Assert.AreEqual(expected, ChestPayloads.OutcomeFromStatusCode(statusCode));
        }

        [TestCase(null, "UNKNOWN")]
        [TestCase("  ", "UNKNOWN")]
        [TestCase(" appearance ", "APPEARANCE")]
        public void RewardViewNormalizesType(string type, string expected)
        {
            var view = new RewardView(type, "  ", 1, -3);

            Assert.AreEqual(expected, view.Type);
            Assert.IsNull(view.Key);
            Assert.AreEqual(1, view.Amount);
        }
    }
}
