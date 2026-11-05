using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class ScrollUiLayoutTests
    {
        private const string ScrollGuid = "1aa08ab6e0800fa44ae55d278d1423e3";
        private const string BarGuid = "2a4db7a114972834c8e4117be1d82ba3";
        private const string MaskGuid = "31a19414c41e5ae4aae2af33fee712f6";
        private const string ImageGuid = "fe87c0e1cc204ed48ad3b37840f39efc";
        private const string ChipGuid = "008da381db86437b85037fe7cf340d8a";

        private static string Field(string document, string field)
        {
            return Regex.Match(document, @"(?m)^  " + field + @": ([^\r\n]+)").Groups[1].Value;
        }

        private static string Reference(string document, string field)
        {
            return Regex.Match(Field(document, field), @"fileID: (-?\d+)").Groups[1].Value;
        }

        private static float X(string vector) => float.Parse(
            Regex.Match(vector, @"x: (-?[\d.]+)").Groups[1].Value, CultureInfo.InvariantCulture);

        private static string Component(Dictionary<string, string> documents, string gameObject, string guid)
        {
            return documents.Values.SingleOrDefault(d => Reference(d, "m_GameObject") == gameObject && d.Contains(guid));
        }

        [Test]
        public void ProfileHistoryReservesSpaceAroundFirstAndLastRowOutlines()
        {
            string text = File.ReadAllText("Assets/Scenes/ProfileScene.unity");
            string layout = Regex.Match(text, @"(?ms)^--- !u!114 &543593548(?=\s|$).*?(?=^---|\z)").Value;
            foreach (string side in new[] { "Left", "Right", "Top", "Bottom" })
            {
                int padding = int.Parse(Regex.Match(layout, @"m_" + side + @": (\d+)").Groups[1].Value);
                Assert.That(padding, Is.GreaterThanOrEqualTo(4), side);
            }
            Assert.That(Field(layout, "m_ChildControlWidth"), Is.EqualTo("1"));
        }

        [Test]
        public void AuthoredScrollListsKeepRectangularClipsAndReadableThumbs()
        {
            int checkedBars = 0;
            foreach (string path in new[] { "Assets/Scenes", "Assets/Prefabs", "Assets/Resources" }
                .SelectMany(p => Directory.EnumerateFiles(p, "*", SearchOption.AllDirectories))
                .Where(p => p.EndsWith(".unity") || p.EndsWith(".prefab")))
            {
                string text = File.ReadAllText(path);
                if (!text.Contains(ScrollGuid)) continue;
                var matches = Regex.Matches(text, @"(?ms)^--- !u!\d+ &(-?\d+)(?=\s|$)[^\r\n]*\r?\n.*?(?=^---|\z)")
                    .Cast<Match>().ToArray();
                Assert.That(matches.Select(m => m.Groups[1].Value).Distinct().Count(), Is.EqualTo(matches.Length), path);
                var documents = matches.ToDictionary(m => m.Groups[1].Value, m => m.Value);
                foreach (Match reference in Regex.Matches(text, @"\{fileID: (-?\d+)\}"))
                {
                    string id = reference.Groups[1].Value;
                    Assert.That(id == "0" || documents.ContainsKey(id), Is.True, path + " missing " + id);
                }
                foreach (string scroll in documents.Values.Where(d => d.Contains(ScrollGuid)))
                {
                    string viewport = documents[Reference(scroll, "m_Viewport")];
                    string viewportObject = Reference(viewport, "m_GameObject");
                    if (Component(documents, viewportObject, MaskGuid) != null)
                    {
                        string image = Component(documents, viewportObject, ImageGuid);
                        Assert.That(Field(image, "m_Sprite"), Is.EqualTo("{fileID: 0}"), path);
                    }
                    if (Reference(scroll, "m_VerticalScrollbar") == "0") continue;
                    Assert.That(Field(scroll, "m_VerticalScrollbarVisibility"), Is.Not.EqualTo("0"), path);
                    Assert.That(float.Parse(Field(scroll, "m_VerticalScrollbarSpacing"), CultureInfo.InvariantCulture), Is.GreaterThanOrEqualTo(0), path);
                }
                foreach (string bar in documents.Values.Where(d => d.Contains(BarGuid)))
                {
                    string barObject = Reference(bar, "m_GameObject");
                    string rect = documents.Values.Single(d => d.StartsWith("--- !u!224 ") && Reference(d, "m_GameObject") == barObject);
                    float width = X(Field(rect, "m_SizeDelta"));
                    string handle = documents[Reference(bar, "m_HandleRect")];
                    foreach (string image in new[] { Component(documents, barObject, ImageGuid), Component(documents, Reference(handle, "m_GameObject"), ImageGuid) })
                    {
                        Assert.That(Field(image, "m_Sprite"), Does.Contain(ChipGuid), path);
                        float ppu = float.Parse(Field(image, "m_PixelsPerUnitMultiplier"), CultureInfo.InvariantCulture);
                        Assert.That(84 / ppu, Is.EqualTo(width).Within(0.01f), path + " compressed sliced caps");
                    }
                    checkedBars++;
                }
            }
            Assert.That(checkedBars, Is.GreaterThanOrEqualTo(34));
        }

        [TestCase("Assets/Prefabs/UI/Lobby/PracticeDifficultyPanel.prefab")]
        [TestCase("Assets/Prefabs/UI/CustomBattleDeckDropdown.prefab")]
        public void SmallThumbRemainsInsideTrackAtBothEnds(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var source = prefab.GetComponentInChildren<Scrollbar>(true);
            var instance = Object.Instantiate(source.gameObject);
            try
            {
                instance.SetActive(true);
                var track = (RectTransform)instance.transform;
                track.anchorMin = track.anchorMax = Vector2.one * 0.5f;
                track.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 240);
                var bar = instance.GetComponent<Scrollbar>();
                bar.size = 0.001f;
                foreach (float value in new[] { 0f, 1f })
                {
                    bar.value = value;
                    var corners = new Vector3[4];
                    bar.handleRect.GetWorldCorners(corners);
                    foreach (Vector3 corner in corners)
                    {
                        Vector3 point = track.InverseTransformPoint(corner);
                        Assert.That(point.y, Is.InRange(track.rect.yMin - 0.01f, track.rect.yMax + 0.01f), path);
                    }
                    Assert.That(bar.handleRect.rect.height, Is.GreaterThanOrEqualTo(track.rect.width * 3), path);
                }
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}
