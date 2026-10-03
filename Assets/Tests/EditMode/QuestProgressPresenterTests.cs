using System.Collections.Generic;
using Data.Appearances;
using Data.Quests;
using NUnit.Framework;

namespace WordOnline.Tests
{
    /// <summary>
    /// Fixes which presenter draws which <c>conditionType</c>, that an unknown type still draws through the
    /// generic fallback, and the order the quest list and the appearance catalog are shown in.
    /// </summary>
    public class QuestProgressPresenterTests
    {
        private sealed class RecordingRow : IQuestRowView
        {
            public string Title;
            public string Label;
            public string Widget;
            public int Current;
            public int Total;

            public void SetTitle(string title) => Title = title;
            public void SetProgressLabel(string label) => Label = label;
            public void ShowBar(int current, int total) => Record("bar", current, total);
            public void ShowSegments(int current, int total) => Record("segments", current, total);
            public void ShowPips(int current, int total) => Record("pips", current, total);

            private void Record(string widget, int current, int total)
            {
                Widget = widget;
                Current = current;
                Total = total;
            }
        }

        /// <summary>Returns the English fallback, as a client with no localization table would.</summary>
        private sealed class FallbackText : IQuestText
        {
            private readonly Dictionary<long, string> adventureNames;

            public FallbackText(Dictionary<long, string> adventureNames = null)
            {
                this.adventureNames = adventureNames ?? new Dictionary<long, string>();
            }

            public string Localize(string key, string fallback, params object[] arguments)
            {
                return QuestTextFormat.Format(fallback, arguments);
            }

            public string FindAdventureName(long adventureId)
            {
                return adventureNames.TryGetValue(adventureId, out string name) ? name : null;
            }
        }

        private static RecordingRow Render(QuestDto quest, IQuestText text = null)
        {
            var row = new RecordingRow();
            QuestProgressPresenterRegistry.CreateDefault().Render(quest, row, text ?? new FallbackText());
            return row;
        }

        [TestCase("STAGE_CLEAR", typeof(StageClearQuestProgressPresenter))]
        [TestCase("stage_clear", typeof(StageClearQuestProgressPresenter))]
        [TestCase("TOTAL_WIN", typeof(TotalWinQuestProgressPresenter))]
        [TestCase("ADVENTURE_CLEAR", typeof(AdventureClearQuestProgressPresenter))]
        [TestCase("WEEKLY_PVP_WIN", typeof(GenericQuestProgressPresenter))]
        [TestCase("", typeof(GenericQuestProgressPresenter))]
        [TestCase(null, typeof(GenericQuestProgressPresenter))]
        public void SelectsPresenterByConditionType(string conditionType, System.Type expected)
        {
            Assert.IsInstanceOf(expected, QuestProgressPresenterRegistry.CreateDefault().Select(conditionType));
        }

        [Test]
        public void StageClearDrawsOneSegmentPerStage()
        {
            RecordingRow row = Render(new QuestDto { conditionType = "STAGE_CLEAR", progress = 2, requireValue = 3 });

            Assert.AreEqual("Clear 3 stages", row.Title);
            Assert.AreEqual("2/3", row.Label);
            Assert.AreEqual("segments", row.Widget);
            Assert.AreEqual(2, row.Current);
            Assert.AreEqual(3, row.Total);
        }

        [Test]
        public void StageClearWithManyStagesDrawsBar()
        {
            RecordingRow row = Render(new QuestDto { conditionType = "STAGE_CLEAR", progress = 4, requireValue = 30 });

            Assert.AreEqual("bar", row.Widget);
        }

        [Test]
        public void TotalWinDrawsBar()
        {
            RecordingRow row = Render(new QuestDto { conditionType = "TOTAL_WIN", progress = 7, requireValue = 10 });

            Assert.AreEqual("Win 10 matches", row.Title);
            Assert.AreEqual("7/10", row.Label);
            Assert.AreEqual("bar", row.Widget);
        }

        [Test]
        public void AdventureClearUsesAdventureNameAndPips()
        {
            var text = new FallbackText(new Dictionary<long, string> { { 2, "Stone Fortress" } });
            RecordingRow row = Render(
                new QuestDto { conditionType = "ADVENTURE_CLEAR", conditionTargetId = 2, progress = 1, requireValue = 4 },
                text);

            Assert.AreEqual("Clear Stone Fortress", row.Title);
            Assert.AreEqual("Stage 1/4", row.Label);
            Assert.AreEqual("pips", row.Widget);
        }

        [Test]
        public void AdventureClearFallsBackToAdventureNumber()
        {
            RecordingRow row = Render(new QuestDto { conditionType = "ADVENTURE_CLEAR", conditionTargetId = 5, requireValue = 3 });

            Assert.AreEqual("Clear Adventure 5", row.Title);
        }

        [Test]
        public void AdventureClearWithoutTargetStillHasTitle()
        {
            RecordingRow row = Render(new QuestDto { conditionType = "ADVENTURE_CLEAR", conditionTargetId = null, requireValue = 3 });

            Assert.AreEqual("Clear an adventure", row.Title);
        }

        [Test]
        public void UnknownTypeShowsHumanisedTitleAndPlainBar()
        {
            RecordingRow row = Render(new QuestDto { conditionType = "WEEKLY_PVP_WIN", progress = 1, requireValue = 5 });

            Assert.AreEqual("Weekly Pvp Win", row.Title);
            Assert.AreEqual("1/5", row.Label);
            Assert.AreEqual("bar", row.Widget);
            Assert.AreEqual(1, row.Current);
            Assert.AreEqual(5, row.Total);
        }

        [Test]
        public void RendersWithoutTextSource()
        {
            var row = new RecordingRow();
            QuestProgressPresenterRegistry.CreateDefault().Render(
                new QuestDto { conditionType = "TOTAL_WIN", progress = 1, requireValue = 2 }, row, null);

            Assert.AreEqual("Win 2 matches", row.Title);
        }

        [Test]
        public void RegistryAsksPresentersInOrderAndFallsBack()
        {
            var registry = new QuestProgressPresenterRegistry(
                new IQuestProgressPresenter[] { new TotalWinQuestProgressPresenter() },
                new GenericQuestProgressPresenter());

            Assert.IsInstanceOf<TotalWinQuestProgressPresenter>(registry.Select("TOTAL_WIN"));
            Assert.IsInstanceOf<GenericQuestProgressPresenter>(registry.Select("STAGE_CLEAR"));
        }

        [TestCase("WEEKLY_PVP_WIN", "Weekly Pvp Win")]
        [TestCase("total-win", "Total Win")]
        [TestCase("  ", "Quest")]
        [TestCase(null, "Quest")]
        public void HumanizesConditionType(string conditionType, string expected)
        {
            Assert.AreEqual(expected, QuestTextFormat.Humanize(conditionType));
        }

        [Test]
        public void OrdersOpenQuestsFirstThenByQuestId()
        {
            QuestDto[] ordered = QuestListOrder.Order(new[]
            {
                new QuestDto { questId = 4, state = QuestStates.Completed },
                new QuestDto { questId = 3, state = QuestStates.InProgress },
                null,
                new QuestDto { questId = 1, state = QuestStates.Completed },
                new QuestDto { questId = 2, state = QuestStates.Pending },
            });

            CollectionAssert.AreEqual(new long[] { 2, 3, 1, 4 }, System.Array.ConvertAll(ordered, quest => quest.questId));
            Assert.AreEqual(0, QuestListOrder.Order(null).Length);
        }

        [TestCase("MAGIC", null, QuestRewardIconKind.Magic)]
        [TestCase("card", null, QuestRewardIconKind.Magic)]
        [TestCase("DECORATION", null, QuestRewardIconKind.Decoration)]
        [TestCase("APPEARANCE", "storm", QuestRewardIconKind.Appearance)]
        [TestCase("APPEARANCE", null, QuestRewardIconKind.Placeholder)]
        [TestCase("CHEST", "wooden", QuestRewardIconKind.Placeholder)]
        [TestCase("TROPHY", null, QuestRewardIconKind.Placeholder)]
        [TestCase(null, null, QuestRewardIconKind.Placeholder)]
        public void ClassifiesRewardIcons(string rewardType, string rewardKey, QuestRewardIconKind expected)
        {
            Assert.AreEqual(expected, QuestRewardIconRule.Classify(new QuestRewardDto { rewardType = rewardType, rewardKey = rewardKey }));
        }

        [Test]
        public void OrdersAppearanceCatalogBySortOrderThenKey()
        {
            AppearanceDto[] ordered = AppearanceCatalog.Order(new[]
            {
                new AppearanceDto { key = "tide", sortOrder = 2 },
                new AppearanceDto { key = "storm", sortOrder = 1 },
                new AppearanceDto { key = "blaze", sortOrder = 1 },
                new AppearanceDto { key = " ", sortOrder = 0 },
                null,
                new AppearanceDto { key = "default", sortOrder = 0, owned = true, selected = true },
            });

            CollectionAssert.AreEqual(
                new[] { "default", "blaze", "storm", "tide" },
                System.Array.ConvertAll(ordered, appearance => appearance.key));
            Assert.AreEqual("default", AppearanceCatalog.FindSelectedKey(ordered, "fallback"));
        }

        [Test]
        public void MarkSelectedMovesTheSelectionAndOnlyOwnedCanBeSelected()
        {
            var catalog = new[]
            {
                new AppearanceDto { key = "default", owned = true, selected = true },
                new AppearanceDto { key = "storm", owned = true },
                new AppearanceDto { key = "tide", owned = false },
            };

            AppearanceCatalog.MarkSelected(catalog, "storm");

            Assert.IsFalse(catalog[0].selected);
            Assert.IsTrue(catalog[1].selected);
            Assert.IsTrue(AppearanceCatalog.CanSelect(catalog[1]));
            Assert.IsFalse(AppearanceCatalog.CanSelect(catalog[2]));
        }

        [TestCase(200, true, AppearanceSelectionOutcome.Success)]
        [TestCase(403, false, AppearanceSelectionOutcome.NotOwned)]
        [TestCase(404, false, AppearanceSelectionOutcome.UnknownAppearance)]
        [TestCase(500, false, AppearanceSelectionOutcome.Failed)]
        [TestCase(0, false, AppearanceSelectionOutcome.Failed)]
        public void ClassifiesSelectionResponse(long responseCode, bool transportSucceeded, AppearanceSelectionOutcome expected)
        {
            Assert.AreEqual(expected, AppearanceCatalog.ClassifyResponse(responseCode, transportSucceeded));
        }
    }
}
