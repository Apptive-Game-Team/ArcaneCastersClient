using System.Collections;
using System.Linq;
using System.Reflection;
using Data;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WordOnline.Tests {
    public class ProgressionExperiencePlayModeTests {
        private GameObject root;
        private MonoBehaviour view;
        private float previousTimeScale;
        private FieldInfo Field(string name) => view.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        private T Read<T>(string name) => (T)Field(name).GetValue(view);
        private void Bind(PlayerProgressionDto value) => view.GetType().GetMethod("Bind").Invoke(view, new object[] { value });

        [SetUp]
        public void SetUp() {
            previousTimeScale = Time.timeScale;
#if UNITY_EDITOR
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Progression/PlayerProgressionView.prefab"));
            view = root.GetComponents<MonoBehaviour>().Single(component => component.GetType().Name == "PlayerProgressionView");
            Field("xpLevels").SetValue(view, new[] {
                new ProgressionLevelRewardDto { level = 1, requiredXp = 0 },
                new ProgressionLevelRewardDto { level = 2, requiredXp = 100 },
                new ProgressionLevelRewardDto { level = 3, requiredXp = 200 },
                new ProgressionLevelRewardDto { level = 4, requiredXp = 300 }
            });
#endif
        }

        [TearDown]
        public void TearDown() {
            Time.timeScale = previousTimeScale;
            if (root != null) Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator ExperienceFillsBeforeLevelChangesAndKeepsFinalRemainder() {
            Time.timeScale = 0;
            Bind(new PlayerProgressionDto { faction = "NATURE", level = 1, xp = 80, levelXp = 80, nextLevelXp = 100 });
            var target = new PlayerProgressionDto { faction = "NATURE", level = 2, xp = 130, levelXp = 30, nextLevelXp = 100, gainedXp = 50 };
            Bind(target);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(Read<TMP_Text>("levelText").text, Is.EqualTo("Lv. 1"));
            Assert.That(Read<Image>("xpFill").rectTransform.anchorMax.x, Is.InRange(0.81f, 0.99f));
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.That(Read<TMP_Text>("levelText").text, Is.EqualTo("Lv. 2"));
            Assert.That(Read<Image>("xpFill").rectTransform.anchorMax.x, Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(Read<GameObject>("xpNoticeRoot").activeSelf, Is.True);
            Assert.That(Read<TMP_Text>("xpNotice").text, Does.Contain("2"));
            Bind(target);
            yield return null;
            Assert.That(Read<Image>("xpFill").rectTransform.anchorMax.x, Is.EqualTo(0.3f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator MultipleLevelsAnimateAndDisablingSettlesWithoutReplaying() {
            Bind(new PlayerProgressionDto { faction = "HELL", level = 1, xp = 0, nextLevelXp = 100 });
            var target = new PlayerProgressionDto { faction = "HELL", level = 3, xp = 250, levelXp = 50, nextLevelXp = 100, gainedXp = 250 };
            Bind(target);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(Read<TMP_Text>("levelText").text, Is.EqualTo("Lv. 2"));
            root.SetActive(false);
            Assert.That(Read<TMP_Text>("levelText").text, Is.EqualTo("Lv. 3"));
            Assert.That(Read<Image>("xpFill").rectTransform.anchorMax.x, Is.EqualTo(0.5f).Within(0.001f));
            root.SetActive(true);
            Bind(target);
            yield return null;
            Assert.That(Read<GameObject>("xpNoticeRoot").activeSelf, Is.False);
            Assert.That(Read<Image>("xpFill").rectTransform.anchorMax.x, Is.EqualTo(0.5f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator MaximumLevelFinishesAtMaxInsteadOfDividingByZero() {
            Field("xpLevels").SetValue(view, new[] {
                new ProgressionLevelRewardDto { level = 49, requiredXp = 12170 },
                new ProgressionLevelRewardDto { level = 50, requiredXp = 12500 }
            });
            Bind(new PlayerProgressionDto { faction = "HELL", level = 49, xp = 12450, levelXp = 280, nextLevelXp = 330 });
            Bind(new PlayerProgressionDto { faction = "HELL", level = 50, xp = 12550, levelXp = 0, nextLevelXp = 0, gainedXp = 100 });
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.That(Read<TMP_Text>("levelText").text, Is.EqualTo("Lv. 50"));
            Assert.That(Read<TMP_Text>("xpText").text, Is.EqualTo("MAX"));
            Assert.That(Read<Image>("xpFill").rectTransform.anchorMax.x, Is.EqualTo(1));
        }
    }
}
