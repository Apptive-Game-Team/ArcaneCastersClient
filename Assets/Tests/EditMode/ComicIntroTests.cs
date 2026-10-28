using System;
using System.IO;
using System.Linq;
using System.Reflection;
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
                    Is.EqualTo(AspectRatioFitter.AspectMode.EnvelopeParent));
                Mask mask = art.transform.parent.GetComponent<Mask>();
                Assert.That(mask, Is.Not.Null);
                Assert.That(mask.showMaskGraphic, Is.False);
                Assert.That(mask.GetComponent<Graphic>().GetType().Name, Is.EqualTo("ComicShapeGraphic"));
                Assert.That(mask.GetComponent<RectMask2D>(), Is.Null);
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
                var caption = (Component)sourceData.FindProperty("captionTexts")
                    .GetArrayElementAtIndex(beat.FindPropertyRelative("captionIndex").intValue).objectReferenceValue;
                for (int j = i + 1; j < beats.arraySize; j++)
                {
                    var later = (Component)beats.GetArrayElementAtIndex(j).FindPropertyRelative("panel").objectReferenceValue;
                    if (later == source || new SerializedObject(later).FindProperty("page").intValue
                        != sourceData.FindProperty("page").intValue) continue;
                    var frame = later.transform.Find("Frame").GetComponent<Graphic>();
                    Assert.That(PolygonsOverlap(WorldCorners((RectTransform)caption.transform), ShapeWorldPoints(frame)),
                        Is.False, source.name + " caption is covered by " + later.name);
                }
            }
        }

        private static Vector2[] WorldCorners(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return corners.Select(p => (Vector2)p).ToArray();
        }

        private static Vector2[] ShapeWorldPoints(Graphic shape)
        {
            var data = new SerializedObject(shape);
            var points = data.FindProperty("vertices");
            float inset = data.FindProperty("inset").floatValue;
            Rect rect = shape.rectTransform.rect;
            Vector2 size = rect.size - Vector2.one * inset * 2;
            Vector2 origin = rect.min + Vector2.one * inset;
            return Enumerable.Range(0, points.arraySize).Select(i => (Vector2)shape.rectTransform.TransformPoint(
                origin + Vector2.Scale(points.GetArrayElementAtIndex(i).vector2Value, size))).ToArray();
        }

        private static bool PolygonsOverlap(Vector2[] a, Vector2[] b)
        {
            foreach (Vector2[] polygon in new[] { a, b })
            {
                for (int i = 0; i < polygon.Length; i++)
                {
                    Vector2 edge = polygon[(i + 1) % polygon.Length] - polygon[i];
                    Vector2 axis = new Vector2(-edge.y, edge.x).normalized;
                    float[] projectedA = a.Select(p => Vector2.Dot(p, axis)).ToArray();
                    float[] projectedB = b.Select(p => Vector2.Dot(p, axis)).ToArray();
                    if (projectedA.Max() <= projectedB.Min() || projectedB.Max() <= projectedA.Min()) return false;
                }
            }
            return true;
        }

        [Test]
        public void AuthoredShapesHaveValidFanMeshesAndDoNotInterceptPointerInput()
        {
            var shapes = root.GetComponentsInChildren<Graphic>(true)
                .Where(g => g.GetType().Name == "ComicShapeGraphic");
            foreach (Graphic shape in shapes)
            {
                Assert.That(shape.raycastTarget, Is.False, shape.name);
                Assert.That(shape.GetComponent<CanvasRenderer>(), Is.Not.Null, shape.name);
                Assert.That(shape.GetComponents<Graphic>().Length, Is.EqualTo(1), shape.name);
                using (var helper = new VertexHelper())
                {
                    var mesh = new Mesh();
                    try
                    {
                        shape.GetType().GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                            null, new[] { typeof(VertexHelper) }, null)
                            .Invoke(shape, new object[] { helper });
                        helper.FillMesh(mesh);
                        Assert.That(mesh.vertexCount, Is.GreaterThan(3), shape.name);
                        Vector3[] vertices = mesh.vertices;
                        foreach (Vector3 vertex in vertices)
                        {
                            Assert.That(float.IsNaN(vertex.x) || float.IsNaN(vertex.y), Is.False);
                            Rect bounds = shape.rectTransform.rect;
                            Assert.That(vertex.x, Is.InRange(bounds.xMin - 0.001f, bounds.xMax + 0.001f), shape.name);
                            Assert.That(vertex.y, Is.InRange(bounds.yMin - 0.001f, bounds.yMax + 0.001f), shape.name);
                        }
                        int[] triangles = mesh.triangles;
                        for (int i = 0; i < triangles.Length; i += 3)
                        {
                            float area = Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]],
                                vertices[triangles[i + 2]] - vertices[triangles[i]]).z;
                            Assert.That(area, Is.GreaterThan(0.001f), shape.name + " fan folds over itself");
                        }
                    }
                    finally { UnityEngine.Object.DestroyImmediate(mesh); }
                }
            }
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
