using NUnit.Framework;
using RewardChest;

namespace WordOnline.Tests
{
    /// <summary>
    /// After a claim the client finds the new chest in <c>GET /api/users/mine/chests</c> by key and takes the
    /// newest unopened one, because the claim response names the chest definition, not the owned row.
    /// </summary>
    public class ChestSelectionTests
    {
        private static ChestDto Chest(long id, string key, string acquiredAt)
        {
            return new ChestDto { id = id, chestKey = key, acquiredAt = acquiredAt };
        }

        [Test]
        public void TakesTheNewestOfTheKey()
        {
            ChestDto[] chests =
            {
                Chest(1, "forest_chest", "2026-10-01T10:00:00"),
                Chest(2, "forest_chest", "2026-10-03T09:00:00"),
                Chest(3, "fortress_chest", "2026-10-04T09:00:00"),
                Chest(4, "forest_chest", "2026-10-02T09:00:00"),
            };

            Assert.AreEqual(2L, ChestSelection.FindNewestUnopened(chests, "forest_chest").id);
            Assert.AreEqual(3L, ChestSelection.FindNewestUnopened(chests, "fortress_chest").id);
        }

        [Test]
        public void KeyMatchIgnoresCaseAndSpaces()
        {
            ChestDto[] chests = { Chest(7, "Forest_Chest ", "2026-10-01T10:00:00") };

            Assert.AreEqual(7L, ChestSelection.FindNewestUnopened(chests, " forest_chest").id);
        }

        [Test]
        public void SameTimestampGoesToTheHigherId()
        {
            ChestDto[] chests =
            {
                Chest(5, "forest_chest", "2026-10-01T10:00:00"),
                Chest(9, "forest_chest", "2026-10-01T10:00:00"),
                Chest(6, "forest_chest", "2026-10-01T10:00:00"),
            };

            Assert.AreEqual(9L, ChestSelection.FindNewestUnopened(chests, "forest_chest").id);
        }

        [Test]
        public void UnreadableTimestampCountsAsOldest()
        {
            ChestDto[] chests =
            {
                Chest(50, "forest_chest", "yesterday"),
                Chest(2, "forest_chest", "2026-10-01T10:00:00"),
                Chest(60, "forest_chest", null),
            };

            Assert.AreEqual(2L, ChestSelection.FindNewestUnopened(chests, "forest_chest").id);
        }

        [Test]
        public void NoChestOfTheKeyGivesNull()
        {
            ChestDto[] chests = { Chest(1, "fortress_chest", "2026-10-01T10:00:00"), null };

            Assert.IsNull(ChestSelection.FindNewestUnopened(chests, "forest_chest"));
            Assert.IsNull(ChestSelection.FindNewestUnopened(null, "forest_chest"));
            Assert.IsNull(ChestSelection.FindNewestUnopened(new ChestDto[0], "forest_chest"));
        }

        [Test]
        public void EmptyKeyMatchesEveryChest()
        {
            ChestDto[] chests =
            {
                Chest(1, "fortress_chest", "2026-10-02T10:00:00"),
                Chest(2, "forest_chest", "2026-10-01T10:00:00"),
            };

            Assert.AreEqual(1L, ChestSelection.FindNewestUnopened(chests, null).id);
            Assert.AreEqual(1L, ChestSelection.FindNewestUnopened(chests, " ").id);
        }
    }
}
