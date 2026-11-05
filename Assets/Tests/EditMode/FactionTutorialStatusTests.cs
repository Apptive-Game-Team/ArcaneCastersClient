using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using GameScene.Dto;
using Newtonsoft.Json;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class FactionTutorialStatusTests
    {
        private const string Folder = "Assets/Localization/Onboarding/";
        private static Type Runtime(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("TutorialScene." + name)).First(type => type != null);

        [TestCase("ko-KR")]
        [TestCase("en")]
        public void ImportedOnboardingEntriesMatchTheirUniqueAuthoredIds(string locale)
        {
            var shared = AssetDatabase.LoadAssetAtPath<SharedTableData>(Folder + "Onboarding Shared Data.asset");
            var table = AssetDatabase.LoadAssetAtPath<StringTable>(Folder + "Onboarding_" + locale + ".asset");
            var ids = shared.Entries.Select(entry => entry.Id).ToArray();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length));
            Assert.That(shared.Entries.Select(entry => entry.Key).Distinct().Count(), Is.EqualTo(ids.Length));
            string source = File.ReadAllText(Folder + "Onboarding_" + locale + ".asset");
            Assert.That(Regex.IsMatch(source, @"(?m)^    - m_Id:"), Is.False);
            var rows = Regex.Matches(source, @"(?m)^  - m_Id: (\d+)\r?\n    m_Localized: ([^\r\n]+)")
                .Cast<Match>().ToArray();
            Assert.That(rows.Length, Is.EqualTo(ids.Length));
            var authored = new Dictionary<long, string>();
            foreach (Match row in rows)
            {
                string value = row.Groups[2].Value;
                authored.Add(long.Parse(row.Groups[1].Value), value.StartsWith("\"")
                    ? JsonConvert.DeserializeObject<string>(value) : value);
            }
            Assert.That(authored.Keys, Is.EquivalentTo(ids));
            foreach (var entry in shared.Entries)
                Assert.That(table.GetEntry(entry.Key)?.LocalizedValue, Is.EqualTo(authored[entry.Id]), entry.Key);
            foreach (string key in new[] { "loadingTitle", "connectingTitle", "errorTitle", "expiredTitle", "castRetryTitle", "selectionErrorTitle" })
                Assert.That(table.GetEntry("FactionTutorial." + key).LocalizedValue,
                    Is.Not.EqualTo(table.GetEntry("FactionTutorial.title").LocalizedValue), key);
        }

        [UnityTest]
        public IEnumerator KoreanBattleStatusesKeepTitlesBodiesAndButtonsTogether() => CheckStatuses("ko-KR");

        [UnityTest]
        public IEnumerator EnglishBattleStatusesKeepTitlesBodiesAndButtonsTogether() => CheckStatuses("en");

        private static IEnumerator CheckStatuses(string localeCode)
        {
            yield return LocalizationSettings.InitializationOperation;
            var previousLocale = LocalizationSettings.SelectedLocale;
            var locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
            Assert.That(locale, Is.Not.Null);
            LocalizationSettings.SelectedLocale = locale;
            var operation = LocalizationSettings.StringDatabase.GetTableAsync("Onboarding", locale);
            yield return operation;
            var table = operation.Result;
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/UI/Tutorial/FactionTutorialPanel.prefab"));
            var driverRoot = new GameObject("TutorialStatusFixture");
            driverRoot.SetActive(false);
            var driver = (MonoBehaviour)driverRoot.AddComponent(Runtime("FactionTutorialBattleDriver"));
            var selection = (MonoBehaviour)driverRoot.AddComponent(Runtime("FactionSelectionController"));
            var panel = root.GetComponents<MonoBehaviour>().Single(component => component.GetType().Name == "FactionTutorialPanel");
            foreach (var controller in new[] { driver, selection })
                controller.GetType().GetField("panel", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, panel);
            var data = new SerializedObject(panel);
            var title = (TMP_Text)data.FindProperty("compactTitle").objectReferenceValue;
            var body = (TMP_Text)data.FindProperty("compactBody").objectReferenceValue;
            var button = (UnityEngine.UI.Button)data.FindProperty("compactNext").objectReferenceValue;
            Action retry = () => { };
            try
            {
                Invoke(driver, "ShowConnecting");
                yield return CheckText(table, title, body, button, "connectingTitle", "connecting", null);
                Invoke(driver, "ShowConnectionError", retry);
                yield return CheckText(table, title, body, button, "errorTitle", "error", "retry");
                Invoke(driver, "ShowExpired");
                yield return CheckText(table, title, body, button, "expiredTitle", "expired", "reconnect");
                Invoke(driver, "ShowCastRejected");
                yield return CheckText(table, title, body, button, "castRetryTitle", "castRetry", "continue");
                Assert.That(body.text, Does.Not.Contain(localeCode == "ko-KR" ? "골렘" : "golem"));
                Invoke(driver, "ShowState", new TutorialStateDto { status = "FAILED", stage = "formation", revision = 1 });
                yield return CheckText(table, title, body, button, "retryTitle", "retryStep", "retry");
                Invoke(driver, "ShowState", new TutorialStateDto { status = "UNKNOWN", stage = "formation", revision = 2 });
                yield return CheckText(table, title, body, button, "errorTitle", "error", "retry");
                Invoke(selection, "ShowStatus", "FactionTutorial.loadingTitle", "FactionTutorial.loading", null, null);
                yield return CheckText(table, title, body, button, "loadingTitle", "loading", null);
                Invoke(selection, "Error", retry, false);
                yield return CheckText(table, title, body, button, "selectionErrorTitle", "selectionError", "retry");
                Invoke(selection, "Error", retry, true);
                yield return CheckText(table, title, body, button, "errorTitle", "error", "retry");
            }
            finally
            {
                Object.DestroyImmediate(driverRoot);
                Object.DestroyImmediate(root);
                LocalizationSettings.SelectedLocale = previousLocale;
            }
        }

        private static void Invoke(MonoBehaviour target, string method, params object[] arguments) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);

        private static IEnumerator CheckText(StringTable table, TMP_Text title, TMP_Text body,
            UnityEngine.UI.Button button, string titleKey, string bodyKey, string buttonKey)
        {
            string expectedTitle = table.GetEntry("FactionTutorial." + titleKey).LocalizedValue;
            string expectedBody = table.GetEntry("FactionTutorial." + bodyKey).LocalizedValue;
            float deadline = Time.realtimeSinceStartup + 5;
            while ((title.text != expectedTitle || body.text != expectedBody) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(title.text, Is.EqualTo(expectedTitle));
            Assert.That(body.text, Is.EqualTo(expectedBody));
            Assert.That(button.gameObject.activeSelf, Is.EqualTo(buttonKey != null));
            if (buttonKey != null)
            {
                var label = button.GetComponentInChildren<TMP_Text>(true);
                string expected = table.GetEntry("FactionTutorial." + buttonKey).LocalizedValue;
                while (label.text != expected && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(label.text, Is.EqualTo(expected));
                Assert.That(button.interactable, Is.True);
            }
        }
    }
}
