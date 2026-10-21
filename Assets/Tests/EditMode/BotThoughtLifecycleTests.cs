using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using GameScene.Dto;
using Global.Util;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class BotThoughtLifecycleTests
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Debug/BotThoughtLog.prefab";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static Type RuntimeType(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(name)).First(type => type != null);
        private static T Field<T>(Component component, string name) =>
            (T)component.GetType().GetField(name, PrivateInstance).GetValue(component);
        private static void Call(object target, string name, params object[] args) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, args);
        private static void LogCall(string name, params object[] args) => RuntimeType("GameScene.BotThoughtLog").GetMethod(name).Invoke(null, args);
        private static int Count => ((ICollection)RuntimeType("GameScene.BotThoughtLog").GetProperty("Entries").GetValue(null)).Count;
        private static string Encode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        private static string Token(RSA key, string role)
        {
            string payload = Encode(Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"kid\":\"bot-test\"}")) + "." +
                Encode(Encoding.UTF8.GetBytes("{\"scope\":\"" + role + "\"}"));
            return payload + "." + Encode(key.SignData(Encoding.UTF8.GetBytes(payload), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        }
        private static void FetchKeys(RSA key)
        {
            RSAParameters publicKey = key.ExportParameters(false);
            JwksService.Reset();
            JwksService.WebRequestOverride = _ => "{\"keys\":[{\"kty\":\"RSA\",\"alg\":\"RS256\",\"use\":\"sig\",\"kid\":\"bot-test\",\"n\":\"" +
                Encode(publicKey.Modulus) + "\",\"e\":\"" + Encode(publicKey.Exponent) + "\"}]}";
            IEnumerator fetch = JwksService.FetchJwks();
            while (fetch.MoveNext()) { }
        }
        private static Component CreateUI(Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab);
            var instance = Object.Instantiate(prefab, parent);
            var ui = instance.GetComponentInChildren(RuntimeType("GameScene.BotThoughtLogUI"), true);
            Assert.IsNotNull(ui);
            Call(ui, "RefreshAuthorization");
            return ui;
        }

        [Test]
        public void RealScenesContainTheSharedLogPrefab()
        {
            string guid = AssetDatabase.AssetPathToGUID(PrefabPath);
            Assert.IsNotEmpty(guid);
            foreach (string scene in new[] { "GameScene", "SpectatingScene", "ResultScene" })
            {
                StringAssert.Contains(guid, File.ReadAllText("Assets/Scenes/" + scene + ".unity"), scene);
                var loaded = EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity", OpenSceneMode.Additive);
                try
                {
                    Assert.AreEqual(1, loaded.GetRootGameObjects().SelectMany(root =>
                        root.GetComponentsInChildren(RuntimeType("GameScene.BotThoughtLogUI"), true)).Count(), scene);
                }
                finally
                {
                    EditorSceneManager.CloseScene(loaded, true);
                }
            }
        }

        [UnityTest]
        public IEnumerator ActualPrefabRetainsHistoryAcrossResultsAndResetsForNextMatch()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            Type context = RuntimeType("Global.SceneContext");
            var token = context.GetProperty("JwtToken");
            object previousToken = token.GetValue(null);
            var root = new GameObject("Bot thought regression fixture");
            using RSA key = RSA.Create(2048);
            try
            {
                FetchKeys(key);
                string adminToken = Token(key, "WORDONLINE_ADMIN");
                object handler = Activator.CreateInstance(RuntimeType("GameScene.Handler.BotThoughtHandler"));
                var thought = new BotThoughtInfo { reason = "first", ruleId = "test", botSide = "RightPlayer" };
                foreach (bool spectator in new[] { false, true })
                {
                    token.SetValue(null, adminToken);
                    var connection = new GameObject("Connection");
                    connection.transform.SetParent(root.transform);
                    var connector = (Behaviour)connection.AddComponent(RuntimeType("GameScene.StompConnector"));
                    connector.enabled = false; // Real Awake; suppress network Start.
                    connector.GetType().GetField("isSpectator", PrivateInstance).SetValue(connector, spectator);
                    Assert.AreEqual(0, Count, "New match must reset history.");
                    Component ui = CreateUI(root.transform);
                    var visible = Field<GameObject>(ui, "visibilityRoot");
                    var panel = Field<GameObject>(ui, "panel");
                    var text = Field<TMP_Text>(ui, "logText");
                    Assert.IsTrue(visible.activeSelf, "Admin can open an empty log.");
                    Assert.IsFalse(panel.activeSelf);
                    Call(ui, "Toggle");
                    Assert.IsTrue(panel.activeSelf);
                    Assert.IsFalse(text.richText);
                    JwksService.Reset();
                    Call(ui, "RefreshAuthorization");
                    Call(handler, "Handler", thought);
                    Assert.IsFalse(visible.activeSelf);
                    Assert.AreEqual(0, Count, "Missing keys fail closed.");
                    FetchKeys(key);
                    token.SetValue(null, Token(key, "WORDONLINE_USER"));
                    Call(ui, "RefreshAuthorization");
                    Call(handler, "Handler", thought);
                    Assert.IsFalse(visible.activeSelf);
                    Assert.AreEqual(0, Count, "Non-admin frames are never retained.");
                    token.SetValue(null, adminToken);
                    Call(ui, "Toggle");
                    thought.reason = "first";
                    Call(handler, "Handler", thought);
                    Call(ui, "LateUpdate");
                    StringAssert.Contains("first", text.text);
                    StringAssert.IsMatch(@"\[\d+:\d{2}\] R", text.text);
                    for (int i = 0; i < 100; i++)
                    {
                        thought.reason = "entry-" + i;
                        LogCall("Append", thought);
                    }
                    Call(ui, "LateUpdate");
                    Assert.AreEqual(100, Count);
                    StringAssert.DoesNotContain("first", text.text);
                    string snapshot = text.text;
                    Call(ui, "OnScroll", new Vector2(0f, 0.7f));
                    thought.reason = "<b>newest while reading</b>";
                    LogCall("Append", thought);
                    Call(ui, "LateUpdate");
                    Assert.AreEqual(snapshot, text.text, "Eviction must not move the reader's rendered snapshot.");
                    Call(ui, "ShowLatest");
                    StringAssert.Contains(thought.reason, text.text);
                    Assert.AreEqual(100, Count);
                    Component capture = Field<Component>(ui, "scrollCapture");
                    var pointer = new PointerEventData(null);
                    Call(capture, "OnPointerDown", pointer);
                    Assert.IsTrue((bool)RuntimeType("GameScene.PointerInputUtility")
                        .GetProperty("IsPointerCapturedByUi").GetValue(null));
                    snapshot = text.text;
                    thought.reason = "while dragging";
                    LogCall("Append", thought);
                    Call(ui, "LateUpdate");
                    Assert.AreEqual(snapshot, text.text);
                    Call(capture, "OnPointerUp", pointer);
                    Assert.IsTrue((bool)RuntimeType("GameScene.PointerInputUtility")
                        .GetProperty("IsPointerCapturedByUi").GetValue(null), "Release must not cast through to the field in this frame.");
                    Call(ui, "ShowLatest");
                    StringAssert.Contains("while dragging", text.text);
                    thought.reason = new string('x', 5000);
                    LogCall("Append", thought);
                    var entries = (IEnumerable)RuntimeType("GameScene.BotThoughtLog").GetProperty("Entries").GetValue(null);
                    Assert.Less(entries.Cast<string>().Last().Length, 1100);
                    Call(connector, "NotifyMatchEnded");
                    int total = (int)RuntimeType("GameScene.BotThoughtLog").GetProperty("TotalCount").GetValue(null);
                    thought.reason = "late forbidden";
                    Call(handler, "Handler", thought);
                    Assert.AreEqual(total, RuntimeType("GameScene.BotThoughtLog").GetProperty("TotalCount").GetValue(null));
                    Assert.IsTrue((bool)RuntimeType("GameScene.BotThoughtLog").GetProperty("HasEnded").GetValue(null));
                    Object.DestroyImmediate(ui.gameObject);
                    Object.DestroyImmediate(connection);
                    Component resultUI = CreateUI(root.transform);
                    Call(resultUI, "Toggle");
                    Assert.AreEqual(100, Count, "Result UI without a connector retains history.");
                    StringAssert.Contains(new string('x', 100), Field<TMP_Text>(resultUI, "logText").text);
                    Object.DestroyImmediate(resultUI.gameObject);
                }
                token.SetValue(null, Token(key, "WORDONLINE_USER"));
                LogCall("DiscardOtherOwner");
                Assert.AreEqual(0, Count, "Account change discards retained history.");
                token.SetValue(null, Token(key, "WORDONLINE_ADMIN"));
                LogCall("BeginMatch");
                LogCall("Append", new BotThoughtInfo { reason = "logout" });
                context.GetMethod("ClearContext").Invoke(null, null);
                Assert.AreEqual(0, Count, "Logout must clear history.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                LogCall("Clear");
                token.SetValue(null, previousToken);
                JwksService.Reset();
            }
            yield return new ExitPlayMode();
        }
    }
}
