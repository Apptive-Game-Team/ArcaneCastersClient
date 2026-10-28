using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using Data;
using Global.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace WordOnline.Tests {
    public class LockstepPlayableSceneTests {
        private static Type GameType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        private sealed class CastCounter {
            public int Count;
            public void Accepted() => Count++;
        }

        [UnityTest]
        public IEnumerator SoloBattleReachesRenderedResultScene() {
            string configPath = Environment.GetEnvironmentVariable("AC_LOCKSTEP_UNITY_CONFIG");
            if (string.IsNullOrEmpty(configPath)) Assert.Ignore("Requires the disposable lockstep network fixture.");
            yield return new EnterPlayMode();
            configPath = Environment.GetEnvironmentVariable("AC_LOCKSTEP_UNITY_CONFIG");
            var config = JObject.Parse(File.ReadAllText(configPath));
            var context = GameType("Global.SceneContext");
            new GameObject("SceneContext").AddComponent(context);
            Assert.IsTrue(JwksService.ParseJwksResponse((string)config["jwks"]));
            Server lobby = ServerList.MatchingServer;
            string oldHost = lobby.host;
            int oldPort = lobby.port;
            bool oldSecure = lobby.isSecure;
            var local = new Uri((string)config["baseUrl"]);
            lobby.host = local.Host; lobby.port = local.Port; lobby.isSecure = false;
            bool success = false;
            bool cast = (bool?)config["cast"] == true;
            var casts = new CastCounter();
            Action accepted = casts.Accepted;
            var senderType = GameType("GameScene.Card.CardInputSender");
            var acceptedEvent = senderType.GetEvent("OnMagicSucceeded");
            acceptedEvent.AddEventHandler(null, accepted);
            try {
                JObject readiness;
                if (cast) {
                    var sourceType = GameType("Data.Magic.MagicInfoDataSource");
                    var source = sourceType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).GetValue(null);
                    var payload = JsonConvert.DeserializeObject(config["magics"].ToString(), GameType("Data.Magic.MagicInfoResponse"));
                    sourceType.GetMethod("ProcessResponse", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(source, new[] { payload });
                }
                using (var request = new UnityWebRequest(local + "api/server/lockstep-matches", "POST")) {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(config["request"].ToString()));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", "Bearer " + (string)config["serverToken"]);
                    request.timeout = 20;
                    yield return request.SendWebRequest();
                    Assert.AreEqual(UnityWebRequest.Result.Success, request.result, request.error);
                    readiness = JObject.Parse(request.downloadHandler.text);
                    Assert.IsTrue((bool)readiness["ready"]);
                    Assert.AreEqual((string)config["request"]["sessionId"], (string)readiness["sessionId"]);
                    Assert.AreEqual("lockstep-v1", (string)readiness["lockstepSimulationVersion"]);
                    Assert.IsNotEmpty((string)readiness["mapType"]);
                }
                if (cast) File.WriteAllText((string)config["readyPath"], "ready");
                var user = new User(11, "Scene Test", "", 1);
                context.GetProperty("User").SetValue(null, user);
                context.GetProperty("JwtToken").SetValue(null, (string)config["playerToken"]);
                context.GetProperty("MatchResult").SetValue(null, null);
                context.GetMethod("ClearAdventureMatch").Invoke(null, null);
                context.GetProperty("MatchInfo").SetValue(null, new MatchedInfoDto {
                    server = (string)config["baseUrl"], sessionId = (string)config["request"]["sessionId"],
                    leftUser = user, rightUser = new User((long)config["request"]["participants"][1], "Opponent", "", -1),
                    lockstepSimulationVersion = (string)readiness["lockstepSimulationVersion"], mapType = (string)readiness["mapType"]
                });
                context.GetMethod("PrepareMap").Invoke(null, new object[] { (string)readiness["mapType"], null });
                yield return SceneManager.LoadSceneAsync("GameScene");
                float deadline = Time.realtimeSinceStartup + 90;
                bool rendered = false;
                bool castSent = false;
                while (SceneManager.GetActiveScene().name != "ResultScene" && Time.realtimeSinceStartup < deadline) {
                    var objects = UnityEngine.Object.FindObjectsOfType(GameType("GameScene.ServedObjectComponent.ServedObject"));
                    if (!rendered && objects.Length >= 2 && Camera.main != null) {
                        Capture(Camera.main, (string)config["screenshotPath"]);
                        rendered = true;
                    }
                    if (cast && !castSent) {
                        var uiType = GameType("GameScene.GameSceneUIController");
                        var ui = UnityEngine.Object.FindObjectOfType(uiType);
                        var card = ui == null ? null : uiType.GetMethod("GetCardAt").Invoke(ui, new object[] { 0 });
                        var sender = UnityEngine.Object.FindObjectOfType(senderType);
                        if (card != null && sender != null && (bool)senderType.GetMethod("CanChangeSelection").Invoke(sender, new object[] { card, true })) {
                            card.GetType().GetMethod("OnCardClicked").Invoke(card, null);
                            castSent = (bool)senderType.GetMethod("TrySendInput").Invoke(sender, new object[] { new Vector3(17, 0, 5) });
                            Assert.IsTrue(castSent, "Selected card did not enter the input queue.");
                        }
                    }
                    yield return null;
                }
                Assert.AreEqual("ResultScene", SceneManager.GetActiveScene().name, "The real GameScene did not finish.");
                yield return null;
                Assert.IsTrue(rendered, "No player presentation was rendered.");
                if (cast) {
                    Assert.IsTrue(castSent, "No hand card was selected and cast.");
                    Assert.AreEqual(1, casts.Count, "The confirmed cast must acknowledge the actual card UI once.");
                }
                var controllerType = GameType("ResultScene.ResultSceneUIController");
                var controller = UnityEngine.Object.FindObjectOfType(controllerType);
                Assert.IsNotNull(controller);
                const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
                var result = controllerType.GetField("resultInfo", fields).GetValue(controller);
                Assert.IsNotNull(result);
                string expected = (string)config["expectedResult"];
                Assert.AreEqual(expected, result.GetType().GetField("leftPlayer").GetValue(result));
                string title = expected == "Win" ? "winTitle" : expected == "Lose" ? "loseTitle" : "drawTitle";
                Assert.IsTrue(((GameObject)controllerType.GetField(title, fields).GetValue(controller)).activeInHierarchy);
                success = true;
            } finally {
                acceptedEvent.RemoveEventHandler(null, accepted);
                lobby.host = oldHost; lobby.port = oldPort; lobby.isSecure = oldSecure;
                File.WriteAllText((string)config["reportPath"], new JObject { ["success"] = success, ["acceptedCasts"] = casts.Count }.ToString());
            }
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator ReportInterruptedTest() {
            string path = Environment.GetEnvironmentVariable("AC_LOCKSTEP_UNITY_CONFIG");
            if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
                var config = JObject.Parse(File.ReadAllText(path));
                string report = (string)config["reportPath"];
                if (!File.Exists(report)) File.WriteAllText(report, "{\"success\":false}");
            }
            JwksService.Reset();
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        private static void Capture(Camera camera, string path) {
            var target = new RenderTexture(960, 540, 24);
            var texture = new Texture2D(960, 540, TextureFormat.RGB24, false);
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            try {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            } finally {
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                UnityEngine.Object.Destroy(texture);
                UnityEngine.Object.Destroy(target);
            }
        }
    }
}
