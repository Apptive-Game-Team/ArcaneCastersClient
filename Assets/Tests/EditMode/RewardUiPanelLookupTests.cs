using LobbyScene;
using NUnit.Framework;
using UnityEngine;

namespace WordOnline.Tests
{
    public class RewardUiPanelLookupTests
    {
        [Test]
        public void FindRewardPanel_FindsPanelWhenPresent()
        {
            var root = new GameObject("RewardUI");
            var panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform);

            var found = QuestRewardTracker.FindRewardPanel(root.transform);

            Assert.AreEqual(panel.transform, found);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void FindRewardPanel_FindsLegacyPanalWhenPanelMissing()
        {
            var root = new GameObject("RewardUI");
            var panal = new GameObject("Panal");
            panal.transform.SetParent(root.transform);

            var found = QuestRewardTracker.FindRewardPanel(root.transform);

            Assert.AreEqual(panal.transform, found);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void FindRewardPanel_DefaultsToRootWhenNeitherPresent()
        {
            var root = new GameObject("RewardUI");

            var found = QuestRewardTracker.FindRewardPanel(root.transform);

            Assert.AreEqual(root.transform, found);

            Object.DestroyImmediate(root);
        }
    }
}
