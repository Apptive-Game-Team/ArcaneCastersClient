using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WordOnline.Tests
{
    public class ComicIntroTests
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Tutorial/ComicIntroPlayer.prefab";
        private GameObject root;
        private SerializedObject player;

        [SetUp]
        public void SetUp()
        {
            root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            var component = root.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "ComicIntroPlayer");
            player = new SerializedObject(component);
            Canvas.ForceUpdateCanvases();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void SevenIllustrationsShareOnePageAndEightOrderedDialogueBeats()
        {
            Assert.That(player.FindProperty("pages").arraySize, Is.EqualTo(1));
            Assert.That(player.FindProperty("panels").arraySize, Is.EqualTo(7));
            var beats = player.FindProperty("beats");
            Assert.That(beats.arraySize, Is.EqualTo(8));
            string[] keys = { "mage", "tree", "invasion", "attack", "destroy", "defend", "war", "continue" };
            for (int i = 0; i < beats.arraySize; i++)
            {
                Assert.That(beats.GetArrayElementAtIndex(i).FindPropertyRelative("localizationKey").stringValue,
                    Is.EqualTo("onboarding.comic." + keys[i]));
            }
            Assert.That(beats.GetArrayElementAtIndex(4).FindPropertyRelative("panel").objectReferenceValue,
                Is.SameAs(beats.GetArrayElementAtIndex(5).FindPropertyRelative("panel").objectReferenceValue));
            Assert.That(beats.GetArrayElementAtIndex(5).FindPropertyRelative("captionIndex").intValue, Is.EqualTo(1));
            var panels = player.FindProperty("panels");
            for (int i = 0; i < panels.arraySize; i++)
            {
                var panel = (Component)panels.GetArrayElementAtIndex(i).objectReferenceValue;
                Image art = panel.transform.Find("Viewport/Artwork").GetComponent<Image>();
                Assert.That(art.sprite, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(art.sprite), Does.EndWith($"intro_{i + 1:00}.png"));
                Assert.That(art.GetComponent<AspectRatioFitter>().aspectMode,
                    Is.EqualTo(AspectRatioFitter.AspectMode.FitInParent));
            }
        }

        [TestCase("ko-KR")]
        [TestCase("en")]
        public void EveryAuthoredCaptionFitsInBothLanguages(string locale)
        {
            string shared = File.ReadAllText("Assets/Localization/Onboarding/Onboarding Shared Data.asset");
            string localized = File.ReadAllText($"Assets/Localization/Onboarding/Onboarding_{locale}.asset");
            var beats = player.FindProperty("beats");
            for (int i = 0; i < beats.arraySize; i++)
            {
                var beat = beats.GetArrayElementAtIndex(i);
                string key = beat.FindPropertyRelative("localizationKey").stringValue;
                Match id = Regex.Match(shared, @"m_Id: (\d+)\s+m_Key: " + Regex.Escape(key) + @"\s");
                Assert.That(id.Success, Is.True, key);
                Match value = Regex.Match(localized, @"m_Id: " + id.Groups[1].Value + @"\s+m_Localized: (""(?:\\.|[^""\\])*"")");
                Assert.That(value.Success, Is.True, key + " / " + locale);
                string text = JsonConvert.DeserializeObject<string>(value.Groups[1].Value);
                var panel = new SerializedObject(beat.FindPropertyRelative("panel").objectReferenceValue);
                var label = (TMP_Text)panel.FindProperty("captionTexts")
                    .GetArrayElementAtIndex(beat.FindPropertyRelative("captionIndex").intValue).objectReferenceValue;
                label.text = text;
                label.maxVisibleCharacters = int.MaxValue;
                label.ForceMeshUpdate(true, true);
                Assert.That(label.isTextOverflowing, Is.False, key + " / " + locale);
            }
        }

        [Test]
        public void LaterPanelsDoNotCoverEarlierDialogueOnTheSamePage()
        {
            var beats = player.FindProperty("beats");
            for (int i = 0; i < beats.arraySize; i++)
            {
                var beat = beats.GetArrayElementAtIndex(i);
                var source = (Component)beat.FindPropertyRelative("panel").objectReferenceValue;
                var sourceData = new SerializedObject(source);
                var caption = (Component)sourceData.FindProperty("captionGroups")
                    .GetArrayElementAtIndex(beat.FindPropertyRelative("captionIndex").intValue).objectReferenceValue;
                for (int j = i + 1; j < beats.arraySize; j++)
                {
                    var later = (Component)beats.GetArrayElementAtIndex(j).FindPropertyRelative("panel").objectReferenceValue;
                    if (later == source || new SerializedObject(later).FindProperty("page").intValue
                        != sourceData.FindProperty("page").intValue) continue;
                    Assert.That(WorldRect((RectTransform)caption.transform).Overlaps(WorldRect((RectTransform)later.transform)),
                        Is.False, source.name + " caption is covered by " + later.name);
                }
            }
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [Test]
        public void TutorialRoutingKeepsExistingBuildIndicesAndComicButtonsCannotSubmitTwice()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.That(scenes.Any(s => s.enabled && s.path == "Assets/Scenes/ComicIntroScene.unity"), Is.True);
            Assert.That(scenes[0].path, Is.EqualTo("Assets/Scenes/LoginScene.unity"));
            Assert.That(scenes.Any(s => s.enabled && s.path == "Assets/Scenes/InteractiveTutorialScene.unity"), Is.True);
            foreach (string name in new[] { "advanceButton", "skipButton" })
            {
                Button button = (Button)player.FindProperty(name).objectReferenceValue;
                Assert.That(button, Is.Not.Null);
                Assert.That(button.navigation.mode, Is.EqualTo(Navigation.Mode.None));
            }
        }
    }
}
