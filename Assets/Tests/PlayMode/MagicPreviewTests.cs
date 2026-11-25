#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using System.Linq;
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
            Type clipType = previewType.GetNestedType("PreviewClip", BindingFlags.NonPublic);
            // Keep recordings in Editor-only fixtures; the player prefab has no bundled JSON references.
            string[] paths = System.IO.Directory.GetFiles("Assets/Tests/Editor/MagicPreviews", "*.json");
            paths = paths.OrderBy(path => System.IO.Path.GetFileNameWithoutExtension(path) == "fire_shot" ? 0 : 1).ToArray();
            Array fixtures = Array.CreateInstance(clipType, paths.Length);
            for (int i = 0; i < paths.Length; i++) {
                object fixture = Activator.CreateInstance(clipType, true);
                clipType.GetField("magicId").SetValue(fixture, System.IO.Path.GetFileNameWithoutExtension(paths[i]));
                clipType.GetField("recordingAsset").SetValue(fixture, AssetDatabase.LoadAssetAtPath<TextAsset>(paths[i].Replace('\\', '/')));
                fixtures.SetValue(fixture, i);
            }
            previewType.GetField("clips", Private).SetValue(preview, fixtures);
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
            Assert.That(count, Is.EqualTo(129));
        }

        [UnityTest]
        public IEnumerator SummonCountsMatchOneCastAndGenericDeathCasesAreAbsent()
        {
            Create();
            foreach (object clip in (Array)Field("clips"))
            {
                var asset = (TextAsset)clip.GetType().GetField("recordingAsset").GetValue(clip);
                foreach (JToken scenario in JObject.Parse(asset.text)["scenarios"])
                    Assert.That((string)scenario["id"], Is.Not.EqualTo("combat_death"));
            }
            var names = new[] { "aqua_archer", "mini_rock_swarm", "ember_spirit_swarm", "seed_spirit_swarm", "vine_spirit", "zap_mouse" };
            var types = new[] { "AquaArcher", "MiniRock", "EmberSpirit", "SeedSpirit", "VineSpirit", "ZapMouse" };
            var quantities = new[] { 1, 2, 5, 4, 2, 2 };
            for (int i = 0; i < names.Length; i++)
            {
                Configure(names[i]);
                Scenario(0);
                float moment = -1;
                var batch = new System.Collections.Generic.List<int>();
                foreach (JToken frame in Recording(names[i])["scenarios"][0]["frames"])
                {
                    foreach (JToken created in frame["objects"]["create"])
                        if ((string)created["type"] == types[i]) batch.Add((int)created["id"]);
                    if (batch.Count == 0) continue;
                    moment = (float)frame["time"] + 0.05f;
                    break;
                }
                Assert.That(batch.Count, Is.EqualTo(quantities[i]), names[i]);
                yield return PlayUntil(moment);
                foreach (int id in batch) Assert.That(Target(id), Is.Not.Null, names[i]);
            }
        }

        [UnityTest]
        public IEnumerator AreaAttacksReplayMultipleVictimsInBothViewports()
        {
            Create();
            string directory = System.IO.Path.Combine(Application.dataPath, "../Temp/AreaPreviewCaptures");
            System.IO.Directory.CreateDirectory(directory);
            foreach (string magic in new[] { "magma_spirit", "bubble_spirit", "sea_serpent", "dragon_tower", "bomb_sprite", "firework_tower",
                         "tower", "towerback", "wind_spirit", "fire_spirit", "electric_tower", "titan_remnant" })
            {
                Configure(magic);
                int index = magic == "towerback" ? 1 : 0; // Its ground attack is single-target.
                Scenario(index);
                JToken scenario = Recording(magic)["scenarios"][index];
                var fixtureIds = scenario["fixtureTargetIds"].ToObject<System.Collections.Generic.HashSet<int>>();
                float moment = -1;
                System.Collections.Generic.HashSet<int> victims = null;
                foreach (JToken frame in scenario["frames"])
                {
                    var hits = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.HashSet<int>>();
                    foreach (JToken hit in frame["events"])
                    {
                        if ((string)hit["type"] != "hit" || !fixtureIds.Contains((int)hit["targetId"])) continue;
                        int actor = (int)hit["actorId"];
                        if (!hits.ContainsKey(actor)) hits[actor] = new System.Collections.Generic.HashSet<int>();
                        hits[actor].Add((int)hit["targetId"]);
                    }
                    foreach (var batch in hits.Values) if (batch.Count >= 2) { victims = batch; break; }
                    if (victims == null) continue;
                    moment = (float)frame["time"] + 0.03f;
                    break;
                }
                Assert.That(victims, Is.Not.Null, magic + ": one attack must hit multiple enemies");
                yield return PlayUntil(moment);
                foreach (int id in victims)
                {
                    Component target = Target(id);
                    Assert.That(target, Is.Not.Null, magic);
                    var gauges = (IList)target.GetType().GetField("gauges").GetValue(target);
                    Assert.That((float)gauges[0].GetType().GetField("value").GetValue(gauges[0]), Is.LessThan(10000f));
                }
                foreach (bool book in new[] { true, false }) SaveViewport(directory, magic, book);
            }
        }

        private void SaveViewport(string directory, string magic, bool book)
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

        [UnityTest]
        public IEnumerator RollingRockReversesOnRecordedImpactAndKeepsItsSpriteRegistration()
        {
            Create();
            const string magic = "rock_rolling";
            Assert.That(Configure(magic), Is.True);
            var frames = (JArray)Recording(magic)["scenarios"][0]["frames"];
            int rockId = (int)frames.SelectMany(frame => frame["objects"]["create"])
                .First(created => (string)created["type"] == "RockRolling")["id"];
            Sprite reference = Resources.Load<Sprite>("Game/sprites/RockRolling");

            yield return PlayUntil(0.61f);
            SpriteRenderer renderer = ServedObjectRenderer(Target(rockId));
            Assert.That(renderer.sprite, Is.SameAs(reference));
            Quaternion localRotation = renderer.transform.localRotation;
            yield return PlayUntil(0.97f);
            Sprite towardEnemy = renderer.sprite;
            Assert.That(RockRollIndex(towardEnemy), Is.InRange(2, 4));
            yield return PlayUntil(1.16f);
            int atBounce = RockRollIndex(renderer.sprite);
            yield return PlayUntil(1.38f);
            Assert.That(RockRollIndex(renderer.sprite), Is.LessThan(atBounce));
            Assert.That(Quaternion.Angle(renderer.transform.localRotation, localRotation), Is.LessThan(0.01f));
            Assert.That(renderer.flipX, Is.False);
            Assert.That(renderer.sprite.rect, Is.EqualTo(reference.rect));
            Assert.That(renderer.sprite.pivot, Is.EqualTo(reference.pivot));
            Assert.That(renderer.sprite.pixelsPerUnit, Is.EqualTo(reference.pixelsPerUnit));

            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                string directory = System.IO.Path.Combine(Application.dataPath, "../Temp/RollingRockCaptures");
                System.IO.Directory.CreateDirectory(directory);
                SaveViewport(directory, magic, true);
            }
        }

        [UnityTest]
        public IEnumerator RollingRockTravelDrivesPosesAt20Hz() => CheckRollingRockTravel(20, 1);

        [UnityTest]
        public IEnumerator RollingRockTravelDrivesPosesAt60Hz() => CheckRollingRockTravel(60, -1);

        private IEnumerator CheckRollingRockTravel(int tickRate, int direction)
        {
            Create();
            Component world = CreateWorld();
            world.transform.position = new Vector3(100f, 100f, 100f);
            world.transform.rotation = Quaternion.Euler(0f, 17f, 0f);
            world.transform.localScale = Vector3.one * 0.4f;

            Type config = Runtime("GameScene.GameConfig");
            MethodInfo setRate = config.GetMethod("SetTickRate");
            int previousTickRate = (int)config.GetProperty("TickRate").GetValue(null);
            int previousRenderRate = Time.captureFramerate;
            const int renderRate = 120;
            string master = direction > 0 ? "LeftPlayer" : "RightPlayer";
            float x = direction > 0 ? 4f : 14f, z = 4f;
            Type infoType = Runtime("GameScene.Dto.ObjectsInfo");
            MethodInfo decode = Runtime("Global.Serialization.JsonCodec").GetMethod("Deserialize").MakeGenericMethod(infoType);
            MethodInfo apply = Runtime("GameScene.Object.PresentationFramePlayer").GetMethod("Apply");
            void Apply(string json) => apply.Invoke(null, new[] { decode.Invoke(null, new object[] { json }), null, world });
            void Send(string status = "Idle") => Apply(FormattableString.Invariant(
                $"{{\"update\":[{{\"id\":42,\"master\":\"{master}\",\"status\":\"{status}\",\"position\":{{\"x\":{x},\"y\":0,\"z\":{z}}}}}]}}"));

            try
            {
                Time.captureFramerate = renderRate;
                setRate.Invoke(null, new object[] { tickRate });
                Apply(FormattableString.Invariant(
                    $"{{\"create\":[{{\"id\":42,\"type\":\"RockRolling\",\"master\":\"{master}\",\"position\":{{\"x\":{x},\"y\":0,\"z\":{z}}}}}]}}"));
                MethodInfo find = world.GetType().GetMethod("FindById");
                var rock = (Component)find.Invoke(world, new object[] { 42 });
                SpriteRenderer renderer = ServedObjectRenderer(rock);
                Assert.That(renderer.flipX, Is.False);
                Assert.That(RockRollIndex(renderer.sprite), Is.Zero);

                int rendersPerTick = renderRate / tickRate;
                var seen = new System.Collections.Generic.HashSet<int> { 0 };
                int lastPose = 0;
                for (int tick = 0; tick < tickRate; tick++)
                {
                    // 기록된 발사 속도와 서버의 충돌 이격량을 재현한다.
                    x += direction * (8f / tickRate + (tick == 3 ? 0.05f : 0f));
                    Send();
                    for (int i = 0; i < rendersPerTick; i++)
                    {
                        yield return null;
                        Assert.That(renderer.flipX, Is.False);
                        int pose = RockRollIndex(renderer.sprite);
                        if (pose == lastPose) continue;
                        Assert.That(pose, Is.EqualTo((lastPose + direction + 8) % 8));
                        lastPose = pose;
                        seen.Add(pose);
                    }
                }
                Assert.That(seen.Count, Is.EqualTo(8));

                x -= 3f * direction;
                Send();
                for (int i = 0; i < rendersPerTick + 2; i++) yield return null;
                Assert.That(RockRollIndex(renderer.sprite), Is.EqualTo(lastPose));
                int beforeDepthTravel = lastPose;
                for (int tick = 0; tick < Mathf.CeilToInt(0.35f * tickRate); tick++)
                {
                    z += 8f / tickRate;
                    Send();
                    for (int i = 0; i < rendersPerTick; i++)
                    {
                        yield return null;
                        int pose = RockRollIndex(renderer.sprite);
                        if (pose == lastPose) continue;
                        Assert.That(pose, Is.EqualTo((lastPose + 1) % 8));
                        lastPose = pose;
                    }
                }
                Assert.That(lastPose, Is.Not.EqualTo(beforeDepthTravel));
                Sprite held = renderer.sprite;
                for (int i = 0; i < rendersPerTick + 2; i++) yield return null;
                Assert.That(renderer.sprite, Is.SameAs(held));
                Send("Destroyed");
                yield return new WaitForSeconds(0.12f);
                Assert.That((Component)find.Invoke(world, new object[] { 42 }) == null, Is.True);

                Apply("{\"projectile\":[{\"type\":\"RockShot\",\"duration\":0.5," +
                      "\"start\":{\"targetType\":\"position\",\"x\":2,\"y\":1,\"z\":4}," +
                      "\"end\":{\"targetType\":\"position\",\"x\":4,\"y\":1,\"z\":4}}]}");
                var shot = world.transform.Find("PresentationContent/RockShot(Clone)");
                Assert.That(shot, Is.Not.Null);
                var shotRenderer = shot.Find("actualObject").GetComponent<SpriteRenderer>();
                Vector3 start = shot.position;
                yield return new WaitForSeconds(0.2f);
                Assert.That(shot.position, Is.Not.EqualTo(start));
                Assert.That(shotRenderer.sprite, Is.SameAs(Resources.Load<Sprite>("Game/sprites/RockRolling")));
            }
            finally
            {
                Time.captureFramerate = previousRenderRate;
                setRate.Invoke(null, new object[] { previousTickRate });
            }
        }

        private static int RockRollIndex(Sprite sprite) => sprite.name == "RockRolling" ? 0 :
            int.Parse(sprite.name.Substring("RockRollingRoll".Length)) - 1;

        [UnityTest]
        public IEnumerator WallGolemRecordingShowsPunchOnTheDamageFrame()
        {
            Create();
            const string magic = "wall_golem";
            Assert.That(Configure(magic), Is.True);
            var ground = Recording(magic)["scenarios"][0];
            var frames = (JArray)ground["frames"];
            int golemId = (int)frames.SelectMany(frame => frame["objects"]["create"])
                .First(unit => (string)unit["type"] == "WallGolem")["id"];
            int targetId = (int)frames.SelectMany(frame => frame["objects"]["create"])
                .First(unit => (string)unit["master"] == "RightPlayer")["id"];
            float impact = (float)frames.First(frame => frame["objects"]["update"]
                .Any(unit => (int)unit["id"] == golemId && (string)unit["status"] == "Attack"))["time"];
            int previousRenderRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(.78f);
                Component golem = Target(golemId);
                Assert.That(golem, Is.Not.Null);
                Assert.That(golem.GetComponent("WaddleMotionController"), Is.Null);
                Assert.That(golem.GetComponent("HoppingMotionController"), Is.Null);
                Assert.That((bool)golem.GetType().GetField("_swingOnAttack", Private).GetValue(golem), Is.False);
                var sprite = (SpriteRenderer)golem.GetType().GetField("_spriteRenderer", Private).GetValue(golem);
                Component opponent = Target(targetId);
                var seen = new System.Collections.Generic.HashSet<string>();
                bool canCapture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
                string directory = System.IO.Path.Combine(Application.dataPath, "../Library/WallGolemCaptures");
                if (canCapture) System.IO.Directory.CreateDirectory(directory);
                while ((float)Field("elapsed") < 1.65f)
                {
                    seen.Add(sprite.sprite.name);
                    yield return null;
                }
                Assert.That(seen.Count(name => name.StartsWith("WallGolemWalk", StringComparison.Ordinal)), Is.EqualTo(8));
                Assert.That(sprite.flipX, Is.False);
                if (canCapture) SaveViewport(directory, "walk", true);

                yield return PlayUntil(impact - .023f);
                Assert.That(sprite.sprite.name, Is.EqualTo("WallGolem"));
                float before = CurrentHp(opponent);
                Assert.That(before, Is.EqualTo(10000f));

                for (int pose = 1; pose <= 4; pose++)
                {
                    yield return PlayUntil(impact + .023f + (pose - 1) * .08f);
                    Assert.That(sprite.sprite.name, Is.EqualTo($"WallGolemPunch{pose:00}"));
                    Assert.That(sprite.flipX, Is.False);
                    Assert.That(CurrentHp(opponent), Is.EqualTo(9900f));
                    if (canCapture && pose == 1) SaveViewport(directory, "impact", true);
                }

                yield return PlayUntil(impact + .37f);
                Assert.That(sprite.sprite.name, Is.EqualTo("WallGolem"));
            }
            finally
            {
                Time.captureFramerate = previousRenderRate;
            }
        }

        private static float CurrentHp(Component opponent)
        {
            var gauges = (IList)opponent.GetType().GetField("gauges").GetValue(opponent);
            return (float)gauges[0].GetType().GetField("value").GetValue(gauges[0]);
        }

        [UnityTest]
        public IEnumerator WallGolemRecordingLeavesTheServerRemnantAfterTheLocalFade()
        {
            Create();
            const string magic = "wall_golem";
            Assert.That(Configure(magic), Is.True);
            var scenarios = (JArray)Recording(magic)["scenarios"];
            int deathIndex = scenarios.IndexOf(scenarios.First(s => (string)s["id"] == "death"));
            var frames = (JArray)scenarios[deathIndex]["frames"];
            int golemId = (int)frames.SelectMany(frame => frame["objects"]["create"])
                .First(unit => (string)unit["type"] == "WallGolem")["id"];
            var killed = frames.First(frame => frame["objects"]["update"]
                .Any(unit => (int)unit["id"] == golemId && (string)unit["status"] == "Destroyed"));
            int remnantId = (int)killed["objects"]["create"]
                .Single(unit => (string)unit["type"] == "MediumRockRemnant")["id"];
            float death = (float)killed["time"];
            int previousRenderRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                Scenario(deathIndex);
                yield return PlayUntil(death - .02f);
                Component golem = Target(golemId);
                Assert.That(golem, Is.Not.Null);
                var renderer = (SpriteRenderer)golem.GetType().GetField("_spriteRenderer", Private).GetValue(golem);
                Sprite finalPose = renderer.sprite;
                Vector3 position = golem.transform.position;
                string fadeName = golem.name + "DeathFade";
                Assert.That(Target(remnantId), Is.Null);

                yield return PlayUntil(death + .11f);
                Assert.That(Target(golemId), Is.Null);
                Component remnant = Target(remnantId);
                Assert.That(remnant, Is.Not.Null);
                Assert.That(Vector3.Distance(remnant.transform.position, position), Is.LessThan(.005f));
                var content = ((Component)Field("world")).transform.Find("PresentationContent");
                SpriteRenderer fade = content.Find(fadeName)?.GetComponent<SpriteRenderer>();
                Assert.That(fade, Is.Not.Null);
                Assert.That(fade.sprite, Is.SameAs(finalPose));
                int Fragments() => content.Cast<Transform>().Count(child => child.name.EndsWith("DeathFragment", StringComparison.Ordinal));
                Assert.That(Fragments(), Is.EqualTo(9));

                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    string directory = System.IO.Path.Combine(Application.dataPath, "../Library/WallGolemCaptures");
                    System.IO.Directory.CreateDirectory(directory);
                    SaveViewport(directory, "death", true);
                }

                yield return PlayUntil(death + .65f);
                Assert.That(content.Find(fadeName), Is.Null);
                Assert.That(Fragments(), Is.Zero);
                Assert.That(Target(remnantId), Is.SameAs(remnant));
                Assert.That(content.Cast<Transform>().Count(child => child.name.StartsWith("MediumRockRemnant", StringComparison.Ordinal)), Is.EqualTo(1));
            }
            finally
            {
                Time.captureFramerate = previousRenderRate;
            }
        }

        [UnityTest]
        public IEnumerator AquaArcherRecordedWalkAndWaterShotUseTheirOwnPoses()
        {
            Create();
            Assert.That(Configure("rallying_totem"), Is.True);
            var rally = Recording("rallying_totem")["scenarios"][0];
            int walkerId = AquaArcherId(rally);
            int previousRenderRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                string directory = System.IO.Path.Combine(Application.dataPath, "../Library/AquaArcherCaptures");
                bool canCapture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
                if (canCapture) System.IO.Directory.CreateDirectory(directory);
                yield return PlayUntil(.85f);
                Component walker = Target(walkerId);
                var walk = (SpriteRenderer)walker.GetType().GetField("_spriteRenderer", Private).GetValue(walker);
                var seen = new System.Collections.Generic.HashSet<string>();
                while ((float)Field("elapsed") < 1.85f)
                {
                    seen.Add(walk.sprite.name);
                    yield return null;
                }
                Assert.That(seen.Count(name => name.StartsWith("AquaArcherWalk", StringComparison.Ordinal)), Is.EqualTo(8));
                if (canCapture) SaveViewport(directory, "walk", true);
                yield return PlayUntil(2.05f);
                Assert.That(walk.sprite.name, Is.EqualTo("AquaArcher"));

                Assert.That(Configure("aqua_archer"), Is.True);
                var matches = (JArray)Recording("aqua_archer")["scenarios"];
                for (int i = 0; i < matches.Count; i++)
                {
                    string scenarioName = (string)matches[i]["id"];
                    int id = AquaArcherId(matches[i]);
                    var shot = FirstProjectile(matches[i], "WaterShot");
                    Scenario(i);
                    yield return PlayUntil(shot.start - .035f);
                    Component archer = Target(id);
                    Assert.That(archer, Is.Not.Null);
                    var body = (SpriteRenderer)archer.GetType().GetField("_spriteRenderer", Private).GetValue(archer);
                    Assert.That(body.sprite.name, Is.EqualTo("AquaArcher"));
                    Assert.That(FindProjectile("WaterShot"), Is.Null);
                    yield return PlayUntil(shot.start + .012f);
                    Transform arrow = FindProjectile("WaterShot");
                    Assert.That(arrow, Is.Not.Null);
                    Assert.That(body.sprite.name, Is.EqualTo("AquaArcherAttack"));
                    var camera = (Camera)Field("previewCamera");
                    float x = body.flipX ? -.37f : .37f;
                    Vector3 grip = body.transform.TransformPoint(new Vector3(x, .565f, 0f));
                    float launchGap = Vector2.Distance(camera.WorldToViewportPoint(grip),
                        camera.WorldToViewportPoint(arrow.position));
                    Assert.That(launchGap, Is.LessThan(.018f), $"{scenarioName}: 활에서 출발한 직후의 물화살 화면 거리");
                    if (canCapture) SaveViewport(directory, scenarioName + "-release", true);
                    yield return PlayUntil(shot.start + .11f);
                    Assert.That(body.sprite.name, Is.EqualTo("AquaArcherRecover01"));
                    yield return PlayUntil(shot.start + .19f);
                    Assert.That(body.sprite.name, Is.EqualTo("AquaArcherRecover02"));
                    if (canCapture) SaveViewport(directory, scenarioName + "-recover", true);
                    yield return PlayUntil(shot.start + .29f);
                    Assert.That(body.sprite.name, Is.EqualTo("AquaArcher"));
                }
            }
            finally
            {
                Time.captureFramerate = previousRenderRate;
            }
        }

        [UnityTest]
        public IEnumerator AquaArcherLaunchesFromItsMirroredBowWhileCloudDragonKeepsItsOrigin()
        {
            Create();
            Component world = CreateWorld();
            MethodInfo decode = Runtime("Global.Serialization.JsonCodec").GetMethod("Deserialize")
                .MakeGenericMethod(Runtime("GameScene.Dto.ObjectsInfo"));
            MethodInfo apply = Runtime("GameScene.Object.PresentationFramePlayer").GetMethod("Apply");
            MethodInfo find = world.GetType().GetMethod("FindById");
            void Apply(string json) =>
                apply.Invoke(null, new[] { decode.Invoke(null, new object[] { json }), null, world });
            Component Actor(int id) => (Component)find.Invoke(world, new object[] { id });
            Transform LastWaterShot() => world.transform.Find("PresentationContent").Cast<Transform>()
                .Last(child => child.name == "WaterShot(Clone)");

            Apply("{\"create\":[" +
                "{\"id\":21,\"type\":\"AquaArcher\",\"master\":\"LeftPlayer\",\"position\":{\"x\":9,\"y\":0,\"z\":5}}," +
                "{\"id\":22,\"type\":\"ElectricSlime\",\"master\":\"RightPlayer\",\"position\":{\"x\":5,\"y\":0,\"z\":5}}," +
                "{\"id\":23,\"type\":\"CloudDragon\",\"master\":\"LeftPlayer\",\"position\":{\"x\":9,\"y\":1,\"z\":4}}]}");
            Component archer = Actor(21);
            var body = (SpriteRenderer)archer.GetType().GetField("_spriteRenderer", Private).GetValue(archer);
            Assert.That(body.flipX, Is.False);
            Apply("{\"projectile\":[{\"type\":\"WaterShot\",\"duration\":0.5," +
                "\"start\":{\"targetType\":\"reference\",\"id\":21},\"end\":{\"targetType\":\"reference\",\"id\":22}}]," +
                "\"update\":[{\"id\":21,\"position\":{\"x\":9,\"y\":0,\"z\":5},\"status\":\"Attack\"}]}");
            Transform archerShot = LastWaterShot();
            Assert.That(body.flipX, Is.True);
            Assert.That(body.sprite.name, Is.EqualTo("AquaArcherAttack"));
            Vector3 bow = body.transform.TransformPoint(new Vector3(-.37f, .565f, 0f));
            Assert.That(Vector3.Distance(bow, archerShot.position), Is.LessThan(.005f));

            Apply("{\"projectile\":[{\"type\":\"WaterShot\",\"duration\":0.5," +
                "\"start\":{\"targetType\":\"reference\",\"id\":23},\"end\":{\"targetType\":\"reference\",\"id\":22}}]}");
            Transform dragonShot = LastWaterShot();
            Vector3 dragonStart = Actor(23).transform.position;
            Assert.That(Vector3.Distance(dragonStart, dragonShot.position), Is.LessThan(.005f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator WaterSlimeLaunchesFromItsMirroredMouth()
        {
            Create();
            Component world = CreateWorld();
            MethodInfo decode = Runtime("Global.Serialization.JsonCodec").GetMethod("Deserialize")
                .MakeGenericMethod(Runtime("GameScene.Dto.ObjectsInfo"));
            MethodInfo apply = Runtime("GameScene.Object.PresentationFramePlayer").GetMethod("Apply");
            MethodInfo find = world.GetType().GetMethod("FindById");
            void Apply(string json) => apply.Invoke(null, new[] { decode.Invoke(null, new object[] { json }), null, world });
            Component Actor(int id) => (Component)find.Invoke(world, new object[] { id });
            Transform LastWaterShot() => world.transform.Find("PresentationContent").Cast<Transform>()
                .Last(child => child.name == "WaterShot(Clone)");
            string Shot(int actor, int target) =>
                $"\"projectile\":[{{\"type\":\"WaterShot\",\"duration\":0.5,\"start\":{{\"targetType\":\"reference\",\"id\":{actor}}}," +
                $"\"end\":{{\"targetType\":\"reference\",\"id\":{target}}}}}]";

            Apply("{\"create\":[" +
                "{\"id\":21,\"type\":\"WaterSlime\",\"master\":\"LeftPlayer\",\"position\":{\"x\":9,\"y\":0,\"z\":5}}," +
                "{\"id\":22,\"type\":\"ElectricSlime\",\"master\":\"RightPlayer\",\"position\":{\"x\":5,\"y\":0,\"z\":5}}," +
                "{\"id\":23,\"type\":\"ElectricSlime\",\"master\":\"RightPlayer\",\"position\":{\"x\":13,\"y\":0,\"z\":5}}," +
                "{\"id\":24,\"type\":\"BubbleGenerator\",\"master\":\"LeftPlayer\",\"position\":{\"x\":9,\"y\":0,\"z\":4}}]}");
            var body = ServedObjectRenderer(Actor(21));
            Assert.That(body.sprite.name, Is.EqualTo("WaterSlime"));
            const string attack = "\"update\":[{\"id\":21,\"position\":{\"x\":9.05,\"y\":0,\"z\":5},\"status\":\"Attack\"}]";
            Apply("{" + Shot(21, 22) + "," + attack + "}");
            AssertWaterSlimeMouth(body, LastWaterShot(), true);
            yield return null;

            Apply("{" + Shot(21, 23) + "}");
            AssertWaterSlimeMouth(body, LastWaterShot(), false);
            Apply("{" + Shot(24, 23) + "}");
            Assert.That(Vector3.Distance(Actor(24).transform.position, LastWaterShot().position), Is.LessThan(.005f));

            Apply("{\"projectile\":[{\"type\":\"WaterShot\",\"duration\":0.5," +
                "\"start\":{\"targetType\":\"position\",\"x\":7,\"y\":0.3,\"z\":5}," +
                "\"end\":{\"targetType\":\"position\",\"x\":8,\"y\":0.3,\"z\":5}}]}");
            Vector3 positionStart = world.transform.TransformPoint(new Vector3(7f, .3f, 5f));
            Assert.That(Vector3.Distance(positionStart, LastWaterShot().position), Is.LessThan(.005f));
        }

        [UnityTest]
        public IEnumerator WaterSlimeSnapshotRegistersBothObjectsBeforeLaunching()
        {
            root = new GameObject("WaterSlime snapshot fixture");
            Component container = root.AddComponent(Runtime("GameScene.Object.ObjectContainer"));
            root.AddComponent(Runtime("GameScene.Object.ObjectSpawner"));
            root.AddComponent(Runtime("GameScene.Object.ObjectUpdater"));
            Component syncer = root.AddComponent(Runtime("GameScene.Object.ObjectSyncer"));
            MethodInfo find = container.GetType().GetMethod("FindById");
            MethodInfo decode = Runtime("Global.Serialization.JsonCodec").GetMethod("Deserialize");
            Type objectsType = Runtime("GameScene.Dto.ObjectsInfo");
            Type snapshotArray = Runtime("GameScene.Dto.SnapshotObjectDto").MakeArrayType();
            Transform shot = null;
            try
            {
                object launch = decode.MakeGenericMethod(objectsType).Invoke(null, new object[] {
                    "{\"projectile\":[{\"type\":\"WaterShot\",\"duration\":0.5," +
                    "\"start\":{\"targetType\":\"reference\",\"id\":31},\"end\":{\"targetType\":\"reference\",\"id\":32}}]}"
                });
                object snapshot = decode.MakeGenericMethod(snapshotArray).Invoke(null, new object[] {
                    "[{\"id\":31,\"prefab\":\"WaterSlime\",\"master\":\"LeftPlayer\",\"x\":9,\"y\":0,\"z\":5,\"status\":\"Attack\"}," +
                    "{\"id\":32,\"prefab\":\"ElectricSlime\",\"master\":\"RightPlayer\",\"x\":13,\"y\":0,\"z\":5,\"status\":\"Idle\"}]"
                });
                object deferred = Runtime("GameScene.Object.ProjectileSpawner").GetMethod("SpawnBeforeSnapshotUpdates")
                    .Invoke(null, new[] { objectsType.GetField("projectile").GetValue(launch), snapshot });
                Assert.That((IList)deferred, Has.Count.EqualTo(1));
                Assert.That(find.Invoke(container, new object[] { 31 }), Is.Null);
                Assert.That(find.Invoke(container, new object[] { 32 }), Is.Null);
                Assert.That(GameObject.Find("WaterShot(Clone)"), Is.Null);

                syncer.GetType().GetMethod("Sync").Invoke(syncer, new[] { snapshot, deferred });
                var body = ServedObjectRenderer((Component)find.Invoke(container, new object[] { 31 }));
                Assert.That(find.Invoke(container, new object[] { 32 }), Is.Not.Null);
                var instance = GameObject.Find("WaterShot(Clone)");
                Assert.That(instance, Is.Not.Null);
                shot = instance.transform;
                AssertWaterSlimeMouth(body, shot, false);
                yield return null;
                Assert.That(shot != null, Is.True);
            }
            finally
            {
                container.GetType().GetMethod("Clear").Invoke(container, null);
                if (shot != null) UnityEngine.Object.Destroy(shot.gameObject);
            }
        }

        private static void AssertWaterSlimeMouth(SpriteRenderer body, Transform shot, bool mirrored)
        {
            Assert.That(body.sprite.name, Is.EqualTo("WaterSlimeSpit01"));
            Assert.That(body.flipX, Is.EqualTo(mirrored));
            Assert.That(body.sprite.pivot, Is.EqualTo(new Vector2(160f, 0f)));
            const float mouthPixelX = 169.5f;
            const float mouthPixelY = 164.5f;
            float localX = (mouthPixelX - body.sprite.pivot.x) / body.sprite.pixelsPerUnit;
            float localY = (body.sprite.rect.height - mouthPixelY - body.sprite.pivot.y) / body.sprite.pixelsPerUnit;
            Vector3 mouth = body.transform.TransformPoint(new Vector3(mirrored ? -localX : localX, localY, 0f));
            Assert.That(Vector3.Distance(mouth, shot.position), Is.LessThan(.005f));
        }

        private static int AquaArcherId(JToken scenario) =>
            (int)scenario["frames"].SelectMany(frame => frame["objects"]["create"])
                .First(unit => (string)unit["type"] == "AquaArcher")["id"];

        [UnityTest]
        public IEnumerator TowerbackRecordingShowsGroundHitAndAirFireOnTheirOwnTicks()
        {
            Create();
            const string magic = "towerback";
            Assert.That(Configure(magic), Is.True);
            var scenarios = (JArray)Recording(magic)["scenarios"];
            int groundId = RecordedTowerbackId(scenarios[0]);
            int groundTarget = (int)scenarios[0]["fixtureTargetIds"][0];
            float impact = (float)scenarios[0]["frames"].First(frame => frame["events"]
                .Any(hit => (string)hit["type"] == "hit" && (int)hit["actorId"] == groundId))["time"];
            int previousRenderRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                string directory = System.IO.Path.Combine(Application.dataPath, "../Library/TowerbackCaptures");
                bool canCapture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
                if (canCapture) System.IO.Directory.CreateDirectory(directory);

                yield return PlayUntil(.72f);
                Component tower = Target(groundId);
                var body = (SpriteRenderer)tower.GetType().GetField("_spriteRenderer", Private).GetValue(tower);
                Assert.That(tower.GetComponent("HoppingMotionController"), Is.Null);
                var seen = new System.Collections.Generic.HashSet<string>();
                while ((float)Field("elapsed") < 1.20f)
                {
                    if (seen.Add(body.sprite.name) && canCapture && body.sprite.name.StartsWith("TowerbackWalk"))
                        SaveViewport(directory, body.sprite.name, true);
                    yield return null;
                }
                Assert.That(seen.Count(name => name.StartsWith("TowerbackWalk")), Is.EqualTo(8));
                yield return PlayUntil(impact - .025f);
                Assert.That(body.sprite.name, Is.EqualTo("Towerback"));
                Assert.That(CurrentHp(Target(groundTarget)), Is.EqualTo(1000f));
                for (int pose = 1; pose <= 4; pose++)
                {
                    yield return PlayUntil(impact + .02f + (pose - 1) * .08f);
                    Assert.That(body.sprite.name, Is.EqualTo($"TowerbackPunch{pose:00}"));
                    Assert.That(CurrentHp(Target(groundTarget)), Is.EqualTo(920f));
                    Assert.That(FindProjectile("RockShot"), Is.Null);
                    if (canCapture && pose == 1) SaveViewport(directory, "ground-impact", true);
                }
                yield return PlayUntil(impact + .36f);
                Assert.That(body.sprite.name, Is.EqualTo("Towerback"));

                Scenario(1);
                var shot = FirstProjectile(scenarios[1], "RockShot");
                yield return PlayUntil(shot.start - .025f);
                tower = Target(RecordedTowerbackId(scenarios[1]));
                body = (SpriteRenderer)tower.GetType().GetField("_spriteRenderer", Private).GetValue(tower);
                Component airborne = Target(shot.targetId);
                Assert.That(body.sprite.name, Is.EqualTo("Towerback"));
                Assert.That(FindProjectile("RockShot"), Is.Null);

                yield return PlayUntil(shot.start + .012f);
                Transform rock = FindProjectile("RockShot");
                Assert.That(rock, Is.Not.Null);
                Assert.That(body.sprite.name, Is.EqualTo("TowerbackFire01"));
                Vector3 muzzle = body.transform.TransformPoint(new Vector3(.2115f, 1.7805f, 0f));
                float range = Vector3.Distance(muzzle, airborne.transform.position);
                Assert.That(Vector3.Distance(muzzle, rock.position), Is.LessThan(range * .22f));
                Assert.That(CurrentHp(airborne), Is.EqualTo(1000f));
                if (canCapture) SaveViewport(directory, "air-launch", true);

                yield return PlayUntil(shot.start + .11f);
                Assert.That(body.sprite.name, Is.EqualTo("TowerbackFire02"));
                if (canCapture) SaveViewport(directory, "air-recoil", true);
                yield return PlayUntil(shot.start + shot.duration - .025f);
                Assert.That(body.sprite.name, Is.EqualTo("TowerbackFire03"));
                Assert.That(Vector3.Distance(rock.position, airborne.transform.position), Is.LessThan(range * .24f));
                yield return PlayUntil(shot.start + .23f);
                Assert.That(FindProjectile("RockShot"), Is.Null);
                Assert.That(CurrentHp(airborne), Is.EqualTo(1000f));
                yield return PlayUntil(shot.start + .275f);
                Assert.That(body.sprite.name, Is.EqualTo("TowerbackFire04"));
                Assert.That(CurrentHp(airborne), Is.EqualTo(900f));
                yield return PlayUntil(shot.start + .37f);
                Assert.That(body.sprite.name, Is.EqualTo("Towerback"));
                yield return PlayUntil(shot.start + .82f);
                Assert.That(body.sprite.name, Is.EqualTo("TowerbackFire01"));
            }
            finally
            {
                Time.captureFramerate = previousRenderRate;
            }
        }

        private static int RecordedTowerbackId(JToken scenario) =>
            (int)scenario["frames"].SelectMany(frame => frame["objects"]["create"])
                .First(unit => (string)unit["type"] == "Towerback")["id"];

        [UnityTest]
        public IEnumerator TowerbackMirrorsItsMuzzleAndKeepsAttackSelectionInsideThePacket()
        {
            Create();
            Component world = CreateWorld();
            MethodInfo decode = Runtime("Global.Serialization.JsonCodec").GetMethod("Deserialize")
                .MakeGenericMethod(Runtime("GameScene.Dto.ObjectsInfo"));
            MethodInfo apply = Runtime("GameScene.Object.PresentationFramePlayer").GetMethod("Apply");
            MethodInfo find = world.GetType().GetMethod("FindById");
            void Apply(string json) =>
                apply.Invoke(null, new[] { decode.Invoke(null, new object[] { json }), null, world });
            Component Actor(int id) => (Component)find.Invoke(world, new object[] { id });
            Transform LastRock() => world.transform.Find("PresentationContent").Cast<Transform>()
                .Last(child => child.name == "RockShot(Clone)");
            string Shot(int actor, int target) =>
                $"\"projectile\":[{{\"type\":\"RockShot\",\"duration\":0.2,\"start\":{{\"targetType\":\"reference\",\"id\":{actor}}}," +
                $"\"end\":{{\"targetType\":\"reference\",\"id\":{target}}}}}]";
            string Update(string x, string state) =>
                $"\"update\":[{{\"id\":21,\"position\":{{\"x\":{x},\"y\":0,\"z\":5}},\"status\":\"{state}\"}}]";

            Apply("{\"create\":[" +
                "{\"id\":21,\"type\":\"Towerback\",\"master\":\"LeftPlayer\",\"position\":{\"x\":9,\"y\":0,\"z\":5}}," +
                "{\"id\":22,\"type\":\"ElectricSlime\",\"master\":\"RightPlayer\",\"position\":{\"x\":5,\"y\":2,\"z\":5}}," +
                "{\"id\":23,\"type\":\"WallGolem\",\"master\":\"LeftPlayer\",\"position\":{\"x\":9,\"y\":0,\"z\":4}}," +
                "{\"id\":24,\"type\":\"ElectricSlime\",\"master\":\"RightPlayer\",\"position\":{\"x\":13,\"y\":2,\"z\":5}}]}");
            Component tower = Actor(21);
            var body = (SpriteRenderer)tower.GetType().GetField("_spriteRenderer", Private).GetValue(tower);
            string status = Update("9.08", "Attack");
            Apply("{" + Update("9.04", "Move") + "}");
            Apply("{" + Update("9.08", "Move") + "}");
            yield return null;
            Assert.That(body.flipX, Is.False);

            Apply("{" + Shot(21, 22) + "," + status + "}");
            Assert.That(body.sprite.name, Is.EqualTo("TowerbackFire01"));
            Assert.That(body.flipX, Is.True);
            Vector3 leftMuzzle = body.transform.TransformPoint(new Vector3(-.2115f, 1.7805f, 0f));
            Assert.That(Vector3.Distance(leftMuzzle, LastRock().position), Is.LessThan(.005f));
            yield return null;
            yield return null;
            Assert.That(body.flipX, Is.True);

            Apply("{" + Shot(21, 24) + "}");
            Assert.That(body.sprite.name, Is.EqualTo("TowerbackFire01"));
            Assert.That(body.flipX, Is.False);
            Vector3 rightMuzzle = body.transform.TransformPoint(new Vector3(.2115f, 1.7805f, 0f));
            Assert.That(Vector3.Distance(rightMuzzle, LastRock().position), Is.LessThan(.005f));
            Apply("{" + Shot(23, 24) + "," + status + "}");
            Assert.That(body.sprite.name, Is.EqualTo("TowerbackPunch01"));
            Assert.That(Vector3.Distance(Actor(23).transform.position, LastRock().position), Is.LessThan(.005f));
        }

        [UnityTest]
        public IEnumerator TowerbackSnapshotLaunchesBeforeRemovingTheTarget()
        {
            root = new GameObject("Towerback snapshot fixture");
            Component container = root.AddComponent(Runtime("GameScene.Object.ObjectContainer"));
            root.AddComponent(Runtime("GameScene.Object.ObjectUpdater"));
            Component syncer = root.AddComponent(Runtime("GameScene.Object.ObjectSyncer"));
            MethodInfo find = container.GetType().GetMethod("FindById");
            MethodInfo decode = Runtime("Global.Serialization.JsonCodec").GetMethod("Deserialize");
            Type objectsType = Runtime("GameScene.Dto.ObjectsInfo");
            MethodInfo decodeObjects = decode.MakeGenericMethod(objectsType);
            Transform shot = null;
            try
            {
                object initial = decodeObjects.Invoke(null, new object[] {
                    "{\"create\":[" +
                    "{\"id\":31,\"type\":\"Towerback\",\"master\":\"LeftPlayer\",\"position\":{\"x\":9,\"y\":0,\"z\":5}}," +
                    "{\"id\":32,\"type\":\"ElectricSlime\",\"master\":\"RightPlayer\",\"position\":{\"x\":13,\"y\":2,\"z\":5}}]}"
                });
                Runtime("GameScene.Object.PresentationFramePlayer").GetMethod("Apply")
                    .Invoke(null, new[] { initial, null, null });
                Assert.That(find.Invoke(container, new object[] { 32 }), Is.Not.Null);

                object launch = decodeObjects.Invoke(null, new object[] {
                    "{\"projectile\":[{\"type\":\"RockShot\",\"duration\":0.2," +
                    "\"start\":{\"targetType\":\"reference\",\"id\":31},\"end\":{\"targetType\":\"reference\",\"id\":32}}]}"
                });
                object deferred = Runtime("GameScene.Object.ProjectileSpawner").GetMethod("SpawnBeforeObjectUpdates")
                    .Invoke(null, new[] { objectsType.GetField("projectile").GetValue(launch), null });
                Assert.That(deferred, Is.Not.Null);
                Assert.That(GameObject.Find("RockShot(Clone)"), Is.Null);

                Type snapshots = Runtime("GameScene.Dto.SnapshotObjectDto").MakeArrayType();
                object snapshot = decode.MakeGenericMethod(snapshots).Invoke(null, new object[] {
                    "[{\"id\":31,\"prefab\":\"Towerback\",\"master\":\"LeftPlayer\"," +
                    "\"x\":9,\"y\":0,\"z\":5,\"status\":\"Attack\"}]"
                });
                syncer.GetType().GetMethod("Sync").Invoke(syncer, new[] { snapshot, deferred });
                Assert.That(find.Invoke(container, new object[] { 32 }), Is.Null);
                var actor = (Component)find.Invoke(container, new object[] { 31 });
                var body = (SpriteRenderer)actor.GetType().GetField("_spriteRenderer", Private).GetValue(actor);
                Assert.That(body.sprite.name, Is.EqualTo("TowerbackFire01"));
                GameObject projectile = GameObject.Find("RockShot(Clone)");
                Assert.That(projectile, Is.Not.Null);
                shot = projectile.transform;
                yield return null;
                Assert.That(shot != null, Is.True, "스냅샷에서 빠진 표적의 마지막 위치까지 탄이 이동한다.");
            }
            finally
            {
                container.GetType().GetMethod("Clear").Invoke(container, null);
                if (shot != null) UnityEngine.Object.Destroy(shot.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator StormStagRecordingUsesFourLegGaitAndOnlyBracesOnContact()
        {
            Create();
            const string magic = "storm_stag";
            Assert.That(Configure(magic), Is.True);
            var frames = (JArray)Recording(magic)["scenarios"][0]["frames"];
            int id = (int)frames.SelectMany(frame => frame["objects"]["create"])
                .First(created => (string)created["type"] == "StormStag")["id"];
            var impacts = frames.Where(frame => frame["events"]
                .Any(hit => (string)hit["type"] == "hit" && (int)hit["actorId"] == id))
                .Select(frame => (float)frame["time"]).Take(2).ToArray();
            Assert.That(impacts, Has.Length.EqualTo(2));
            Type config = Runtime("GameScene.GameConfig");
            MethodInfo setRate = config.GetMethod("SetTickRate");
            int previousTickRate = (int)config.GetProperty("TickRate").GetValue(null);
            int previousRenderRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            // 20Hz 기록은 60Hz 서버에 연결되어 있어도 기록 자체의 간격을 사용한다.
            setRate.Invoke(null, new object[] { 60 });
            try
            {
                string directory = System.IO.Path.Combine(Application.dataPath, "../Library/StormStagCaptures");
                bool canCapture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
                if (canCapture) System.IO.Directory.CreateDirectory(directory);

                yield return PlayUntil(impacts[0] - .18f);
                Component actor = Target(id);
                Assert.That(actor, Is.Not.Null);
                var renderer = (SpriteRenderer)actor.GetType().GetField("_spriteRenderer", Private).GetValue(actor);
                Component pose = actor.GetComponent("GroundedUnitPoseController");
                Assert.That(renderer.sprite.name, Does.StartWith("StormStagGallop"));
                Assert.That(renderer.flipX, Is.False);
                if (canCapture) SaveViewport(directory, "gallop", true);

                foreach (float impact in impacts)
                {
                    yield return PlayUntil(impact + .025f);
                    Assert.That(renderer.sprite.name, Is.EqualTo("StormStagImpact01"));
                    Assert.That(renderer.flipX, Is.False);
                    if (canCapture && impact == impacts[0]) SaveViewport(directory, "impact", true);
                    yield return PlayUntil(impact + .085f);
                    Assert.That(renderer.sprite.name, Is.EqualTo("StormStagImpact02"));
                    Assert.That(renderer.flipX, Is.False);
                    yield return PlayUntil(impact + .145f);
                    Assert.That(renderer.sprite.name, Is.EqualTo("StormStagImpact03"));
                    Assert.That(renderer.flipX, Is.False);
                    yield return PlayUntil(impact + .24f);
                    bool correcting = (bool)pose.GetType().GetField("correctingServerGround", Private).GetValue(pose);
                    Assert.That(renderer.sprite.name, Does.StartWith("StormStagGallop"),
                        $"충돌 {impact:F2}초 이후 좌표 {actor.transform.position}, 보정 {correcting}");
                    Assert.That(renderer.flipX, Is.True);
                    if (canCapture && impact == impacts[0]) SaveViewport(directory, "retreat", true);
                }
            }
            finally
            {
                Time.captureFramerate = previousRenderRate;
                setRate.Invoke(null, new object[] { previousTickRate });
            }
        }

        [UnityTest]
        public IEnumerator StormStagGaitDistinguishesRetreatFromCorrectionAtBothTickRates()
        {
            Create();
            Type config = Runtime("GameScene.GameConfig");
            MethodInfo setRate = config.GetMethod("SetTickRate");
            int previousTickRate = (int)config.GetProperty("TickRate").GetValue(null);
            int previousRenderRate = Time.captureFramerate;
            try
            {
                foreach (int tickRate in new[] { 20, 60 })
                foreach (int renderRate in new[] { 30, 60 })
                {
                    setRate.Invoke(null, new object[] { tickRate });
                    Time.captureFramerate = renderRate;
                    yield return CheckStormStagGait(tickRate);
                }
            }
            finally
            {
                Time.captureFramerate = previousRenderRate;
                setRate.Invoke(null, new object[] { previousTickRate });
            }
        }

        private IEnumerator CheckStormStagGait(int tickRate)
        {
            Component world = CreateWorld();
            Type info = Runtime("GameScene.Dto.ObjectsInfo");
            var decode = Runtime("Global.Serialization.JsonCodec").GetMethod("Deserialize").MakeGenericMethod(info);
            var apply = Runtime("GameScene.Object.PresentationFramePlayer").GetMethod("Apply");
            Type hitType = Runtime("GameScene.Dto.Event.HitEvent");
            object hit = Activator.CreateInstance(hitType);
            hitType.GetField("actorId").SetValue(hit, 41);
            hitType.GetField("targetId").SetValue(hit, 42);
            var events = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>)
                .MakeGenericType(Runtime("GameScene.Dto.Event.GameEvent")));
            events.Add(hit);

            void Apply(string json, bool withHit = false) =>
                apply.Invoke(null, new[] { decode.Invoke(null, new object[] { json }), withHit ? events : null, world });

            void Send(float x, string status = "Idle", string effects = "", bool withHit = false) =>
                Apply(FormattableString.Invariant(
                    $"{{\"update\":[{{\"id\":41,\"position\":{{\"x\":{x},\"y\":0,\"z\":5}},\"status\":\"{status}\",\"effects\":[{effects}]}}]}}"), withHit);

            Apply("{\"create\":[" +
                "{\"id\":41,\"type\":\"StormStag\",\"master\":\"LeftPlayer\",\"position\":{\"x\":5,\"y\":0,\"z\":5}}," +
                "{\"id\":42,\"type\":\"ElectricSlime\",\"master\":\"RightPlayer\",\"position\":{\"x\":22,\"y\":0,\"z\":5}}]}");
            Component actor = (Component)world.GetType().GetMethod("FindById").Invoke(world, new object[] { 41 });
            var renderer = (SpriteRenderer)actor.GetType().GetField("_spriteRenderer", Private).GetValue(actor);
            Assert.That(actor.GetComponent("HoppingMotionController"), Is.Null);
            Assert.That((bool)actor.GetType().GetField("_swingOnAttack", Private).GetValue(actor), Is.False);
            Sprite idle = renderer.sprite;
            Send(5f, "Attack");
            Assert.That(renderer.sprite, Is.SameAs(idle));
            Send(5f, "Hindered", "\"Panic\"");
            Assert.That(renderer.sprite, Is.SameAs(idle));
            yield return null;

            var seen = new System.Collections.Generic.HashSet<string>();
            float x = 5f;
            IEnumerator MoveFor(float seconds, Func<float, float> getSpeed, string effects = "")
            {
                seen.Clear();
                int sent = 0;
                int expected = Mathf.RoundToInt(seconds * tickRate);
                float start = Time.time;
                while (sent < expected)
                {
                    float elapsed = Time.time - start;
                    int due = Mathf.Min(expected, Mathf.FloorToInt(elapsed * tickRate + .001f));
                    while (sent < due)
                    {
                        sent++;
                        x += getSpeed((float)sent / tickRate) / tickRate;
                        Send(x, effects: effects);
                    }

                    yield return null;
                    seen.Add(renderer.sprite.name);
                }
            }

            yield return MoveFor(3f, elapsed => Mathf.Min(3f, 1.5f * elapsed));
            Assert.That(seen.Count(name => name.StartsWith("StormStagGallop", StringComparison.Ordinal)), Is.EqualTo(8));
            Assert.That(renderer.flipX, Is.False);
            Send(x, effects: "\"StormStagCharge2\"");
            Send(x, "Attack", "\"Panic\"", withHit: true);
            Assert.That(renderer.sprite.name, Is.EqualTo("StormStagImpact01"));
            yield return new WaitForSeconds(.09f);
            Assert.That(renderer.sprite.name, Is.EqualTo("StormStagImpact02").Or.EqualTo("StormStagImpact03"));
            Sprite currentImpact = renderer.sprite;
            Send(x, "Attack", "\"Panic\"");
            Assert.That(renderer.sprite, Is.SameAs(currentImpact));

            yield return MoveFor(1.2f, _ => -3.75f, "\"Panic\"");
            Assert.That(seen.Count(name => name.StartsWith("StormStagGallop", StringComparison.Ordinal)), Is.EqualTo(8));
            Assert.That(renderer.flipX, Is.True);
            Send(x + .02f);
            yield return null;
            Assert.That(renderer.flipX, Is.True);
            yield return MoveFor(.8f, _ => -7.5f, "\"Panic\"");
            Assert.That(seen.Count(name => name.StartsWith("StormStagGallop", StringComparison.Ordinal)), Is.GreaterThanOrEqualTo(7));
            Assert.That(renderer.flipX, Is.True);

            x += 6f;
            Send(x);
            for (int i = 0; i < 5; i++)
            {
                yield return null;
                Assert.That(renderer.sprite, Is.SameAs(idle));
                Assert.That(renderer.flipX, Is.True);
            }
            Send(x);
            yield return null;
            Assert.That(renderer.sprite, Is.SameAs(idle));
            UnityEngine.Object.Destroy(world.gameObject);
        }

        [UnityTest]
        public IEnumerator ChickenCommandoRecordingPlaysDescentLandingRunAndHits()
        {
            Create();
            const string magic = "chicken_commando";
            Assert.That(Configure(magic), Is.True);
            var frames = (JArray)Recording(magic)["scenarios"][0]["frames"];
            int id = (int)frames.SelectMany(frame => frame["objects"]["create"])
                .First(created => (string)created["type"] == "ChickenCommando")["id"];
            int previousRenderRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(0.61f);
                SpriteRenderer renderer = ServedObjectRenderer(Target(id));
                var appearances = new System.Collections.Generic.Dictionary<string, float>();
                var hitTimes = new System.Collections.Generic.List<float>();
                int captureIndex = 0;
                float[] captureTimes = { 1.19f, 1.41f, 2.36f, 3.39f };
                string[] captureNames = { "descent", "land", "run", "hit" };
                Sprite previous = null;
                float deadline = Time.realtimeSinceStartup + 15f;
                while ((float)Field("elapsed") < 4.42f && Time.realtimeSinceStartup < deadline)
                {
                    float time = (float)Field("elapsed");
                    Sprite pose = renderer.sprite;
                    if (pose != previous && pose.name == "ChickenCommandoAttack01")
                    {
                        hitTimes.Add(time);
                    }
                    previous = pose;
                    if (!appearances.ContainsKey(pose.name)) appearances.Add(pose.name, time);
                    if (captureIndex < captureTimes.Length && time >= captureTimes[captureIndex])
                    {
                        if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                        {
                            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/ChickenCommandoCaptures");
                            System.IO.Directory.CreateDirectory(directory);
                            SaveViewport(directory, captureNames[captureIndex], true);
                        }
                        captureIndex++;
                    }
                    yield return null;
                }

                Assert.That((float)Field("elapsed"), Is.GreaterThanOrEqualTo(4.42f));
                Assert.That(appearances["ChickenCommando"], Is.InRange(0.60f, 0.65f));
                foreach (var series in new[] { ("Descent", 2, 4), ("Land", 1, 4), ("Run", 1, 8), ("Attack", 1, 4) })
                {
                    float lastTime = -1f;
                    for (int i = series.Item2; i <= series.Item3; i++)
                    {
                        string poseName = $"ChickenCommando{series.Item1}{i:00}";
                        Assert.That(appearances.ContainsKey(poseName), Is.True, poseName);
                        Assert.That(appearances[poseName], Is.GreaterThan(lastTime), poseName);
                        lastTime = appearances[poseName];
                    }
                }
                Assert.That(appearances["ChickenCommandoLand01"], Is.InRange(1.30f, 1.41f));
                Assert.That(hitTimes, Has.Count.EqualTo(2));
                Assert.That(hitTimes[0], Is.EqualTo(3.35f).Within(0.04f));
                Assert.That(hitTimes[1] - hitTimes[0], Is.EqualTo(0.8f).Within(0.04f));
            }
            finally
            {
                Time.captureFramerate = previousRenderRate;
            }
        }

        [UnityTest]
        public IEnumerator ChickenCommandoGroundContactAndCorrectionAt20Hz() => CheckChickenCommandoGroundContact(20, 1);

        [UnityTest]
        public IEnumerator ChickenCommandoGroundContactAndCorrectionAt60Hz() => CheckChickenCommandoGroundContact(60, -1);

        private IEnumerator CheckChickenCommandoGroundContact(int tickRate, int direction)
        {
            Create();
            Component world = CreateWorld();
            world.transform.position = new Vector3(45f, 36f, -17f);
            world.transform.rotation = Quaternion.Euler(0f, 21f, 0f);
            world.transform.localScale = Vector3.one * 0.4f;

            const int renderRate = 120;
            Type config = Runtime("GameScene.GameConfig");
            MethodInfo setRate = config.GetMethod("SetTickRate");
            int previousTickRate = (int)config.GetProperty("TickRate").GetValue(null);
            int previousRenderRate = Time.captureFramerate;
            Type infoType = Runtime("GameScene.Dto.ObjectsInfo");
            MethodInfo decode = Runtime("Global.Serialization.JsonCodec").GetMethod("Deserialize").MakeGenericMethod(infoType);
            MethodInfo apply = Runtime("GameScene.Object.PresentationFramePlayer").GetMethod("Apply");
            MethodInfo find = world.GetType().GetMethod("FindById");
            string master = direction > 0 ? "LeftPlayer" : "RightPlayer";

            void Apply(string json) => apply.Invoke(null, new[] { decode.Invoke(null, new object[] { json }), null, world });
            Component Find(int id) => (Component)find.Invoke(world, new object[] { id });
            void Send(int id, float x, float y, string status = "Idle") => Apply(FormattableString.Invariant(
                $"{{\"update\":[{{\"id\":{id},\"master\":\"{master}\",\"status\":\"{status}\",\"position\":{{\"x\":{x},\"y\":{y},\"z\":4}}}}]}}"));
            SpriteRenderer Spawn(int id, float x, float y)
            {
                Apply(FormattableString.Invariant(
                    $"{{\"create\":[{{\"id\":{id},\"type\":\"ChickenCommando\",\"master\":\"{master}\",\"position\":{{\"x\":{x},\"y\":{y},\"z\":4}}}}]}}"));
                return ServedObjectRenderer(Find(id));
            }

            try
            {
                Time.captureFramerate = renderRate;
                setRate.Invoke(null, new object[] { tickRate });
                int rendersPerTick = renderRate / tickRate;
                float x = direction > 0 ? 4f : 14f;
                SpriteRenderer unit = Spawn(41, x, 0f);
                Sprite idle = Resources.Load<Sprite>("Game/drop/chicken_commando_default");
                Assert.That(unit.sprite, Is.SameAs(idle));
                Assert.That(unit.flipX, Is.EqualTo(direction < 0));
                Assert.That(idle.pivot.y, Is.Zero);
                Assert.That(idle.bounds.size.y, Is.EqualTo(1.28f).Within(0.001f));

                var run = new System.Collections.Generic.HashSet<string>();
                for (int tick = 0; tick < tickRate; tick++)
                {
                    x += direction * 2f / tickRate;
                    Send(41, x, 0f);
                    for (int render = 0; render < rendersPerTick; render++)
                    {
                        yield return null;
                        Assert.That(unit.sprite.pivot, Is.EqualTo(idle.pivot));
                        Assert.That(unit.sprite.pixelsPerUnit, Is.EqualTo(idle.pixelsPerUnit));
                        if (unit.sprite.name.StartsWith("ChickenCommandoRun")) run.Add(unit.sprite.name);
                    }
                }
                Assert.That(run, Has.Count.EqualTo(8));
                x += direction * 3f;
                Send(41, x, 0f);
                yield return null;
                yield return null;
                Assert.That(unit.sprite, Is.SameAs(idle));
                Send(41, x, 0.8f);
                yield return new WaitForSeconds(0.08f);
                Assert.That(unit.sprite, Is.SameAs(idle));
                Send(41, x, 0f);
                yield return new WaitForSeconds(0.08f);
                Assert.That(unit.sprite, Is.SameAs(idle));

                SpriteRenderer air = Spawn(42, x, 0.25f);
                Assert.That(air.sprite.name, Is.EqualTo("ChickenCommandoDescent04"));
                Assert.That(air.sprite.pivot.y, Is.Zero);
                Assert.That(air.sprite.bounds.size.y, Is.EqualTo(2.06f).Within(0.001f));
                Send(42, x, 0f, "Attack");
                var touchdown = new System.Collections.Generic.List<string>();
                for (int i = 0; i < rendersPerTick + 5; i++)
                {
                    yield return null;
                    touchdown.Add(air.sprite.name);
                }
                Assert.That(touchdown, Does.Contain("ChickenCommandoAttack01"));
                Assert.That(touchdown.Any(pose => pose.StartsWith("ChickenCommandoLand")), Is.False);
                yield return new WaitForSeconds(0.24f);
                Assert.That(air.sprite, Is.SameAs(idle));

                SpriteRenderer removed = Spawn(43, x, 0.25f);
                Send(43, x, 0f, "Attack");
                Send(43, x, 0f, "Destroyed");
                Assert.That(removed.sprite.name, Is.EqualTo("ChickenCommandoDescent04"));
                yield return new WaitForSeconds(0.12f);
                Assert.That(Find(43) == null, Is.True);
            }
            finally
            {
                Time.captureFramerate = previousRenderRate;
                setRate.Invoke(null, new object[] { previousTickRate });
            }
        }

        private static SpriteRenderer ServedObjectRenderer(Component servedObject)
        {
            Assert.That(servedObject, Is.Not.Null);
            var actual = (Transform)servedObject.GetType().GetMethod("GetActualTransform").Invoke(servedObject, null);
            return actual.GetComponent<SpriteRenderer>();
        }

        [UnityTest]
        public IEnumerator EvilEntBodyKeepsItsArmAttachedWhenLaunchingAndAfterRecovery()
        {
            Create();
            const string magic = "evil_ent";
            Assert.That(Configure(magic), Is.True);
            var scenarios = (JArray)Recording(magic)["scenarios"];
            int previousRenderRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                foreach (var attack in new[]
                {
                    (scenario: "pull", type: "EvilEntGrabArm", later: .53f),
                    (scenario: "heavy_punch", type: "EvilEntPunchArm", later: .41f)
                })
                {
                    int index = scenarios.IndexOf(scenarios.First(s => (string)s["id"] == attack.scenario));
                    Scenario(index);
                    var shot = FirstProjectile(scenarios[index], attack.type);
                    int casterId = (int)scenarios[index]["frames"].SelectMany(frame => frame["objects"]["create"])
                        .First(unit => (string)unit["type"] == "EvilEnt")["id"];
                    yield return PlayUntil(shot.start + .12f);
                    Component caster = Target(casterId);
                    Assert.That(caster, Is.Not.Null);
                    var body = (SpriteRenderer)caster.GetType().GetField("_spriteRenderer", Private).GetValue(caster);
                    Assert.That(body.sprite.name, Is.EqualTo("EvilEntBrace02"));
                    AssertEvilEntArmMount(attack.type, body);
                    if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                    {
                        string directory = System.IO.Path.Combine(Application.dataPath, "../Library/EvilEntBodyCaptures");
                        System.IO.Directory.CreateDirectory(directory);
                        SaveViewport(directory, attack.scenario + "-launch", true);
                    }

                    yield return PlayUntil(shot.start + attack.later);
                    Assert.That(body.sprite.name, Is.EqualTo("EvilEnt"));
                    AssertEvilEntArmMount(attack.type, body);
                    if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                    {
                        string directory = System.IO.Path.Combine(Application.dataPath, "../Library/EvilEntBodyCaptures");
                        SaveViewport(directory, attack.scenario + "-contact", true);
                    }
                }
            }
            finally
            {
                Time.captureFramerate = previousRenderRate;
            }
        }

        private void AssertEvilEntArmMount(string type, SpriteRenderer body)
        {
            var projectile = FindProjectile(type);
            Assert.That(projectile, Is.Not.Null);
            Assert.That(projectile.Find("Tip").GetComponent<SpriteRenderer>().enabled, Is.True);
            Assert.That(Quaternion.Angle(body.transform.localRotation, Quaternion.identity), Is.LessThan(.01f));
            var camera = (Camera)Field("previewCamera");
            // 두 상태의 몸통 그림은 피벗에서 1.55칸 위에 팔 시작점을 둔다.
            Vector3 mount = body.transform.TransformPoint(new Vector3(0f, 1.55f, 0f));
            float gap = Vector2.Distance(camera.WorldToViewportPoint(mount), camera.WorldToViewportPoint(projectile.position));
            Assert.That(gap, Is.LessThan(.02f));
        }

        [UnityTest]
        public IEnumerator EvilEntPunchRecordingPlaysAllVisibleTipsAtNativeTiming()
        {
            Create();
            const string magic = "evil_ent";
            Assert.That(Configure(magic), Is.True);
            var scenarios = (JArray)Recording(magic)["scenarios"];
            int index = scenarios.IndexOf(scenarios.First(s => (string)s["id"] == "heavy_punch"));
            Scenario(index);
            var shot = FirstProjectile(scenarios[index], "EvilEntPunchArm");
            float hitStart = shot.start + shot.duration * 0.55f;
            int previousRenderRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(shot.start + 0.03f);
                Transform projectile = FindProjectile("EvilEntPunchArm");
                Assert.That(projectile, Is.Not.Null);
                var arm = projectile.Find("Arm").GetComponent<SpriteRenderer>();
                var tip = projectile.Find("Tip").GetComponent<SpriteRenderer>();
                var camera = (Camera)Field("previewCamera");
                Component victim = Target(shot.targetId);
                MethodInfo getAimPoint = victim.GetType().GetMethod("GetEdgeWorldPositionTowards");
                Sprite reference = tip.sprite;
                Assert.That(arm.drawMode, Is.EqualTo(SpriteDrawMode.Tiled));
                Assert.That(arm.sprite.rect.size, Is.EqualTo(new Vector2(64, 96)));
                Assert.That(reference.rect.size, Is.EqualTo(new Vector2(120, 80)));
                Assert.That(reference.pivot, Is.EqualTo(new Vector2(0, 40)));
                Assert.That(reference.pixelsPerUnit, Is.EqualTo(100f));

                var appeared = new System.Collections.Generic.Dictionary<string, float>();
                float peak = 0f, impactLength = 0f, retractedLength = 0f;
                float impactGap = float.PositiveInfinity;
                float[] capturesAt = { .34f, .52f, .57f, .74f, .93f };
                int captureIndex = 0;
                float deadline = Time.realtimeSinceStartup + 15f;
                while (projectile != null && Time.realtimeSinceStartup < deadline)
                {
                    float time = (float)Field("elapsed");
                    if (tip.enabled)
                    {
                        if (!appeared.ContainsKey(tip.sprite.name))
                            appeared.Add(tip.sprite.name, time);
                        Assert.That(tip.sprite.pivot, Is.EqualTo(reference.pivot));
                        Assert.That(tip.sprite.rect, Is.EqualTo(reference.rect));
                        Assert.That(tip.transform.localPosition.x, Is.EqualTo(arm.size.x).Within(0.001f));
                        peak = Mathf.Max(peak, arm.size.x);
                        if (tip.sprite.name == "evil_ent_arm_fist_03" && arm.size.x > impactLength)
                        {
                            impactLength = arm.size.x;
                            Vector3 target = (Vector3)getAimPoint.Invoke(victim, new object[] { projectile.position, 0f });
                            Vector3 knuckles = tip.transform.TransformPoint(new Vector3(1.05f, 0f, 0f));
                            impactGap = Vector2.Distance(camera.WorldToViewportPoint(target), camera.WorldToViewportPoint(knuckles));
                        }
                        if (time >= shot.start + shot.duration * .9f)
                            retractedLength = arm.size.x;
                    }

                    if (captureIndex < capturesAt.Length && time >= shot.start + shot.duration * capturesAt[captureIndex])
                    {
                        if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                        {
                            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/EvilEntPunchCaptures");
                            System.IO.Directory.CreateDirectory(directory);
                            SaveViewport(directory, $"punch-{captureIndex + 1:00}", true);
                        }
                        captureIndex++;
                    }
                    yield return null;
                }

                CollectionAssert.AreEqual(new[]
                {
                    "evil_ent_arm_fist", "evil_ent_arm_fist_02", "evil_ent_arm_fist_03", "evil_ent_arm_fist_04"
                }, appeared.OrderBy(pair => pair.Value).Select(pair => pair.Key));
                Assert.That(appeared["evil_ent_arm_fist_03"], Is.EqualTo(hitStart).Within(.035f));
                Assert.That(impactLength, Is.GreaterThanOrEqualTo(peak * .95f));
                Assert.That(impactGap, Is.LessThan(.04f));
                Assert.That(retractedLength, Is.GreaterThan(0f).And.LessThan(peak * .9f));
                Assert.That(projectile == null, Is.True);
            }
            finally
            {
                Time.captureFramerate = previousRenderRate;
            }
        }

        [UnityTest]
        public IEnumerator EvilEntGrabHoldsVictimThroughLongAndMinimumPulls()
        {
            Create();
            const string magic = "evil_ent";
            Assert.That(Configure(magic), Is.True);
            var scenarios = (JArray)Recording(magic)["scenarios"];
            int index = scenarios.IndexOf(scenarios.First(s => (string)s["id"] == "pull"));
            Scenario(index);
            var shots = scenarios[index]["frames"].SelectMany(frame => frame["objects"]["projectile"]
                    .Where(projectile => (string)projectile["type"] == "EvilEntGrabArm")
                    .Select(projectile => (start: (float)frame["time"], duration: (float)projectile["duration"])))
                .Take(2).ToArray();
            Assert.That(shots.Length, Is.EqualTo(2));
            Assert.That(shots[1].duration * .78f, Is.EqualTo(.8f).Within(.001f));
            int previousRenderRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                for (int shotIndex = 0; shotIndex < shots.Length; shotIndex++)
                {
                    var shot = shots[shotIndex];
                    yield return PlayUntil(shot.start + .04f);
                    Transform projectile = FindProjectile("EvilEntGrabArm");
                    Assert.That(projectile, Is.Not.Null);
                    var arm = projectile.Find("Arm").GetComponent<SpriteRenderer>();
                    var tip = projectile.Find("Tip").GetComponent<SpriteRenderer>();
                    var firstSeen = new System.Collections.Generic.Dictionary<string, float>();
                    float contactLength = 0f, shortestGrip = float.PositiveInfinity;
                    float[] captureAt = { .10f, .235f, .49f, .91f };
                    int capture = 0;
                    float deadline = Time.realtimeSinceStartup + 15f;
                    while (projectile != null && Time.realtimeSinceStartup < deadline)
                    {
                        float age = (float)Field("elapsed") - shot.start;
                        Sprite pose = tip.sprite;
                        if (tip.enabled)
                        {
                            if (!firstSeen.ContainsKey(pose.name)) firstSeen.Add(pose.name, age);
                            Assert.That(pose.rect.size, Is.EqualTo(new Vector2(120, 80)));
                            Assert.That(pose.pivot, Is.EqualTo(new Vector2(0, 40)));
                            Assert.That(tip.transform.localPosition.x, Is.EqualTo(arm.size.x).Within(.001f));
                            if (pose.name == "evil_ent_grab_claw_02") contactLength = Mathf.Max(contactLength, arm.size.x);
                            if (pose.name == "evil_ent_grab_claw_03") shortestGrip = Mathf.Min(shortestGrip, arm.size.x);
                        }
                        if (shotIndex == 0 && capture < captureAt.Length && age >= shot.duration * captureAt[capture])
                        {
                            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                            {
                                string directory = System.IO.Path.Combine(Application.dataPath, "../Library/EvilEntGrabCaptures");
                                System.IO.Directory.CreateDirectory(directory);
                                SaveViewport(directory, $"grab-{capture + 1:00}", true);
                            }
                            capture++;
                        }
                        yield return null;
                    }

                    Assert.That(projectile == null, Is.True);
                    string[] expected = { "evil_ent_grab_claw", "evil_ent_grab_claw_02", "evil_ent_grab_claw_03", "evil_ent_grab_claw_04" };
                    CollectionAssert.AreEqual(expected.Take(shotIndex == 0 ? 4 : 3),
                        firstSeen.OrderBy(pair => pair.Value).Select(pair => pair.Key));
                    Assert.That(firstSeen["evil_ent_grab_claw_02"], Is.EqualTo(shot.duration * .22f).Within(.04f));
                    if (shotIndex == 0)
                    {
                        Assert.That(shortestGrip, Is.LessThan(contactLength * .8f));
                        Assert.That(firstSeen["evil_ent_grab_claw_04"], Is.GreaterThan(.8f + shot.duration * .22f));
                    }
                }
            }
            finally
            {
                Time.captureFramerate = previousRenderRate;
            }
        }

        [UnityTest]
        public IEnumerator EvilEntFireFistShowsImpactInShortRecordedAttack()
        {
            Create();
            const string magic = "evil_ent";
            Assert.That(Configure(magic), Is.True);
            var scenarios = (JArray)Recording(magic)["scenarios"];
            int index = scenarios.IndexOf(scenarios.First(s => (string)s["id"] == "pull"));
            var shot = FirstProjectile(scenarios[index], "EvilEntFireFist");
            Assert.That(shot.duration, Is.InRange(.15f, .17f));

            int previousRenderRate = Time.captureFramerate;
            try
            {
                foreach (int renderRate in new[] { 240, 60 })
                {
                    Time.captureFramerate = renderRate;
                    Scenario(index);
                    yield return PlayUntil(shot.start + .005f);
                    Transform projectile = FindProjectile("EvilEntFireFist");
                    Assert.That(projectile, Is.Not.Null);
                    var tip = projectile.Find("Tip").GetComponent<SpriteRenderer>();
                    var arm = projectile.Find("Arm").GetComponent<SpriteRenderer>();
                    Assert.That(arm.sprite.rect.size, Is.EqualTo(new Vector2(64, 96)));
                    Assert.That(arm.drawMode, Is.EqualTo(SpriteDrawMode.Tiled));
                    var seen = new System.Collections.Generic.Dictionary<string, float>();
                    int impactRenders = 0;
                    float deadline = Time.realtimeSinceStartup + 10f;
                    while (projectile != null && Time.realtimeSinceStartup < deadline)
                    {
                        if (tip.enabled)
                        {
                            float age = (float)Field("elapsed") - shot.start;
                            Sprite pose = tip.sprite;
                            Assert.That(pose.rect.size, Is.EqualTo(new Vector2(120, 92)));
                            Assert.That(pose.pivot, Is.EqualTo(new Vector2(0, 46)));
                            Assert.That(tip.transform.localPosition.x, Is.EqualTo(arm.size.x).Within(.001f));
                            if (pose.name == "evil_ent_burn_fist_03") impactRenders++;
                            if (!seen.ContainsKey(pose.name))
                            {
                                seen.Add(pose.name, age);
                                if (renderRate == 240 && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                                {
                                    string directory = System.IO.Path.Combine(Application.dataPath, "../Library/EvilEntFireFistCaptures");
                                    System.IO.Directory.CreateDirectory(directory);
                                    SaveViewport(directory, $"fire-fist-{seen.Count:00}", true);
                                }
                            }
                        }
                        yield return null;
                    }
                    Assert.That(projectile == null, Is.True);
                    Assert.That(impactRenders, Is.GreaterThanOrEqualTo(2), $"Visible impact at {renderRate} Hz");
                    if (renderRate == 240)
                    {
                        CollectionAssert.AreEqual(new[]
                        {
                            "evil_ent_burn_fist", "evil_ent_burn_fist_02", "evil_ent_burn_fist_03", "evil_ent_burn_fist_04"
                        }, seen.OrderBy(entry => entry.Value).Select(entry => entry.Key));
                        Assert.That(seen["evil_ent_burn_fist_03"], Is.EqualTo(shot.duration * .45f).Within(.017f));
                        Assert.That(seen["evil_ent_burn_fist_04"], Is.EqualTo(shot.duration * .8f).Within(.017f));
                    }
                }
            }
            finally
            {
                Time.captureFramerate = previousRenderRate;
            }
        }

        [UnityTest]
        public IEnumerator EvilEntPunchKeepsVisibleReachAtCloseRange()
        {
            Create();
            Component world = CreateWorld();
            var camera = (Camera)world.GetType().GetProperty("Camera").GetValue(world);
            Vector3 start = new Vector3(2f, 0f, 4f);
            Vector3 end = start + camera.transform.up * .95f + camera.transform.right * .35f;
            Type infoType = Runtime("GameScene.Dto.ObjectsInfo");
            MethodInfo decode = Runtime("Global.Serialization.JsonCodec").GetMethod("Deserialize").MakeGenericMethod(infoType);
            MethodInfo apply = Runtime("GameScene.Object.PresentationFramePlayer").GetMethod("Apply");
            object frame = decode.Invoke(null, new object[] { FormattableString.Invariant(
                $"{{\"projectile\":[{{\"type\":\"EvilEntPunchArm\",\"duration\":1,\"start\":{{\"targetType\":\"position\",\"x\":{start.x},\"y\":{start.y},\"z\":{start.z}}},\"end\":{{\"targetType\":\"position\",\"x\":{end.x},\"y\":{end.y},\"z\":{end.z}}}}}]}}") });
            apply.Invoke(null, new[] { frame, null, world });
            Transform projectile = world.transform.Find("PresentationContent/EvilEntPunchArm(Clone)");
            Assert.That(projectile, Is.Not.Null);
            yield return new WaitForSeconds(.56f);
            var arm = projectile.Find("Arm").GetComponent<SpriteRenderer>();
            var tip = projectile.Find("Tip").GetComponent<SpriteRenderer>();
            Assert.That(arm.size.x, Is.InRange(.07f, .10f));
            Assert.That(tip.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator SeaSerpentGlidesAndCompletesHydroPumpInRecordedFights()
        {
            Create();
            const string magic = "sea_serpent";
            Assert.That(Configure(magic), Is.True);
            var scenarios = (JArray)Recording(magic)["scenarios"];
            const string beamType = "SeaSerpentHydroPump";
            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/SeaSerpentCaptures");
            if (capture) System.IO.Directory.CreateDirectory(directory);
            try
            {
                for (int i = 0; i < scenarios.Count; i++)
                {
                    var scenario = scenarios[i];
                    string name = (string)scenario["id"];
                    var frames = scenario["frames"];
                    int id = (int)frames.SelectMany(f => f["objects"]["create"])
                        .First(obj => (string)obj["type"] == "SeaSerpent")["id"];
                    int opponentId = (int)scenario["fixtureTargetIds"][0];
                    var first = frames.First(f => f["objects"]["projectile"].Any(p => (string)p["type"] == beamType));
                    float shotTime = (float)first["time"];
                    Scenario(i);
                    yield return PlayUntil(.73f);
                    Component actor = Target(id);
                    var body = (SpriteRenderer)actor.GetType().GetField("_spriteRenderer", Private).GetValue(actor);
                    Transform trail = actor.transform.Find("SeaSerpentBody");
                    Assert.That(trail.childCount, Is.EqualTo(5));
                    Assert.That(actor.GetComponent("HoppingMotionController"), Is.Null);
                    if (name == "water_trail")
                    {
                        var seen = new System.Collections.Generic.HashSet<string>();
                        Vector3 originalTail = trail.GetChild(4).position;
                        while ((float)Field("elapsed") < 2.58f)
                        {
                            if (body.sprite.name.StartsWith("SeaSerpentGlide", StringComparison.Ordinal) &&
                                seen.Add(body.sprite.name) && capture)
                                SaveViewport(directory, body.sprite.name, true);
                            yield return null;
                        }
                        Assert.That(seen.Count, Is.EqualTo(8));
                        Assert.That(Vector3.Distance(trail.GetChild(4).position, originalTail), Is.GreaterThan(.5f));
                    }

                    yield return PlayUntil(shotTime - .024f);
                    Assert.That(body.sprite.name, Is.EqualTo("SeaSerpentHead"));
                    Assert.That(FindProjectile(beamType), Is.Null);
                    Component opponent = Target(opponentId);
                    float before = CurrentHp(opponent);

                    for (int frame = 0; frame < 4; frame++)
                    {
                        yield return PlayUntil(shotTime + .034f + frame * .0875f);
                        Assert.That(body.sprite.name, Is.EqualTo($"SeaSerpentFire{frame+1:00}"), name);
                        Assert.That(CurrentHp(opponent), Is.EqualTo(before), name);
                        Transform beam = FindProjectile(beamType);
                        Assert.That(beam, Is.Not.Null);
                        var camera = (Camera)Field("previewCamera");
                        Vector3 mouth = body.transform.TransformPoint(new Vector3(.36f, 1.23f, 0f));
                        Assert.That(Vector2.Distance(camera.WorldToViewportPoint(beam.position),
                            camera.WorldToViewportPoint(mouth)), Is.LessThan(.012f), name);
                        if (capture && ((frame == 0 && name == "water_trail") || frame == 1))
                            SaveViewport(directory, name + $"-fire-{frame+1}", true);
                    }

                    yield return PlayUntil(shotTime + .385f);
                    Assert.That(body.sprite.name, Is.EqualTo("SeaSerpentHead"), name);
                    Assert.That(FindProjectile(beamType), Is.Null);
                    Assert.That(CurrentHp(opponent), Is.EqualTo(before - 100f), name);
                }
            }
            finally
            {
                Time.captureFramerate = previousRate;
            }
        }

        [UnityTest]
        public IEnumerator SeaSerpentConnectsBothBeamsAndKeepsUnmatchedMuzzlesAtTheServerOrigin()
        {
            Create();
            Component world = CreateWorld();
            Type dtoType = Runtime("GameScene.Dto.ObjectsInfo");
            MethodInfo decode = Runtime("Global.Serialization.JsonCodec").GetMethod("Deserialize").MakeGenericMethod(dtoType);
            MethodInfo apply = Runtime("GameScene.Object.PresentationFramePlayer").GetMethod("Apply");
            MethodInfo find = world.GetType().GetMethod("FindById");
            void Apply(JObject packet) =>
                apply.Invoke(null, new[] { decode.Invoke(null, new object[] { packet.ToString() }), null, world });
            JObject Created(int id, float x, float z, string master) =>
                JObject.FromObject(new { id, type = "SeaSerpent", master, position = new { x, y = 0, z } });
            JObject Attacking(int id, float x, float z) =>
                JObject.FromObject(new { id, status = "Attack", position = new { x, y = 0, z } });
            JObject Beam(float x, float z, float endX, float endZ) =>
                JObject.FromObject(new { type = "SeaSerpentHydroPump", duration = .35f,
                    start = new { targetType = "position", x, y = 1.6f, z },
                    end = new { targetType = "position", x = endX, y = 0, z = endZ } });
            SpriteRenderer Body(int id)
            {
                var actor = (Component)find.Invoke(world, new object[] { id });
                return (SpriteRenderer)actor.GetType().GetField("_spriteRenderer", Private).GetValue(actor);
            }
            Transform[] Beams() => world.transform.Find("PresentationContent").Cast<Transform>()
                .Where(item => item.name == "SeaSerpentHydroPump(Clone)").ToArray();

            Apply(new JObject { ["create"] = new JArray(Created(41, 6, 4, "LeftPlayer"),
                Created(42, 9, 6, "RightPlayer"), Created(43, 6, 4, "LeftPlayer")) });
            SpriteRenderer left = Body(41), right = Body(42);
            Assert.That(left.flipX, Is.False);
            Assert.That(right.flipX, Is.True);
            Apply(new JObject
            {
                ["update"] = new JArray(Attacking(41, 6, 4), Attacking(42, 9, 6)),
                ["projectile"] = new JArray(Beam(6, 4, 2, 8), Beam(9, 6, 13, 4))
            });

            Transform[] shots = Beams();
            Assert.That(shots, Has.Length.EqualTo(2));
            Assert.That(left.flipX && !right.flipX, Is.True);
            Assert.That(left.sprite.name, Is.EqualTo("SeaSerpentFire01"));
            Assert.That(right.sprite.name, Is.EqualTo("SeaSerpentFire01"));
            Assert.That(Vector3.Distance(shots[0].position,
                left.transform.TransformPoint(new Vector3(-.36f, 1.23f, 0f))), Is.LessThan(.005f));
            Assert.That(Vector3.Distance(shots[1].position,
                right.transform.TransformPoint(new Vector3(.36f, 1.23f, 0f))), Is.LessThan(.005f));
            yield return null;
            yield return null;
            Assert.That(left.flipX && !right.flipX, Is.True);

            Apply(new JObject
            {
                ["update"] = new JArray(Attacking(41, 6, 4), Attacking(43, 6, 4)),
                ["projectile"] = new JArray(Beam(6, 4, 11, 4), Beam(7, 5, 11, 5))
            });
            shots = Beams();
            Assert.That(shots, Has.Length.EqualTo(4));
            Assert.That(Vector3.Distance(shots[2].position, world.transform.TransformPoint(new Vector3(6, 1.6f, 4))), Is.LessThan(.005f));
            Assert.That(Vector3.Distance(shots[3].position, world.transform.TransformPoint(new Vector3(7, 1.6f, 5))), Is.LessThan(.005f));
        }


        [UnityTest]
        public IEnumerator SeaSerpentSnapshotUsesTheSameMouthAsDeltaPlayback()
        {
            root = new GameObject("SeaSerpent snapshot fixture");
            Component container = root.AddComponent(Runtime("GameScene.Object.ObjectContainer"));
            root.AddComponent(Runtime("GameScene.Object.ObjectUpdater"));
            Component syncer = root.AddComponent(Runtime("GameScene.Object.ObjectSyncer"));
            Type dtoType = Runtime("GameScene.Dto.ObjectsInfo");
            MethodInfo decode = Runtime("Global.Serialization.JsonCodec").GetMethod("Deserialize");
            MethodInfo decodeObjects = decode.MakeGenericMethod(dtoType);
            GameObject projectile = null;
            try
            {
                object initial = decodeObjects.Invoke(null, new object[] {
                    "{\"create\":[{\"id\":51,\"type\":\"SeaSerpent\",\"master\":\"LeftPlayer\"," +
                    "\"position\":{\"x\":8,\"y\":0,\"z\":5}}]}" });
                Runtime("GameScene.Object.PresentationFramePlayer").GetMethod("Apply")
                    .Invoke(null, new[] { initial, null, null });
                object launch = decodeObjects.Invoke(null, new object[] {
                    "{\"projectile\":[{\"type\":\"SeaSerpentHydroPump\",\"duration\":0.35," +
                    "\"start\":{\"targetType\":\"position\",\"x\":8,\"y\":1.6,\"z\":5}," +
                    "\"end\":{\"targetType\":\"position\",\"x\":4,\"y\":2,\"z\":5}}]}" });
                object pending = Runtime("GameScene.Object.ProjectileSpawner").GetMethod("SpawnBeforeObjectUpdates")
                    .Invoke(null, new[] { dtoType.GetField("projectile").GetValue(launch), null });
                object snapshot = decode.MakeGenericMethod(Runtime("GameScene.Dto.SnapshotObjectDto").MakeArrayType())
                    .Invoke(null, new object[] { "[{\"id\":51,\"prefab\":\"SeaSerpent\",\"master\":\"LeftPlayer\"," +
                        "\"x\":8,\"y\":0,\"z\":5,\"status\":\"Attack\"}]" });
                syncer.GetType().GetMethod("Sync").Invoke(syncer, new[] { snapshot, pending });
                var owner = (Component)container.GetType().GetMethod("FindById").Invoke(container, new object[] { 51 });
                var head = (SpriteRenderer)owner.GetType().GetField("_spriteRenderer", Private).GetValue(owner);
                projectile = GameObject.Find("SeaSerpentHydroPump(Clone)");
                Assert.That(projectile, Is.Not.Null);
                Assert.That(head.flipX, Is.True);
                Assert.That(head.sprite.name, Is.EqualTo("SeaSerpentFire01"));
                Vector3 mouth = head.transform.TransformPoint(new Vector3(-.36f, 1.23f, 0f));
                Assert.That(Vector3.Distance(projectile.transform.position, mouth), Is.LessThan(.005f));
                yield return null;
            }
            finally
            {
                container.GetType().GetMethod("Clear").Invoke(container, null);
                if (projectile != null) UnityEngine.Object.Destroy(projectile);
            }
        }


        [UnityTest]
        public IEnumerator DimensionToadRetreatsAndKeepsItsAlternatingSummons()
        {
            Create();
            const string magic = "dimension_toad";
            Assert.That(Configure(magic), Is.True);
            var scenarios = (JArray)Recording(magic)["scenarios"];
            int oldRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/DimensionToadCaptures");
            if (capture) System.IO.Directory.CreateDirectory(directory);
            try
            {
                for (int index = 0; index < scenarios.Count; index++)
                {
                    string name = (string)scenarios[index]["id"];
                    var frames = scenarios[index]["frames"];
                    int toadId = (int)frames.SelectMany(f => f["objects"]["create"])
                        .First(obj => (string)obj["type"] == "DimensionToad")["id"];
                    var arrivals = frames.SelectMany(f => f["objects"]["create"]
                        .Where(obj => (string)obj["type"] == "FireTadpole" || (string)obj["type"] == "LightningTadpole")
                        .Select(obj => (time: (float)f["time"], id: (int)obj["id"], type: (string)obj["type"])))
                        .Take(4).ToArray();
                    Assert.That(arrivals.Select(a => a.type), Is.EqualTo(new[] {
                        "FireTadpole", "LightningTadpole", "FireTadpole", "LightningTadpole" }));
                    Scenario(index);
                    yield return PlayUntil(.668f);
                    Component toad = Target(toadId);
                    var body = (SpriteRenderer)toad.GetType().GetField("_spriteRenderer", Private).GetValue(toad);
                    Assert.That(body.sprite.name, Is.EqualTo("DimensionToad"));
                    Assert.That(toad.GetComponent("WaddleMotionController"), Is.Null);

                    for (int i = 0; i < arrivals.Length; i++)
                    {
                        if (i == 1 && name == "panic")
                        {
                            var seen = new System.Collections.Generic.HashSet<string>();
                            var phases = new System.Collections.Generic.List<int>();
                            yield return PlayUntil(.77f);
                            Assert.That(body.flipX, Is.True);
                            while ((float)Field("elapsed") < 1.46f)
                            {
                                string pose = body.sprite.name;
                                if (pose.StartsWith("DimensionToadRetreat", StringComparison.Ordinal))
                                {
                                    int phase = int.Parse(pose.Substring("DimensionToadRetreat".Length));
                                    if (phases.Count == 0 || phases[phases.Count - 1] != phase) phases.Add(phase);
                                    if (seen.Add(pose) && capture) SaveViewport(directory, pose, true);
                                }
                                yield return null;
                            }
                            Assert.That(seen.Count, Is.EqualTo(8));
                            var transitions = phases.Zip(phases.Skip(1), (from, to) => (from, to)).ToArray();
                            CollectionAssert.Contains(transitions, (4, 5));
                            CollectionAssert.Contains(transitions, (8, 1));
                        }

                        var arrival = arrivals[i];
                        Assert.That(arrival.time, Is.EqualTo(.65f + i * .8f).Within(.002f));
                        yield return PlayUntil(arrival.time + .018f);
                        Assert.That(Target(arrival.id), Is.Not.Null, $"{name} {arrival.type}");
                        if (i == 1)
                        {
                            if (capture) SaveViewport(directory, name + "-summon", true);
                            yield return PlayUntil(1.62f);
                            Assert.That(body.sprite.name, Is.EqualTo("DimensionToad"));
                        }
                    }
                }
            }
            finally
            {
                Time.captureFramerate = oldRate;
            }
        }

        [UnityTest]
        public IEnumerator DimensionToadRunsInBothDirectionsAndIgnoresCorrectionsAtBothTickRates()
        {
            Create();
            Type config = Runtime("GameScene.GameConfig");
            MethodInfo setRate = config.GetMethod("SetTickRate");
            int oldTick = (int)config.GetProperty("TickRate").GetValue(null);
            int oldRender = Time.captureFramerate;
            const int renderRate = 120;
            Time.captureFramerate = renderRate;
            try
            {
                foreach (int tick in new[] { 20, 60 })
                {
                    setRate.Invoke(null, new object[] { tick });
                    Component world = CreateWorld();
                    Type dto = Runtime("GameScene.Dto.ObjectsInfo");
                    MethodInfo decode = Runtime("Global.Serialization.JsonCodec").GetMethod("Deserialize").MakeGenericMethod(dto);
                    MethodInfo apply = Runtime("GameScene.Object.PresentationFramePlayer").GetMethod("Apply");
                    void Apply(string json) =>
                        apply.Invoke(null, new[] { decode.Invoke(null, new object[] { json }), null, world });
                    void MoveTo(float position) => Apply(FormattableString.Invariant(
                        $"{{\"update\":[{{\"id\":61,\"position\":{{\"x\":{position},\"y\":0,\"z\":5}},\"status\":\"Idle\"}}]}}"));

                    Apply("{\"create\":[{\"id\":61,\"type\":\"DimensionToad\",\"master\":\"LeftPlayer\"," +
                        "\"position\":{\"x\":9,\"y\":0,\"z\":5}}]}");
                    var actor = (Component)world.GetType().GetMethod("FindById").Invoke(world, new object[] { 61 });
                    var sprite = (SpriteRenderer)actor.GetType().GetField("_spriteRenderer", Private).GetValue(actor);
                    float x = 9f;
                    yield return null;
                    foreach (float speed in new[] { -.65f, 1.3f })
                    {
                        var seen = new System.Collections.Generic.HashSet<Sprite>();
                        int packets = Mathf.CeilToInt(2.6f / Mathf.Abs(speed) * tick);
                        for (int n = 0; n < packets; n++)
                        {
                            x += speed / tick;
                            MoveTo(x);
                            for (int render = 0; render < renderRate / tick; render++)
                            {
                                yield return null;
                                if (sprite.flipX == (speed < 0) &&
                                    sprite.sprite.name.StartsWith("DimensionToadRetreat", StringComparison.Ordinal))
                                    seen.Add(sprite.sprite);
                            }
                            if (n == 2) Assert.That(sprite.flipX, Is.EqualTo(speed < 0), $"{tick} Hz 방향");
                        }
                        Assert.That(seen.Count, Is.EqualTo(8), $"{tick} Hz, {speed}칸/초");
                        yield return new WaitForSeconds(.13f);
                        Assert.That(sprite.sprite.name, Is.EqualTo("DimensionToad"));

                        x += 4f;
                        MoveTo(x);
                        for (int render = 0; render < renderRate / tick + 2; render++)
                        {
                            yield return null;
                            Assert.That(sprite.sprite.name, Is.EqualTo("DimensionToad"));
                        }
                    }
                    UnityEngine.Object.Destroy(world.gameObject);
                    yield return null;
                }
            }
            finally
            {
                Time.captureFramerate = oldRender;
                setRate.Invoke(null, new object[] { oldTick });
            }
        }


        [UnityTest]
        public IEnumerator MiniRockWalksAndHitsOnTheDamageTickInAllThreeRecordings()
        {
            Create();
            var clips = new[] {
                (spell: "mini_rock_swarm", scenario: "ground", team: "LeftPlayer", start: .72f, stop: 1.54f, impact: 2.30f, hp: 1000f, count: 2),
                (spell: "earth_call", scenario: "small", team: "LeftPlayer", start: 1.13f, stop: 1.50f, impact: 2.20f, hp: 1000f, count: 1),
                (spell: "will_o_wisp", scenario: "control", team: "RightPlayer", start: 1.27f, stop: 1.96f, impact: 2.70f, hp: 10000f, count: 1)
            };
            int priorRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/MiniRockCaptures");
            if (capture) System.IO.Directory.CreateDirectory(directory);
            try
            {
                foreach (var clip in clips)
                {
                    Assert.That(Configure(clip.spell), Is.True);
                    var scenarios = (JArray)Recording(clip.spell)["scenarios"];
                    var scenario = scenarios.Single(item => (string)item["id"] == clip.scenario);
                    Scenario(scenarios.IndexOf(scenario));
                    var frames = scenario["frames"];
                    int[] ids = frames.SelectMany(f => f["objects"]["create"])
                        .Where(c => (string)c["type"] == "MiniRock" && (string)c["master"] == clip.team)
                        .Select(c => (int)c["id"]).ToArray();
                    Assert.That(ids, Has.Length.EqualTo(clip.count));
                    var damage = frames.First(f => f["events"].Any(e =>
                        (string)e["type"] == "hit" && ids.Contains((int)e["actorId"])));
                    Assert.That((float)damage["time"], Is.EqualTo(clip.impact).Within(.001f));
                    var hits = damage["events"].Where(e => (string)e["type"] == "hit" &&
                        ids.Contains((int)e["actorId"])).ToArray();
                    CollectionAssert.AreEquivalent(ids, hits.Select(e => (int)e["actorId"]));
                    int victim = hits.Select(e => (int)e["targetId"]).Distinct().Single();

                    yield return PlayUntil(clip.start);
                    var bodies = ids.Select(id =>
                    {
                        Component unit = Target(id);
                        Assert.That(unit.GetComponent("HoppingMotionController"), Is.Null);
                        return (SpriteRenderer)unit.GetType().GetField("_spriteRenderer", Private).GetValue(unit);
                    }).ToArray();
                    var walks = ids.Select(_ => new System.Collections.Generic.HashSet<string>()).ToArray();
                    while ((float)Field("elapsed") < clip.stop)
                    {
                        for (int i = 0; i < bodies.Length; i++)
                        {
                            string pose = bodies[i].sprite.name;
                            if (pose.StartsWith("MiniRockWalk", StringComparison.Ordinal) &&
                                walks[i].Add(pose) && capture && clip.count == 2 && i == 0)
                                SaveViewport(directory, pose, true);
                        }
                        yield return null;
                    }
                    Assert.That(walks.Select(s => s.Count), Is.All.EqualTo(8), clip.spell);
                    if (clip.count == 2)
                        Assert.That(Mathf.Abs(Target(ids[0]).transform.position.z - Target(ids[1]).transform.position.z), Is.GreaterThan(.25f));

                    yield return PlayUntil(clip.impact - .027f);
                    Assert.That(bodies.Select(b => b.sprite.name), Is.All.EqualTo("MiniRockSwarm"), clip.spell);
                    Assert.That(CurrentHp(Target(victim)), Is.EqualTo(clip.hp));
                    for (int pose = 1; pose <= 4; pose++)
                    {
                        yield return PlayUntil(clip.impact + .02f + (pose - 1) * .08f);
                        Assert.That(bodies.Select(b => b.sprite.name), Is.All.EqualTo($"MiniRockPunch{pose:00}"));
                        Assert.That(CurrentHp(Target(victim)), Is.EqualTo(clip.hp - 100f * clip.count));
                        if (capture && pose == 1) SaveViewport(directory, clip.spell + "-impact", true);
                    }
                    yield return PlayUntil(clip.impact + .36f);
                    Assert.That(bodies.Select(b => b.sprite.name), Is.All.EqualTo("MiniRockSwarm"));
                }
            }
            finally
            {
                Time.captureFramerate = priorRate;
            }
        }

        [UnityTest]
        public IEnumerator MiniRockAttackAndRecoveryStayIndependentAcrossReciprocalHits()
        {
            Create();
            Component world = CreateWorld();
            MethodInfo decode = Runtime("Global.Serialization.JsonCodec").GetMethod("Deserialize")
                .MakeGenericMethod(Runtime("GameScene.Dto.ObjectsInfo"));
            MethodInfo apply = Runtime("GameScene.Object.PresentationFramePlayer").GetMethod("Apply");
            MethodInfo find = world.GetType().GetMethod("FindById");
            Type hitType = Runtime("GameScene.Dto.Event.HitEvent");
            void Apply(string packet, IList hits = null) =>
                apply.Invoke(null, new[] { decode.Invoke(null, new object[] { packet }), hits, world });
            void Hit(int actor, int target)
            {
                object hit = Activator.CreateInstance(hitType);
                hitType.GetField("actorId").SetValue(hit, actor);
                hitType.GetField("targetId").SetValue(hit, target);
                var events = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>)
                    .MakeGenericType(Runtime("GameScene.Dto.Event.GameEvent")));
                events.Add(hit);
                Apply("{}", events);
            }
            SpriteRenderer ActorBody(int id)
            {
                var unit = (Component)find.Invoke(world, new object[] { id });
                return (SpriteRenderer)unit.GetType().GetField("_spriteRenderer", Private).GetValue(unit);
            }
            int oldRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                Apply("{\"create\":[" +
                    "{\"id\":51,\"type\":\"MiniRock\",\"master\":\"LeftPlayer\",\"position\":{\"x\":9,\"y\":0,\"z\":5}}," +
                    "{\"id\":52,\"type\":\"MiniRock\",\"master\":\"RightPlayer\",\"position\":{\"x\":8.1,\"y\":0,\"z\":5}}]}");
                SpriteRenderer leftHit = ActorBody(51), rightHit = ActorBody(52);
                Assert.That(leftHit.flipX, Is.False);
                Assert.That(rightHit.flipX, Is.True);

                Hit(51,52);
                Assert.That(leftHit.sprite.name, Is.EqualTo("MiniRockPunch01"));
                Assert.That(leftHit.flipX, Is.True);
                Assert.That(rightHit.sprite.name, Is.EqualTo("MiniRockSwarm"));

                yield return new WaitForSeconds(.11f);
                Assert.That(leftHit.sprite.name, Is.EqualTo("MiniRockPunch02"));
                Assert.That(leftHit.flipX, Is.True);
                Hit(52,51);
                Assert.That(rightHit.sprite.name, Is.EqualTo("MiniRockPunch01"));
                Assert.That(rightHit.flipX, Is.False);
                Assert.That(leftHit.sprite.name, Is.EqualTo("MiniRockPunch02"));

                yield return new WaitForSeconds(.23f);
                Assert.That(leftHit.sprite.name, Is.EqualTo("MiniRockSwarm"));
                Assert.That(rightHit.sprite.name, Does.StartWith("MiniRockPunch"));
                yield return new WaitForSeconds(.10f);
                Assert.That(rightHit.sprite.name, Is.EqualTo("MiniRockSwarm"));
            }
            finally
            {
                Time.captureFramerate = oldRate;
            }
        }



        [UnityTest]
        public IEnumerator StormRiderGlidesAndThrustsOnTheRecordedDamageTicks()
        {
            Create();
            Assert.That(Configure("storm_rider"), Is.True);
            var scenarios = (JArray)Recording("storm_rider")["scenarios"];
            var ground = scenarios.Single(scenario => (string)scenario["id"] == "ground");
            Scenario(scenarios.IndexOf(ground));

            int priorRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/StormRiderCaptures");
            if (capture) System.IO.Directory.CreateDirectory(directory);
            try
            {
                yield return PlayUntil(.63f);
                Component rider = Target(3);
                Assert.That(rider.GetComponent("HoppingMotionController"), Is.Null);
                Assert.That(rider.GetComponent("AttackSpriteSwapController"), Is.Null);
                var body = (SpriteRenderer)rider.GetType().GetField("_spriteRenderer", Private).GetValue(rider);
                Assert.That(body.sprite.name, Is.EqualTo("StormRider"));
                var sequence = new System.Collections.Generic.List<string>();
                while ((float)Field("elapsed") < 1.92f)
                {
                    string pose = body.sprite.name;
                    if (pose.StartsWith("StormRiderGlide", StringComparison.Ordinal) && pose != sequence.LastOrDefault())
                    {
                        sequence.Add(pose);
                        if (capture && sequence.Count <= 8) SaveViewport(directory, pose, true);
                    }
                    yield return null;
                }

                CollectionAssert.AreEqual(
                    Enumerable.Range(1, 8).Select(index => $"StormRiderGlide{index:00}"),
                    sequence.Take(8));
                Assert.That(sequence.Count, Is.GreaterThan(8));
                Assert.That(sequence[8], Is.EqualTo("StormRiderGlide01"));
                var hits = ground["frames"].Where(frame => frame["events"].Any(e =>
                    (string)e["type"] == "hit" && (int)e["actorId"] == 3 && (int)e["targetId"] == 2))
                    .Take(2).Select(frame => (float)frame["time"]).ToArray();
                Assert.That(hits, Has.Length.EqualTo(2));
                Assert.That(hits[0], Is.EqualTo(2.65f).Within(.001f));
                Assert.That(hits[1], Is.EqualTo(3.45f).Within(.001f));
                var target = Target(2);

                for (int hit = 0; hit < hits.Length; hit++)
                {
                    yield return PlayUntil(hits[hit] - .025f);
                    Assert.That(body.sprite.name, Is.EqualTo("StormRider"));
                    Assert.That(CurrentHp(target), Is.EqualTo(10000 - 100 * hit));
                    for (int pose = 1; pose <= 5; pose++)
                    {
                        yield return PlayUntil(hits[hit] + .02f + (pose - 1) * .08f);
                        Assert.That(body.sprite.name, Is.EqualTo($"StormRiderThrust{pose:00}"));
                        Assert.That(body.flipX, Is.False);
                        Assert.That(CurrentHp(target), Is.EqualTo(9900 - 100 * hit));
                        if (capture && hit == 0) SaveViewport(directory, body.sprite.name, true);
                    }

                    yield return PlayUntil(hits[hit] + .45f);
                    Assert.That(body.sprite.name, Is.EqualTo("StormRider"));
                }
            }
            finally
            {
                Time.captureFramerate = priorRate;
            }
        }

        [UnityTest]
        public IEnumerator StormRiderRemovalStillSendsTheRecordedAbsorptionProjectile()
        {
            Create();
            Assert.That(Configure("storm_rider"), Is.True);
            var scenarios = (JArray)Recording("storm_rider")["scenarios"];
            var death = scenarios.Single(scenario => (string)scenario["id"] == "death_energy");
            var (start, duration, targetId) = FirstProjectile(death, "ElectricAbsorb");
            Assert.That(start, Is.EqualTo(1.20f).Within(.001f));
            Assert.That(duration, Is.EqualTo(.35f).Within(.001f));
            Assert.That(targetId, Is.EqualTo(3));
            Scenario(scenarios.IndexOf(death));

            int priorRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(start - .026f);
                var rider = Target(2);
                var body = (SpriteRenderer)rider.GetType().GetField("_spriteRenderer", Private).GetValue(rider);
                Assert.That(body.sprite.name, Is.EqualTo("StormRider"));
                Assert.That(FindProjectile("ElectricShot"), Is.Null);

                yield return PlayUntil(start + .025f);
                Assert.That((bool)rider.GetType().GetProperty("IsDespawning", Private).GetValue(rider), Is.True);
                Assert.That(body.sprite.name, Is.EqualTo("StormRider"));
                var bolt = FindProjectile("ElectricShot");
                Assert.That(bolt, Is.Not.Null);
                var ally = Target(targetId);
                var activeEffects = (System.Collections.Generic.IReadOnlyCollection<string>)
                    ally.GetType().GetProperty("ActiveEffects").GetValue(ally);
                Assert.That(activeEffects, Does.Contain("Overcharge"));
                float initialDistance = Vector3.Distance(bolt.position, ally.transform.position);

                yield return PlayUntil(start + duration * .65f);
                Assert.That(Target(2), Is.Null);
                Assert.That(Vector3.Distance(bolt.position, ally.transform.position), Is.LessThan(initialDistance));
                yield return PlayUntil(start + duration + .065f);
                Assert.That(FindProjectile("ElectricShot"), Is.Null);
            }
            finally
            {
                Time.captureFramerate = priorRate;
            }
        }



        [UnityTest]
        public IEnumerator EmberSpiritSwarmUsesIndependentStepsAndDamagePoses()
        {
            Create();
            Assert.That(Configure("ember_spirit_swarm"), Is.True);
            var scenarios = (JArray)Recording("ember_spirit_swarm")["scenarios"];
            var ground = scenarios.Single(scenario => (string)scenario["id"] == "ground");
            Scenario(scenarios.IndexOf(ground));

            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/EmberSpiritCaptures");
            if (capture) System.IO.Directory.CreateDirectory(directory);
            try
            {
                yield return PlayUntil(.65f);
                int[] ids = { 3, 4, 5, 6, 7 };
                var bodies = ids.ToDictionary(id => id, id =>
                {
                    var unit = Target(id);
                    Assert.That(unit.GetComponent("CrawlMotionController"), Is.Null);
                    Assert.That(unit.GetComponent("HoppingMotionController"), Is.Null);
                    return (SpriteRenderer)unit.GetType().GetField("_spriteRenderer", Private).GetValue(unit);
                });
                Assert.That(bodies.Values.Select(body => body.sprite.name), Is.All.EqualTo("EmberSpiritSwarm"));
                var gaits = ids.ToDictionary(id => id, _ => new System.Collections.Generic.HashSet<string>());
                while ((float)Field("elapsed") < 2.34f)
                {
                    foreach (int id in ids)
                    {
                        string pose = bodies[id].sprite.name;
                        if (!pose.StartsWith("EmberSpiritTrot", StringComparison.Ordinal)) continue;
                        if (gaits[id].Add(pose) && capture && id == 6) SaveViewport(directory, pose, true);
                    }
                    yield return null;
                }
                Assert.That(gaits.Values.Select(set => set.Count), Is.All.EqualTo(8));

                var hits = ground["frames"].SelectMany(frame => frame["events"]
                    .Where(evt => (string)evt["type"] == "hit" && ids.Contains((int)evt["actorId"]))
                    .Select(evt => (actor: (int)evt["actorId"], target: (int)evt["targetId"], time: (float)frame["time"])))
                    .Take(10).ToArray();
                CollectionAssert.AreEqual(new[] { 7, 4, 3, 5, 6, 7, 4, 3, 5, 6 }, hits.Select(hit => hit.actor));
                Assert.That(hits.Select(hit => hit.target), Is.All.EqualTo(2));
                for (int i = 0; i < 5; i++)
                {
                    Assert.That(hits[i].time, Is.EqualTo(new[] { 2.55f, 2.75f, 2.85f, 3f, 3.05f }[i]).Within(.001f));
                    Assert.That(hits[i + 5].time - hits[i].time, Is.EqualTo(.8f).Within(.001f));
                }

                var target = Target(2);
                var lastHit = new System.Collections.Generic.Dictionary<int, float>();
                var recovered = new System.Collections.Generic.HashSet<int>();
                bool overlappingRecovery = false;
                for (int i = 0; i < hits.Length; i++)
                {
                    var hit = hits[i];
                    yield return PlayUntil(hit.time - .026f);
                    Assert.That(CurrentHp(target), Is.EqualTo(10000 - 100 * i));
                    Assert.That(bodies[hit.actor].sprite.name, Is.EqualTo("EmberSpiritSwarm"));

                    yield return PlayUntil(hit.time + .015f);
                    Assert.That(CurrentHp(target), Is.EqualTo(9900 - 100 * i));
                    Assert.That(bodies[hit.actor].sprite.name, Is.EqualTo("EmberSpiritSwipe01"));
                    Assert.That(bodies[hit.actor].flipX, Is.False);
                    lastHit[hit.actor] = hit.time;
                    float now = (float)Field("elapsed");
                    foreach (int id in ids.Where(id => id != hit.actor))
                    {
                        string pose = bodies[id].sprite.name;
                        if (!lastHit.TryGetValue(id, out float previousHit))
                        {
                            Assert.That(pose, Is.EqualTo("EmberSpiritSwarm"), $"개체 {id}는 첫 타격 전이다.");
                            continue;
                        }

                        float age = now - previousHit;
                        if (age > .34f)
                        {
                            Assert.That(pose, Is.EqualTo("EmberSpiritSwarm"));
                            recovered.Add(id);
                        }
                        else if (age > .1f && age < .30f)
                        {
                            Assert.That(pose, Does.StartWith("EmberSpiritSwipe").And.Not.EqualTo("EmberSpiritSwipe01"));
                            overlappingRecovery = true;
                        }
                    }

                    if (capture && i is 0 or 2 or 4) SaveViewport(directory, $"swarm-hit-{i + 1:00}", true);
                }

                Assert.That(overlappingRecovery, Is.True);
                CollectionAssert.AreEquivalent(ids, recovered);
            }
            finally
            {
                Time.captureFramerate = previousRate;
            }
        }



        [UnityTest]
        public IEnumerator SeedSpiritSwarmShowsSimultaneousLeafHitsAndIndependentRecovery()
        {
            Create();
            Assert.That(Configure("seed_spirit_swarm"), Is.True);
            Scenario(0);
            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/SeedSpiritCaptures");
            if (capture) System.IO.Directory.CreateDirectory(directory);
            try
            {
                yield return PlayUntil(.65f);
                int[] ids = { 3, 4, 5, 6 };
                var bodies = ids.ToDictionary(id => id, SeedSpiritBody);
                var seen = ids.ToDictionary(id => id, _ => new System.Collections.Generic.HashSet<string>());
                Assert.That(bodies.Values.Select(body => body.sprite.name), Is.All.EqualTo("SeedSpiritSwarm"));
                while ((float)Field("elapsed") < 2.26f)
                {
                    foreach (int id in ids)
                    {
                        string pose = bodies[id].sprite.name;
                        if (!pose.StartsWith("SeedSpiritStep", StringComparison.Ordinal)) continue;
                        if (seen[id].Add(pose) && capture && id == 6) SaveViewport(directory, pose, true);
                    }
                    yield return null;
                }

                Assert.That(seen.Values.Select(gait => gait.Count), Is.All.EqualTo(8));
                var target = Target(2);
                var clips = new[]
                {
                    (time: 2.70f, attackers: new[] { 4 }, before: 10000, after: 9900),
                    (time: 2.80f, attackers: new[] { 3 }, before: 9900, after: 9800),
                    (time: 3.00f, attackers: new[] { 5, 6 }, before: 9800, after: 9600),
                    (time: 3.50f, attackers: new[] { 4 }, before: 9600, after: 9500),
                    (time: 3.60f, attackers: new[] { 3 }, before: 9500, after: 9400),
                    (time: 3.80f, attackers: new[] { 5, 6 }, before: 9400, after: 9200)
                };
                foreach (var clip in clips)
                {
                    yield return PlayUntil(clip.time - .027f);
                    Assert.That(CurrentHp(target), Is.EqualTo(clip.before));
                    Assert.That(clip.attackers.Select(id => bodies[id].sprite.name), Is.All.EqualTo("SeedSpiritSwarm"));
                    yield return PlayUntil(clip.time + .016f);
                    Assert.That(CurrentHp(target), Is.EqualTo(clip.after));
                    CollectionAssert.AreEquivalent(clip.attackers, ids.Where(id => bodies[id].sprite.name == "SeedSpiritLeafHit01"));
                    Assert.That(clip.attackers.Select(id => bodies[id].flipX), Is.All.False);
                    if (clip.attackers.Length == 2)
                    {
                        Assert.That(bodies[3].sprite.name, Is.EqualTo("SeedSpiritLeafHit03"));
                        if (capture && clip.after == 9600) SaveViewport(directory, "swarm-simultaneous-hit", true);
                    }
                }

                yield return PlayUntil(4.16f);
                Assert.That(bodies.Values.Select(body => body.sprite.name), Is.All.EqualTo("SeedSpiritSwarm"));
            }
            finally
            {
                Time.captureFramerate = previousRate;
            }
        }

        [UnityTest]
        public IEnumerator SeedNestSpawnsNineSeedSpiritsAndKeepsTheirLeafHitsIndependent()
        {
            Create();
            Assert.That(Configure("seed_nest"), Is.True);
            Scenario(0);
            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/SeedSpiritCaptures");
            if (capture) System.IO.Directory.CreateDirectory(directory);
            try
            {
                yield return PlayUntil(1.48f);
                SpriteRenderer first = SeedSpiritBody(4);
                Assert.That(Target(6), Is.Null);
                yield return PlayUntil(2.33f);
                SpriteRenderer second = SeedSpiritBody(6);
                Assert.That(first, Is.Not.SameAs(second));
                yield return PlayUntil(2.64f);
                Assert.That(first.sprite.name, Does.StartWith("SeedSpiritStep"));
                Assert.That(second.sprite.name, Does.StartWith("SeedSpiritStep"));
                if (capture) SaveViewport(directory, "nest-two-approaches", true);

                var target = Target(2);
                yield return PlayUntil(3.48f);
                Assert.That(CurrentHp(target), Is.EqualTo(10000));
                yield return PlayUntil(3.52f);
                Assert.That(first.sprite.name, Is.EqualTo("SeedSpiritLeafHit01"));
                Assert.That(second.sprite.name, Is.Not.EqualTo("SeedSpiritLeafHit01"));
                Assert.That(CurrentHp(target), Is.EqualTo(9900));

                yield return PlayUntil(4.322f);
                Assert.That(first.sprite.name, Is.EqualTo("SeedSpiritLeafHit01"));
                Assert.That(second.sprite.name, Is.Not.EqualTo("SeedSpiritLeafHit01"));
                Assert.That(CurrentHp(target), Is.EqualTo(9800));
                yield return PlayUntil(4.40f);
                Assert.That(first.sprite.name, Is.EqualTo("SeedSpiritLeafHit02"));
                Assert.That(second.sprite.name, Is.EqualTo("SeedSpiritLeafHit01"));
                Assert.That(CurrentHp(target), Is.EqualTo(9700));
                if (capture) SaveViewport(directory, "nest-two-hit-phases", true);

                yield return PlayUntil(8.28f);
                foreach (int id in new[] { 4, 6, 9, 12, 15, 18, 21, 24, 27 }) SeedSpiritBody(id);
            }
            finally
            {
                Time.captureFramerate = previousRate;
            }
        }

        [UnityTest]
        public IEnumerator SeedSpiritEvolutionReplacesTheRecoveringSeedWithAGolem()
        {
            Create();
            Assert.That(Configure("overgrowth"), Is.True);
            var scenarios = (JArray)Recording("overgrowth")["scenarios"];
            Scenario(scenarios.IndexOf(scenarios.Single(scenario => (string)scenario["id"] == "evolution")));
            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/SeedSpiritCaptures");
            if (capture) System.IO.Directory.CreateDirectory(directory);
            try
            {
                yield return PlayUntil(.876f);
                var seed = Target(2);
                var body = SeedSpiritBody(2);
                Assert.That(body.sprite.name, Is.EqualTo("SeedSpiritLeafHit01"));
                Assert.That(CurrentHp(Target(3)), Is.EqualTo(9900));
                Assert.That(Target(5), Is.Null);
                Vector3 seedPosition = seed.transform.position;
                var master = seed.GetType().GetMethod("GetMaster");
                object seedTeam = master.Invoke(seed, null);
                if (capture) SaveViewport(directory, "evolution-seed-hit", true);

                yield return PlayUntil(1.13f);
                Assert.That(body.sprite.name, Is.EqualTo("SeedSpiritLeafHit04"));
                yield return PlayUntil(1.151f);
                var golem = Target(5);
                Assert.That(golem, Is.Not.Null);
                Assert.That(golem.name, Does.StartWith("TreeGolem"));
                Assert.That(master.Invoke(golem, null), Is.EqualTo(seedTeam));
                Assert.That(Vector3.Distance(golem.transform.position, seedPosition), Is.LessThan(.025f));
                var despawning = seed.GetType().GetProperty("IsDespawning", Private);
                Assert.That((bool)despawning.GetValue(seed), Is.True);
                Assert.That(CurrentHp(Target(3)), Is.EqualTo(9800));

                yield return PlayUntil(1.25f);
                Assert.That(Target(2), Is.Null);
                Assert.That(Target(4), Is.Null);
                Assert.That(Target(5), Is.SameAs(golem));
                var golemBody = (SpriteRenderer)golem.GetType().GetField("_spriteRenderer", Private).GetValue(golem);
                Assert.That(golemBody.sprite.name, Is.EqualTo("TreeGolem"));
                if (capture) SaveViewport(directory, "evolution-golem", true);
            }
            finally
            {
                Time.captureFramerate = previousRate;
            }
        }

        private SpriteRenderer SeedSpiritBody(int id)
        {
            Component unit = Target(id);
            Assert.That(unit, Is.Not.Null, $"씨앗 정령 ID {id}");
            Assert.That(unit.name, Does.StartWith("SeedSpirit"));
            Assert.That(unit.GetComponent("CrawlMotionController"), Is.Null);
            return (SpriteRenderer)unit.GetType().GetField("_spriteRenderer", Private).GetValue(unit);
        }


        [UnityTest]
        public IEnumerator ZapMouseRunsAtRecordingSpeedAndRecoversOnEachActorsTiming()
        {
            Create();
            Assert.That(Configure("zap_mouse"), Is.True);
            var scenarios = (JArray)Recording("zap_mouse")["scenarios"];
            Scenario(scenarios.IndexOf(scenarios.Single(scenario => (string)scenario["id"] == "ground")));
            int priorRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/ZapMouseCaptures");
            if (capture) System.IO.Directory.CreateDirectory(directory);
            try
            {
                yield return PlayUntil(.634f);
                var later = ZapMouseBody(3);
                var earlier = ZapMouseBody(4);
                Assert.That(later.sprite.name, Is.EqualTo("ZapMouse"));
                Assert.That(earlier.sprite.name, Is.EqualTo("ZapMouse"));
                var earlierRun = new System.Collections.Generic.List<string>();
                var laterRun = new System.Collections.Generic.List<string>();
                while ((float)Field("elapsed") < 1.49f)
                {
                    string firstPose = earlier.sprite.name;
                    if (firstPose.StartsWith("ZapMouseRun", StringComparison.Ordinal) &&
                        firstPose != earlierRun.LastOrDefault())
                    {
                        earlierRun.Add(firstPose);
                        if (capture && earlierRun.Count <= 8) SaveViewport(directory, firstPose, true);
                    }
                    string otherPose = later.sprite.name;
                    if (otherPose.StartsWith("ZapMouseRun", StringComparison.Ordinal) &&
                        otherPose != laterRun.LastOrDefault())
                        laterRun.Add(otherPose);
                    yield return null;
                }
                var firstLoop = Enumerable.Range(1, 8).Select(i => $"ZapMouseRun{i:00}").Append("ZapMouseRun01");
                CollectionAssert.AreEqual(firstLoop, earlierRun.Take(9));
                CollectionAssert.AreEqual(firstLoop, laterRun.Take(9));

                var target = Target(2);
                for (int i = 0; i < 2; i++)
                {
                    float impact = 2.15f + .8f * i;
                    float beforeHp = 10000 - 200 * i;
                    yield return PlayUntil(impact - .027f);
                    Assert.That(earlier.sprite.name, Is.EqualTo("ZapMouse"));
                    Assert.That(later.sprite.name, Is.EqualTo("ZapMouse"));
                    Assert.That(CurrentHp(target), Is.EqualTo(beforeHp));
                    yield return PlayUntil(impact + .018f);
                    Assert.That(earlier.sprite.name, Is.EqualTo("ZapMouseSpikeHit01"));
                    Assert.That(later.sprite.name, Is.EqualTo("ZapMouse"));
                    Assert.That(earlier.flipX, Is.False);
                    Assert.That(CurrentHp(target), Is.EqualTo(beforeHp - 100));
                    if (capture && i == 0) SaveViewport(directory, "first-mouse-hit", true);

                    yield return PlayUntil(impact + .10f);
                    Assert.That(earlier.sprite.name, Is.EqualTo("ZapMouseSpikeHit02"));
                    Assert.That(later.sprite.name, Is.EqualTo("ZapMouseSpikeHit01"));
                    Assert.That(later.flipX, Is.False);
                    Assert.That(CurrentHp(target), Is.EqualTo(beforeHp - 200));
                    if (capture && i == 0) SaveViewport(directory, "two-hit-phases", true);
                    yield return PlayUntil(impact + .348f);
                    Assert.That(earlier.sprite.name, Is.EqualTo("ZapMouse"));
                    Assert.That(later.sprite.name, Is.EqualTo("ZapMouseSpikeHit04"));
                    yield return PlayUntil(impact + .41f);
                    Assert.That(later.sprite.name, Is.EqualTo("ZapMouse"));
                }
            }
            finally
            {
                Time.captureFramerate = priorRate;
            }
        }

        [UnityTest]
        public IEnumerator ZapMouseDeathKeepsTheSurvivorAndSendsEnergyFromTheRemovedMouse()
        {
            Create();
            Assert.That(Configure("zap_mouse"), Is.True);
            var scenarios = (JArray)Recording("zap_mouse")["scenarios"];
            var death = scenarios.Single(scenario => (string)scenario["id"] == "death_energy");
            var (start, duration, targetId) = FirstProjectile(death, "ElectricAbsorb");
            Assert.That(start, Is.EqualTo(1.20f).Within(.001f));
            Assert.That(duration, Is.EqualTo(.35f).Within(.001f));
            Assert.That(targetId, Is.EqualTo(4));
            Scenario(scenarios.IndexOf(death));
            int priorRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(start - .025f);
                var dying = Target(2);
                var survivor = Target(3);
                SpriteRenderer dyingBody = ZapMouseBody(2);
                SpriteRenderer survivorBody = ZapMouseBody(3);
                Assert.That(dyingBody.sprite.name, Is.EqualTo("ZapMouse"));
                Assert.That(survivorBody.sprite.name, Is.EqualTo("ZapMouse"));
                Component receiver = Target(targetId);
                SpriteRenderer receiverBody = ServedObjectRenderer(receiver);
                Assert.That(receiverBody.sprite.name, Is.EqualTo("ElectricSlimeIdle"));
                Vector3 origin = dying.transform.position;
                Assert.That(FindProjectile("ElectricShot"), Is.Null);

                yield return PlayUntil(start + .001f);
                var despawning = dying.GetType().GetProperty("IsDespawning", Private);
                Assert.That((bool)despawning.GetValue(dying), Is.True);
                Assert.That((bool)despawning.GetValue(survivor), Is.False);
                Assert.That(dyingBody.sprite.name, Is.EqualTo("ZapMouse"));
                Transform bolt = FindProjectile("ElectricShot");
                Assert.That(bolt, Is.Not.Null);
                Assert.That(Vector3.Distance(bolt.position, origin), Is.LessThan(.16f));
                var effects = receiver.GetType().GetProperty("ActiveEffects");
                Assert.That(receiverBody.sprite.name, Is.EqualTo("ElectricSlimeIdle"));
                Assert.That((IEnumerable)effects.GetValue(receiver), Does.Contain("Overcharge"));
                Assert.That((IEnumerable)effects.GetValue(survivor), Does.Not.Contain("Overcharge"));
                float initialDistance = Vector3.Distance(bolt.position, receiver.transform.position);

                yield return PlayUntil(start + .2f);
                Assert.That(Target(2), Is.Null);
                Assert.That(Target(3), Is.SameAs(survivor));
                Assert.That(survivorBody.sprite.name, Is.EqualTo("ZapMouse"));
                Assert.That(Vector3.Distance(bolt.position, receiver.transform.position), Is.LessThan(initialDistance));
                Assert.That(receiverBody.sprite.name, Is.EqualTo("ElectricSlimeIdle"));
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    string path = System.IO.Path.Combine(Application.dataPath, "../Library/ElectricSlimeCaptures");
                    System.IO.Directory.CreateDirectory(path);
                    SaveViewport(path, "energy-recipient", true);
                }
                yield return PlayUntil(start + duration + .07f);
                Assert.That(FindProjectile("ElectricShot"), Is.Null);
                Assert.That(Target(3), Is.SameAs(survivor));
                Assert.That(receiverBody.sprite.name, Is.EqualTo("ElectricSlimeIdle"));
            }
            finally
            {
                Time.captureFramerate = priorRate;
            }
        }

        [UnityTest]
        public IEnumerator ThunderBirdFlapsDivesAndStrikesOnTheFirstServerHit()
        {
            Create();
            Assert.That(Configure("thunder_bird_swarm"), Is.True);
            SelectThunderBirdScenario("thunder_bird_swarm", "ground");
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/ThunderBirdCaptures");
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            if (capture) System.IO.Directory.CreateDirectory(directory);
            int priorRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(.635f);
                SpriteRenderer[] birds = Enumerable.Range(3, 3).Select(id => ThunderBirdBody(Target(id))).ToArray();
                var flap = new System.Collections.Generic.HashSet<string>();
                var dive = new System.Collections.Generic.HashSet<string>();
                while ((float)Field("elapsed") < 2.48f)
                {
                    string pose = birds[0].sprite.name;
                    if (pose.StartsWith("ThunderBirdFlap", StringComparison.Ordinal) && flap.Add(pose) && capture)
                        SaveViewport(directory, pose, true);
                    if (pose.StartsWith("ThunderBirdDive", StringComparison.Ordinal) && dive.Add(pose) && capture)
                        SaveViewport(directory, pose, true);
                    yield return null;
                }
                CollectionAssert.AreEquivalent(Enumerable.Range(1, 8).Select(i => $"ThunderBirdFlap{i:00}"), flap);
                CollectionAssert.AreEquivalent(Enumerable.Range(1, 4).Select(i => $"ThunderBirdDive{i:00}"), dive);
                var enemy = Target(2);
                Assert.That(CurrentHp(enemy), Is.EqualTo(1000));
                Assert.That(birds.All(bird => bird.sprite.name == "ThunderBirdDive04"), Is.True);

                for (int i = 0; i < 4; i++)
                {
                    yield return PlayUntil(2.522f + .08f * i);
                    string pose = $"ThunderBirdStrike{i + 1:00}";
                    Assert.That(birds.All(bird => bird.sprite.name == pose && !bird.flipX), Is.True, pose);
                    Assert.That(CurrentHp(enemy), Is.EqualTo(751));
                    if (capture) SaveViewport(directory, pose, true);
                }
                yield return PlayUntil(3.12f);
                Assert.That(birds.All(bird => bird.sprite.name.StartsWith("ThunderBirdClimb", StringComparison.Ordinal)), Is.True);
                Assert.That(ThunderBirdHeight(Target(3)), Is.GreaterThan(1.45f));
                if (capture) SaveViewport(directory, "climb", true);
                yield return PlayUntil(5.922f);
                Assert.That(birds.All(bird => bird.sprite.name == "ThunderBirdStrike01"), Is.True);
                Assert.That(CurrentHp(enemy), Is.EqualTo(550));
            }
            finally
            {
                Time.captureFramerate = priorRate;
            }
        }

        [UnityTest]
        public IEnumerator ThunderBirdOverchargeShotKeepsTheClimbAfterTheBeakStrike()
        {
            Create();
            Assert.That(Configure("lightning_shot"), Is.True);
            JToken scenario = SelectThunderBirdScenario("lightning_shot", "overcharge");
            var shot = FirstProjectile(scenario, "ElectricShot");
            Assert.That(shot.start, Is.GreaterThan(4.8f).And.LessThan(5.15f));
            int priorRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(3.82f);
                SpriteRenderer[] birds = Enumerable.Range(2, 3).Select(id => ThunderBirdBody(Target(id))).ToArray();
                Assert.That(birds[0].sprite.name, Does.StartWith("ThunderBirdFlap"));
                yield return PlayUntil(3.93f);
                Assert.That(birds[0].sprite.name, Does.StartWith("ThunderBirdDive"));
                yield return PlayUntil(4.822f);
                Assert.That(birds.All(bird => bird.sprite.name == "ThunderBirdStrike01"), Is.True);
                Assert.That(CurrentHp(Target(6)), Is.EqualTo(775));
                yield return PlayUntil(shot.start + .019f);
                Assert.That(FindProjectile("ElectricShot"), Is.Not.Null);
                Assert.That(birds.All(bird => bird.sprite.name != "ThunderBirdStrike01"), Is.True);
                yield return PlayUntil(5.19f);
                Assert.That(birds.All(bird => bird.sprite.name.StartsWith("ThunderBirdClimb", StringComparison.Ordinal)), Is.True);
                yield return PlayUntil(6.86f);
                Assert.That(ThunderBirdHeight(Target(2)), Is.EqualTo(3f).Within(.03f));
                Assert.That(birds[0].sprite.name, Does.StartWith("ThunderBirdFlap"));
            }
            finally
            {
                Time.captureFramerate = priorRate;
            }
        }

        [UnityTest]
        public IEnumerator ThunderBirdFoldsOnDescentAndTransfersEnergyFromTheLandingPoint()
        {
            Create();
            Assert.That(Configure("thunder_bird_swarm"), Is.True);
            JToken scenario = SelectThunderBirdScenario("thunder_bird_swarm", "death_energy");
            var shot = FirstProjectile(scenario, "ElectricAbsorb");
            Assert.That(shot.start, Is.EqualTo(2.25f).Within(.001f));
            Assert.That(shot.targetId, Is.EqualTo(3));
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/ThunderBirdCaptures");
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            if (capture) System.IO.Directory.CreateDirectory(directory);
            int priorRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(.98f);
                Component dying = Target(2);
                Component receiver = Target(3);
                Component survivor = Target(4);
                SpriteRenderer body = ThunderBirdBody(dying);
                Assert.That(body.sprite.name, Does.StartWith("ThunderBirdFlap"));
                yield return PlayUntil(1.018f);
                Assert.That(body.sprite.name, Is.EqualTo("ThunderBirdFall01"));
                Assert.That(ThunderBirdHeight(dying), Is.EqualTo(3f).Within(.02f));

                var fall = new System.Collections.Generic.HashSet<string>();
                while ((float)Field("elapsed") < 2.24f)
                {
                    if (fall.Add(body.sprite.name) && capture) SaveViewport(directory, body.sprite.name, true);
                    yield return null;
                }
                CollectionAssert.AreEquivalent(Enumerable.Range(1, 4).Select(i => $"ThunderBirdFall{i:00}"), fall);
                Assert.That(body.sprite.name, Is.EqualTo("ThunderBirdFall04"));
                Assert.That(ThunderBirdHeight(dying), Is.EqualTo(0f).Within(.06f));
                Vector3 origin = dying.transform.position;

                yield return PlayUntil(shot.start + .006f);
                Transform bolt = FindProjectile("ElectricShot");
                Assert.That(bolt, Is.Not.Null);
                Assert.That(Vector3.Distance(bolt.position, origin), Is.LessThan(.16f));
                Assert.That(body.sprite.name, Is.EqualTo("ThunderBirdFall04"));
                var effects = receiver.GetType().GetProperty("ActiveEffects");
                Assert.That((IEnumerable)effects.GetValue(receiver), Does.Contain("Overcharge"));
                Assert.That((IEnumerable)effects.GetValue(survivor), Does.Not.Contain("Overcharge"));
                if (capture) SaveViewport(directory, "energy-transfer", true);

                yield return PlayUntil(shot.start + .21f);
                Assert.That(Target(2), Is.Null);
                Assert.That(Target(3), Is.SameAs(receiver));
                Assert.That(Target(4), Is.SameAs(survivor));
                Assert.That(ThunderBirdBody(receiver).sprite.name, Does.StartWith("ThunderBirdFlap"));
                Assert.That(ThunderBirdBody(survivor).sprite.name, Does.StartWith("ThunderBirdFlap"));
                yield return PlayUntil(shot.start + shot.duration + .07f);
                Assert.That(FindProjectile("ElectricShot"), Is.Null);
            }
            finally
            {
                Time.captureFramerate = priorRate;
            }
        }

        [UnityTest]
        public IEnumerator ThunderBirdDistinguishesAHeightCorrectionFromFlightAtBothTickRates()
        {
            Create();
            Type gameConfig = Runtime("GameScene.GameConfig");
            MethodInfo setTickRate = gameConfig.GetMethod("SetTickRate");
            int priorTickRate = (int)gameConfig.GetProperty("TickRate").GetValue(null);
            int priorRenderRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                foreach (int tickRate in new[] { 20, 60 })
                {
                    setTickRate.Invoke(null, new object[] { tickRate });
                    Component world = CreateWorld();
                    Type createdType = Runtime("GameScene.Dto.CreatedObjectDto");
                    object created = Activator.CreateInstance(createdType);
                    createdType.GetField("id").SetValue(created, 42);
                    createdType.GetField("type").SetValue(created, "ThunderBird");
                    createdType.GetField("master").SetValue(created, "LeftPlayer");
                    createdType.GetField("position").SetValue(created, new Vector3(5f, 3f, 5f));
                    Component bird = (Component)Runtime("GameScene.Object.ObjectSpawner").GetMethod("SpawnShared")
                        .Invoke(null, new[] { created, world, false });
                    SpriteRenderer sprite = ThunderBirdBody(bird);
                    Assert.That(bird.GetComponent("HoverMotionController"), Is.Null);
                    Type updateType = Runtime("GameScene.Dto.UpdatedObjectDto");
                    object dto = Activator.CreateInstance(updateType);
                    FieldInfo position = updateType.GetField("position");
                    updateType.GetField("status").SetValue(dto, "Idle");
                    MethodInfo update = bird.GetType().GetMethod("UpdateObject");

                    void SetHeight(float height)
                    {
                        position.SetValue(dto, new Vector3(5f, height, 5f));
                        update.Invoke(bird, new[] { dto });
                    }

                    SetHeight(3f);
                    SetHeight(2.96f);
                    yield return null;
                    Assert.That(sprite.sprite.name, Does.StartWith("ThunderBirdFlap"), $"{tickRate}Hz 한 틱 하강");
                    SetHeight(2.92f);
                    yield return null;
                    Assert.That(sprite.sprite.name, Does.StartWith("ThunderBirdDive"), $"{tickRate}Hz 연속 하강");
                    SetHeight(2f);
                    yield return null;
                    Assert.That(sprite.sprite.name, Does.StartWith("ThunderBirdFlap"), $"{tickRate}Hz 좌표 보정");
                    SetHeight(2.04f);
                    SetHeight(2.08f);
                    yield return null;
                    Assert.That(sprite.sprite.name, Does.StartWith("ThunderBirdClimb"), $"{tickRate}Hz 연속 상승");
                }
            }
            finally
            {
                Time.captureFramerate = priorRenderRate;
                setTickRate.Invoke(null, new object[] { priorTickRate });
            }
        }

        [UnityTest]
        public IEnumerator WindSpiritKeepsTheApproachPoseWhenItHitsBothAirTargets()
        {
            Create();
            const string magic = "wind_spirit";
            Assert.That(Configure(magic), Is.True);
            var scenarios = (JArray)Recording(magic)["scenarios"];
            var air = scenarios.Single(scenario => (string)scenario["id"] == "air");
            Scenario(scenarios.IndexOf(air));
            JToken impact = air["frames"].Single(frame => frame["events"].Any(hit => (int?)hit["actorId"] == 4));
            Assert.That((float)impact["time"], Is.EqualTo(1.95f).Within(.001f));
            CollectionAssert.AreEquivalent(new[] { 2, 3 }, impact["events"].Select(hit => (int)hit["targetId"]));

            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/WindSpiritCaptures");
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            if (capture) System.IO.Directory.CreateDirectory(directory);
            int priorRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(.645f);
                var spirit = Target(4);
                Assert.That(spirit, Is.Not.Null);
                var body = ServedObjectRenderer(spirit);
                var world = (Component)Field("world");
                var glide = new System.Collections.Generic.HashSet<string>();
                while ((float)Field("elapsed") < 1.58f)
                {
                    string sprite = body.sprite.name;
                    Assert.That(world.transform.InverseTransformPoint(spirit.transform.position).y, Is.EqualTo(3f).Within(.02f));
                    if (sprite.StartsWith("WindSpiritGlide", StringComparison.Ordinal) && glide.Add(sprite) && capture)
                        SaveViewport(directory, sprite, true);
                    yield return null;
                }
                CollectionAssert.AreEquivalent(Enumerable.Range(1, 4).Select(i => $"WindSpiritGlide{i:00}"), glide);

                float[] approachTimes = { 1.65f, 1.75f, 1.85f, 1.90f };
                for (int i = 0; i < approachTimes.Length; i++)
                {
                    yield return PlayUntil(approachTimes[i] + .018f);
                    string pose = $"WindSpiritDescent{i + 1:00}";
                    Assert.That(body.sprite.name, Is.EqualTo(pose));
                    if (capture) SaveViewport(directory, pose, true);
                }

                Assert.That(CurrentHp(Target(2)), Is.EqualTo(1000));
                Assert.That(CurrentHp(Target(3)), Is.EqualTo(1000));
                yield return PlayUntil(1.968f);
                Assert.That(body.sprite.name, Is.EqualTo("WindSpiritDescent04"));
                Assert.That(CurrentHp(Target(2)), Is.EqualTo(900));
                Assert.That(CurrentHp(Target(3)), Is.EqualTo(900));
                Assert.That((bool)spirit.GetType().GetProperty("IsDespawning", Private).GetValue(spirit), Is.True);
                if (capture) SaveViewport(directory, "impact", true);

                yield return PlayUntil(2.08f);
                Assert.That(Target(4), Is.Null);
                var effects = world.GetComponentsInChildren<Transform>()
                    .Where(item => item.name == "Explode(Clone)").ToArray();
                Assert.That(effects, Has.Length.EqualTo(1));
                Assert.That(world.transform.InverseTransformPoint(effects[0].position).y, Is.EqualTo(2.72f).Within(.05f));
                if (capture) SaveViewport(directory, "explosion", true);
            }
            finally
            {
                Time.captureFramerate = priorRate;
            }
        }

        [UnityTest]
        public IEnumerator FireTadpoleWalksStrikesSeparatelyAndKeepsTheNextPortalArrival()
        {
            Create();
            Assert.That(Configure("dimension_toad"), Is.True);
            var scenarios = (JArray)Recording("dimension_toad")["scenarios"];
            Scenario(scenarios.IndexOf(scenarios.Single(s => (string)s["id"] == "ground")));
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/FireTadpoleCaptures");
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            if (capture) System.IO.Directory.CreateDirectory(directory);
            int priorRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(.772f);
                Component first = Target(4);
                var body = ServedObjectRenderer(first);
                Assert.That(first.name, Does.StartWith("FireTadpole"));
                Assert.That(first.GetComponent("CrawlMotionController"), Is.Null);
                Assert.That(first.GetComponentsInChildren<Transform>(true).Any(child => child.name == "TTLBar"), Is.True);
                var poses = new System.Collections.Generic.HashSet<string>();
                while ((float)Field("elapsed") < 1.97f)
                {
                    string pose = body.sprite.name;
                    if (pose.StartsWith("FireTadpoleWalk", StringComparison.Ordinal) && poses.Add(pose) && capture)
                        SaveViewport(directory, pose, true);
                    yield return null;
                }
                CollectionAssert.AreEquivalent(Enumerable.Range(1, 8).Select(i => $"FireTadpoleWalk{i:00}"), poses);

                yield return PlayUntil(2.823f);
                Component second = Target(6);
                var secondBody = ServedObjectRenderer(second);
                var target = Target(2);
                Assert.That(CurrentHp(target), Is.EqualTo(10000));
                Assert.That(body.sprite.name, Is.EqualTo("FireTadpoleIdle"));
                for (int i = 0; i < 4; i++)
                {
                    yield return PlayUntil(2.875f + .08f * i);
                    string pose = $"FireTadpoleStomp{i + 1:00}";
                    Assert.That(body.sprite.name, Is.EqualTo(pose));
                    Assert.That(body.flipX, Is.False);
                    Assert.That(secondBody.sprite.name, Does.Not.StartWith("FireTadpoleStomp"));
                    Assert.That(CurrentHp(target), Is.EqualTo(9900));
                    if (capture) SaveViewport(directory, pose, true);
                }

                yield return PlayUntil(3.674f);
                Assert.That(body.sprite.name, Is.EqualTo("FireTadpoleStomp01"));
                Assert.That(secondBody.sprite.name, Does.Not.StartWith("FireTadpoleStomp"));
                Assert.That(CurrentHp(target), Is.EqualTo(9700));

                yield return PlayUntil(3.932f);
                Component third = Target(8);
                var thirdBody = ServedObjectRenderer(third);
                Vector3 inPortal = third.transform.position;
                yield return PlayUntil(4.077f);
                var world = (Component)Field("world");
                Assert.That(third.transform.position.x, Is.GreaterThan(inPortal.x));
                Assert.That(world.transform.InverseTransformPoint(third.transform.position).y, Is.GreaterThan(.03f));
                Assert.That(thirdBody.sprite.name, Is.EqualTo("FireTadpoleIdle"));
                if (capture) SaveViewport(directory, "portal-landing", true);

                yield return PlayUntil(4.174f);
                Assert.That(secondBody.sprite.name, Is.EqualTo("FireTadpoleStomp01"));
                Assert.That(body.sprite.name, Does.Not.StartWith("FireTadpoleStomp"));
                Assert.That(CurrentHp(target), Is.EqualTo(9600));
                yield return PlayUntil(4.28f);
                Assert.That(thirdBody.sprite.name, Does.StartWith("FireTadpoleWalk"));

                yield return PlayUntil(8.672f);
                Assert.That((bool)first.GetType().GetProperty("IsDespawning", Private).GetValue(first), Is.True);
                Component newest = Target(14);
                Assert.That(newest, Is.Not.Null);
                Assert.That(TimeToLive(first), Is.EqualTo(0f).Within(.001f));
                Assert.That(TimeToLive(second), Is.EqualTo(1.6f).Within(.001f));
                Assert.That(TimeToLive(newest), Is.EqualTo(8f).Within(.001f));
                if (capture) SaveViewport(directory, "old-and-new", true);

                yield return PlayUntil(8.762f);
                Assert.That(Target(4), Is.Null);
                Assert.That(Target(6), Is.SameAs(second));
                Assert.That(Target(14), Is.SameAs(newest));
                yield return PlayUntil(8.973f);
                Assert.That(secondBody.sprite.name, Is.EqualTo("FireTadpoleStomp01"));
            }
            finally
            {
                Time.captureFramerate = priorRate;
            }
        }

        [UnityTest]
        public IEnumerator FireTadpoleTakesDamageAndCompletesAStompInTheSameExchange()
        {
            Create();
            Assert.That(Configure("dimension_toad"), Is.True);
            var scenarios = (JArray)Recording("dimension_toad")["scenarios"];
            Scenario(scenarios.IndexOf(scenarios.Single(s => (string)s["id"] == "panic")));
            int priorRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(1.968f);
                var target = Target(2);
                var tadpole = Target(4);
                var body = ServedObjectRenderer(tadpole);
                Assert.That(CurrentHp(target), Is.EqualTo(1000));
                Assert.That(CurrentHp(tadpole), Is.EqualTo(1000));
                for (int i = 0; i < 4; i++)
                {
                    yield return PlayUntil(2.025f + .08f * i);
                    Assert.That(body.sprite.name, Is.EqualTo($"FireTadpoleStomp{i + 1:00}"));
                    Assert.That(CurrentHp(target), Is.EqualTo(900));
                    Assert.That(CurrentHp(tadpole), Is.EqualTo(950));
                    if (i == 0)
                        Assert.That(ServedObjectRenderer(target).sprite.name, Is.EqualTo("MiniRockPunch01"));
                }
                yield return PlayUntil(2.36f);
                Assert.That(body.sprite.name, Is.EqualTo("FireTadpoleIdle"));
            }
            finally
            {
                Time.captureFramerate = priorRate;
            }
        }

        [UnityTest]
        public IEnumerator LightningTadpoleLandsWalksAndFinishesItsOwnHits()
        {
            Create();
            Assert.That(Configure("dimension_toad"), Is.True);
            var scenarios = (JArray)Recording("dimension_toad")["scenarios"];
            Scenario(scenarios.IndexOf(scenarios.Single(s => (string)s["id"] == "ground")));
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/LightningTadpoleCaptures");
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            if (capture) System.IO.Directory.CreateDirectory(directory);
            int priorRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(1.465f);
                Component first = Target(5);
                var firstBody = ServedObjectRenderer(first);
                Assert.That(TimeToLive(first), Is.EqualTo(8f).Within(.001f));
                Assert.That(firstBody.sprite.name, Is.EqualTo("LightningTadpoleIdle"));
                Assert.That(first.GetComponent("CrawlMotionController"), Is.Null);
                Assert.That(first.GetComponentsInChildren<Transform>(true).Any(child => child.name == "TTLBar"), Is.True);

                var seen = new System.Collections.Generic.HashSet<string>();
                while ((float)Field("elapsed") < 2.68f)
                {
                    string name = firstBody.sprite.name;
                    if (name.StartsWith("LightningTadpoleWalk", StringComparison.Ordinal) && seen.Add(name) && capture)
                        SaveViewport(directory, name, true);
                    yield return null;
                }
                CollectionAssert.AreEquivalent(Enumerable.Range(1, 8).Select(i => $"LightningTadpoleWalk{i:00}"), seen);

                yield return PlayUntil(3.127f);
                Component second = Target(7);
                var secondBody = ServedObjectRenderer(second);
                var world = (Component)Field("world");
                Vector3 before = world.transform.InverseTransformPoint(second.transform.position);
                yield return PlayUntil(3.315f);
                Vector3 airborne = world.transform.InverseTransformPoint(second.transform.position);
                Assert.That(airborne.y, Is.GreaterThan(.03f));
                Assert.That(airborne.x, Is.GreaterThan(before.x));
                Assert.That(secondBody.sprite.name, Is.EqualTo("LightningTadpoleIdle"));
                if (capture) SaveViewport(directory, "before-landing", true);
                yield return PlayUntil(3.370f);
                Assert.That(secondBody.sprite.name, Is.EqualTo("LightningTadpoleIdle"));
                yield return PlayUntil(3.462f);
                Assert.That(secondBody.sprite.name, Does.StartWith("LightningTadpoleWalk"));
                Assert.That(CurrentHp(Target(2)), Is.EqualTo(9900));
                for (int i = 0; i < 4; i++)
                {
                    yield return PlayUntil(3.525f + .08f * i);
                    string pose = $"LightningTadpoleZap{i + 1:00}";
                    Assert.That(firstBody.sprite.name, Is.EqualTo(pose));
                    Assert.That(firstBody.flipX, Is.False);
                    Assert.That(secondBody.sprite.name, Does.Not.StartWith("LightningTadpoleZap"));
                    // 세 번째 자세를 확인하기 전에 화산편의 3.65초 피해가 따로 들어온다.
                    Assert.That(CurrentHp(Target(2)), Is.EqualTo(i < 2 ? 9800 : 9700));
                    if (capture) SaveViewport(directory, pose, true);
                }

                yield return PlayUntil(4.322f);
                Assert.That(firstBody.sprite.name, Is.EqualTo("LightningTadpoleZap01"));
                Assert.That(secondBody.sprite.name, Does.Not.StartWith("LightningTadpoleZap"));
                Assert.That(CurrentHp(Target(2)), Is.EqualTo(9500));
                yield return PlayUntil(5.124f);
                Assert.That(firstBody.sprite.name, Is.EqualTo("LightningTadpoleZap01"));
                Assert.That(secondBody.sprite.name, Does.Not.StartWith("LightningTadpoleZap"));
                Assert.That(CurrentHp(Target(2)), Is.EqualTo(9200));
                yield return PlayUntil(5.324f);
                Assert.That(firstBody.sprite.name, Is.EqualTo("LightningTadpoleZap03"));
                Assert.That(secondBody.sprite.name, Is.EqualTo("LightningTadpoleZap01"));
                Assert.That(CurrentHp(Target(2)), Is.EqualTo(9000));
                if (capture) SaveViewport(directory, "independent-attacks", true);

                yield return PlayUntil(8.972f);
                Assert.That(Target(5), Is.SameAs(first));
                Assert.That(Target(7), Is.SameAs(second));
                Assert.That(TimeToLive(first), Is.EqualTo(.5f).Within(.001f));
                Assert.That(TimeToLive(second), Is.EqualTo(2.1f).Within(.001f));
            }
            finally
            {
                Time.captureFramerate = priorRate;
            }
        }

        [UnityTest]
        public IEnumerator LightningTadpoleRepeatsItsStrikeAfterTheToadRetreats()
        {
            Create();
            Assert.That(Configure("dimension_toad"), Is.True);
            var scenarios = (JArray)Recording("dimension_toad")["scenarios"];
            Scenario(scenarios.IndexOf(scenarios.Single(s => (string)s["id"] == "panic")));
            int priorRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(4.126f);
                var attacker = ServedObjectRenderer(Target(5));
                var target = Target(2);
                Assert.That(CurrentHp(target), Is.EqualTo(700));
                float[] hitTimes = { 4.15f, 4.95f, 5.75f };
                float[] hp = { 600f, 300f, 0f };
                for (int i = 0; i < hitTimes.Length; i++)
                {
                    yield return PlayUntil(hitTimes[i] + .023f);
                    Assert.That(attacker.sprite.name, Is.EqualTo("LightningTadpoleZap01"));
                    Assert.That(CurrentHp(target), Is.EqualTo(hp[i]));
                }
            }
            finally
            {
                Time.captureFramerate = priorRate;
            }
        }

        [UnityTest]
        public IEnumerator ElectricSlimeMovesAndEachSummonKeepsItsOwnContactPose()
        {
            Create();
            const string magic = "lightning_slime_nest";
            Assert.That(Configure(magic), Is.True);
            var scenarios = (JArray)Recording(magic)["scenarios"];
            var ground = scenarios.Single(s => (string)s["id"] == "ground");
            Scenario(scenarios.IndexOf(ground));
            CollectionAssert.AreEqual(new[] { 2 }, ground["fixtureTargetIds"].Select(id => (int)id));
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/ElectricSlimeCaptures");
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            if (capture) System.IO.Directory.CreateDirectory(directory);

            SpriteRenderer SlimeBody(int id)
            {
                var slime = Target(id);
                Assert.That(slime, Is.Not.Null);
                Assert.That(slime.name, Does.StartWith("ElectricSlime"));
                Assert.That(slime.GetType().GetMethod("GetMaster").Invoke(slime, null), Is.EqualTo("LeftPlayer"));
                Assert.That(slime.GetComponent("CrawlMotionController"), Is.Null);
                return ServedObjectRenderer(slime);
            }

            int prior = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(2.724f);
                var first = SlimeBody(4);
                var source = Resources.Load<Sprite>("Game/spawn/lightning_slime");
                Assert.That(first.sprite.name, Is.EqualTo("ElectricSlimeIdle"));
                Assert.That(first.sprite.rect.size, Is.EqualTo(source.rect.size));
                Assert.That(first.sprite.pivot, Is.EqualTo(source.pivot));
                Assert.That(first.sprite.pixelsPerUnit, Is.EqualTo(source.pixelsPerUnit));

                var seen = new System.Collections.Generic.HashSet<string>();
                while ((float)Field("elapsed") < 3.93f)
                {
                    string pose = first.sprite.name;
                    if (pose.StartsWith("ElectricSlimeCrawl", StringComparison.Ordinal) && seen.Add(pose) && capture)
                        SaveViewport(directory, pose, true);
                    yield return null;
                }
                CollectionAssert.AreEquivalent(Enumerable.Range(1, 8).Select(i => $"ElectricSlimeCrawl{i:00}"), seen);

                yield return PlayUntil(4.723f);
                var target = Target(2);
                Assert.That(target.GetType().GetMethod("GetMaster").Invoke(target, null), Is.EqualTo("RightPlayer"));
                Assert.That(CurrentHp(target), Is.EqualTo(10000));
                for (int i = 0; i < 4; i++)
                {
                    yield return PlayUntil(4.775f + .08f * i);
                    string pose = $"ElectricSlimeBump{i + 1:00}";
                    Assert.That(first.sprite.name, Is.EqualTo(pose));
                    Assert.That(first.flipX, Is.False);
                    Assert.That(CurrentHp(target), Is.EqualTo(9900));
                    if (capture) SaveViewport(directory, pose, true);
                }

                var second = SlimeBody(5);
                yield return PlayUntil(6.821f);
                Assert.That(CurrentHp(target), Is.EqualTo(9700));
                yield return PlayUntil(6.875f);
                Assert.That(second.sprite.name, Is.EqualTo("ElectricSlimeBump01"));
                Assert.That(first.sprite.name, Does.Not.StartWith("ElectricSlimeBump"));
                Assert.That(CurrentHp(target), Is.EqualTo(9600));

                yield return PlayUntil(11.076f);
                var later = SlimeBody(7);
                Assert.That(CurrentHp(target), Is.EqualTo(8300));
                yield return PlayUntil(11.123f);
                Assert.That(later.sprite.name, Is.EqualTo("ElectricSlimeBump01"));
                Assert.That(first.sprite.name, Does.Not.StartWith("ElectricSlimeBump"));
                Assert.That(CurrentHp(target), Is.EqualTo(8200));
                yield return PlayUntil(11.207f);
                Assert.That(later.sprite.name, Is.EqualTo("ElectricSlimeBump02"));
                Assert.That(first.sprite.name, Is.EqualTo("ElectricSlimeBump01"));
                Assert.That(CurrentHp(target), Is.EqualTo(8100));
                if (capture) SaveViewport(directory, "independent-hits", true);
            }
            finally
            {
                Time.captureFramerate = prior;
            }
        }

        [UnityTest]
        public IEnumerator FireSlimeMovesAndStrikesWithoutMovingItsFireTrail()
        {
            Create();
            const string magic = "fire_slime_nest";
            Assert.That(Configure(magic), Is.True);
            var scenarios = (JArray)Recording(magic)["scenarios"];
            var ground = scenarios.Single(s => (string)s["id"] == "ground");
            Scenario(scenarios.IndexOf(ground));
            CollectionAssert.AreEqual(new[] { 2 }, ground["fixtureTargetIds"].Select(id => (int)id));
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/FireSlimeCaptures");
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            if (capture) System.IO.Directory.CreateDirectory(directory);
            var world = (Component)Field("world");

            SpriteRenderer SlimeBody(int id)
            {
                var slime = Target(id);
                Assert.That(slime, Is.Not.Null);
                Assert.That(slime.name, Does.StartWith("FireSlime"));
                Assert.That(slime.GetType().GetMethod("GetMaster").Invoke(slime, null), Is.EqualTo("LeftPlayer"));
                Assert.That(slime.GetComponent("CrawlMotionController"), Is.Null);
                return ServedObjectRenderer(slime);
            }

            Vector3 LocalPosition(Component subject) => world.transform.InverseTransformPoint(subject.transform.position);
            Component FieldAt(int id, float x)
            {
                var field = Target(id);
                Assert.That(field, Is.Not.Null);
                Assert.That(field.name, Does.StartWith("FireField"));
                Assert.That(Vector3.Distance(LocalPosition(field), new Vector3(x, 0f, 5f)), Is.LessThan(.008f));
                return field;
            }

            int prior = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(2.724f);
                var slime = Target(4);
                var body = SlimeBody(4);
                var source = Resources.Load<Sprite>("Game/spawn/fire_slime");
                Assert.That(body.sprite.name, Is.EqualTo("FireSlimeIdle"));
                Assert.That(body.sprite.rect.size, Is.EqualTo(source.rect.size));
                Assert.That(body.sprite.pivot, Is.EqualTo(source.pivot));
                Assert.That(body.sprite.pixelsPerUnit, Is.EqualTo(source.pixelsPerUnit));
                var poses = new System.Collections.Generic.List<string>();
                while ((float)Field("elapsed") < 3.33f)
                {
                    string pose = body.sprite.name;
                    if (pose.StartsWith("FireSlimeCrawl", StringComparison.Ordinal) && pose != poses.LastOrDefault())
                    {
                        poses.Add(pose);
                        if (capture && poses.Count <= 8) SaveViewport(directory, pose, true);
                    }
                    yield return null;
                }
                var expected = Enumerable.Range(1, 8).Select(i => $"FireSlimeCrawl{i:00}").Append("FireSlimeCrawl01");
                CollectionAssert.AreEqual(expected, poses.Take(9));

                Assert.That(Target(5), Is.Null);
                yield return PlayUntil(3.373f);
                var firstField = FieldAt(5, 5.10f);
                Assert.That(TimeToLive(firstField), Is.EqualTo(8f).Within(.001f));
                Assert.That(LocalPosition(slime).x, Is.GreaterThanOrEqualTo(5.09f));
                if (capture) SaveViewport(directory, "first-fire-field", true);

                yield return PlayUntil(3.876f);
                Assert.That(Target(6), Is.Null);
                yield return PlayUntil(3.923f);
                var secondField = FieldAt(6, 6.20f);
                Assert.That(TimeToLive(secondField), Is.EqualTo(8f).Within(.001f));
                Assert.That(FieldAt(5, 5.10f), Is.SameAs(firstField));
                Assert.That(TimeToLive(firstField), Is.EqualTo(7.45f).Within(.001f));
                if (capture) SaveViewport(directory, "two-ground-fields", true);

                yield return PlayUntil(4.723f);
                var enemy = Target(2);
                Assert.That(CurrentHp(enemy), Is.EqualTo(10000));
                for (int i = 0; i < 4; i++)
                {
                    yield return PlayUntil(4.775f + .08f * i);
                    string pose = $"FireSlimeBump{i + 1:00}";
                    Assert.That(body.sprite.name, Is.EqualTo(pose));
                    Assert.That(body.flipX, Is.False);
                    Assert.That(CurrentHp(enemy), Is.EqualTo(9900));
                    if (capture) SaveViewport(directory, pose, true);
                }

                var second = SlimeBody(7);
                yield return PlayUntil(6.821f);
                Assert.That(CurrentHp(enemy), Is.EqualTo(9700));
                yield return PlayUntil(6.875f);
                Assert.That(second.sprite.name, Is.EqualTo("FireSlimeBump01"));
                Assert.That(body.sprite.name, Does.Not.StartWith("FireSlimeBump"));
                Assert.That(CurrentHp(enemy), Is.EqualTo(9600));

                yield return PlayUntil(11.076f);
                var later = SlimeBody(13);
                Assert.That(CurrentHp(enemy), Is.EqualTo(8300));
                yield return PlayUntil(11.124f);
                Assert.That(later.sprite.name, Is.EqualTo("FireSlimeBump01"));
                Assert.That(body.sprite.name, Does.Not.StartWith("FireSlimeBump"));
                Assert.That(CurrentHp(enemy), Is.EqualTo(8200));
                yield return PlayUntil(11.208f);
                Assert.That(later.sprite.name, Is.EqualTo("FireSlimeBump02"));
                Assert.That(body.sprite.name, Is.EqualTo("FireSlimeBump01"));
                Assert.That(CurrentHp(enemy), Is.EqualTo(8100));
                if (capture) SaveViewport(directory, "independent-hits", true);
                yield return PlayUntil(11.515f);
                Assert.That(Target(5), Is.Null);
                Assert.That(FieldAt(6, 6.20f), Is.SameAs(secondField));
            }
            finally
            {
                Time.captureFramerate = prior;
            }
        }

        [UnityTest]
        public IEnumerator LeafSlimeCrawlsAndStrikesBesideItsIndependentLeafFields()
        {
            Create();
            const string magic = "nature_slime_nest";
            Assert.That(Configure(magic), Is.True);
            var scenarios = (JArray)Recording(magic)["scenarios"];
            var ground = scenarios.Single(s => (string)s["id"] == "ground");
            Scenario(scenarios.IndexOf(ground));
            CollectionAssert.AreEqual(new[] { 2 }, ground["fixtureTargetIds"].Select(id => (int)id));
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/LeafSlimeCaptures");
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            if (capture) System.IO.Directory.CreateDirectory(directory);
            var world = (Component)Field("world");

            Vector3 LocalPosition(Component subject) => world.transform.InverseTransformPoint(subject.transform.position);

            Component FieldAt(int id, float x, float ttl)
            {
                var field = Target(id);
                Assert.That(field, Is.Not.Null);
                Assert.That(field.name, Does.StartWith("LeafField"));
                Assert.That(Vector3.Distance(LocalPosition(field), new Vector3(x, 0f, 5f)), Is.LessThan(.008f));
                Assert.That(TimeToLive(field), Is.EqualTo(ttl).Within(.001f));
                return field;
            }

            int prior = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(10.674f);
                var slime = Target(4);
                Assert.That(slime, Is.Not.Null);
                Assert.That(slime.name, Does.StartWith("LeafSlime"));
                Assert.That(slime.GetType().GetMethod("GetMaster").Invoke(slime, null), Is.EqualTo("LeftPlayer"));
                Assert.That(slime.GetComponent("CrawlMotionController"), Is.Null);
                var body = ServedObjectRenderer(slime);
                var source = Resources.Load<Sprite>("Game/spawn/leaf_slime");
                Assert.That(body.sprite.name, Is.EqualTo("LeafSlimeIdle"));
                Assert.That(body.sprite.rect.size, Is.EqualTo(source.rect.size));
                Assert.That(body.sprite.pivot, Is.EqualTo(source.pivot));
                Assert.That(body.sprite.pixelsPerUnit, Is.EqualTo(source.pixelsPerUnit));

                var poses = new System.Collections.Generic.List<string>();
                while ((float)Field("elapsed") < 11.28f)
                {
                    string pose = body.sprite.name;
                    if (pose.StartsWith("LeafSlimeCrawl", StringComparison.Ordinal) && pose != poses.LastOrDefault())
                    {
                        poses.Add(pose);
                        if (capture && poses.Count <= 8) SaveViewport(directory, pose, true);
                    }
                    yield return null;
                }
                var expected = Enumerable.Range(1, 8).Select(i => $"LeafSlimeCrawl{i:00}").Append("LeafSlimeCrawl01");
                CollectionAssert.AreEqual(expected, poses.Take(9));

                Assert.That(Target(5), Is.Null);
                yield return PlayUntil(11.324f);
                var first = FieldAt(5, 5.10f, 8f);
                if (capture) SaveViewport(directory, "first-leaf-field", true);
                yield return PlayUntil(11.824f);
                Assert.That(Target(6), Is.Null);
                yield return PlayUntil(11.874f);
                var second = FieldAt(6, 6.20f, 8f);
                Assert.That(FieldAt(5, 5.10f, 7.45f), Is.SameAs(first));
                if (capture) SaveViewport(directory, "two-ground-fields", true);

                yield return PlayUntil(12.674f);
                var enemy = Target(2);
                Assert.That(CurrentHp(enemy), Is.EqualTo(10000));
                Assert.That(body.sprite.name, Is.EqualTo("LeafSlimeIdle"));
                for (int i = 0; i < 4; i++)
                {
                    yield return PlayUntil(12.725f + .08f * i);
                    string pose = $"LeafSlimeBump{i + 1:00}";
                    Assert.That(body.sprite.name, Is.EqualTo(pose));
                    Assert.That(body.flipX, Is.False);
                    Assert.That(CurrentHp(enemy), Is.EqualTo(9900));
                    if (i == 0)
                    {
                        Assert.That(FieldAt(5, 5.10f, 6.60f), Is.SameAs(first));
                        Assert.That(FieldAt(6, 6.20f, 7.15f), Is.SameAs(second));
                    }
                    if (capture) SaveViewport(directory, pose, true);
                }

                yield return PlayUntil(13.124f);
                Assert.That(body.sprite.name, Is.EqualTo("LeafSlimeIdle"));
                yield return PlayUntil(13.524f);
                Assert.That(body.sprite.name, Is.EqualTo("LeafSlimeBump01"));
                Assert.That(CurrentHp(enemy), Is.EqualTo(9800));
                if (capture) SaveViewport(directory, "second-hit", true);
            }
            finally
            {
                Time.captureFramerate = prior;
            }
        }

        [UnityTest]
        public IEnumerator RockSlimeCrawlsAndShowsIndependentRecordedHits() =>
            CheckSlimeRecordedHits("RockSlime", "rock_slime_nest", "Game/spawn/rock_slime");

        [UnityTest]
        public IEnumerator WindSlimeCrawlsAndShowsIndependentRecordedHits() =>
            CheckSlimeRecordedHits("WindSlime", "wind_slime_nest", "Game/spawn/wind_slime");

        private IEnumerator CheckSlimeRecordedHits(string prefabName, string magic, string sourceSprite)
        {
            Create();
            Assert.That(Configure(magic), Is.True);
            var scenarios = (JArray)Recording(magic)["scenarios"];
            var ground = scenarios.Single(s => (string)s["id"] == "ground");
            Scenario(scenarios.IndexOf(ground));
            CollectionAssert.AreEqual(new[] { 2 }, ground["fixtureTargetIds"].Select(id => (int)id));
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library", $"{prefabName}Captures");
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            if (capture) System.IO.Directory.CreateDirectory(directory);

            SpriteRenderer SlimeBody(int id)
            {
                var slime = Target(id);
                Assert.That(slime, Is.Not.Null);
                Assert.That(slime.name, Does.StartWith(prefabName));
                Assert.That(slime.GetType().GetMethod("GetMaster").Invoke(slime, null), Is.EqualTo("LeftPlayer"));
                Assert.That(slime.GetComponent("CrawlMotionController"), Is.Null);
                Assert.That(slime.GetComponent("HoppingMotionController"), Is.Null);
                return ServedObjectRenderer(slime);
            }

            int prior = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(2.674f);
                Assert.That(Target(4), Is.Null);
                yield return PlayUntil(2.724f);
                var body = SlimeBody(4);
                var source = Resources.Load<Sprite>(sourceSprite);
                Assert.That(body.sprite.name, Is.EqualTo($"{prefabName}Idle"));
                Assert.That(body.sprite.rect.size, Is.EqualTo(source.rect.size));
                Assert.That(body.sprite.pivot, Is.EqualTo(source.pivot));
                Assert.That(body.sprite.pixelsPerUnit, Is.EqualTo(source.pixelsPerUnit));

                var poses = new System.Collections.Generic.List<string>();
                while ((float)Field("elapsed") < 3.375f)
                {
                    string pose = body.sprite.name;
                    if (pose.StartsWith($"{prefabName}Crawl", StringComparison.Ordinal) && pose != poses.LastOrDefault())
                    {
                        poses.Add(pose);
                        if (capture && poses.Count <= 8) SaveViewport(directory, pose, true);
                    }
                    yield return null;
                }
                var expected = Enumerable.Range(1, 8).Select(i => $"{prefabName}Crawl{i:00}").Append($"{prefabName}Crawl01");
                CollectionAssert.AreEqual(expected, poses.Take(9));

                yield return PlayUntil(4.723f);
                var enemy = Target(2);
                Assert.That(CurrentHp(enemy), Is.EqualTo(10000));
                Assert.That(body.sprite.name, Is.EqualTo($"{prefabName}Idle"));
                for (int i = 0; i < 4; i++)
                {
                    yield return PlayUntil(4.775f + .08f * i);
                    string pose = $"{prefabName}Bump{i + 1:00}";
                    Assert.That(body.sprite.name, Is.EqualTo(pose));
                    Assert.That(body.flipX, Is.False);
                    Assert.That(CurrentHp(enemy), Is.EqualTo(9900));
                    if (capture) SaveViewport(directory, pose, true);
                }

                yield return PlayUntil(5.524f);
                Assert.That(body.sprite.name, Is.EqualTo($"{prefabName}Idle"));
                yield return PlayUntil(5.575f);
                Assert.That(body.sprite.name, Is.EqualTo($"{prefabName}Bump01"));
                Assert.That(CurrentHp(enemy), Is.EqualTo(9800));
                if (capture) SaveViewport(directory, "repeat-hit", true);

                yield return PlayUntil(11.076f);
                var other = SlimeBody(7);
                Assert.That(CurrentHp(enemy), Is.EqualTo(8300));
                yield return PlayUntil(11.124f);
                Assert.That(other.sprite.name, Is.EqualTo($"{prefabName}Bump01"));
                Assert.That(body.sprite.name, Does.Not.StartWith($"{prefabName}Bump"));
                Assert.That(SlimeBody(8).sprite.name, Is.EqualTo($"{prefabName}Idle"));
                Assert.That(CurrentHp(enemy), Is.EqualTo(8200));
                yield return PlayUntil(11.208f);
                Assert.That(other.sprite.name, Is.EqualTo($"{prefabName}Bump02"));
                Assert.That(body.sprite.name, Is.EqualTo($"{prefabName}Bump01"));
                Assert.That(CurrentHp(enemy), Is.EqualTo(8100));
                if (capture) SaveViewport(directory, "independent-hits", true);
            }
            finally
            {
                Time.captureFramerate = prior;
            }
        }

        [UnityTest]
        public IEnumerator WaterSlimeSwarmCrawlsAndLaunchesOnItsOwnTicks()
        {
            Create();
            const string magic = "water_slime_swarm";
            Assert.That(Configure(magic), Is.True);
            var scenarios = (JArray)Recording(magic)["scenarios"];
            var ground = scenarios.Single(s => (string)s["id"] == "ground");
            Scenario(scenarios.IndexOf(ground));
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/WaterSlimeCaptures");
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            if (capture) System.IO.Directory.CreateDirectory(directory);
            var world = (Component)Field("world");

            void AssertWaterField(Component field, float ttl)
            {
                Assert.That(Target(6), Is.SameAs(field));
                Assert.That(field.name, Does.StartWith("WaterField"));
                Vector3 local = world.transform.InverseTransformPoint(field.transform.position);
                Assert.That(Vector3.Distance(local, new Vector3(5.000978f, 0f, 5f)), Is.LessThan(.008f));
                Assert.That(TimeToLive(field), Is.EqualTo(ttl).Within(.001f));
            }

            int prior = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(.624f);
                var body = ServedObjectRenderer(Target(4));
                var other = ServedObjectRenderer(Target(3));
                var third = ServedObjectRenderer(Target(5));
                var enemy = Target(2);
                Assert.That(body.sprite.name, Is.EqualTo("WaterSlime"));
                Assert.That(Target(4).GetComponent("CrawlMotionController"), Is.Null);
                Assert.That(Target(6), Is.Null);

                var poses = new System.Collections.Generic.List<string>();
                while ((float)Field("elapsed") < 1.223f)
                {
                    string pose = body.sprite.name;
                    if (pose.StartsWith("WaterSlimeCrawl", StringComparison.Ordinal) && pose != poses.LastOrDefault())
                    {
                        poses.Add(pose);
                        if (capture && poses.Count <= 8) SaveViewport(directory, pose, true);
                    }
                    yield return null;
                }
                var expected = Enumerable.Range(1, 8).Select(i => $"WaterSlimeCrawl{i:00}").Append("WaterSlimeCrawl01");
                CollectionAssert.AreEqual(expected, poses.Take(9));
                var firstField = Target(6);
                Assert.That(firstField, Is.Not.Null);
                AssertWaterField(firstField, 3f);
                if (capture) SaveViewport(directory, "first-water-field", true);

                yield return PlayUntil(2.223f);
                Assert.That(body.sprite.name, Is.EqualTo("WaterSlime"));
                Assert.That(CurrentHp(enemy), Is.EqualTo(1000));
                yield return PlayUntil(2.274f);
                Assert.That(body.sprite.name, Is.EqualTo("WaterSlimeSpit01"));
                Assert.That(other.sprite.name, Is.EqualTo("WaterSlime"));
                Assert.That(third.sprite.name, Is.EqualTo("WaterSlime"));
                Assert.That(CurrentHp(enemy), Is.EqualTo(900));
                AssertWaterField(firstField, 1.95f);
                var content = world.transform.Find("PresentationContent");
                Assert.That(content.Cast<Transform>().Count(child => child.name == "WaterShot(Clone)"), Is.EqualTo(1));
                if (capture) SaveViewport(directory, "first-launch", true);

                yield return PlayUntil(2.324f);
                Assert.That(CurrentHp(enemy), Is.EqualTo(700));
                AssertWaterField(firstField, 1.90f);
                Assert.That(content.Cast<Transform>().Count(child => child.name == "WaterShot(Clone)"), Is.EqualTo(3));
                for (int index = 1; index < 4; index++)
                {
                    yield return PlayUntil(2.354f + .08f * (index - 1));
                    Assert.That(body.sprite.name, Is.EqualTo($"WaterSlimeSpit{index + 1:00}"));
                    Assert.That(other.sprite.name, Is.EqualTo($"WaterSlimeSpit{index:00}"));
                    Assert.That(third.sprite.name, Is.EqualTo($"WaterSlimeSpit{index:00}"));
                    if (capture && index == 1) SaveViewport(directory, "independent-launches", true);
                }

                yield return PlayUntil(2.624f);
                Assert.That(body.sprite.name, Is.EqualTo("WaterSlime"));
                yield return PlayUntil(3.074f);
                Assert.That(body.sprite.name, Is.EqualTo("WaterSlimeSpit01"));
                Assert.That(CurrentHp(enemy), Is.EqualTo(600));
            }
            finally
            {
                Time.captureFramerate = prior;
            }
        }

        [UnityTest]
        public IEnumerator PveWaterSlimeNestUsesTheSameWaterSlimeShotPose()
        {
            Create();
            const string magic = "pve_water_slime_nest";
            Assert.That(Configure(magic), Is.True);
            var scenarios = (JArray)Recording(magic)["scenarios"];
            Scenario(scenarios.IndexOf(scenarios.Single(s => (string)s["id"] == "ground")));
            int prior = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(11.524f);
                var body = ServedObjectRenderer(Target(4));
                var enemy = Target(2);
                Assert.That(CurrentHp(enemy), Is.EqualTo(10000));
                Assert.That(body.sprite.name, Is.EqualTo("WaterSlime"));
                yield return PlayUntil(11.574f);
                Assert.That(body.sprite.name, Is.EqualTo("WaterSlimeSpit01"));
                Assert.That(CurrentHp(enemy), Is.EqualTo(9900));
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    string directory = System.IO.Path.Combine(Application.dataPath, "../Library/WaterSlimeCaptures");
                    System.IO.Directory.CreateDirectory(directory);
                    SaveViewport(directory, "nest-launch", true);
                }
            }
            finally
            {
                Time.captureFramerate = prior;
            }
        }

        [UnityTest]
        public IEnumerator WaterShotShowsEveryPoseDuringBubbleGeneratorFlight() =>
            CheckRecordedProjectileFlight("WaterShot", "Game/shoot/water_shoot", "bubble_generator", "support", 1.40f, .125f);

        [UnityTest]
        public IEnumerator WaterShotShowsEveryPoseDuringWaterSlimeFlight() =>
            CheckRecordedProjectileFlight("WaterShot", "Game/shoot/water_shoot", "water_slime_swarm", "ground", 2.25f, .5f);

        [UnityTest]
        public IEnumerator FireShotShowsEveryPoseDuringFireLordFlight() =>
            CheckRecordedProjectileFlight("FireShot", "Game/sprites/FireShot", "fire_lord_spirit", "ground", 6.55f, .4f);

        private IEnumerator CheckRecordedProjectileFlight(string projectileName, string sourceSprite, string magic,
            string scenarioId, float launch, float duration)
        {
            Create();
            Assert.That(Configure(magic), Is.True);
            var scenarios = (JArray)Recording(magic)["scenarios"];
            Scenario(scenarios.IndexOf(scenarios.Single(s => (string)s["id"] == scenarioId)));
            var world = (Component)Field("world");
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library", $"{projectileName}Captures");
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            if (capture) System.IO.Directory.CreateDirectory(directory);

            Transform FirstProjectile() => world.transform.Find("PresentationContent").Cast<Transform>()
                .FirstOrDefault(child => child.name == $"{projectileName}(Clone)");

            int prior = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(launch - .023f);
                Assert.That(FirstProjectile(), Is.Null);
                yield return PlayUntil(launch + .012f);
                Transform shot = FirstProjectile();
                Assert.That(shot, Is.Not.Null);
                var source = Resources.Load<Sprite>(sourceSprite);
                var renderer = shot.GetComponentInChildren<SpriteRenderer>();
                Assert.That(renderer.sprite.name, Is.EqualTo($"{projectileName}Flight01"));
                Assert.That(renderer.sprite.rect.size, Is.EqualTo(source.rect.size));
                Assert.That(renderer.sprite.pivot, Is.EqualTo(source.pivot));
                Assert.That(renderer.sprite.pixelsPerUnit, Is.EqualTo(source.pixelsPerUnit));
                Vector3 initialPosition = shot.position;
                Vector3 lastPosition = initialPosition;
                var poses = new System.Collections.Generic.List<string>();
                while (shot != null && (float)Field("elapsed") < launch + duration + .08f)
                {
                    string pose = renderer.sprite.name;
                    if (poses.LastOrDefault() != pose)
                    {
                        poses.Add(pose);
                        if (capture) SaveViewport(directory, $"{magic}-{pose}", true);
                    }
                    lastPosition = shot.position;
                    yield return null;
                }
                CollectionAssert.AreEqual(Enumerable.Range(1, 4).Select(i => $"{projectileName}Flight{i:00}"), poses);
                Assert.That(Vector3.Distance(initialPosition, lastPosition), Is.GreaterThan(.04f));
                Assert.That(shot == null, Is.True);
                Assert.That((float)Field("elapsed"), Is.InRange(launch + duration - .02f, launch + duration + .05f));
            }
            finally
            {
                Time.captureFramerate = prior;
            }
        }

        [UnityTest]
        public IEnumerator CastWaterShotRepeatsFlightPosesAndKeepsItsRecordedImpact()
        {
            Create();
            const string magic = "water_shot";
            Assert.That(Configure(magic), Is.True);
            var scenarios = (JArray)Recording(magic)["scenarios"];
            Scenario(scenarios.IndexOf(scenarios.Single(s => (string)s["id"] == "impact")));
            var world = (Component)Field("world");
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/WaterShotCaptures");
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            if (capture) System.IO.Directory.CreateDirectory(directory);

            int prior = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(.622f);
                var shot = Target(4);
                Assert.That(shot, Is.Not.Null);
                var renderer = ServedObjectRenderer(shot);
                var phaseOrder = new System.Collections.Generic.List<string>();
                while ((float)Field("elapsed") < 1.122f)
                {
                    string pose = renderer.sprite.name;
                    if (phaseOrder.LastOrDefault() != pose)
                    {
                        phaseOrder.Add(pose);
                        if (capture && phaseOrder.Count <= 4) SaveViewport(directory, $"cast-{pose}", true);
                    }
                    yield return null;
                }
                var expected = Enumerable.Range(1, 4).Select(i => $"WaterShotFlight{i:00}").Append("WaterShotFlight01");
                CollectionAssert.AreEqual(expected, phaseOrder.Take(5));

                var first = Target(2);
                var second = Target(3);
                Assert.That(CurrentHp(first), Is.EqualTo(1000));
                Assert.That(CurrentHp(second), Is.EqualTo(1000));
                Assert.That(world.transform.InverseTransformPoint(shot.transform.position).x, Is.InRange(5.6f, 6.1f));
                if (capture) SaveViewport(directory, "cast-before-hit", true);

                yield return PlayUntil(1.174f);
                Assert.That(CurrentHp(first), Is.EqualTo(900));
                Assert.That(CurrentHp(second), Is.EqualTo(900));
                Assert.That(first.GetType().GetMethod("HasEffect").Invoke(first, new object[] { "Wet" }), Is.True);
                Assert.That(Target(4), Is.SameAs(shot));
                Assert.That(shot.GetType().GetProperty("IsDespawning", Private).GetValue(shot), Is.True);
                if (capture) SaveViewport(directory, "cast-hit", true);

                yield return PlayUntil(1.244f);
                Assert.That(Target(4), Is.Null);
            }
            finally
            {
                Time.captureFramerate = prior;
            }
        }

        [UnityTest]
        public IEnumerator WindBladeKeepsCyclingAcrossThreeRecordedPiercingHits()
        {
            Create();
            const string magic = "wind_blade";
            Assert.That(Configure(magic), Is.True);
            var scenarios = (JArray)Recording(magic)["scenarios"];
            Scenario(scenarios.IndexOf(scenarios.Single(s => (string)s["id"] == "pierce")));
            var world = (Component)Field("world");
            const int bladeId = 5;
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/WindBladeCaptures");
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            if (capture) System.IO.Directory.CreateDirectory(directory);

            int prior = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(.575f);
                Assert.That(Target(bladeId), Is.Null);
                yield return PlayUntil(.624f);
                var blade = Target(bladeId);
                Assert.That(blade, Is.Not.Null);
                var renderer = ServedObjectRenderer(blade);
                var source = Resources.Load<Sprite>("Game/sprites/WindBlade");
                Assert.That(renderer.sprite.name, Is.EqualTo("WindBladeFlight01"));
                Assert.That(renderer.sprite.rect.size, Is.EqualTo(source.rect.size));
                Assert.That(renderer.sprite.pivot, Is.EqualTo(source.pivot));
                Assert.That(renderer.sprite.pixelsPerUnit, Is.EqualTo(source.pixelsPerUnit));
                Vector3 start = world.transform.InverseTransformPoint(blade.transform.position);
                Assert.That(Vector3.Distance(start, new Vector3(2f, 0f, 5f)), Is.LessThan(.008f));
                var poses = new System.Collections.Generic.List<string>();

                IEnumerator ObserveUntil(float moment)
                {
                    do
                    {
                        string pose = renderer.sprite.name;
                        if (poses.LastOrDefault() != pose)
                        {
                            poses.Add(pose);
                            if (capture && poses.Count <= 8) SaveViewport(directory, pose, true);
                        }
                        if ((float)Field("elapsed") >= moment) yield break;
                        yield return null;
                    } while (true);
                }

                foreach (var hit in new[]
                {
                    (time: .90f, target: 2, remainingHp: 900f, pose: 3),
                    (time: 1.15f, target: 3, remainingHp: 934f, pose: 5),
                    (time: 1.40f, target: 4, remainingHp: 956f, pose: 7)
                })
                {
                    yield return ObserveUntil(hit.time - .024f);
                    Assert.That(CurrentHp(Target(hit.target)), Is.EqualTo(1000));
                    yield return ObserveUntil(hit.time + .024f);
                    Assert.That(CurrentHp(Target(hit.target)), Is.EqualTo(hit.remainingHp));
                    Assert.That(Target(bladeId), Is.SameAs(blade));
                    Assert.That(renderer.sprite.name, Is.EqualTo($"WindBladeFlight{hit.pose:00}"));
                    if (capture) SaveViewport(directory, $"pierce-{hit.target}", true);
                }

                yield return ObserveUntil(1.614f);
                var expected = Enumerable.Range(1, 8).Select(i => $"WindBladeFlight{i:00}").Append("WindBladeFlight01");
                CollectionAssert.AreEqual(expected, poses.Take(9));

                var despawning = blade.GetType().GetProperty("IsDespawning", Private);
                yield return PlayUntil(2.624f);
                Assert.That(despawning.GetValue(blade), Is.False);
                Assert.That(world.transform.InverseTransformPoint(blade.transform.position).x, Is.InRange(17.75f, 18.15f));
                yield return PlayUntil(2.674f);
                Assert.That(despawning.GetValue(blade), Is.True);
                Assert.That(Target(bladeId), Is.SameAs(blade));
                yield return PlayUntil(2.744f);
                Assert.That(Target(bladeId), Is.Null);
            }
            finally
            {
                Time.captureFramerate = prior;
            }
        }

        [UnityTest]
        public IEnumerator CastFireShotCyclesAndKeepsItsBurnImpact() =>
            CheckCastFireShot("burn", 900f);

        [UnityTest]
        public IEnumerator CastFireShotCyclesAndKeepsItsWetTargetDamage() =>
            CheckCastFireShot("wet", 950f);

        private IEnumerator CheckCastFireShot(string scenarioId, float firstTargetHp)
        {
            Create();
            const string magic = "fire_shot";
            Assert.That(Configure(magic), Is.True);
            var scenarios = (JArray)Recording(magic)["scenarios"];
            Scenario(scenarios.IndexOf(scenarios.Single(s => (string)s["id"] == scenarioId)));
            var world = (Component)Field("world");
            string directory = System.IO.Path.Combine(Application.dataPath, "../Library/FireShotCaptures");
            bool capture = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            if (capture) System.IO.Directory.CreateDirectory(directory);

            int prior = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                yield return PlayUntil(.575f);
                Assert.That(Target(4), Is.Null);
                yield return PlayUntil(.624f);
                var shot = Target(4);
                Assert.That(shot, Is.Not.Null);
                var renderer = ServedObjectRenderer(shot);
                Vector3 start = world.transform.InverseTransformPoint(shot.transform.position);
                Assert.That(Vector3.Distance(start, new Vector3(2f, 0f, 5f)), Is.LessThan(.008f));
                var poses = new System.Collections.Generic.List<string>();
                while ((float)Field("elapsed") < 1.223f)
                {
                    string pose = renderer.sprite.name;
                    if (poses.LastOrDefault() != pose)
                    {
                        poses.Add(pose);
                        if (capture && poses.Count <= 4) SaveViewport(directory, $"{scenarioId}-{pose}", true);
                    }
                    yield return null;
                }
                var expected = Enumerable.Range(1, 4).Select(i => $"FireShotFlight{i:00}").Append("FireShotFlight01");
                CollectionAssert.AreEqual(expected, poses.Take(5));

                var first = Target(2);
                var second = Target(3);
                Assert.That(CurrentHp(first), Is.EqualTo(1000));
                Assert.That(CurrentHp(second), Is.EqualTo(1000));
                // 렌더러는 1.15초와 1.20초 서버 좌표 사이를 보간한다.
                Assert.That(world.transform.InverseTransformPoint(shot.transform.position).x, Is.InRange(6.39f, 6.81f));
                if (capture) SaveViewport(directory, $"{scenarioId}-before-hit", true);

                yield return PlayUntil(1.273f);
                Assert.That(CurrentHp(first), Is.EqualTo(firstTargetHp));
                Assert.That(CurrentHp(second), Is.EqualTo(900));
                var hasEffect = first.GetType().GetMethod("HasEffect");
                Assert.That(hasEffect.Invoke(first, new object[] { "Burn" }), Is.True);
                Assert.That(hasEffect.Invoke(first, new object[] { "Wet" }), Is.EqualTo(scenarioId == "wet"));
                Assert.That(Target(4), Is.SameAs(shot));
                Assert.That(shot.GetType().GetProperty("IsDespawning", Private).GetValue(shot), Is.True);
                if (capture) SaveViewport(directory, $"{scenarioId}-hit", true);

                yield return PlayUntil(1.345f);
                Assert.That(Target(4), Is.Null);
            }
            finally
            {
                Time.captureFramerate = prior;
            }
        }

        [UnityTest]
        public IEnumerator TutorialFireShotCyclesItsActualSpriteAlongTheExistingPath()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/Tutorial/FireShot");
            Assert.That(prefab, Is.Not.Null);
            root = new GameObject("Tutorial FireShot motion fixture");
            root.transform.position = new Vector3(1000, 0, 0);
            int prior = Time.captureFramerate;
            Time.captureFramerate = 120;
            try
            {
                var shot = UnityEngine.Object.Instantiate(prefab, root.transform);
                var actual = shot.transform.Find("actualObject").GetComponent<SpriteRenderer>();
                var shadow = shot.transform.Find("Shadow").GetComponent<SpriteRenderer>();
                var shadowSprite = prefab.transform.Find("Shadow").GetComponent<SpriteRenderer>().sprite;
                Assert.That(shadowSprite.name, Is.EqualTo("CircleObstacle"));
                Assert.That(shot.GetComponent("ShotMocker"), Is.Not.Null);
                Assert.That(shot.GetComponent<Collider2D>().isTrigger, Is.True);
                var poses = new System.Collections.Generic.List<string>();
                float started = Time.time;
                Vector3 start = shot.transform.position;
                while (Time.time - started < .475f)
                {
                    string pose = actual.sprite.name;
                    if (poses.LastOrDefault() != pose) poses.Add(pose);
                    yield return null;
                }
                var expected = Enumerable.Range(1, 4).Select(i => $"FireShotFlight{i:00}").Append("FireShotFlight01");
                CollectionAssert.AreEqual(expected, poses.Take(5));
                Vector3 travel = shot.transform.position - start;
                Assert.That(travel.x, Is.EqualTo((Time.time - started) * 5f).Within(.1f));
                Assert.That(travel.y, Is.EqualTo(0f).Within(.001f));
                Assert.That(travel.z, Is.EqualTo(0f).Within(.001f));
                Assert.That(shadow.sprite, Is.SameAs(shadowSprite));
            }
            finally
            {
                Time.captureFramerate = prior;
            }
        }

        private static float TimeToLive(Component subject)
        {
            var gauges = (IList)subject.GetType().GetField("gauges").GetValue(subject);
            object ttl = gauges.Cast<object>()
                .Single(gauge => (string)gauge.GetType().GetField("category").GetValue(gauge) == "TTL");
            return (float)ttl.GetType().GetField("value").GetValue(ttl);
        }

        private JToken SelectThunderBirdScenario(string magic, string id)
        {
            var scenarios = (JArray)Recording(magic)["scenarios"];
            var scenario = scenarios.Single(item => (string)item["id"] == id);
            Scenario(scenarios.IndexOf(scenario));
            return scenario;
        }

        private float ThunderBirdHeight(Component bird) =>
            ((Component)Field("world")).transform.InverseTransformPoint(bird.transform.position).y;

        private static SpriteRenderer ThunderBirdBody(Component bird)
        {
            Assert.That(bird, Is.Not.Null);
            Assert.That(bird.name, Does.StartWith("ThunderBird"));
            return (SpriteRenderer)bird.GetType().GetField("_spriteRenderer", Private).GetValue(bird);
        }

        private SpriteRenderer ZapMouseBody(int id)
        {
            Component unit = Target(id);
            Assert.That(unit, Is.Not.Null, $"찌릿쥐 ID {id}");
            Assert.That(unit.name, Does.StartWith("ZapMouse"));
            Assert.That(unit.GetComponent("CrawlMotionController"), Is.Null);
            return (SpriteRenderer)unit.GetType().GetField("_spriteRenderer", Private).GetValue(unit);
        }

        private Transform FindProjectile(string type)
        {
            var world = (Component)Field("world");
            return world.transform.Find($"PresentationContent/{type}(Clone)");
        }

        private static (float start, float duration, int targetId) FirstProjectile(JToken scenario, string type)
        {
            foreach (JToken frame in scenario["frames"])
            {
                JToken shot = frame["objects"]["projectile"].FirstOrDefault(p => (string)p["type"] == type);
                if (shot != null)
                    return ((float)frame["time"], (float)shot["duration"], (int)shot["end"]["id"]);
            }
            throw new InvalidOperationException(type);
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
                foreach (bool book in new[] { true, false }) SaveViewport(directory, magic, book);
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
            previewType.GetField("clips", Private).SetValue(second, Field("clips"));
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
        public IEnumerator RallyingTotemEmbersLoopWithFixedGroundRadius()
        {
            Create();
            Component world = CreateWorld();
            const float radius = 2.75f;
            world.transform.localScale = Vector3.one * 0.4f;
            world.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
            world.GetType().GetMethod("SetParameters").Invoke(world, new object[]
            {
                new System.Collections.Generic.Dictionary<string, float> { { "rallying_totem.effect_radius", radius } }
            });

            Type dtoType = Runtime("GameScene.Dto.CreatedObjectDto");
            object dto = Activator.CreateInstance(dtoType);
            dtoType.GetField("id").SetValue(dto, 42);
            dtoType.GetField("type").SetValue(dto, "RallyingTotem");
            dtoType.GetField("master").SetValue(dto, "LeftPlayer");
            dtoType.GetField("position").SetValue(dto, new Vector3(3f, 2f, 4f));

            int previousRate = Time.captureFramerate;
            try
            {
                Time.captureFramerate = 100;
                var totem = (Component)Runtime("GameScene.Object.ObjectSpawner").GetMethod("SpawnShared")
                    .Invoke(null, new[] { dto, world, false });
                var renderer = totem.GetComponentsInChildren<SpriteRenderer>().Single(item => item.name == "RallyZone");
                var observed = new System.Collections.Generic.List<string>();
                Vector3 localPosition = new Vector3(3f, 2f, 4f);
                for (int frame = 0; frame < 190; frame++)
                {
                    // 낙하하는 토템의 위치가 바뀌어도 구역은 월드 바닥을 따른다.
                    if (frame == 90)
                    {
                        localPosition = new Vector3(4f, 0.5f, 5f);
                        totem.transform.position = world.transform.TransformPoint(localPosition);
                    }
                    yield return null;
                    Sprite sprite = renderer.sprite;
                    if (observed.Count == 0 || observed[observed.Count - 1] != sprite.name)
                        observed.Add(sprite.name);

                    Transform ring = renderer.transform;
                    Vector3 localRing = world.transform.InverseTransformPoint(ring.position);
                    if (frame == 0) continue; // 첫 LateUpdate에서 고리의 높이와 회전이 설정된다.
                    Vector3 expectedGround = new Vector3(localPosition.x, 0.02f, localPosition.z);
                    Assert.That(Vector3.Distance(localRing, expectedGround), Is.LessThan(0.005f));
                    Assert.That(Vector3.Angle(ring.forward, -world.transform.up), Is.LessThan(0.05f));
                    AssertAuraRadius(renderer, world, radius);
                }
                CollectionAssert.AreEqual(Enumerable.Range(1, 8).Append(1)
                    .Select(index => $"RallyZoneEmber{index:00}"), observed);
            }
            finally
            {
                Time.captureFramerate = previousRate;
            }
        }

        [UnityTest]
        public IEnumerator RepairTotemLeavesUseRecordedEffectRangeAndLoop()
        {
            Create();
            int previousRate = Time.captureFramerate;
            var cameraField = previewType.GetField("previewCamera", Private);
            Camera camera = null;
            try
            {
                Time.captureFramerate = 100;
                Assert.That(Configure("repair_totem"), Is.True);
                camera = (Camera)cameraField.GetValue(preview);
                bool canRender = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
                if (!canRender) cameraField.SetValue(preview, null);

                yield return PlayUntil(0.7f);
                var world = (Component)Field("world");
                Component totem = Target(4);
                var renderer = totem.GetComponentsInChildren<SpriteRenderer>().Single(item => item.name == "RepairAura");
                Vector3 localPosition = world.transform.InverseTransformPoint(totem.transform.position);
                Assert.That(renderer.sortingOrder, Is.EqualTo(6));
                if (canRender)
                    Assert.That(CountVisibleAuraPixels(camera, (RenderTexture)Field("texture"), renderer), Is.GreaterThan(500));

                var observed = new System.Collections.Generic.List<string>();
                for (int frame = 0; frame < 172; frame++)
                {
                    string art = renderer.sprite.name;
                    if (observed.Count == 0 || observed[observed.Count - 1] != art) observed.Add(art);

                    Vector3 ground = new Vector3(localPosition.x, 0.02f, localPosition.z);
                    Vector3 actual = world.transform.InverseTransformPoint(renderer.transform.position);
                    Assert.That(Vector3.Distance(actual, ground), Is.LessThan(0.005f));
                    Assert.That(Vector3.Angle(renderer.transform.forward, -world.transform.up), Is.LessThan(0.05f));
                    // 녹화의 수리 범위는 3칸이고 토템의 충돌 반지름은 0.4칸이다.
                    AssertAuraRadius(renderer, world, 3f, 0.72f);
                    yield return null;
                }
                var expected = new[] { "RepairAura" }.Concat(Enumerable.Range(2, 5)
                    .Select(index => $"RepairAuraLight{index:00}")).Append("RepairAura");
                CollectionAssert.AreEqual(expected, observed);

                Component noParameters = CreateWorld();
                var dtoType = Runtime("GameScene.Dto.CreatedObjectDto");
                object dto = Activator.CreateInstance(dtoType);
                dtoType.GetField("id").SetValue(dto, 4);
                dtoType.GetField("type").SetValue(dto, "RepairTotem");
                dtoType.GetField("master").SetValue(dto, "LeftPlayer");
                var defaultTotem = (Component)Runtime("GameScene.Object.ObjectSpawner").GetMethod("SpawnShared")
                    .Invoke(null, new[] { dto, noParameters, false });
                var defaultRing = defaultTotem.GetComponentsInChildren<SpriteRenderer>().Single(item => item.name == "RepairAura");
                AssertAuraRadius(defaultRing, noParameters, 4f, 0.72f);
            }
            finally
            {
                cameraField.SetValue(preview, camera);
                preview.gameObject.SetActive(false);
                Time.captureFramerate = previousRate;
            }
        }

        private static void AssertAuraRadius(SpriteRenderer renderer, Component world, float radius, float opacity = 0.5f)
        {
            Sprite sprite = renderer.sprite;
            Transform ring = renderer.transform;
            Vector2 halfSize = sprite.rect.size / (2f * sprite.pixelsPerUnit);
            float xRadius = world.transform.InverseTransformVector(ring.TransformVector(Vector3.right * halfSize.x)).magnitude;
            float zRadius = world.transform.InverseTransformVector(ring.TransformVector(Vector3.up * halfSize.y)).magnitude;
            Assert.That(xRadius, Is.EqualTo(radius).Within(0.001f));
            Assert.That(zRadius, Is.EqualTo(radius).Within(0.001f));
            Assert.That(renderer.color.a, Is.EqualTo(opacity).Within(0.001f));
        }

        private static int CountVisibleAuraPixels(Camera camera, RenderTexture target, SpriteRenderer aura)
        {
            RenderTexture previous = RenderTexture.active;
            bool wasEnabled = aura.enabled;
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                Color32[] Read()
                {
                    camera.Render();
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
                    return pixels.GetPixels32();
                }
                Color32[] visible = Read();
                aura.enabled = false;
                Color32[] hidden = Read();
                return visible.Where((rgb, i) => Math.Abs(rgb.r - hidden[i].r) + Math.Abs(rgb.g - hidden[i].g) +
                    Math.Abs(rgb.b - hidden[i].b) > 42).Count();
            }
            finally
            {
                aura.enabled = wasEnabled;
                RenderTexture.active = previous;
                UnityEngine.Object.Destroy(pixels);
            }
        }

        [UnityTest]
        public IEnumerator BrickGolemSweepPoseInRecordingUsesSourceDirectionAndDuration()
        {
            Create();
            object clip = ((Array)Field("clips")).GetValue(0);
            const string sweep = "{\"type\":\"BrickGolemSweepPose\",\"duration\":0.4,\"start\":{\"targetType\":\"reference\",\"id\":42},\"end\":{\"targetType\":\"position\",\"x\":1,\"y\":0,\"z\":5}}";
            const string golems = "[{\"id\":42,\"master\":\"LeftPlayer\",\"type\":\"PveBrickGolem\",\"position\":{\"x\":4,\"y\":0,\"z\":5}},{\"id\":43,\"master\":\"LeftPlayer\",\"type\":\"PveBrickGolem\",\"position\":{\"x\":8,\"y\":0,\"z\":5}}]";
            var recording = new TextAsset("{\"version\":1,\"magic\":\"fire_shot\",\"frameDuration\":0.05,\"duration\":2," +
                "\"frames\":[{\"time\":0,\"objects\":{\"create\":" + golems + ",\"projectile\":[" + sweep + "]," +
                "\"update\":[{\"id\":42,\"master\":\"LeftPlayer\",\"position\":{\"x\":4,\"y\":0,\"z\":5},\"status\":\"Attack\"}]}}]}");
            clip.GetType().GetField("recordingAsset").SetValue(clip, recording);

            var previewCameraField = previewType.GetField("previewCamera", Private);
            Camera previewCamera = null;
            int previousRate = Time.captureFramerate;
            try
            {
                Time.captureFramerate = 100;
                Assert.That(Configure("fire_shot"), Is.True);
                // 그래픽을 끈 배치 모드에서도 녹화 로딩과 자세 재생을 검증한다.
                previewCamera = (Camera)previewCameraField.GetValue(preview);
                previewCameraField.SetValue(preview, null);
                Assert.That(Property("ScenarioCount"), Is.EqualTo(1));
                Component actor = Target(42);
                Component spectator = Target(43);
                Type presenterType = Runtime("GameScene.ServedObjectComponent.Motion.BrickGolemPoseController");
                var presenter = (Behaviour)actor.GetComponent(presenterType);
                var rendererField = presenterType.GetField("targetRenderer", Private);
                var renderer = (SpriteRenderer)rendererField.GetValue(presenter);
                var otherRenderer = (SpriteRenderer)rendererField.GetValue(spectator.GetComponent(presenterType));
                var idle = Resources.Load<Sprite>("Game/sprites/PveBrickGolem");
                var raised = Resources.Load<Sprite>("Game/sprites/PveBrickGolem2");
                Sprite[] frames = Enumerable.Range(1, 4)
                    .Select(index => Resources.Load<Sprite>($"Game/sprites/PveBrickGolemSweep{index:00}")).ToArray();
                Assert.That(renderer.sprite, Is.SameAs(frames[0]));
                Assert.That(renderer.flipX, Is.True);
                Assert.That(otherRenderer.sprite, Is.SameAs(idle));
                Assert.That(otherRenderer.flipX, Is.False);
                Vector3 rootPosition = actor.transform.position;
                float started = Time.time;
                var observed = new System.Collections.Generic.List<Sprite>();
                while (Time.time < started + 0.45f)
                {
                    if (!observed.Contains(renderer.sprite)) observed.Add(renderer.sprite);
                    if (Time.time < started + 0.39f) Assert.That(renderer.sprite, Is.Not.SameAs(raised));
                    yield return null;
                }
                CollectionAssert.AreEqual(frames.Append(idle).ToArray(), observed);
                Assert.That(actor.transform.position, Is.EqualTo(rootPosition));
                foreach (Sprite frame in frames)
                {
                    Assert.That(frame.bounds.size, Is.EqualTo(idle.bounds.size));
                    Assert.That(frame.pivot, Is.EqualTo(idle.pivot));
                }

                // 실시간 갱신에서 Attack이 먼저 도착해도 뒤따른 휩쓸기 신호가 자세를 선택한다.
                actor.GetType().GetMethod("PlayAttackPresentation").Invoke(actor, null);
                Assert.That(renderer.sprite, Is.SameAs(raised));
                var deserialize = Runtime("Global.Serialization.JsonCodec").GetMethod("Deserialize")
                    .MakeGenericMethod(Runtime("GameScene.Dto.Projectile.ProjectileDto"));
                object cue = deserialize.Invoke(null, new object[] { sweep });
                Runtime("GameScene.Object.ProjectileSpawner").GetMethod("SpawnShared")
                    .Invoke(null, new[] { cue, Field("world") });
                Assert.That(renderer.sprite, Is.SameAs(frames[0]));
                presenter.enabled = false;
                Assert.That(renderer.sprite, Is.SameAs(idle));
            }
            finally
            {
                if (previewCamera != null)
                {
                    previewCameraField.SetValue(preview, previewCamera);
                    preview.gameObject.SetActive(false);
                }
                Time.captureFramerate = previousRate;
                UnityEngine.Object.Destroy(recording);
            }
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
