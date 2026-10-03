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
    // The test asmdef cannot reference Assembly-CSharp.
    public class MagicPreviewTests
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private GameObject root;
        private Component preview;
        private Type previewType;
        private static Type Runtime(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(name);
                if (type != null) return type;
            }
            throw new TypeLoadException(name);
        }
        private void Create()
        {
            root = new GameObject("PreviewTestCanvas", typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/MagicPreview.prefab");
            var instance = UnityEngine.Object.Instantiate(prefab, root.transform);
            previewType = Runtime("Global.MagicPreview");
            preview = instance.GetComponent(previewType);
        }
        private object Field(string name) => previewType.GetField(name, Private).GetValue(preview);
        private object Property(string name) => previewType.GetProperty(name).GetValue(preview);
        private void Invoke(string name) => previewType.GetMethod(name, Private).Invoke(preview, null);
        private bool Configure(string name)
        {
            var type = Runtime("Data.Magic.CombinedMagicData");
            object magic = Activator.CreateInstance(type);
            type.GetField("serverName").SetValue(magic, name);
            bool supported = (bool)previewType.GetMethod("Configure").Invoke(preview, new[] { magic });
            preview.gameObject.SetActive(true);
            return supported;
        }
        private Component Target(int id)
        {
            object world = Field("world");
            return (Component)world.GetType().GetMethod("FindById").Invoke(world, new object[] { id });
        }
        private JObject Recording(string magic)
        {
            foreach (object clip in (Array)Field("clips"))
                if ((string)clip.GetType().GetField("magicId").GetValue(clip) == magic)
                    return JObject.Parse(((TextAsset)clip.GetType().GetField("recordingAsset").GetValue(clip)).text);
            throw new Exception(magic);
        }
        private void Scenario(int index)
        {
            previewType.GetField("scenarioIndex", Private).SetValue(preview, index);
            Invoke("ResetPlayback");
        }
        private Component CreateWorld()
        {
            var fixture = new GameObject("IsolatedPresentationFixture");
            fixture.transform.SetParent(root.transform);
            fixture.transform.position = new Vector3(10000, 10000, 10000);
            var world = fixture.AddComponent(Runtime("GameScene.Object.PresentationWorld"));
            var cameraObject = new GameObject("FixtureCamera");
            cameraObject.transform.SetParent(fixture.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.transform.localPosition = new Vector3(6, 12, -6);
            camera.transform.localRotation = Quaternion.Euler(45, 0, 0);
            world.GetType().GetProperty("Camera").SetValue(world, camera);
            return world;
        }
        private IEnumerator PlayUntil(float moment)
        {
            float deadline = Time.realtimeSinceStartup + moment + 15f;
            while (preview.gameObject.activeSelf && (float)Field("elapsed") < moment && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(preview.gameObject.activeSelf, Is.True);
            Assert.That((float)Field("elapsed"), Is.GreaterThanOrEqualTo(moment), "Native animations must advance with Unity time.");
        }

        [UnityTest]
        public IEnumerator BookZoomUsesNativePerspectiveWithoutChangingViewport()
        {
            Create();
            Assert.That(Configure("earth_call"), Is.True);
            yield return null;
            Invoke("LateUpdate");
            var camera = (Camera)Field("previewCamera");
            Assert.That(camera.orthographic, Is.False);
            float original = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad / 2f);
            Rect rect = preview.GetComponent<RectTransform>().rect;
            previewType.GetMethod("SetViewZoom").Invoke(preview, new object[] { 1.35f });
            Invoke("LateUpdate");
            Assert.That(Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad / 2f), Is.EqualTo(original / 1.35f).Within(0.001f));
            Assert.That(preview.GetComponent<RectTransform>().rect, Is.EqualTo(rect));
            Assert.That(preview.GetComponent<RawImage>().uvRect, Is.EqualTo(new Rect(0, 0, 1, 1)));
            Assert.That(((Component)Field("scenarioCaption")).gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator OnlyExplicitFixturesBecomeGroundAndAirEnemies()
        {
            Create();
            Configure("towerback");
            yield return null;
            Assert.That(Target(2).name, Does.StartWith("MiniRock"));
            Scenario(1);
            yield return null;
            Assert.That(Target(2).name, Does.StartWith("ThunderBird"));
            Configure("thunder_spirit");
            var scenarios = (JArray)Recording("thunder_spirit")["scenarios"];
            int index = 0;
            for (int i = 0; i < scenarios.Count; i++)
                if ((string)scenarios[i]["id"] == "death_energy") index = i;
            Scenario(index);
            yield return null;
            bool absorber = false;
            foreach (JToken created in scenarios[index]["frames"][0]["objects"]["create"])
                if ((string)created["type"] == "ElectricSlime" && !((JArray)scenarios[index]["fixtureTargetIds"]).Contains(created["id"]))
                {
                    Assert.That(Target((int)created["id"]).name, Does.StartWith("ElectricSlime"));
                    absorber = true;
                }
            Assert.That(absorber, Is.True, "The actual absorption mechanic must retain its real prefab.");
        }

        [UnityTest]
        public IEnumerator AllRecordingsUseRuntimePrefabsAndReuseStageOnScenarioReset()
        {
            Create();
            Assert.That(((Array)Field("clips")).Length, Is.EqualTo(85));
            int count = 0;
            foreach (object clip in (Array)Field("clips"))
            {
                string magic = (string)clip.GetType().GetField("magicId").GetValue(clip);
                Assert.That(Configure(magic), Is.True);
                Assert.That(Field("world"), Is.Not.Null, magic);
                object stage = Field("stage"), texture = Field("texture");
                var scenarios = (JArray)Recording(magic)["scenarios"];
                for (int i = 0; i < scenarios.Count; i++)
                {
                    Scenario(i);
                    Assert.That(Property("CurrentScenarioId"), Is.EqualTo((string)scenarios[i]["id"]));
                    Assert.That(Field("stage"), Is.SameAs(stage));
                    Assert.That(Field("texture"), Is.SameAs(texture));
                    yield return null; // Let prefab Awake/Start and deferred cleanup run.
                    Assert.That(Field("world"), Is.Not.Null, magic);
                    count++;
                }
                preview.gameObject.SetActive(false);
                Assert.That(Field("stage"), Is.Null);
                Assert.That(preview.GetComponent<RawImage>().texture, Is.Null);
                yield return null;
            }
            Assert.That(count, Is.EqualTo(164));
        }

        [UnityTest]
        public IEnumerator NativeProjectilesAnimateAndCaptureBothViewports()
        {
            Create();
            string directory = System.IO.Path.Combine(Application.dataPath, "../Temp/SharedPreviewCaptures");
            System.IO.Directory.CreateDirectory(directory);
            foreach (string magic in new[] { "sea_serpent", "evil_ent", "spirit_bomb", "cloud_dragon" })
            {
                Configure(magic);
                var scenarios = (JArray)Recording(magic)["scenarios"];
                string wanted = magic == "evil_ent" ? "pull" : magic == "sea_serpent" ? "multiple" : magic == "spirit_bomb" ? "channel" : "air";
                string projectile = magic == "evil_ent" ? "EvilEntGrabArm" : magic == "sea_serpent" ? "SeaSerpentHydroPump" : magic == "spirit_bomb" ? "SpiritBombBeam" : "WaterShot";
                int index = 0;
                for (int i = 0; i < scenarios.Count; i++) if ((string)scenarios[i]["id"] == wanted) index = i;
                float moment = -1;
                foreach (JToken frame in scenarios[index]["frames"])
                {
                    foreach (JToken item in frame["objects"]["projectile"])
                        if ((string)item["type"] == projectile) { moment = (float)frame["time"] + (float)item["duration"] * 0.4f; break; }
                    if (moment >= 0) break;
                }
                Assert.That(moment, Is.GreaterThanOrEqualTo(0), magic);
                Scenario(index);
                yield return PlayUntil(moment);
                var stage = (GameObject)Field("stage");
                string component = magic == "sea_serpent" ? "BeamProjectile" : magic == "evil_ent" ? "StretchProjectile" : magic == "spirit_bomb" ? "SpiritBombBeamProjectile" : "DefaultProjectile";
                Assert.That(stage.GetComponentsInChildren(Runtime("GameScene.Object.Projectile." + component)).Length, Is.GreaterThan(0), magic);
                foreach (bool book in new[] { true, false })
                {
                    int width = book ? 540 : 400, height = book ? 420 : 220;
                    var rect = preview.GetComponent<RectTransform>();
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(width, height);
                    previewType.GetMethod("SetViewZoom").Invoke(preview, new object[] { book ? 1.35f : 1f });
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
        }

        [UnityTest]
        public IEnumerator DotFlashesAtOneSecondCadenceAndKeepsEveryHpUpdate()
        {
            Create();
            Configure("towerback");
            yield return null;
            // Stop only this DTO source; native ServedObject/LateUpdate remain active.
            ((Behaviour)preview).enabled = false;
            // OnDisable releases the world, so create a dedicated isolated runtime fixture.
            var fixture = new GameObject("DotFixture");
            fixture.transform.SetParent(root.transform);
            var world = fixture.AddComponent(Runtime("GameScene.Object.PresentationWorld"));
            object dto = Activator.CreateInstance(Runtime("GameScene.Dto.CreatedObjectDto"));
            dto.GetType().GetField("id").SetValue(dto, 42);
            dto.GetType().GetField("type").SetValue(dto, "MiniRock");
            dto.GetType().GetField("master").SetValue(dto, "RightPlayer");
            var served = (Component)Runtime("GameScene.Object.ObjectSpawner").GetMethod("SpawnShared").Invoke(null, new object[] { dto, world, false });
            var updatedType = Runtime("GameScene.Dto.UpdatedObjectDto");
            var codec = Runtime("Global.Serialization.JsonCodec").GetMethod("Deserialize");
            var update = served.GetType().GetMethod("UpdateObject");
            float previous = float.NegativeInfinity;
            int flashes = 0;
            bool whiteGap = false;
            for (int i = 0; i < 45; i++)
            {
                object frame = codec.MakeGenericMethod(updatedType).Invoke(null, new object[] {
                    "{\"id\":42,\"effects\":[\"Burn\"],\"gauges\":[{\"category\":\"HP\",\"value\":" + (100 - i) + ",\"maxValue\":100}]}"
                });
                update.Invoke(served, new[] { frame });
                float time = (float)served.GetType().GetField("lastDamageFlash", Private).GetValue(served);
                if (time != previous && !float.IsNegativeInfinity(time))
                {
                    if (!float.IsNegativeInfinity(previous)) Assert.That(time - previous, Is.GreaterThanOrEqualTo(0.999f));
                    previous = time;
                    flashes++;
                }
                var sprite = (SpriteRenderer)served.GetType().GetField("_spriteRenderer", Private).GetValue(served);
                if (flashes > 0 && sprite.color == Color.white) whiteGap = true;
                yield return new WaitForSeconds(0.05f);
            }
            Assert.That(flashes, Is.GreaterThanOrEqualTo(2));
            Assert.That(whiteGap, Is.True);
            var gauges = (IList)served.GetType().GetField("gauges").GetValue(served);
            Assert.That((float)gauges[0].GetType().GetField("value").GetValue(gauges[0]), Is.EqualTo(56f));
        }

        [UnityTest]
        public IEnumerator ConcurrentWorldsDoNotReplaceMainCameraOrShareIds()
        {
            Create();
            Camera main = Camera.main;
            Configure("fire_shot");
            yield return null;
            var first = (GameObject)Field("stage");
            var firstTarget = Target(2);
            var clone = UnityEngine.Object.Instantiate(preview.gameObject, root.transform);
            clone.SetActive(false);
            var second = clone.GetComponent(previewType);
            var type = Runtime("Data.Magic.CombinedMagicData");
            object magic = Activator.CreateInstance(type);
            type.GetField("serverName").SetValue(magic, "fire_shot");
            previewType.GetMethod("Configure").Invoke(second, new[] { magic });
            clone.SetActive(true);
            yield return null;
            var secondWorld = previewType.GetField("world", Private).GetValue(second);
            var secondTarget = (Component)secondWorld.GetType().GetMethod("FindById").Invoke(secondWorld, new object[] { 2 });
            Assert.That(secondTarget, Is.Not.SameAs(firstTarget));
            Assert.That(Vector3.Distance(secondTarget.transform.position, firstTarget.transform.position), Is.GreaterThan(50));
            Assert.That(Camera.main, Is.SameAs(main));
            clone.SetActive(false);
            yield return null;
            Assert.That(first, Is.Not.Null);
            Assert.That(Target(2), Is.SameAs(firstTarget));
            foreach (var collider in first.GetComponentsInChildren<Collider>()) Assert.That(collider.enabled, Is.False);
            foreach (var audio in first.GetComponentsInChildren<AudioSource>()) Assert.That(audio.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator EveryRecordedObjectRunsNativeInitializationWithoutMatchContext()
        {
            Create();
            var world = CreateWorld();
            var clear = world.GetType().GetMethod("Clear");
            var parameters = world.GetType().GetMethod("SetParameters");
            var codec = Runtime("Global.Serialization.JsonCodec").GetMethod("Deserialize");
            var createdType = Runtime("GameScene.Dto.CreatedObjectDto");
            var spawn = Runtime("GameScene.Object.ObjectSpawner").GetMethod("SpawnShared");
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (object clip in (Array)Field("clips"))
            {
                var asset = (TextAsset)clip.GetType().GetField("recordingAsset").GetValue(clip);
                foreach (JToken scenario in JObject.Parse(asset.text)["scenarios"])
                {
                    var values = scenario["parameters"].ToObject<System.Collections.Generic.Dictionary<string, float>>();
                    foreach (JToken frame in scenario["frames"])
                        foreach (JToken item in frame["objects"]["create"])
                        {
                            if (!seen.Add((string)item["type"])) continue;
                            clear.Invoke(world, null);
                            parameters.Invoke(world, new object[] { values });
                            object dto = codec.MakeGenericMethod(createdType).Invoke(null, new object[] { item.ToString() });
                            var obj = (Component)spawn.Invoke(null, new object[] { dto, world, true });
                            Assert.That(obj, Is.Not.Null, (string)item["type"]);
                            yield return null;
                            yield return null;
                            foreach (var source in obj.GetComponentsInChildren<AudioSource>(true))
                                Assert.That(source.enabled, Is.False, (string)item["type"]);
                        }
                }
            }
            Assert.That(seen.Count, Is.GreaterThan(85));
        }

        [UnityTest]
        public IEnumerator SameDtoUsesLiveRegistryAndIsolatedRegistryWithEquivalentState()
        {
            Create();
            var world = CreateWorld();
            var containerType = Runtime("GameScene.Object.ObjectContainer");
            var instanceProperty = containerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            object previous = instanceProperty.GetValue(null);
            var liveRoot = new GameObject("LivePresentationTestContainer");
            liveRoot.transform.SetParent(root.transform);
            var container = liveRoot.AddComponent(containerType);
            try
            {
                var codec = Runtime("Global.Serialization.JsonCodec").GetMethod("Deserialize");
                object info = codec.MakeGenericMethod(Runtime("GameScene.Dto.ObjectsInfo")).Invoke(null, new object[] {
                    "{\"create\":[{\"id\":987645,\"type\":\"MiniRock\",\"master\":\"RightPlayer\",\"position\":{\"x\":3,\"y\":0,\"z\":5}}],\"update\":[{\"id\":987645,\"position\":{\"x\":4,\"y\":0,\"z\":5},\"effects\":[],\"gauges\":[{\"category\":\"HP\",\"value\":75,\"maxValue\":100}]}]}"
                });
                var apply = Runtime("GameScene.Object.PresentationFramePlayer").GetMethod("Apply");
                apply.Invoke(null, new object[] { info, null, null });
                apply.Invoke(null, new object[] { info, null, world });
                var live = (Component)containerType.GetMethod("FindById").Invoke(container, new object[] { 987645 });
                var isolated = (Component)world.GetType().GetMethod("FindById").Invoke(world, new object[] { 987645 });
                yield return new WaitForSeconds(0.1f);
                Assert.That(instanceProperty.GetValue(null), Is.SameAs(container));
                Assert.That(live.name, Is.EqualTo(isolated.name));
                Vector3 origin = world.transform.position;
                Assert.That(Vector3.Distance(live.transform.position, isolated.transform.position - origin), Is.LessThan(0.01f));
                var liveGauges = (IList)live.GetType().GetField("gauges").GetValue(live);
                var previewGauges = (IList)isolated.GetType().GetField("gauges").GetValue(isolated);
                Assert.That(liveGauges.Count, Is.EqualTo(previewGauges.Count));
                Assert.That((float)previewGauges[0].GetType().GetField("value").GetValue(previewGauges[0]), Is.EqualTo(75f));
                var events = Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(Runtime("GameScene.Dto.Event.GameEvent")));
                object hit = Activator.CreateInstance(Runtime("GameScene.Dto.Event.HitEvent"));
                hit.GetType().GetField("actorId").SetValue(hit, 987645);
                hit.GetType().GetField("targetId").SetValue(hit, 987645);
                ((IList)events).Add(hit);
                apply.Invoke(null, new object[] { info.GetType().GetConstructor(Type.EmptyTypes).Invoke(null), events, world });
                bool hitEffect = false;
                foreach (Transform child in world.GetComponentsInChildren<Transform>())
                    if (child.name.StartsWith("HitEffect")) hitEffect = true;
                Assert.That(hitEffect, Is.True, "Actual shared hit dispatch owns the runtime hit effect.");
                world.GetType().GetMethod("Clear").Invoke(world, null);
                yield return null;
                Assert.That(containerType.GetMethod("FindById").Invoke(container, new object[] { 987645 }), Is.SameAs(live));
            }
            finally
            {
                containerType.GetMethod("Clear").Invoke(container, null);
                instanceProperty.DeclaringType.GetProperty("Instance").GetSetMethod(true).Invoke(null, new[] { previous });
            }
        }

        [UnityTest]
        public IEnumerator ResetAndDisableRemoveOwnedNativeTransients()
        {
            Create();
            Configure("spirit_bomb");
            yield return PlayUntil(1.5f);
            var world = (Component)Field("world");
            Component victim = Target(2);
            var codec = Runtime("Global.Serialization.JsonCodec").GetMethod("Deserialize");
            object destroy = codec.MakeGenericMethod(Runtime("GameScene.Dto.UpdatedObjectDto")).Invoke(null, new object[] {
                "{\"id\":2,\"status\":\"Destroyed\",\"effects\":[],\"gauges\":[]}"
            });
            victim.GetType().GetMethod("UpdateObject").Invoke(victim, new[] { destroy });
            yield return new WaitForSeconds(0.08f);
            bool nativeDeath = false;
            foreach (Transform child in world.GetComponentsInChildren<Transform>())
                if (child.name.Contains("DeathFade") || child.name.Contains("DeathFragment")) nativeDeath = true;
            Assert.That(nativeDeath, Is.True, "Native delayed destruction must own its death clones.");
            for (int i = 0; i < 5; i++)
            {
                Invoke("ResetPlayback");
                yield return null;
                yield return null;
                Assert.That(world.transform.Find("PresentationContent"), Is.Not.Null);
                Assert.That(world.transform.childCount, Is.EqualTo(3), "Only ground, camera and current content survive.");
            }
            var stage = (GameObject)Field("stage");
            preview.gameObject.SetActive(false);
            yield return null;
            yield return null;
            Assert.That(stage == null, Is.True);
            Assert.That(Field("world"), Is.Null);
        }

        [UnityTest]
        public IEnumerator LegacyRecordingWorksAndMalformedRecordingHidesPreview()
        {
            Create();
            object clip = ((Array)Field("clips")).GetValue(0);
            var assetField = clip.GetType().GetField("recordingAsset");
            var legacy = new TextAsset("{\"version\":1,\"magic\":\"fire_shot\",\"frameDuration\":0.05,\"duration\":1,\"frames\":[{\"time\":0,\"objects\":{\"create\":[],\"update\":[]}}]}");
            assetField.SetValue(clip, legacy);
            Configure("fire_shot");
            yield return null;
            Assert.That(Property("ScenarioCount"), Is.EqualTo(1));
            preview.gameObject.SetActive(false);
            var invalid = new TextAsset("{\"version\":2,\"magic\":\"fire_shot\",\"frameDuration\":0.05,\"scenarios\":[]}");
            assetField.SetValue(clip, invalid);
            LogAssert.Expect(LogType.Warning, new Regex("Magic preview unavailable"));
            Configure("fire_shot");
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
            yield return null;
        }
    }
}
#endif
