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

        [UnityTest]
        public IEnumerator StepTransitionClearsSelectionBeforeReplacingCardsAndKeepsNormalTransportDisabled()
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

        private void DisableNetworkDriver(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "FactionTutorialBattleScene") return;
            driver = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                .Single(component => component.GetType().Name == "FactionTutorialBattleDriver");
            driver.enabled = false;
        }

        private void Apply(int revision, string stage, string status, int card)
        {
            string json = $"{{\"battleId\":\"tutorial-fixture\",\"ownerId\":{Owner},\"faction\":\"HELL\",\"revision\":{revision},\"stage\":\"{stage}\",\"status\":\"{status}\",\"mana\":120,\"cards\":[{card}],\"frames\":[],\"snapshot\":[],\"remainingSeconds\":120}}";
            driver.GetType().GetMethod("Apply", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(driver, new[] { Json("GameScene.Dto.TutorialStateDto", json) });
        }

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
