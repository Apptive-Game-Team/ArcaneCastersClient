using System.Collections.Generic;
using NUnit.Framework;
using RewardChest;

namespace WordOnline.Tests
{
    /// <summary>
    /// Fixes where chest art is looked up and in what order: the key's own folder, then the default folder,
    /// then nothing. Strings stand in for sprites; a path in the set means the asset exists.
    /// </summary>
    public class ChestSpritePathsTests
    {
        private static string Load(string key, string state, params string[] existing)
        {
            var set = new HashSet<string>(existing);
            return ChestSpritePaths.Load(key, state, path => set.Contains(path) ? path : null);
        }

        [Test]
        public void BuildsClosedAndOpenPathsUnderTheKeyFolder()
        {
            Assert.AreEqual("RewardChest/Chests/forest_chest/closed", ChestSpritePaths.Build("forest_chest", ChestSpritePaths.Closed));
            Assert.AreEqual("RewardChest/Chests/forest_chest/open", ChestSpritePaths.Build("forest_chest", ChestSpritePaths.Open));
        }

        [Test]
        public void KnownKeyLoadsItsOwnArt()
        {
            string closed = Load("forest_chest", "closed", "RewardChest/Chests/forest_chest/closed", "RewardChest/Chests/default/closed");
            Assert.AreEqual("RewardChest/Chests/forest_chest/closed", closed);
        }

        [Test]
        public void UnknownKeyFallsBackToDefault()
        {
            Assert.AreEqual("RewardChest/Chests/default/open", Load("no_such_chest", "open", "RewardChest/Chests/default/open"));
        }

        [Test]
        public void OpenFallsBackIndependentlyOfClosed()
        {
            // The key has closed art only; its open lookup must still reach the default open art.
            string[] existing = { "RewardChest/Chests/forest_chest/closed", "RewardChest/Chests/default/open" };
            Assert.AreEqual("RewardChest/Chests/forest_chest/closed", Load("forest_chest", "closed", existing));
            Assert.AreEqual("RewardChest/Chests/default/open", Load("forest_chest", "open", existing));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("../secret")]
        [TestCase("a/b")]
        [TestCase("a\\b")]
        public void EmptyOrUnsafeKeyBuildsTheDefaultPath(string key)
        {
            Assert.AreEqual("RewardChest/Chests/default/closed", ChestSpritePaths.Build(key, ChestSpritePaths.Closed));
            Assert.AreEqual("RewardChest/Chests/default/closed", Load(key, "closed", "RewardChest/Chests/default/closed"));
        }

        [Test]
        public void DefaultKeyAsksForItsPathOnce()
        {
            var asked = new List<string>();
            string result = ChestSpritePaths.Load<string>("default", "closed", path =>
            {
                asked.Add(path);
                return null;
            });
            Assert.IsNull(result);
            Assert.AreEqual(1, asked.Count);
        }

        [Test]
        public void MissingEverywhereReturnsNull()
        {
            Assert.IsNull(Load("forest_chest", "closed"));
        }
    }
}
