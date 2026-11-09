#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using TMPro;
using UnityEngine.UI;
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
            var fixtureMagics = new[] {
                ("fire_shot", "Fire", "Shot"), ("meteor_shower", "Fire", "Drop"), ("tornado_strike", "Wind", "Drop"),
                ("wall_golem", "Rock", "Unit"), ("rock_golem", "Rock", "Unit"), ("stone_gargoyle", "Rock", "Unit"),
                ("tree_golem", "Nature", "Unit"), ("electric_spearman", "Lightning", "Unit"), ("thunder_spirit", "Lightning", "Unit"),
                ("rock_mage", "Rock", "Unit"), ("aqua_archer", "Water", "Unit"),
                ("chain_lightning", "Lightning", "Shot"), ("ember_spirit_swarm", "Fire", "Unit"),
                ("towerback", "Rock", "Unit"), ("magma_explosion", "Fire", "Drop"),
                ("water_slime_swarm", "Water", "Unit"), ("vine_spirit", "Nature", "Unit"),
                ("water_explosion", "Water", "Drop")
            };
            var response = Json("Data.Magic.MagicInfoResponse", JsonConvert.SerializeObject(new {
                magics = fixtureMagics.Select((entry, index) => new {
                    id = index + 1, name = entry.Item1, elements = new[] { entry.Item2 }, manaCost = 35, castKind = entry.Item3
                })
            }));
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
        public IEnumerator CardGuidanceStaysConsumedForRepeatedCastsAndRetryButGuidesNewCardsAndSteps()
        {
            yield return SetUpBattle();
            var lateUpdate = driver.GetType().GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            var pointer = driver.GetType().GetField("pointer", BindingFlags.Instance | BindingFlags.NonPublic);
            var guided = (HashSet<string>)driver.GetType().GetField("guidedCards", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(driver);
            var sender = GetStatic("GameScene.Card.CardInputSender", "Instance");
            var ui = GetStatic("GameScene.GameSceneUIController", "Instance");
            void Receive(int revision, string stage, int card, string recommended) => ApplyJson(JsonConvert.SerializeObject(new {
                battleId = "tutorial-fixture", ownerId = Owner, faction = "HELL", revision, stage, status = "RUNNING",
                mana = 120, cards = new[] { card }, recommended, frames = new object[0], snapshot = new object[0]
            }));
            void Select() => sender.GetType().GetMethod("TryUseCard").Invoke(sender,
                new[] { ui.GetType().GetMethod("GetCardAt").Invoke(ui, new object[] { 0 }) });
            void UpdatePointer() => lateUpdate.Invoke(driver, null);
            Receive(0, "formation", 4, "wall_golem");
            UpdatePointer();
            Assert.That(pointer.GetValue(driver), Is.Not.Null);
            Select();
            UpdatePointer();
            sender.GetType().GetMethod("Cancel").Invoke(sender, null);
            UpdatePointer();
            Assert.That(pointer.GetValue(driver), Is.Not.Null, "선택 취소만으로 안내를 소비하지 않는다.");
            guided.Add("formation:wall_golem");
            UpdatePointer();
            Assert.That(pointer.GetValue(driver), Is.Null);
            Select();
            UpdatePointer();
            Assert.That(pointer.GetValue(driver), Is.Null, "반복 카드 선택에도 필드 손 안내를 되풀이하지 않는다.");
            Receive(1, "formation", 4, "wall_golem");
            UpdatePointer();
            Assert.That(pointer.GetValue(driver), Is.Null, "같은 단계 재시도에서도 안내를 되풀이하지 않는다.");
            Receive(1, "formation", 5, "rock_golem");
            UpdatePointer();
            Assert.That(pointer.GetValue(driver), Is.Not.Null, "새로 배우는 근접 카드는 안내한다.");
            Receive(2, "shot", 4, "wall_golem");
            UpdatePointer();
            Assert.That(pointer.GetValue(driver), Is.Not.Null, "다른 단계의 카드는 처음부터 안내한다.");
        }

        [UnityTest]
        public IEnumerator RewardCardsShowLocalizedSummariesForHoverAndTouchWithoutStaleExitOrHiddenUpdates()
        {
            yield return SetUpBattle();
            yield return LocalizationSettings.InitializationOperation;
            var previousLocale = LocalizationSettings.SelectedLocale;
            try
            {
                var panel = (Component)driver.GetType().GetField("panel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(driver);
                var details = (Component)panel.GetType().GetField("rewardDetails", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(panel);
                Assert.That(details, Is.Not.Null);
                var body = (TMP_Text)details.GetType().GetField("descriptionText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(details);
                var title = (TMP_Text)details.GetType().GetField("cardNameText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(details);
                var panelBody = (TMP_Text)panel.GetType().GetField("body", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(panel);
                foreach (string locale in new[] { "ko-KR", "en" })
                {
                    LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(locale);
                    foreach (string faction in new[] { "HELL", "NATURE" })
                    {
                        panel.GetType().GetMethod("Show").Invoke(panel, new object[] {
                            "FactionTutorial.complete", "FactionTutorial.reward." + faction, "FactionTutorial.lobby",
                            new Action(() => { }), null, null, faction
                        });
                        var icons = (Image[])panel.GetType().GetField(faction == "HELL" ? "hellIcons" : "natureIcons",
                            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(panel);
                        var names = (string[])TypeNamed("TutorialScene.FactionTutorialCards").GetField(faction == "HELL" ? "Hell" : "Nature").GetValue(null);
                        Component previous = null;
                        foreach (var pair in icons.Select((icon, i) => (icon, i)))
                        {
                            var card = pair.icon.transform.parent.GetComponent(TypeNamed("TutorialScene.TutorialRewardCard"));
                            Assert.That(card, Is.Not.Null);
                            var mouse = new PointerEventData(EventSystem.current) { pointerId = -1 };
                            card.GetType().GetMethod("OnPointerEnter").Invoke(card, new object[] { mouse });
                            if (previous != null) previous.GetType().GetMethod("OnPointerExit").Invoke(previous, new object[] { mouse });
                            Assert.That(details.gameObject.activeSelf, Is.True, "이전 카드의 exit가 새 설명을 닫으면 안 된다.");
                            string expected = LocalizationSettings.StringDatabase.GetLocalizedString("MagicBook", "summary." + names[pair.i]);
                            float deadline = Time.realtimeSinceStartup + 5;
                            while (body.text != expected && Time.realtimeSinceStartup < deadline) yield return null;
                            Assert.That(body.text, Is.EqualTo(expected));
                            Assert.That(string.IsNullOrWhiteSpace(title.text), Is.False);
                            Assert.That(details.GetComponentsInChildren<Graphic>(true).All(graphic => !graphic.raycastTarget), Is.True);
                            card.GetType().GetMethod("OnPointerExit").Invoke(card, new object[] { mouse });
                            Assert.That(details.gameObject.activeSelf, Is.False);
                            var touch = new PointerEventData(EventSystem.current) { pointerId = 0 };
                            card.GetType().GetMethod("OnPointerClick").Invoke(card, new object[] { touch });
                            card.GetType().GetMethod("OnPointerExit").Invoke(card, new object[] { touch });
                            Assert.That(details.gameObject.activeSelf, Is.True, "탭 종료 후에도 설명을 읽을 수 있어야 한다.");
                            previous = card;
                        }
                        float bodyDeadline = Time.realtimeSinceStartup + 5;
                        string expectedBody = LocalizationSettings.StringDatabase.GetLocalizedString("Onboarding", "FactionTutorial.reward." + faction);
                        while (panelBody.text != expectedBody && Time.realtimeSinceStartup < bodyDeadline) yield return null;
                        Assert.That(panelBody.text, Is.EqualTo(expectedBody));
                        if (locale == "ko-KR") Assert.That(panelBody.text, Is.EqualTo("기본적인 전투를 배웠습니다. 축하합니다!\n아래 카드들을 획득했습니다!"));
                        panel.GetType().GetMethod("Hide").Invoke(panel, null);
                        yield return null;
                        Assert.That(details.gameObject.activeInHierarchy, Is.False);
                    }
                }
            }
            finally { LocalizationSettings.SelectedLocale = previousLocale; }
        }

        [UnityTest]
        public IEnumerator CastPointerKeepsItsPointWhileTornadoTargetsOrbitAndUpdatesOnReselection()
        {
            yield return SetUpBattle();
            void Receive(float x, float z) => ApplyJson(JsonConvert.SerializeObject(new {
                battleId = "tutorial-fixture", ownerId = Owner, faction = "HELL", revision = 0,
                stage = "tornado", status = "RUNNING", cards = new long[] { 3 }, recommended = "tornado_strike",
                targetX = x, targetZ = z, frames = new object[0], snapshot = new object[0]
            }));
            var lateUpdate = driver.GetType().GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            var pointerField = driver.GetType().GetField("pointer", BindingFlags.Instance | BindingFlags.NonPublic);
            var ui = GetStatic("GameScene.GameSceneUIController", "Instance");
            var sender = GetStatic("GameScene.Card.CardInputSender", "Instance");
            void SelectCard() => sender.GetType().GetMethod("TryUseCard").Invoke(sender,
                new[] { ui.GetType().GetMethod("GetCardAt").Invoke(ui, new object[] { 0 }) });
            Vector2 Point()
            {
                object pointer = pointerField.GetValue(driver);
                Assert.That(pointer, Is.Not.Null);
                var getPoint = (Func<Vector2?>)pointer.GetType().GetField("pressPoint", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pointer);
                Assert.That(getPoint(), Is.Not.Null);
                return getPoint().Value;
            }

            Receive(8, 5);
            SelectCard();
            lateUpdate.Invoke(driver, null);
            Vector2 original = Camera.main.WorldToScreenPoint(new Vector3(8, 0, 5));
            Assert.That(Vector2.Distance(Point(), original), Is.LessThan(.01f));
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                Receive(9 + Mathf.Cos(angle), 5 + Mathf.Sin(angle));
                lateUpdate.Invoke(driver, null);
                Assert.That(Vector2.Distance(Point(), original), Is.LessThan(.01f), "손이 적의 회전 좌표를 따라가면 안 된다.");
                yield return null;
            }

            sender.GetType().GetMethod("Cancel").Invoke(sender, null);
            lateUpdate.Invoke(driver, null);
            Receive(10, 6);
            SelectCard();
            lateUpdate.Invoke(driver, null);
            Vector2 next = Camera.main.WorldToScreenPoint(new Vector3(10, 0, 6));
            Assert.That(Vector2.Distance(Point(), next), Is.LessThan(.01f), "새 선택에서는 사용 지점을 다시 잡는다.");
        }

        [UnityTest]
        public IEnumerator BothFactionsGuidePracticeSummonsInsideRangeAndDistinguishTankFromMelee()
        {
            yield return SetUpBattle();
            var lateUpdate = driver.GetType().GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            var ui = GetStatic("GameScene.GameSceneUIController", "Instance");
            var sender = GetStatic("GameScene.Card.CardInputSender", "Instance");
            int revision = 0;
            foreach (var lesson in new[] {
                ("HELL", "anti_air", 10, "rock_mage"), ("HELL", "formation", 4, "wall_golem"),
                ("HELL", "formation", 5, "rock_golem"), ("HELL", "explosion", 6, "stone_gargoyle"),
                ("NATURE", "anti_air", 11, "aqua_archer"), ("NATURE", "formation", 7, "tree_golem"),
                ("NATURE", "formation", 8, "electric_spearman"), ("NATURE", "explosion", 9, "thunder_spirit")
            })
            {
                Set(driver, "ready", Json("Data.TutorialReadyDto", JsonConvert.SerializeObject(new {
                    battleId = "tutorial-fixture", ownerId = Owner, faction = lesson.Item1, server = "http://localhost:7001"
                })));
                ApplyJson(JsonConvert.SerializeObject(new {
                    battleId = "tutorial-fixture", ownerId = Owner, faction = lesson.Item1, revision = revision++,
                    stage = lesson.Item2, status = "RUNNING", cards = new[] { lesson.Item3 }, recommended = lesson.Item4,
                    targetX = 9, targetZ = 5, frames = new object[0], snapshot = new object[0]
                }));
                sender.GetType().GetMethod("TryUseCard").Invoke(sender,
                    new[] { ui.GetType().GetMethod("GetCardAt").Invoke(ui, new object[] { 0 }) });
                lateUpdate.Invoke(driver, null);
                object pointer = driver.GetType().GetField("pointer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(driver);
                Assert.That(pointer, Is.Not.Null, lesson.Item4);
                var point = (Func<Vector2?>)pointer.GetType().GetField("pressPoint", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pointer);
                Vector2 expected = Camera.main.WorldToScreenPoint(new Vector3(6, 0, 5));
                Assert.That(Vector2.Distance(point().Value, expected), Is.LessThan(.01f), lesson.Item4);
                var hint = Object.FindObjectsOfType<MonoBehaviour>().Single(item => item.GetType().Name == "TutorialHint");
                object text = hint.GetType().GetField("message", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hint);
                float deadline = Time.realtimeSinceStartup + 5;
                while (string.IsNullOrEmpty((string)text.GetType().GetProperty("text").GetValue(text)) && Time.realtimeSinceStartup < deadline) yield return null;
                string actualText = (string)text.GetType().GetProperty("text").GetValue(text);
                string key = lesson.Item2 == "formation" ? "FactionTutorial.formation." + (lesson.Item3 == 4 || lesson.Item3 == 7 ? "tank" : "swarm")
                    : "FactionTutorial." + lesson.Item2 + "." + lesson.Item1;
                var localization = (System.Threading.Tasks.Task<string>)TypeNamed("Data.Localization.LocaleUtils")
                    .GetMethod("GetStringAsync").Invoke(null, new object[] { "Onboarding", key });
                while (!localization.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(localization.IsCompleted, Is.True);
                Assert.That(actualText, Is.EqualTo(localization.Result), lesson.Item4);
            }
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

        [UnityTest]
        public IEnumerator CompletedBattleQueuesLobbyGuidanceInsteadOfCompletingAllOnboarding()
        {
            yield return SetUpBattle();
            Type progress = TypeNamed("TutorialScene.FactionTutorialProgress");
            string server = (string)((object)GetStatic("Data.ServerList", "MatchingServer")).GetType()
                .GetProperty("url").GetValue(GetStatic("Data.ServerList", "MatchingServer"));
            string key = "faction-tutorial-v1:" + server + ":" + Owner;
            string previous = PlayerPrefs.GetString(key, "");
            try
            {
                Apply(0, "final", "COMPLETED", 1);
                float deadline = Time.realtimeSinceStartup + 2;
                while (!(bool)progress.GetMethod("IsLobbyPending").Invoke(null, new object[] { Owner })
                    && Time.realtimeSinceStartup < deadline)
                {
                    driver.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(driver, null);
                    yield return null;
                }
                Assert.That(progress.GetMethod("IsPending").Invoke(null, new object[] { Owner }), Is.False);
                Assert.That(progress.GetMethod("IsLobbyPending").Invoke(null, new object[] { Owner }), Is.True);
            }
            finally
            {
                if (string.IsNullOrEmpty(previous)) PlayerPrefs.DeleteKey(key);
                else PlayerPrefs.SetString(key, previous);
                PlayerPrefs.Save();
            }
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

    public class LobbyOnboardingPlayModeTests
    {
        private const long Owner = 98765463;
        private object previousUser;
        private string previousToken;
        private string checkpointKey;
        private string previousCheckpoint;
        private GameObject fixtureContext;

        private static Type Runtime(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(name)).First(type => type != null);
        private static object GetStatic(string type, string name) => Runtime(type)
            .GetProperty(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetValue(null);
        private static void SetStatic(string type, string name, object value) => Runtime(type).GetProperty(name).SetValue(null, value);
        private static object Progress(string method) => Runtime("TutorialScene.FactionTutorialProgress")
            .GetMethod(method).Invoke(null, new object[] { Owner });
        private static void StartGuidance() => Runtime("TutorialScene.GlobalTutorialManager")
            .GetMethod("StartLobbyOnboardingIfPending").Invoke(null, null);
        private static object Manager => GetStatic("TutorialScene.GlobalTutorialManager", "Instance");
        private static object State => Manager.GetType().GetField("currentState", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Manager);
        private static string Step => Manager.GetType().GetProperty("CurrentProgress").GetValue(Manager).ToString();
        private static void Invoke(object target, string method) => target.GetType()
            .GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);

        [UnitySetUp]
        public IEnumerator PrepareLobby()
        {
            previousUser = GetStatic("Global.SceneContext", "User");
            previousToken = (string)GetStatic("Global.SceneContext", "JwtToken");
            if ((Object)GetStatic("Global.SceneContext", "Instance") == null)
            {
                fixtureContext = new GameObject("Lobby guidance fixture context");
                fixtureContext.AddComponent(Runtime("Global.SceneContext"));
            }
            SetStatic("Global.SceneContext", "User", JsonConvert.DeserializeObject(
                "{\"id\":98765463,\"name\":\"Lobby guidance fixture\",\"selectedDeckId\":-1}", Runtime("Data.User")));
            SetStatic("Global.SceneContext", "JwtToken", "fixture-only");
            object server = GetStatic("Data.ServerList", "MatchingServer");
            checkpointKey = "faction-tutorial-v1:" + server.GetType().GetProperty("url").GetValue(server) + ":" + Owner;
            previousCheckpoint = PlayerPrefs.GetString(checkpointKey, "");
            Progress("CompleteBattle");
            SceneManager.sceneLoaded += DisableNetworkStartup;
            yield return SceneManager.LoadSceneAsync("LobbyScene");
        }

        private static void DisableNetworkStartup(Scene scene, LoadSceneMode mode)
        {
            // 실제 씬의 안내와 버튼 연결을 유지하고 인증·덱·보상 HTTP 초기화만 실행하지 않는다.
            foreach (var component in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)))
            {
                if (component == null || component.GetType().Namespace == "TutorialScene") continue;
                string assembly = component.GetType().Assembly.GetName().Name;
                if (assembly != "Assembly-CSharp" && !assembly.StartsWith("WordOnline.")) continue;
                // OnEnable에서 이미 시작한 요청도 멈춘다. enabled=false만으로는 코루틴이 남는다.
                component.StopAllCoroutines();
                component.enabled = false;
            }
        }

        [UnityTest]
        public IEnumerator LobbyStartsDeckGuidanceOnceAndContinuesThroughMagicBookAndPracticePrompt()
        {
            StartGuidance();
            object manager = Manager;
            Assert.That(Step, Is.EqualTo("Deck_EnterScene"));
            yield return null;
            Assert.That(Step, Is.EqualTo("Deck_EnterScene"), "Start가 전투 단계로 되돌리면 안 된다.");
            StartGuidance();
            Assert.That(Manager, Is.SameAs(manager));
            Invoke(State, "OnDeckButtonClicked");
            yield return SceneManager.LoadSceneAsync("ManageDeckScene");
            Assert.That(Step, Is.EqualTo("Deck_ExplainDeck"));
            var controller = (Component)GetStatic("TutorialScene.DeckTutorialController", "Instance");
            object panel = controller.GetType().BaseType.GetField("panel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
            Assert.That(panel, Is.Not.Null);
            for (int i = 0; i < 6; i++) Invoke(panel, "OnNextClicked");
            Assert.That(Step, Is.EqualTo("Deck_ReturnToLobby"));
            yield return SceneManager.LoadSceneAsync("LobbyScene");
            Assert.That(Step, Is.EqualTo("MagicBook_EnterScene"));
            StartGuidance();
            Assert.That(Step, Is.EqualTo("MagicBook_EnterScene"), "로비 재진입은 기존 진행을 유지한다.");
            Invoke(State, "OnMagicBookButtonClicked");
            yield return SceneManager.LoadSceneAsync("MagicBookScene");
            controller = (Component)GetStatic("TutorialScene.MagicBookTutorialController", "Instance");
            panel = controller.GetType().BaseType.GetField("panel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
            Invoke(panel, "OnNextClicked");
            Assert.That(Step, Is.EqualTo("MagicBook_SelectMagic"));
            Invoke(State, "OnMagicSelected");
            Invoke(panel, "OnNextClicked");
            Assert.That(Step, Is.EqualTo("MagicBook_OpenElementChart"));
            Invoke(State, "OnElementChartOpened");
            Invoke(panel, "OnNextClicked");
            yield return SceneManager.LoadSceneAsync("LobbyScene");
            Assert.That(Step, Is.EqualTo("Common_SelectDeck"));
            controller = (Component)GetStatic("TutorialScene.LobbyTutorialController", "Instance");
            panel = controller.GetType().BaseType.GetField("panel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
            for (int i = 0; i < 5; i++) Invoke(panel, "OnNextClicked");
            Assert.That(Step, Is.EqualTo("HospitalityBattle"));
            Assert.That(Progress("IsLobbyPending"), Is.True);
        }

        [UnityTest]
        public IEnumerator FinalGuidanceCompletionPersistsAndDoesNotRestart()
        {
            StartGuidance();
            Invoke(Manager, "FinishOnboarding");
            Assert.That(Progress("IsLobbyPending"), Is.False);
            yield return null;
            StartGuidance();
            Assert.That((Object)Manager == null, Is.True);
        }

        [UnityTest]
        public IEnumerator CompletionCallbackAfterAccountChangeDoesNotWriteBeforeUpdate()
        {
            StartGuidance();
            SetStatic("Global.SceneContext", "User", null);
            Invoke(Manager, "FinishOnboarding");
            Assert.That(Progress("IsLobbyPending"), Is.True, "이전 계정의 안내 대기를 유지한다.");
            yield return null;
            Assert.That((Object)Manager == null, Is.True);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded -= DisableNetworkStartup;
            if ((Object)Manager != null) Object.Destroy(((Component)Manager).gameObject);
            var empty = SceneManager.CreateScene("LobbyGuidanceCleanup");
            SceneManager.SetActiveScene(empty);
            foreach (string sceneName in new[] { "LobbyScene", "ManageDeckScene", "MagicBookScene" })
                if (SceneManager.GetSceneByName(sceneName).isLoaded) yield return SceneManager.UnloadSceneAsync(sceneName);
            SetStatic("Global.SceneContext", "User", previousUser);
            SetStatic("Global.SceneContext", "JwtToken", previousToken);
            if (fixtureContext != null) Object.Destroy(fixtureContext);
            if (string.IsNullOrEmpty(previousCheckpoint)) PlayerPrefs.DeleteKey(checkpointKey);
            else PlayerPrefs.SetString(checkpointKey, previousCheckpoint);
            PlayerPrefs.Save();
            yield return null;
        }
    }
}
#endif
