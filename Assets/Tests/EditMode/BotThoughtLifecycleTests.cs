using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using GameScene.Dto;
using Global.Util;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class BotThoughtLifecycleTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        // Runtime behaviours live in Assembly-CSharp, which this asmdef cannot reference.
        private static Type RuntimeType(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(name)).First(type => type != null);

        private static string Encode(byte[] bytes) => Convert.ToBase64String(bytes)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static string Token(RSA key, string role)
        {
            string payload = Encode(Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"kid\":\"bot-test\"}")) + "." +
                Encode(Encoding.UTF8.GetBytes("{\"scope\":\"" + role + "\"}"));
            return payload + "." + Encode(key.SignData(Encoding.UTF8.GetBytes(payload),
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        }

        [UnityTest]
        public IEnumerator ThoughtsFollowConnectionLifecycleWithAndWithoutGameEndController()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();

            Type context = RuntimeType("Global.SceneContext");
            Type uiType = RuntimeType("Global.SystemMessageUI");
            Type connectorType = RuntimeType("GameScene.StompConnector");
            PropertyInfo tokenProperty = context.GetProperty("JwtToken");
            object previousToken = tokenProperty.GetValue(null);
            object previousResult = context.GetProperty("MatchResult").GetValue(null);
            var root = new GameObject("Bot thought regression fixture");
            using RSA key = RSA.Create(2048);
            try
            {
                RSAParameters publicKey = key.ExportParameters(false);
                JwksService.Reset();
                JwksService.WebRequestOverride = _ => "{\"keys\":[{\"kty\":\"RSA\",\"alg\":\"RS256\"," +
                    "\"use\":\"sig\",\"kid\":\"bot-test\",\"n\":\"" + Encode(publicKey.Modulus) +
                    "\",\"e\":\"" + Encode(publicKey.Exponent) + "\"}]}";
                IEnumerator fetch = JwksService.FetchJwks();
                while (fetch.MoveNext()) { }

                var uiObject = new GameObject("SystemMessage");
                uiObject.transform.SetParent(root.transform);
                uiObject.SetActive(false);
                var panel = new GameObject("Panel");
                panel.transform.SetParent(uiObject.transform);
                var label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                label.transform.SetParent(panel.transform);
                var text = label.GetComponent<TextMeshProUGUI>();
                Component ui = uiObject.AddComponent(uiType);
                uiType.GetField("messagePanel", PrivateInstance).SetValue(ui, panel);
                uiType.GetField("messageText", PrivateInstance).SetValue(ui, text);
                uiObject.SetActive(true);
                var queue = (ICollection)uiType.GetField("messageQueue", BindingFlags.Static | BindingFlags.NonPublic)
                    .GetValue(null);
                Assert.AreEqual(0, queue.Count);

                object thoughtHandler = Activator.CreateInstance(RuntimeType("GameScene.Handler.BotThoughtHandler"));
                MethodInfo handle = thoughtHandler.GetType().GetMethod("Handler");
                object resultHandler = Activator.CreateInstance(RuntimeType("GameScene.Handler.ResultHandler"));
                var thought = new BotThoughtInfo { reason = "first", ruleId = "test", botSide = "RightPlayer" };

                foreach (bool spectator in new[] { true, false })
                {
                    GameObject endObject = null;
                    if (!spectator)
                    {
                        endObject = new GameObject("GameEnd");
                        endObject.transform.SetParent(root.transform);
                        endObject.AddComponent(RuntimeType("GameScene.GameEndEventController"));
                    }

                    var connectionObject = new GameObject("Connection");
                    connectionObject.transform.SetParent(root.transform);
                    var connector = (Behaviour)connectionObject.AddComponent(connectorType);
                    // Awake initializes the real transport; suppress Start so no network request is made.
                    connector.enabled = false;
                    connectorType.GetField("isSpectator", PrivateInstance).SetValue(connector, spectator);
                    Assert.IsFalse((bool)connectorType.GetProperty("HasMatchEnded").GetValue(connector));

                    tokenProperty.SetValue(null, Token(key, "WORDONLINE_USER"));
                    handle.Invoke(thoughtHandler, new object[] { thought });
                    Assert.IsFalse(panel.activeSelf, "Non-admin telemetry must stay hidden.");

                    tokenProperty.SetValue(null, Token(key, "WORDONLINE_ADMIN"));
                    thought.reason = "first";
                    handle.Invoke(thoughtHandler, new object[] { thought });
                    Assert.IsTrue(panel.activeSelf, "Admin thoughts must display, including in spectator mode.");
                    StringAssert.Contains("first", text.text);
                    thought.reason = "latest";
                    handle.Invoke(thoughtHandler, new object[] { thought });
                    StringAssert.Contains("latest", text.text);
                    Assert.AreEqual(0, queue.Count, "Telemetry must not survive through the static queue.");

                    resultHandler.GetType().GetMethod("Handler").Invoke(resultHandler, new object[] { new ResultInfo() });
                    Assert.IsTrue((bool)connectorType.GetProperty("HasMatchEnded").GetValue(connector));
                    Assert.IsFalse(panel.activeSelf, "Result reception must clear spectator and player telemetry.");
                    handle.Invoke(thoughtHandler, new object[] { thought });
                    Assert.IsFalse(panel.activeSelf, "Late telemetry must stay hidden.");

                    Object.DestroyImmediate(connectionObject);
                    if (endObject != null) Object.DestroyImmediate(endObject);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                tokenProperty.SetValue(null, previousToken);
                context.GetProperty("MatchResult").SetValue(null, previousResult);
                JwksService.Reset();
            }

            yield return new ExitPlayMode();
        }
    }
}
