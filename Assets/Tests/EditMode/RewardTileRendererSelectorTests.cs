using NUnit.Framework;
using RewardChest;

namespace WordOnline.Tests
{
    /// <summary>
    /// Fixes how a reward picks its tile renderer: first registered renderer that handles the type,
    /// otherwise the fallback, so a type the client does not know still gets a tile.
    /// The real renderers live in Assembly-CSharp, which this assembly cannot see; fakes stand in.
    /// </summary>
    public class RewardTileRendererSelectorTests
    {
        private sealed class FakeRenderer : IRewardTileRenderer
        {
            private readonly string type;
            public string Name { get; }

            public FakeRenderer(string type, string name)
            {
                this.type = type;
                Name = name;
            }

            public bool Handles(string rewardType)
            {
                return type == null || rewardType == type;
            }

            public RewardTileContent Build(RewardView reward)
            {
                return new RewardTileContent(null, null, null, Name + ":" + reward.Type);
            }
        }

        private static readonly FakeRenderer Magic = new FakeRenderer(RewardTypes.Magic, "magic");
        private static readonly FakeRenderer Chest = new FakeRenderer(RewardTypes.Chest, "chest");
        private static readonly FakeRenderer Fallback = new FakeRenderer(null, "fallback");

        private static RewardTileRendererSelector CreateSelector()
        {
            return new RewardTileRendererSelector(new IRewardTileRenderer[] { Magic, null, Chest }, Fallback);
        }

        [Test]
        public void KnownTypeUsesItsRenderer()
        {
            Assert.AreSame(Magic, CreateSelector().Select("MAGIC"));
            Assert.AreSame(Chest, CreateSelector().Select("CHEST"));
        }

        [Test]
        public void TypeIsNormalizedBeforeSelection()
        {
            Assert.AreSame(Chest, CreateSelector().Select(" chest "));
        }

        [TestCase("GOLD_COIN")]
        [TestCase("")]
        [TestCase(null)]
        public void UnknownTypeUsesFallback(string type)
        {
            Assert.AreSame(Fallback, CreateSelector().Select(type));
        }

        [Test]
        public void BuildRendersUnknownTypeThroughFallback()
        {
            RewardTileContent content = CreateSelector().Build(new RewardView("GOLD_COIN", null, 0, 250));

            Assert.AreEqual("fallback:GOLD_COIN", content.FallbackName);
            Assert.IsFalse(content.HasLocalizedName);
        }

        [Test]
        public void FirstMatchingRendererWins()
        {
            var second = new FakeRenderer(RewardTypes.Magic, "second");
            var selector = new RewardTileRendererSelector(new IRewardTileRenderer[] { Magic, second }, Fallback);

            Assert.AreSame(Magic, selector.Select(RewardTypes.Magic));
        }

        [Test]
        public void NullRendererListStillHasFallback()
        {
            var selector = new RewardTileRendererSelector(null, Fallback);

            Assert.AreSame(Fallback, selector.Select(RewardTypes.Magic));
        }
    }
}
