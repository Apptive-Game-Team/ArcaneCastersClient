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
        public IEnumerator BookZoomEnlargesVisualsWithoutChangingViewport()
        {
            Create();
            Configure("earth_call");
            preview.gameObject.SetActive(true);
            yield return null;
            Invoke("LateUpdate");
            var camera = (Camera)Field("previewCamera");
            float originalSize = camera.orthographicSize;
            Rect originalRect = preview.GetComponent<RectTransform>().rect;
            previewType.GetMethod("SetViewZoom").Invoke(preview, new object[] { 1.35f });
            Invoke("LateUpdate");
            Assert.That(camera.orthographicSize, Is.EqualTo(originalSize / 1.35f).Within(0.001f));
            Assert.That(preview.GetComponent<RectTransform>().rect, Is.EqualTo(originalRect));
            Assert.That(((Component)Field("scenarioCaption")).gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator EnemyTargetsUseDistinctGroundAndAirSprites()
        {
            Create();
            Configure("towerback");
            preview.gameObject.SetActive(true);
            yield return null;
            var visuals = (IDictionary)Field("visuals");
            object target = visuals[2];
            var renderer = (SpriteRenderer)target.GetType().GetField("renderer").GetValue(target);
            Assert.That(renderer.sprite, Is.EqualTo(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Game/sprites/MiniRockSwarm.png")));
            previewType.GetField("scenarioIndex", Private).SetValue(preview, 1);
            Invoke("ResetPlayback");
            target = ((IDictionary)Field("visuals"))[2];
            renderer = (SpriteRenderer)target.GetType().GetField("renderer").GetValue(target);
            Assert.That(renderer.sprite, Is.EqualTo(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Game/sprites/ThunderBirdSwarm.png")));
        }

        [UnityTest]
        public IEnumerator FrequentDamageTicksFlashThenReturnToWhite()
        {
            Create();
            object clip = ((Array)Field("clips")).GetValue(0);
            var recording = new TextAsset("{\"version\":1,\"magic\":\"fire_shot\",\"frameDuration\":0.05,\"duration\":1,\"frames\":[{\"time\":0,\"objects\":{\"create\":[{\"id\":2,\"type\":\"ElectricSlime\",\"master\":\"RightPlayer\",\"position\":{\"x\":6,\"y\":0,\"z\":5}}],\"update\":[{\"id\":2,\"gauges\":[{\"category\":\"HP\",\"value\":100,\"maxValue\":100}]}]}},{\"time\":0.1,\"objects\":{\"update\":[{\"id\":2,\"gauges\":[{\"category\":\"HP\",\"value\":90,\"maxValue\":100}]}]}},{\"time\":0.15,\"objects\":{\"update\":[{\"id\":2,\"gauges\":[{\"category\":\"HP\",\"value\":80,\"maxValue\":100}]}]}},{\"time\":0.2,\"objects\":{\"update\":[{\"id\":2,\"gauges\":[{\"category\":\"HP\",\"value\":70,\"maxValue\":100}]}]}}]}");
            clip.GetType().GetField("recordingAsset").SetValue(clip, recording);
            Configure("fire_shot");
            preview.gameObject.SetActive(true);
            yield return null;
            object target = ((IDictionary)Field("visuals"))[2];
            var renderer = (SpriteRenderer)target.GetType().GetField("renderer").GetValue(target);
            foreach (float time in new[] { 0.15f, 0.18f, 0.2f, 0.23f })
            {
                previewType.GetField("elapsed", Private).SetValue(preview, time);
                Advance();
                Assert.That(renderer.color, Is.EqualTo(time == 0.15f || time == 0.2f
                    ? new Color(1f, 0.35f, 0.2f) : Color.white));
            }
            UnityEngine.Object.Destroy(recording);
        }

        [UnityTest]
        public IEnumerator DotFlashesOncePerSecondWithoutSkippingHpUpdates()
        {
            Create();
            foreach (string magic in new[] { "sand_storm", "fire_shot" })
            {
                Configure(magic);
                preview.gameObject.SetActive(true);
                yield return null;
                object target = ((IDictionary)Field("visuals"))[2];
                Type type = target.GetType();
                float previousHit = (float)type.GetField("hitAt").GetValue(target);
                float previousHp = (float)type.GetField("hp").GetValue(target);
                int flashes = 0, groupedDamageTicks = 0;
                bool sawWhiteGap = false;
                for (int i = 1; i < 80; i++)
                {
                    previewType.GetField("elapsed", Private).SetValue(preview, i * 0.05f);
                    Advance();
                    float hit = (float)type.GetField("hitAt").GetValue(target);
                    float hp = (float)type.GetField("hp").GetValue(target);
                    if (hit != previousHit)
                    {
                        // FireShot's initial impact is an ordinary hit before Burn
                        // starts. Only the DOT phase has the one-second cadence.
                        bool isDot = magic == "sand_storm" ||
                            ((IDictionary)type.GetField("effects").GetValue(target)).Contains("Burn");
                        if (isDot) Assert.That(hit - previousHit, Is.GreaterThanOrEqualTo(1f - 0.001f), magic);
                        flashes++;
                    }
                    else if (hp < previousHp) groupedDamageTicks++;
                    var renderer = (SpriteRenderer)type.GetField("renderer").GetValue(target);
                    if (flashes > 0 && renderer.color == Color.white) sawWhiteGap = true;
                    previousHit = hit;
                    previousHp = hp;
                }
                Assert.That(flashes, Is.GreaterThan(2));
                Assert.That(groupedDamageTicks, Is.GreaterThan(0));
                Assert.That(sawWhiteGap, Is.True);
            }
        }

        [UnityTest]
        public IEnumerator AllRepresentativesHaveCompleteVisualMappingsAndCycle()
        {
            Create();
            Array clips = (Array)Field("clips");
            Assert.That(clips.Length, Is.EqualTo(21));
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
