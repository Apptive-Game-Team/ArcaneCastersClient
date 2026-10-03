#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace WordOnline.Tests
{
    // Assembly-CSharp cannot be referenced by an asmdef. Reflection deliberately tests
    // the scene component through its public contract and its bundled prefab.
    public class MagicPreviewTests
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private GameObject root;
        private Component preview;
        private Type previewType;

        private void Create()
        {
            var canvas = new GameObject("PreviewTestCanvas", typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root = canvas;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/MagicPreview.prefab");
            var instance = UnityEngine.Object.Instantiate(prefab, canvas.transform);
            previewType = Type.GetType("Global.MagicPreview, Assembly-CSharp", true);
            preview = instance.GetComponent(previewType);
        }

        private object Field(string name) => previewType.GetField(name, Private).GetValue(preview);
        private object Property(string name) => previewType.GetProperty(name).GetValue(preview);
        private void Invoke(string name) => previewType.GetMethod(name, Private).Invoke(preview, null);
        private void Advance() => previewType.GetMethod("AdvancePlayback", Private).Invoke(preview, new object[] { 0f });
        private object Data(string name)
        {
            Type type = Type.GetType("Data.Magic.CombinedMagicData, Assembly-CSharp", true);
            object magic = Activator.CreateInstance(type);
            type.GetField("serverName").SetValue(magic, name);
            return magic;
        }
        private bool Configure(string name) => (bool)previewType.GetMethod("Configure").Invoke(preview, new[] { Data(name) });

        [UnityTest]
        public IEnumerator AllRepresentativesHaveCompleteVisualMappingsAndCycle()
        {
            Create();
            Array clips = (Array)Field("clips");
            Assert.That(clips.Length, Is.EqualTo(9));
            foreach (object clip in clips)
            {
                Type type = clip.GetType();
                string magic = (string)type.GetField("magicId").GetValue(clip);
                TextAsset asset = (TextAsset)type.GetField("recordingAsset").GetValue(clip);
                JObject recording = JObject.Parse(asset.text);
                Assert.That(Configure(magic), Is.True);
                preview.gameObject.SetActive(true);
                yield return null;
                object stage = Field("stage"), texture = Field("texture");
                JArray scenarios = (JArray)recording["scenarios"];
                foreach (JObject scenario in scenarios)
                {
                    Assert.That(Property("CurrentScenarioId"), Is.EqualTo((string)scenario["id"]));
                    foreach (JToken frame in scenario["frames"])
                    {
                        foreach (JToken item in frame["objects"]["create"])
                            Assert.That(previewType.GetMethod("FindVisual", Private).Invoke(preview, new object[] { (string)item["type"] }), Is.Not.Null, magic + " object " + item["type"]);
                        foreach (JToken item in frame["objects"]["projectile"])
                            AssertStyle("projectileVisuals", (string)item["type"], magic);
                        foreach (JToken item in frame["objects"]["update"])
                            foreach (JToken effect in item["effects"])
                                if ((string)effect != "None") AssertStyle("effectVisuals", (string)effect, magic);
                    }
                    // Consume every frame; then advance exactly once and preserve stage resources.
                    previewType.GetField("elapsed", Private).SetValue(preview, (float)scenario["duration"] - 0.06f);
                    Advance();
                    Invoke("LateUpdate");
                    previewType.GetField("elapsed", Private).SetValue(preview, (float)scenario["duration"]);
                    Advance();
                    Assert.That(Field("stage"), Is.SameAs(stage));
                    Assert.That(Field("texture"), Is.SameAs(texture));
                }
                Assert.That(Property("CurrentScenarioId"), Is.EqualTo((string)scenarios[0]["id"]));
                Assert.That(Configure(magic), Is.True); // Reconfigure an active preview.
                Assert.That(Property("CurrentScenarioId"), Is.EqualTo((string)scenarios[0]["id"]));
                preview.gameObject.SetActive(false);
                Assert.That(Field("stage"), Is.Null);
                Assert.That(preview.GetComponent<RawImage>().texture, Is.Null);
                yield return null;
            }
            Assert.That(Configure("unregistered_spell"), Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        private void AssertStyle(string field, string name, string magic)
        {
            var method = previewType.GetMethod("FindStyle", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method.Invoke(null, new[] { Field(field), name }), Is.Not.Null, magic + " " + field + " " + name);
        }

        [UnityTest]
        public IEnumerator LegacyRecordingWorksAndMalformedRecordingHidesPreview()
        {
            Create();
            object clip = ((Array)Field("clips")).GetValue(0);
            FieldInfo assetField = clip.GetType().GetField("recordingAsset");
            var legacy = new TextAsset("{\"version\":1,\"magic\":\"fire_shot\",\"frameDuration\":0.05,\"duration\":1,\"frames\":[{\"time\":0,\"objects\":{\"create\":[],\"update\":[]}}]}");
            assetField.SetValue(clip, legacy);
            Assert.That(Configure("fire_shot"), Is.True);
            preview.gameObject.SetActive(true);
            yield return null;
            Assert.That(Property("ScenarioCount"), Is.EqualTo(1));
            Assert.That(Property("CurrentScenarioId"), Is.EqualTo("default"));
            preview.gameObject.SetActive(false);
            var invalid = new TextAsset("{\"version\":2,\"magic\":\"fire_shot\",\"frameDuration\":0.05,\"scenarios\":[]}");
            assetField.SetValue(clip, invalid);
            LogAssert.Expect(LogType.Warning, new Regex("Magic preview unavailable"));
            Configure("fire_shot");
            preview.gameObject.SetActive(true);
            yield return null;
            Assert.That(preview.gameObject.activeSelf, Is.False);
            Assert.That(Field("stage"), Is.Null);
            UnityEngine.Object.Destroy(legacy);
            UnityEngine.Object.Destroy(invalid);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (root != null) UnityEngine.Object.Destroy(root);
            yield return null;
        }
    }
}
#endif
