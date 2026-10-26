using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace WordOnline.Tests {
    public class CustomBattleDeckMenuTests {
        [Test]
        public void BattlePageWiresIndependentReusableDeckSelectors() {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/CustomBotBattlePage.prefab");
            var menu = prefab.GetComponents<Component>().Single(c => c != null && c.GetType().Name == "CustomBattleDeckMenu");
            var page = prefab.GetComponents<Component>().Single(c => c != null && c.GetType().Name == "CustomBotBattlePage");
            Assert.AreEqual(menu, new SerializedObject(page).FindProperty("deckMenu").objectReferenceValue);
            var serialized = new SerializedObject(menu);
            var human = serialized.FindProperty("human").objectReferenceValue as TMP_Dropdown;
            var bot = serialized.FindProperty("bot").objectReferenceValue as TMP_Dropdown;
            Assert.NotNull(human); Assert.NotNull(bot); Assert.AreNotSame(human,bot);
            Assert.AreEqual(0,human.value); Assert.AreEqual(0,bot.value);
            foreach (var dropdown in new[] {human,bot}) {
                Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(dropdown));
                Assert.NotNull(dropdown.template); Assert.NotNull(dropdown.captionText); Assert.NotNull(dropdown.itemText);
                Assert.IsFalse(dropdown.GetComponents<Component>().Any(c => c != null && c.GetType().Name == "MatchingServerDropdown"));
                Assert.GreaterOrEqual(dropdown.GetComponent<RectTransform>().sizeDelta.y,60);
                Assert.GreaterOrEqual(dropdown.captionText.color.a,0.99f);
            }
            Assert.Less(human.GetComponent<RectTransform>().anchoredPosition.x,bot.GetComponent<RectTransform>().anchoredPosition.x);
        }
    }
}
