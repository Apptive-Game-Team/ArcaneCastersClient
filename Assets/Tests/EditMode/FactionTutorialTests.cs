using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Data;
using GameScene.Dto;
using Global.Serialization;
using Newtonsoft.Json;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class FactionTutorialTests
    {
        private const string PanelPath = "Assets/Resources/UI/Tutorial/FactionTutorialPanel.prefab";
        private static Type Runtime(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("TutorialScene." + name)).First(type => type != null);

        [Test]
        public void ReadyAndStatePayloadsRoundTripWithoutEnumOrConstructorFailures()
        {
            var ready = JsonCodec.Deserialize<TutorialReadyDto>("{\"battleId\":\"tutorial-test\",\"ownerId\":7,\"faction\":\"HELL\",\"server\":\"http://localhost:7001\",\"expiresAt\":\"2030-01-01T00:00:00Z\"}");
            Assert.That(JsonCodec.Deserialize<TutorialReadyDto>(JsonCodec.Serialize(ready)).ownerId, Is.EqualTo(7));
            var state = JsonCodec.Deserialize<TutorialStateDto>("{\"battleId\":\"tutorial-test\",\"revision\":3,\"stage\":\"future-stage\",\"status\":\"FUTURE_STATUS\",\"cards\":[1,2],\"frames\":[{\"frame\":42,\"tick\":35,\"objects\":{\"create\":[],\"update\":[],\"projectile\":[]},\"events\":[]}],\"snapshot\":[],\"lastFrame\":42,\"tick\":35,\"tickRate\":20}");
            var copy = JsonCodec.Deserialize<TutorialStateDto>(JsonCodec.Serialize(state));
            Assert.That(copy.stage, Is.EqualTo("future-stage"));
            Assert.That(copy.status, Is.EqualTo("FUTURE_STATUS"));
            Assert.That(copy.frames.Single().frame, Is.EqualTo(42));
            Assert.That(copy.frames.Single().tick, Is.EqualTo(35));
            Assert.That(copy.tick, Is.EqualTo(35));
            Assert.That(copy.tickRate, Is.EqualTo(20));
        }

        [Test]
        public void PendingRequestAndReadyArePreservedPerUserAcrossReloads()
        {
            Type type = Runtime("FactionTutorialProgress");
            var begin = type.GetMethod("Begin");
            var read = type.GetMethod("Read");
            var save = type.GetMethod("Save");
            object first = begin.Invoke(null, new object[] { 98765431L });
            object other = begin.Invoke(null, new object[] { 98765432L });
            FieldInfo request = first.GetType().GetField("requestId");
            try
            {
                first.GetType().GetField("battle").SetValue(first, new TutorialReadyDto { battleId = "tutorial-kept", server = "http://localhost:7001", ownerId = 98765431 });
                save.Invoke(null, new object[] { 98765431L, first });
                object resumed = begin.Invoke(null, new object[] { 98765431L });
                Assert.That(request.GetValue(resumed), Is.EqualTo(request.GetValue(first)));
                Assert.That(request.GetValue(other), Is.Not.EqualTo(request.GetValue(first)));
                Assert.That(((TutorialReadyDto)resumed.GetType().GetField("battle").GetValue(resumed)).battleId, Is.EqualTo("tutorial-kept"));
                type.GetMethod("Complete").Invoke(null, new object[] { 98765431L });
                Assert.That(type.GetMethod("IsPending").Invoke(null, new object[] { 98765431L }), Is.False);
                Assert.That(type.GetMethod("IsPending").Invoke(null, new object[] { 98765432L }), Is.True);
            }
            finally
            {
                string prefix = "faction-tutorial-v1:" + ServerList.MatchingServer.url + ":";
                PlayerPrefs.DeleteKey(prefix + "98765431");
                PlayerPrefs.DeleteKey(prefix + "98765432");
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void BattleCompletionPersistsLobbyGuidanceWithoutRestartingCombatOrExistingAccounts()
        {
            const long owner = 98765461;
            const long other = 98765462;
            Type progress = Runtime("FactionTutorialProgress");
            object Call(string method, long user) => progress.GetMethod(method).Invoke(null, new object[] { user });
            string prefix = "faction-tutorial-v1:" + ServerList.MatchingServer.url + ":";
            try
            {
                Call("Begin", owner);
                Call("Complete", other);
                Call("CompleteBattle", owner);
                Assert.That(Call("IsPending", owner), Is.False, "전투는 다시 시작하지 않는다.");
                Assert.That(Call("IsLobbyPending", owner), Is.True, "다시 읽은 체크포인트도 로비 안내를 기다린다.");
                Assert.That(Call("IsLobbyPending", other), Is.False, "기존 완료 계정은 안내를 다시 받지 않는다.");
                Assert.That(Call("IsLobbyPending", owner + 2), Is.False, "체크포인트가 없는 계정도 제외한다.");
                Call("Complete", owner);
                Assert.That(Call("IsLobbyPending", owner), Is.False);
                object saved = Call("Read", owner);
                Assert.That(saved.GetType().GetField("state").GetValue(saved), Is.EqualTo("completed"));
            }
            finally
            {
                PlayerPrefs.DeleteKey(prefix + owner);
                PlayerPrefs.DeleteKey(prefix + other);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void StartingRewardsMatchTheFiveFactionPracticeCards()
        {
            Type cards = Runtime("FactionTutorialCards");
            string[] List(string field) => (string[])cards.GetField(field).GetValue(null);
            Assert.That(List("HellPractice"), Is.EqualTo(new[] { "fire_shot", "rock_golem", "rock_mage", "wall_golem", "stone_gargoyle" }));
            Assert.That(List("NaturePractice"), Is.EqualTo(new[] { "chain_lightning", "electric_spearman", "aqua_archer", "tree_golem", "thunder_spirit" }));
            Assert.That(List("Hell"), Is.EqualTo(List("HellPractice")));
            Assert.That(List("Nature"), Is.EqualTo(List("NaturePractice")));
            foreach (string field in new[] { "HellPractice", "NaturePractice" })
                foreach (string name in List(field).Where(name => !name.Contains("shot") && name != "chain_lightning"))
                {
                    string prefabName = string.Concat(name.Split('_').Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1)));
                    Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/" + prefabName + ".prefab"), Is.Not.Null, name);
                }
        }

        [TestCase("FactionSelectionScene", "FactionSelectionController")]
        [TestCase("FactionTutorialBattleScene", "FactionTutorialBattleDriver")]
        public void ScenesAreBuiltAndTheirControllersHaveTheAuthoredPanel(string sceneName, string controller)
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                string path = "Assets/Scenes/" + sceneName + ".unity";
                Assert.That(EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == path), Is.True);
                EditorSceneManager.OpenScene(path);
                var component = Object.FindObjectsOfType<MonoBehaviour>(true).Single(item => item.GetType().Name == controller);
                Assert.That(new SerializedObject(component).FindProperty("panel").objectReferenceValue, Is.Not.Null);
                if (sceneName.Contains("Battle"))
                    Assert.That(Object.FindObjectsOfType<MonoBehaviour>(true).Count(item => item.GetType().Name == "StompConnector" || item.GetType().Name == "PingSender"), Is.Zero);
            }
            finally
            {
                if (setup.Any(scene => scene.isLoaded) && setup.Any(scene => scene.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            }
        }

        [Test]
        public void NewAccountIntentSurvivesFailedUserLookupAndIsAdoptedOnlyByThatAccount()
        {
            Type progress = Runtime("FactionTutorialProgress");
            Type context = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("Global.SceneContext")).First(type => type != null);
            var token = context.GetProperty("JwtToken");
            object previous = token.GetValue(null);
            string prefix = ServerList.MatchingServer.url + ":";
            string Jwt(long member, string signature) => "e30." + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"memberId\":" + member + "}")).TrimEnd('=').Replace('+', '-').Replace('/', '_') + "." + signature;
            try
            {
                token.SetValue(null, Jwt(98765451, "first"));
                progress.GetMethod("MarkNewAccountIntent").Invoke(null, null);
                token.SetValue(null, Jwt(98765452, "other"));
                progress.GetMethod("AdoptNewAccountIntent").Invoke(null, new object[] { 98765453L });
                Assert.That(progress.GetMethod("IsPending").Invoke(null, new object[] { 98765453L }), Is.False);
                token.SetValue(null, Jwt(98765451, "refreshed"));
                progress.GetMethod("AdoptNewAccountIntent").Invoke(null, new object[] { 98765454L });
                Assert.That(progress.GetMethod("IsPending").Invoke(null, new object[] { 98765454L }), Is.True);
                Assert.That(progress.GetMethod("ResumeScene").Invoke(null, new object[] { 98765454L }), Is.EqualTo("ComicIntroScene"));
                progress.GetMethod("MarkComicSeen").Invoke(null, new object[] { 98765454L });
                Assert.That(progress.GetMethod("ResumeScene").Invoke(null, new object[] { 98765454L }), Is.EqualTo("FactionSelectionScene"));
                Assert.That(PlayerPrefs.HasKey("new-account-tutorial-v1:" + prefix + "98765451"), Is.False);
            }
            finally
            {
                token.SetValue(null, previous);
                foreach (string key in new[] { "new-account-tutorial-v1:" + prefix + "98765451", "faction-tutorial-v1:" + prefix + "98765453", "faction-tutorial-v1:" + prefix + "98765454" }) PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        [TestCase("ko-KR")]
        [TestCase("en")]
        public void TutorialTextExistsInBothLocalesAndFitsTheAuthoredPanel(string locale)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath);
            Assert.That(prefab, Is.Not.Null);
            var root = Object.Instantiate(prefab);
            try
            {
                var panel = root.GetComponents<MonoBehaviour>().Single(item => item.GetType().Name == "FactionTutorialPanel");
                var data = new SerializedObject(panel);
                var body = (TMP_Text)data.FindProperty("body").objectReferenceValue;
                string shared = File.ReadAllText("Assets/Localization/Onboarding/Onboarding Shared Data.asset");
                string localized = File.ReadAllText("Assets/Localization/Onboarding/Onboarding_" + locale + ".asset");
                var keys = Regex.Matches(shared, @"m_Id: (\d+)\s+m_Key: (FactionTutorial\.[^\s]+)");
                Assert.That(keys.Count, Is.GreaterThanOrEqualTo(40));
                foreach (Match key in keys)
                {
                    var text = Regex.Match(localized, @"m_Id: " + key.Groups[1].Value + @"\s+m_Localized: (""(?:\\.|[^""\\])*"")");
                    Assert.That(text.Success, Is.True, key.Groups[2].Value + " / " + locale);
                    body.text = JsonConvert.DeserializeObject<string>(text.Groups[1].Value);
                    body.ForceMeshUpdate(true, true);
                    Assert.That(body.isTextOverflowing, Is.False, key.Groups[2].Value + " / " + locale);
                }
                foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                    Assert.That(button.onClick.GetPersistentEventCount(), Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
