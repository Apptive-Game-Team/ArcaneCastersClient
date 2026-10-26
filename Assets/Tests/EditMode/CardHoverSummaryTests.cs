using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class CardHoverSummaryTests
    {
        private readonly List<GameObject> instances = new();
        private Component controller;
        private Component card;
        private float previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            previousTimeScale = Time.timeScale;
            GameObject panel = Create("hover-details");
            panel.AddComponent<RectTransform>();
            Component view = panel.AddComponent(FindType("GameScene.Card.ZoomedCardUI"));
            // 시간 검증은 번역 로딩과 분리하고 실제 확대 카드의 즉시 표시 경로를 쓴다.
            Set(view, "cardNameText", CreateText());
            Set(view, "cardManaText", CreateText());
            Set(view, "cardImage", Create("image").AddComponent<Image>());
            Call(view, "Awake");
            panel.SetActive(false);

            GameObject host = Create("hover-controller");
            host.SetActive(false);
            controller = host.AddComponent(FindType("GameScene.Card.CardUIZoom"));
            Set(controller, "zoomedCardUI", view);
            Set(controller, "detailPrefab", view);
            AudioSource audio = Create("hover-audio").AddComponent<AudioSource>();
            audio.clip = AudioClip.Create("hover-test", 64, 1, 44100, false);
            Set(controller, "audioSource", audio);

            card = Create("hover-card").AddComponent(FindType("GameScene.Card.CardUI"));
            Set(card, "cardNameText", CreateText());
            Set(card, "cardManaText", CreateText());
            Set(card, "image", Create("card-image").AddComponent<Image>());
            object magic = Activator.CreateInstance(FindType("Data.Magic.CombinedMagicData"));
            magic.GetType().GetField("serverName").SetValue(magic, "fire_shot");
            card.GetType().GetProperty("Magic").GetSetMethod(true).Invoke(card, new[] { magic });
        }

        [TearDown]
        public void TearDown()
        {
            Call(controller, "Hide");
            Object.DestroyImmediate(((AudioSource)Get(controller, "audioSource")).clip);
            foreach (GameObject instance in instances) Object.DestroyImmediate(instance);
            instances.Clear();
            Time.timeScale = previousTimeScale;
        }

        [Test]
        public void PendingHoverDoesNotDisplayBeforeDeadlineAndDisplaysAfterIt()
        {
            Pending(Time.unscaledTime + 0.01f);
            Call(controller, "Update");
            Assert.IsFalse((bool)Get(controller, "hasHover"));
            Assert.IsFalse(((Component)Get(controller, "zoomedCardUI")).gameObject.activeSelf);
            Set(controller, "hoverReadyTime", Time.unscaledTime - 0.01f);
            Call(controller, "Update");
            Assert.IsTrue((bool)Get(controller, "hasHover"));
            Assert.IsTrue(((Component)Get(controller, "zoomedCardUI")).gameObject.activeSelf);
        }

        [Test]
        public void PendingHoverUsesUnscaledTimeAndIgnoresEarlyPauseConfirmation()
        {
            Pending(Time.unscaledTime + 1f);
            Call(controller, "ApplyServerPause", true);
            Assert.IsFalse((bool)Get(controller, "locallyPaused"));
            Time.timeScale = 0f;
            Set(controller, "hoverReadyTime", Time.unscaledTime - 0.01f);
            Call(controller, "Update");
            Assert.IsTrue((bool)Get(controller, "hasHover"));
        }

        [TestCase("Hide")]
        [TestCase("OnDisable")]
        public void CancellationClearsPendingHover(string method)
        {
            Pending(Time.unscaledTime - 1f);
            Call(controller, method);
            Call(controller, "Update");
            Assert.IsNull(Get(controller, "hoveredCard"));
            Assert.IsFalse((bool)Get(controller, "hasHover"));
        }

        [Test]
        public void FocusLossAndInactiveCardCancelPendingHover()
        {
            Pending(Time.unscaledTime - 1f);
            Call(controller, "OnApplicationFocus", false);
            Assert.IsNull(Get(controller, "hoveredCard"));
            Pending(Time.unscaledTime - 1f);
            card.gameObject.SetActive(false);
            Call(controller, "Update");
            Assert.IsNull(Get(controller, "hoveredCard"));
            Assert.IsFalse((bool)Get(controller, "hasHover"));
        }

        [Test]
        public void DestroyedCardCancelsPendingHover()
        {
            Pending(Time.unscaledTime - 1f);
            Object.DestroyImmediate(card);
            Call(controller, "Update");
            Assert.IsNull(Get(controller, "hoveredCard"));
            Assert.IsFalse((bool)Get(controller, "hasHover"));
        }

        [Test]
        public void SwitchingCardsResetsOneSecondDelayAndSameCardKeepsDeadline()
        {
            if (!Application.isFocused) Assert.Ignore("batch Editor에 포커스가 없어 Show 진입 검증을 생략한다.");
            Call(controller, "Show", card);
            float deadline = (float)Get(controller, "hoverReadyTime");
            Assert.That(deadline - Time.unscaledTime, Is.InRange(0.95f, 1f));
            Call(controller, "Show", card);
            Assert.AreEqual(deadline, Get(controller, "hoverReadyTime"));
            Set(controller, "hoverReadyTime", Time.unscaledTime - 1f);
            Component second = Create("second-card").AddComponent(card.GetType());
            card.GetType().GetProperty("Magic").GetSetMethod(true).Invoke(second,
                new[] { card.GetType().GetProperty("Magic").GetValue(card) });
            Call(controller, "Show", second);
            Assert.AreSame(second, Get(controller, "hoveredCard"));
            Assert.That((float)Get(controller, "hoverReadyTime") - Time.unscaledTime, Is.InRange(0.95f, 1f));
            Assert.IsFalse((bool)Get(controller, "hasHover"));
        }

        [Test]
        public void HidingDisplayedHoverRestoresPreviousTimeScale()
        {
            Pending(Time.unscaledTime - 1f);
            Call(controller, "Update");
            Time.timeScale = 0.6f;
            Call(controller, "ApplyServerPause", true);
            if (!Application.isFocused) Assert.Ignore("batch Editor에 포커스가 없어 정지 확인 검증을 생략한다.");
            Assert.AreEqual(0f, Time.timeScale);
            Call(controller, "Hide");
            Assert.AreEqual(0.6f, Time.timeScale);
            Call(controller, "ApplyServerPause", true);
            Assert.AreEqual(0.6f, Time.timeScale);
        }

        [Test]
        public void LegacyTutorialCardStillDisplaysImmediately()
        {
            if (!Application.isFocused) Assert.Ignore("batch Editor에 포커스가 없어 Show 진입 검증을 생략한다.");
            Set(controller, "detailPrefab", null);
            Call(controller, "Show", card);
            Assert.IsTrue((bool)Get(controller, "hasHover"));
        }

        [Test]
        public void EveryMagicHasShortNumericFreeTextInBothLocales()
        {
            var names = Entries("Magic Shared Data", "m_Entries").Select(e => e.FindPropertyRelative("m_Key").stringValue.Trim())
                .Where(k => !k.StartsWith("'"))
                .Select(k => "summary." + Regex.Replace(k, "[A-Z]", m => "_" + m.Value.ToLowerInvariant()))
                .Append("summary.pve_nature_slime_nest").ToArray();
            var keys = Entries("MagicBook Shared Data", "m_Entries").ToDictionary(
                e => e.FindPropertyRelative("m_Key").stringValue, e => e.FindPropertyRelative("m_Id").longValue);
            foreach (string locale in new[] { "ko-KR", "en" }) {
                var text = Entries("MagicBook_" + locale, "m_TableData").ToDictionary(
                    e => e.FindPropertyRelative("m_Id").longValue, e => e.FindPropertyRelative("m_Localized").stringValue);
                foreach (string name in names) {
                    Assert.IsTrue(keys.ContainsKey(name), name + " / summary entries: " + keys.Count(k => k.Key.StartsWith("summary.")));
                    Assert.IsTrue(text.ContainsKey(keys[name]), locale + " " + name);
                    string summary = text[keys[name]];
                    Assert.IsNotEmpty(summary, locale + " " + name);
                    Assert.LessOrEqual(summary.Length, locale == "en" ? 180 : 80, name);
                    Assert.IsFalse(Regex.IsMatch(summary, @"\d|[{}<>]"), name);
                }
            }
        }

        [UnityTest]
        public IEnumerator SummaryLookupUsesServerNameAndMissingEntryStaysEmpty()
        {
            // EditMode는 플레이 모드의 자동 언어 선택을 실행하지 않는다.
            PropertyInfo locale = FindType("UnityEngine.Localization.Settings.LocalizationSettings").GetProperty("SelectedLocale");
            object previous = locale.GetValue(null);
            locale.SetValue(null, AssetDatabase.LoadAssetAtPath<Object>("Assets/Localization/English (en).asset"));
            try {
                object magic = card.GetType().GetProperty("Magic").GetValue(card);
                magic.GetType().GetField("textLocalizationKey").SetValue(magic, "an_unrelated_alias");
                MethodInfo lookup = FindType("GameScene.Card.ZoomedCardUI").GetMethod("GetSummaryAsync",
                    BindingFlags.Static | BindingFlags.NonPublic);
                var task = (Task<string>)lookup.Invoke(null, new[] { magic });
                float deadline = Time.realtimeSinceStartup + 15f;
                while (!task.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(task.IsCompleted, "간단 설명 번역 로딩이 완료되어야 한다.");
                string actual = task.GetAwaiter().GetResult();
                Assert.That(actual, Does.Contain("Fires a shot"));
                Assert.IsFalse(Regex.IsMatch(actual, @"\d|[{}<>]"));
                magic.GetType().GetField("serverName").SetValue(magic, "missing_magic_for_test");
                var missing = (Task<string>)lookup.Invoke(null, new[] { magic });
                while (!missing.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(missing.IsCompleted);
                Assert.AreEqual(string.Empty, missing.GetAwaiter().GetResult());
            } finally {
                locale.SetValue(null, previous);
            }
        }

        private static IEnumerable<SerializedProperty> Entries(string name, string property)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>("Assets/Localization/" + name + ".asset");
            Assert.IsNotNull(asset, name);
            var data = new SerializedObject(asset).FindProperty(property);
            for (int i = 0; i < data.arraySize; i++) yield return data.GetArrayElementAtIndex(i);
        }

        private void Pending(float deadline)
        {
            Set(controller, "hoveredCard", card);
            Set(controller, "hoverReadyTime", deadline);
        }

        private GameObject Create(string name)
        {
            var result = new GameObject(name);
            instances.Add(result);
            return result;
        }

        private Component CreateText() => Create("text").AddComponent(FindType("TMPro.TextMeshProUGUI"));
        private static Type FindType(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(name)).First(t => t != null);
        private static object Get(object target, string field) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Call(object target, string method, params object[] args) => target.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(m => m.Name == method && m.GetParameters().Length == args.Length).Invoke(target, args);
    }
}
