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
            Assert.That(clips.Length, Is.EqualTo(85));
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
        public IEnumerator SupportGaugesAndBeamUseRecordedValues()
        {
            Create();
            Configure("mana_well");
            preview.gameObject.SetActive(true);
            yield return null;
            previewType.GetField("elapsed", Private).SetValue(preview, 2f);
            Advance();
            Assert.That((float)Field("manaRate"), Is.EqualTo(1.8f).Within(0.001f));
            Assert.That(Field("manaGauge"), Is.Not.Null);
            previewType.GetField("elapsed", Private).SetValue(preview, 4f);
            Advance();
            Assert.That((float)Field("manaRate"), Is.EqualTo(1f).Within(0.001f));

            Configure("repair_totem");
            previewType.GetField("elapsed", Private).SetValue(preview, 4.2f);
            Advance();
            var visuals = (IDictionary)Field("visuals");
            Assert.That(visuals.Contains(2), Is.True, "Protected building survives");
            Assert.That(visuals.Contains(3), Is.False, "Control building expires");
            object ally = visuals[2];
            Assert.That(ally.GetType().GetField("lifetimeGauge").GetValue(ally), Is.Not.Null);
            Assert.That((float)ally.GetType().GetField("hp").GetValue(ally), Is.EqualTo(1000f));

            Configure("spirit_bomb");
            previewType.GetField("elapsed", Private).SetValue(preview, 1.5f);
            Advance();
            bool sawBeam = false;
            foreach (object transient in (IEnumerable)Field("transients"))
            {
                Type type = transient.GetType();
                if (!(bool)type.GetField("segment").GetValue(transient)) continue;
                sawBeam = true;
                Assert.That((float)type.GetField("width").GetValue(transient), Is.GreaterThan(0f));
                var renderer = (SpriteRenderer)type.GetField("renderer").GetValue(transient);
                Assert.That(renderer.transform.localScale.x * renderer.sprite.bounds.size.x, Is.GreaterThan(3f));
            }
            Assert.That(sawBeam, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CaptureComplexPreviewViewports()
        {
            Create();
            string directory = System.IO.Path.Combine(Application.dataPath, "../Temp/PreviewCaptures");
            System.IO.Directory.CreateDirectory(directory);
            foreach (string magic in new[] { "sea_serpent", "evil_ent", "spirit_bomb", "cloud_dragon" })
            {
                Configure(magic);
                preview.gameObject.SetActive(true);
                yield return null;
                var rect = preview.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                Array clips = (Array)Field("clips");
                JObject recording = null;
                foreach (object clip in clips)
                    if ((string)clip.GetType().GetField("magicId").GetValue(clip) == magic)
                        recording = JObject.Parse(((TextAsset)clip.GetType().GetField("recordingAsset").GetValue(clip)).text);
                JArray scenarios = (JArray)recording["scenarios"];
                string wanted = magic == "evil_ent" ? "pull" : magic == "sea_serpent" ? "multiple" : magic == "spirit_bomb" ? "channel" : "air";
                int index = 0;
                for (int i = 0; i < scenarios.Count; i++) if ((string)scenarios[i]["id"] == wanted) index = i;
                float moment = 2f;
                string projectile = magic == "evil_ent" ? "EvilEntGrabArm" : magic == "sea_serpent" ? "SeaSerpentHydroPump" : magic == "spirit_bomb" ? "SpiritBombBeam" : "WaterShot";
                bool found = false;
                foreach (JToken frame in scenarios[index]["frames"])
                {
                    foreach (JToken item in frame["objects"]["projectile"])
                        if ((string)item["type"] == projectile) { moment = (float)frame["time"] + (float)item["duration"] * 0.4f; found = true; break; }
                    if (found) break;
                }
                Assert.That(found, Is.True, magic);
                foreach (bool book in new[] { true, false })
                {
                    int width = book ? 540 : 400, height = book ? 420 : 220;
                    rect.sizeDelta = new Vector2(width, height);
                    previewType.GetMethod("SetViewZoom").Invoke(preview, new object[] { book ? 1.35f : 1f });
                    previewType.GetField("scenarioIndex", Private).SetValue(preview, index);
                    Invoke("ResetPlayback");
                    previewType.GetField("elapsed", Private).SetValue(preview, moment);
                    Advance();
                    if (magic == "sea_serpent")
                    {
                        bool visibleBeam = false;
                        foreach (object transient in (IEnumerable)Field("transients"))
                        {
                            Type type = transient.GetType();
                            if (!(bool)type.GetField("segment").GetValue(transient)) continue;
                            var renderer = (SpriteRenderer)type.GetField("renderer").GetValue(transient);
                            Assert.That(renderer.transform.localScale.x * renderer.sprite.bounds.size.x, Is.GreaterThan(2f), "Flattened position endpoints must form a visible beam");
                            visibleBeam = true;
                        }
                        Assert.That(visibleBeam, Is.True);
                    }
                    Invoke("LateUpdate");
                    var viewport = new RenderTexture(width, height, 0);
                    RenderTexture previous = RenderTexture.active;
                    Graphics.Blit(preview.GetComponent<RawImage>().texture, viewport);
                    RenderTexture.active = viewport;
                    var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
                    pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    pixels.Apply();
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, magic + (book ? "-book.png" : "-hover.png")), pixels.EncodeToPNG());
                    RenderTexture.active = previous;
                    viewport.Release();
                    UnityEngine.Object.Destroy(viewport);
                    UnityEngine.Object.Destroy(pixels);
                }
            }
            LogAssert.NoUnexpectedReceived();
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
