#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class FactionTutorialPlayModeTests
    {
        private const long Owner = 98765439;
        private object previousUser;
        private object previousMatch;
        private object previousMagics;
        private string previousToken;
        private object source;
        private MonoBehaviour driver;

        private static Type TypeNamed(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(name)).First(type => type != null);
        private static object Json(string type, string json) => JsonConvert.DeserializeObject(json, TypeNamed(type));
        private static object GetStatic(string type, string name) => TypeNamed(type).GetProperty(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetValue(null);
        private static void SetStatic(string type, string name, object value) => TypeNamed(type).GetProperty(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).SetValue(null, value);
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private IEnumerator SetUpBattle()
        {
            previousUser = GetStatic("Global.SceneContext", "User");
            previousMatch = GetStatic("Global.SceneContext", "MatchInfo");
            previousToken = (string)GetStatic("Global.SceneContext", "JwtToken");
            SetStatic("Global.SceneContext", "User", Json("Data.User", "{\"id\":98765439,\"name\":\"Tutorial fixture\",\"email\":\"\",\"selectedDeckId\":-1}"));
            SetStatic("Global.SceneContext", "JwtToken", null);
            SetStatic("Global.SceneContext", "MatchInfo", Json("Data.MatchedInfoDto", "{\"sessionId\":\"tutorial-fixture\",\"server\":\"http://localhost:7001\",\"leftUser\":{\"id\":98765439,\"name\":\"Tutorial fixture\"},\"rightUser\":{\"id\":-1,\"name\":\"Training\"}}"));
            SceneManager.sceneLoaded += DisableNetworkDriver;
            yield return SceneManager.LoadSceneAsync("FactionTutorialBattleScene");
            SceneManager.sceneLoaded -= DisableNetworkDriver;
            yield return null;
            SetStatic("Global.SceneContext", "JwtToken", "fixture-only");
            Assert.That(driver, Is.Not.Null);
            Assert.That(Object.FindObjectsOfType<MonoBehaviour>(true).Count(component => component.GetType().Name == "StompConnector" || component.GetType().Name == "PingSender"), Is.Zero);
            source = GetStatic("Data.Magic.MagicInfoDataSource", "Instance");
            var field = source.GetType().GetField("magics", BindingFlags.Instance | BindingFlags.NonPublic);
            previousMagics = field.GetValue(source);
            var response = Json("Data.Magic.MagicInfoResponse", "{\"magics\":[{\"id\":1,\"name\":\"fire_shot\",\"elements\":[\"Fire\"],\"manaCost\":10,\"castKind\":\"Shot\"},{\"id\":2,\"name\":\"meteor_shower\",\"elements\":[\"Fire\",\"Rock\"],\"manaCost\":50,\"castKind\":\"Drop\"}]}");
            source.GetType().GetMethod("ProcessResponse", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(source, new[] { response });
            Set(driver, "owner", Owner);
            Set(driver, "matchingServer", ((object)GetStatic("Data.ServerList", "MatchingServer")).GetType().GetProperty("url").GetValue(GetStatic("Data.ServerList", "MatchingServer")));
            Set(driver, "ready", Json("Data.TutorialReadyDto", "{\"battleId\":\"tutorial-fixture\",\"ownerId\":98765439,\"faction\":\"HELL\",\"server\":\"http://localhost:7001\"}"));
        }

        [UnityTest]
        public IEnumerator StepTransitionClearsSelectionBeforeReplacingCardsAndKeepsNormalTransportDisabled()
        {
            yield return SetUpBattle();
            Apply(0, "shot", "RUNNING", 1);
            yield return null;
            var ui = GetStatic("GameScene.GameSceneUIController", "Instance");
            var card = ui.GetType().GetMethod("GetCardAt").Invoke(ui, new object[] { 0 });
            Assert.That(card, Is.Not.Null);
            var sender = GetStatic("GameScene.Card.CardInputSender", "Instance");
            sender.GetType().GetMethod("TryUseCard").Invoke(sender, new[] { card });
            Assert.That(sender.GetType().GetMethod("IsFieldSelectMode").Invoke(sender, null), Is.True);
            Apply(0, "shot", "CLEARED", 1);
            Apply(1, "meteor", "RUNNING", 2);
            yield return null;
            Assert.That(sender.GetType().GetMethod("IsFieldSelectMode").Invoke(sender, null), Is.False);
            Assert.That(sender.GetType().GetMethod("GetMagicName").Invoke(sender, null), Is.Null);
            var ids = ((IEnumerable)ui.GetType().GetMethod("GetAllCards").Invoke(ui, null)).Cast<long>().ToArray();
            Assert.That(ids, Is.EqualTo(new long[] { 2 }));
        }

        [UnityTest]
        public IEnumerator BatchedMovementInterpolatesAndTerminalWaitsForTheLastTweenWithoutRepeatingAttacks()
        {
            yield return SetUpBattle();
            const int id = 98765441;
            object Snapshot(float x) => new { id, prefab = "MiniRock", x, y = 0, z = 5, master = "LeftPlayer", status = "Idle",
                effects = new string[0], gauges = new object[0], gizmos = new object[0] };
            object Frame(int number, int tick, float x, string status) => new { frame = number, tick,
                objects = new { create = new object[0], projectile = new object[0], update = new[] {
                    new { id, position = new { x, y = 0, z = 5 }, master = "LeftPlayer", status, effects = new string[0], gauges = new object[0] } } },
                events = new object[0] };
            void Receive(int last, int tick, string status, object[] frames, object[] snapshot) => ApplyJson(JsonConvert.SerializeObject(new {
                battleId = "tutorial-fixture", ownerId = Owner, faction = "HELL", revision = 0, stage = "shot", status,
                mana = 120, cards = new long[] { 1 }, remainingSeconds = 120, lastFrame = last, tick, tickRate = 20, frames, snapshot
            }));
            Receive(1, 0, "RUNNING", new object[0], new[] { Snapshot(2) });
            yield return null;
            var container = GetStatic("GameScene.Object.ObjectContainer", "Instance");
            var served = (Component)container.GetType().GetMethod("FindById").Invoke(container, new object[] { id });
            Assert.That(served, Is.Not.Null);
            int attacks = 0;
            served.GetType().GetEvent("OnAttack").AddEventHandler(served, new Action(() => attacks++));
            var batch = new[] { Frame(2, 1, 2.2f, "Idle"), Frame(3, 2, 2.4f, "Attack"), Frame(4, 3, 2.6f, "Idle") };
            Receive(4, 3, "RUNNING", batch, new[] { Snapshot(2.6f) });
            Assert.That(served.transform.position.x, Is.EqualTo(2).Within(.001), "수신만으로 최신 snapshot 위치로 튀면 안 된다.");
            Receive(4, 3, "CLEARED", batch, new[] { Snapshot(2.6f) });
            object presented = driver.GetType().GetField("presentedState", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(driver);
            Assert.That(presented.GetType().GetField("status").GetValue(presented), Is.EqualTo("RUNNING"));
            bool passedIntermediatePosition = false;
            float deadline = Time.realtimeSinceStartup + 2;
            bool cleared = false;
            while (Time.realtimeSinceStartup < deadline)
            {
                driver.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(driver, null);
                if (served.transform.position.x > 2.01f && served.transform.position.x < 2.19f) passedIntermediatePosition = true;
                presented = driver.GetType().GetField("presentedState", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(driver);
                cleared = (string)presented.GetType().GetField("status").GetValue(presented) == "CLEARED";
                if (cleared) break;
                yield return null;
            }
            Assert.That(cleared, Is.True, "추가 응답이 없어도 마지막 표시를 마치면 성공 안내가 나와야 한다.");
            Assert.That(passedIntermediatePosition, Is.True, "실제 ServedObject가 첫 이동 구간의 중간 위치를 통과해야 한다.");
            Assert.That(served.transform.position.x, Is.EqualTo(2.6).Within(.01));
            Assert.That(attacks, Is.EqualTo(1), "batch 중복이나 full snapshot이 공격 표현을 다시 실행하면 안 된다.");
            Apply(1, "meteor", "RUNNING", 2);
            driver.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(driver, null);
            Assert.That(container.GetType().GetMethod("FindById").Invoke(container, new object[] { id }), Is.Null);
        }

        private void DisableNetworkDriver(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "FactionTutorialBattleScene") return;
            driver = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                .Single(component => component.GetType().Name == "FactionTutorialBattleDriver");
            driver.enabled = false;
        }

        [UnityTest]
        public IEnumerator AppStallDropsQueuedCombatEvenWhenUnityScaledTimeIsClamped()
        {
            yield return SetUpBattle();
            ApplyJson($"{{\"battleId\":\"tutorial-fixture\",\"ownerId\":{Owner},\"faction\":\"HELL\",\"revision\":0,\"stage\":\"shot\",\"status\":\"RUNNING\",\"cards\":[1],\"tickRate\":20,\"tick\":0,\"lastFrame\":1,\"frames\":[],\"snapshot\":[]}}");
            ApplyJson($"{{\"battleId\":\"tutorial-fixture\",\"ownerId\":{Owner},\"faction\":\"HELL\",\"revision\":0,\"stage\":\"shot\",\"status\":\"CLEARED\",\"cards\":[1],\"tickRate\":20,\"tick\":1,\"lastFrame\":2,\"frames\":[{{\"frame\":2,\"tick\":1,\"objects\":{{\"create\":[],\"update\":[],\"projectile\":[]}},\"events\":[]}}],\"snapshot\":[]}}");
            var update = driver.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            var bufferField = driver.GetType().GetField("playback", BindingFlags.Instance | BindingFlags.NonPublic);
            var pendingField = driver.GetType().GetField("pendingTerminal", BindingFlags.Instance | BindingFlags.NonPublic);
            update.Invoke(driver, null);
            object buffer = bufferField.GetValue(driver);
            Assert.That(buffer.GetType().GetProperty("Count").GetValue(buffer), Is.EqualTo(1));
            float previousMaximum = Time.maximumDeltaTime;
            double beforeRealtime = Time.realtimeSinceStartupAsDouble;
            double beforeScaled = Time.timeAsDouble;
            try
            {
                Time.maximumDeltaTime = .33333334f;
                System.Threading.Thread.Sleep(650);
                yield return null;
                Assert.That(Time.realtimeSinceStartupAsDouble - beforeRealtime, Is.GreaterThan(.5));
                Assert.That(Time.timeAsDouble - beforeScaled, Is.LessThan(.5), "Unity 시계 제한만으로는 실제 앱 정지를 감지할 수 없다.");
                update.Invoke(driver, null);
                Assert.That(buffer.GetType().GetProperty("Count").GetValue(buffer), Is.Zero);
                Assert.That(pendingField.GetValue(driver), Is.Null, "복구 snapshot 전에는 이전 terminal 안내도 표시하지 않는다.");
            }
            finally { Time.maximumDeltaTime = previousMaximum; }
        }

        private void Apply(int revision, string stage, string status, int card)
        {
            string json = $"{{\"battleId\":\"tutorial-fixture\",\"ownerId\":{Owner},\"faction\":\"HELL\",\"revision\":{revision},\"stage\":\"{stage}\",\"status\":\"{status}\",\"mana\":120,\"cards\":[{card}],\"frames\":[],\"snapshot\":[],\"remainingSeconds\":120}}";
            ApplyJson(json);
        }

        private void ApplyJson(string json) => driver.GetType().GetMethod("Apply", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(driver, new[] { Json("GameScene.Dto.TutorialStateDto", json) });

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded -= DisableNetworkDriver;
            if (source != null) source.GetType().GetField("magics", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(source, previousMagics);
            if (SceneManager.GetSceneByName("FactionTutorialBattleScene").isLoaded)
            {
                var empty = SceneManager.CreateScene("FactionFixtureCleanup");
                SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync("FactionTutorialBattleScene");
            }
            SetStatic("Global.SceneContext", "User", previousUser);
            SetStatic("Global.SceneContext", "MatchInfo", previousMatch);
            SetStatic("Global.SceneContext", "JwtToken", previousToken);
            yield return null;
        }
    }
}
#endif
