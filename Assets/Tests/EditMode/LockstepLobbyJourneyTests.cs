using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Data;
using Global.Serialization;
using Global.Util;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace WordOnline.Tests {
    public class LockstepLobbyJourneyTests {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private List<ServerState> servers;
        private Dictionary<string, string> preferences;
        private JObject config, report;
        private sealed class ServerState {
            public Server Server;
            public string Host;
            public int Port;
            public bool Secure;
        }
        private sealed class CastCounter { public int Count; public void Accepted() => Count++; }
        private static Type GameType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        private static Uri LocalAddress(string value) {
            Assert.IsTrue(Uri.TryCreate(value, UriKind.Absolute, out var address) && address.IsLoopback && address.Scheme == "http", "Journey fixture endpoints must be local HTTP URLs.");
            return address;
        }
        private static void Write(string path, JObject value) {
            if (string.IsNullOrEmpty(path)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllText(path, value.ToString());
        }
        private void Route(Server server, Uri address) {
            if (!servers.Any(saved => ReferenceEquals(saved.Server, server)))
                servers.Add(new ServerState { Server = server, Host = server.host, Port = server.port, Secure = server.isSecure });
            server.host = address.Host; server.port = address.Port; server.isSecure = false;
        }
        private void SavePreference(string key) {
            preferences[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
            PlayerPrefs.DeleteKey(key);
        }
        private static IEnumerator JsonRequest(string url, string token, Action<JObject> receive, string method = "GET", JObject body = null, bool missingAllowed = false) {
            LocalAddress(url);
            using var request = new UnityWebRequest(url, method);
            request.downloadHandler = new DownloadHandlerBuffer(); request.timeout = 20;
            if (body != null) {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body.ToString()));
                request.SetRequestHeader("Content-Type", "application/json");
            }
            request.SetRequestHeader("Authorization", "Bearer " + token);
            var send = request.SendWebRequest();
            while (!send.isDone) yield return null;
            if (missingAllowed && request.responseCode == 404) { receive(null); yield break; }
            Assert.AreEqual(UnityWebRequest.Result.Success, request.result, method + " " + url + ": " + request.error);
            receive(JObject.Parse(request.downloadHandler.text));
        }
        private static IEnumerator WaitFor(Func<bool> condition, float seconds, string description) {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(condition(), description);
        }
        private static IEnumerator Delay(float seconds) {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until) yield return null;
        }
        private sealed class CanvasState {
            public Canvas Canvas;
            public RenderMode Mode;
            public Camera Camera;
            public float Distance;
        }
        private static IEnumerator ScreenShot(string path) {
            Assert.IsNotEmpty(path, "The rendered scene screenshot path must be configured.");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            var camera = Camera.main;
            if (camera == null) camera = UnityEngine.Object.FindObjectsOfType<Camera>().FirstOrDefault(candidate => candidate.isActiveAndEnabled);
            Assert.IsNotNull(camera, "The actual scene has no active camera to render.");
            const int width = 1280, height = 720;
            var target = new RenderTexture(width, height, 24) { antiAliasing = 1 };
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
            float oldAspect = camera.aspect; var oldRect = camera.rect; int oldMask = camera.cullingMask;
            var overlays = UnityEngine.Object.FindObjectsOfType<Canvas>()
                .Where(canvas => canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                .Select(canvas => new CanvasState { Canvas = canvas, Mode = canvas.renderMode, Camera = canvas.worldCamera, Distance = canvas.planeDistance }).ToArray();
            try {
                Assert.IsTrue(target.Create(), "The real scene render target could not be created.");
                camera.targetTexture = target; camera.rect = new Rect(0, 0, 1, 1); camera.aspect = (float)width / height;
                foreach (var saved in overlays) {
                    var canvas = saved.Canvas;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
                    canvas.planeDistance = Mathf.Max(camera.nearClipPlane + 0.1f, 1f);
                    camera.cullingMask |= 1 << canvas.gameObject.layer;
                    foreach (var renderer in canvas.GetComponentsInChildren<CanvasRenderer>(true)) camera.cullingMask |= 1 << renderer.gameObject.layer;
                }
                Canvas.ForceUpdateCanvases();
                // CanvasScaler와 layout도 실제 target 크기로 한 프레임 갱신한다.
                yield return null;
                Assert.IsNotNull(camera, "The actual scene camera was destroyed before capture.");
                Canvas.ForceUpdateCanvases();
                RenderTexture.active = target;
                GL.Clear(true, true, camera.backgroundColor);
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                byte[] png = texture.EncodeToPNG();
                Assert.IsTrue(png != null && png.Length > 0, "The actual scene renderer produced no PNG.");
                File.WriteAllBytes(path, png);
                Assert.IsTrue(File.Exists(path) && new FileInfo(path).Length == png.Length);
            } finally {
                foreach (var saved in overlays) if (saved.Canvas != null) {
                    saved.Canvas.renderMode = saved.Mode; saved.Canvas.worldCamera = saved.Camera; saved.Canvas.planeDistance = saved.Distance;
                }
                if (camera != null) { camera.targetTexture = oldTarget; camera.aspect = oldAspect; camera.rect = oldRect; camera.cullingMask = oldMask; }
                RenderTexture.active = oldActive; Canvas.ForceUpdateCanvases();
                target.Release(); UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(texture);
            }
        }
        private void Progress(string stage) {
            report["stage"] = stage;
            Write((string)config["progressPath"], report);
        }

        [UnityTest]
        public IEnumerator ActualLobbyButtonMatchCardAndResultUseLocalServices() {
            string path = Environment.GetEnvironmentVariable("AC_LOCKSTEP_UNITY_CONFIG");
            if (string.IsNullOrEmpty(path) || !File.Exists(path) || (bool?)JObject.Parse(File.ReadAllText(path))["journey"] != true)
                Assert.Ignore("Requires the disposable lobby journey fixture.");
            // 기존 장면의 Start가 로컬 주소 설정 전에 실행되지 않도록 빈 장면에서 PlayMode에 진입한다.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            // domain reload 전의 NUnit fixture/closure 객체를 이어 쓰지 않는다.
            servers = new List<ServerState>(); preferences = new Dictionary<string, string>();
            yield return RunLocalJourney();
        }

        private IEnumerator RunLocalJourney() {
            Assert.IsTrue(Application.isPlaying, "The real lobby journey must run after EnterPlayMode.");
            config = JObject.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("AC_LOCKSTEP_UNITY_CONFIG")));
            report = new JObject { ["success"] = false, ["journey"] = true, ["stage"] = "initializing" };
            var lobby = LocalAddress((string)config["lobbyBaseUrl"]);
            var account = LocalAddress((string)config["accountBaseUrl"]);
            long userId = (long?)config["userId"] ?? 11;
            long peerId = (long?)(config["peerUserId"] ?? config["peerId"]) ?? (userId == 11 ? 22 : 11);
            Assert.IsTrue((userId == 11 || userId == 22) && (peerId == 11 || peerId == 22) && peerId != userId);
            bool cast = (bool?)config["cast"] ?? userId == 11;
            string peerMode = (string)config["peerMode"] ?? "api";
            Assert.IsTrue(peerMode == "api" || peerMode == "dotnet" || peerMode == "unity", "Unknown local peer mode.");
            bool dotnetPeer = peerMode != "unity";
            string peerMatchedPath = (string)(config["peerMatchedPath"] ?? config["matchedPath"]);
            string playerToken = Environment.GetEnvironmentVariable("AC_LOCKSTEP_JOURNEY_PLAYER_TOKEN");
            string peerToken = Environment.GetEnvironmentVariable("AC_LOCKSTEP_JOURNEY_PEER_TOKEN");
            Assert.IsFalse(string.IsNullOrEmpty(playerToken), "The local player JWT must be supplied through the child environment.");
            var context = GameType("Global.SceneContext");
            var castType = GameType("GameScene.Card.CardInputSender");
            var casts = new CastCounter(); Action accepted = casts.Accepted;
            var acceptedEvent = castType.GetEvent("OnMagicSucceeded", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(acceptedEvent, "CardInputSender.OnMagicSucceeded must be a public static event.");
            acceptedEvent.AddEventHandler(null, accepted);
            try {
                Progress("routing-local");
                Assert.IsNotNull(servers); Assert.IsNotNull(preferences);
                foreach (var option in MatchingServerCatalog.Options) Route(option.Server, lobby);
                Route(ServerList.AccountServer, account);
                SavePreference("MagicInfoData"); SavePreference("GameParametersData"); SavePreference("MatchingServer.SelectedKey");
                Progress("jwks-fetch");
                JwksService.Reset(); JwksService.AccountServerUrl = account.GetLeftPart(UriPartial.Authority);
                var jwksFetch = JwksService.FetchJwks();
                while (jwksFetch.MoveNext()) {
                    if (jwksFetch.Current is AsyncOperation pending) { while (!pending.isDone) yield return null; }
                    else { Assert.IsNull(jwksFetch.Current, "Unexpected yield from production JwksService.FetchJwks."); yield return null; }
                }
                Assert.IsTrue(JwksService.IsFetched);
                Progress("context-initializing");
                Assert.IsNotNull(new GameObject("SceneContext").AddComponent(context));
                var clearContext = context.GetMethod("ClearContext", BindingFlags.Public | BindingFlags.Static);
                Assert.IsNotNull(clearContext, "SceneContext.ClearContext must be public static.");
                clearContext.Invoke(null, null);
                context.GetProperty("JwtToken").SetValue(null, playerToken);
                context.GetProperty("User").SetValue(null, new User(userId, "Local Journey " + userId, "", (long?)config["selectedDeckId"] ?? 1, 1000));
                Progress("cache-clearing");
                var clearDecks = GameType("LobbyScene.LobbyUIController").GetMethod("ClearCachedDecks", BindingFlags.Public | BindingFlags.Static);
                Assert.IsNotNull(clearDecks, "LobbyUIController.ClearCachedDecks must be public static.");
                clearDecks.Invoke(null, null);
                Progress("catalog-fetch");
                JObject magicResponse = null;
                yield return JsonRequest(lobby.GetLeftPart(UriPartial.Authority) + "/api/data/magics", playerToken, json => magicResponse = json);
                var expectedMagics = (JArray)magicResponse["magics"];
                Assert.IsNotEmpty(expectedMagics);
                report["magicCount"] = expectedMagics.Count;
                // 실제 pinger가 요청할 주소도 일회용 로컬 서버인지 먼저 확인한다.
                using (var targetsRequest = UnityWebRequest.Get(lobby.GetLeftPart(UriPartial.Authority) + "/api/match/servers")) {
                    targetsRequest.timeout = 20; targetsRequest.SetRequestHeader("Authorization", "Bearer " + playerToken);
                    var pingTargets = targetsRequest.SendWebRequest();
                    while (!pingTargets.isDone) yield return null;
                    report["pingTargetsStatus"] = targetsRequest.responseCode;
                    if (targetsRequest.result == UnityWebRequest.Result.Success)
                        foreach (var target in JArray.Parse(targetsRequest.downloadHandler.text)) LocalAddress((string)target["url"]);
                }
                var lobbyLoad = SceneManager.LoadSceneAsync("LobbyScene");
                while (!lobbyLoad.isDone) yield return null;
                var uiType = GameType("LobbyScene.LobbyUIController");
                var vmType = GameType("LobbyScene.LobbySceneViewModel");
                Component ui = null, vm = null, button = null;
                var buttonType = GameType("LobbyScene.Button.EnqueueButton");
                yield return WaitFor(() => {
                    if (SceneManager.GetActiveScene().name != "LobbyScene") return false;
                    ui = UnityEngine.Object.FindObjectOfType(uiType) as Component;
                    vm = UnityEngine.Object.FindObjectOfType(vmType) as Component;
                    button = UnityEngine.Object.FindObjectOfType(buttonType) as Component;
                    return ui != null && vm != null && button != null && !(bool)uiType.GetField("initializing", Private).GetValue(ui);
                }, 90, "Actual LobbyScene did not initialize its user, content and deck UI.");
                Assert.AreEqual("LobbyScene", SceneManager.GetActiveScene().name);
                Assert.IsNull(context.GetProperty("MatchInfo").GetValue(null), "Fixture must enter a fresh lobby without a pre-created game.");
                Assert.AreEqual("SELECTED", vmType.GetProperty("DeckMode").GetValue(vm));
                var source = GameType("Data.Magic.MagicInfoDataSource");
                var cached = (IEnumerable)source.GetMethod("GetCachedMagics").Invoke(null, null);
                var actualNames = new List<string>();
                foreach (var magic in cached) actualNames.Add(magic.GetType().GetField("id").GetValue(magic) + ":" + magic.GetType().GetField("name").GetValue(magic));
                CollectionAssert.AreEquivalent(expectedMagics.Select(magic => (long)magic["id"] + ":" + (string)magic["name"]).ToArray(), actualNames);
                Progress("lobby-ready");
                yield return ScreenShot((string)config["lobbyScreenshotPath"]);
                string startBarrier = (string)config["startMatchingBarrierPath"];
                if (!string.IsNullOrEmpty(startBarrier))
                    yield return WaitFor(() => File.Exists(startBarrier), 180, "The second Unity lobby did not become ready.");
                int clicks = 0; Action clicked = () => clicks++;
                buttonType.GetEvent("OnClick").AddEventHandler(button, clicked);
                buttonType.GetMethod("ButtonEvent").Invoke(button, null);
                Assert.AreEqual(1, clicks); report["matchingButtonClicks"] = clicks;
                Progress("matching-clicked");
                if (dotnetPeer) {
                    Assert.IsNotEmpty(peerToken); Assert.IsNotEmpty(peerMatchedPath);
                    JObject mine = null;
                    float queuedDeadline = Time.realtimeSinceStartup + 30;
                    while (mine == null && Time.realtimeSinceStartup < queuedDeadline) {
                        yield return JsonRequest(lobby.GetLeftPart(UriPartial.Authority) + "/api/match/tickets/active", playerToken, json => mine = json, missingAllowed: true);
                        if (mine == null) yield return Delay(0.1f);
                    }
                    Assert.IsNotNull(mine, "The actual UI did not create its matching ticket.");
                    Assert.AreEqual("QUEUED", ((string)mine["state"]).ToUpperInvariant(), "Local fixture must pair two humans before hospitality/rule fallback.");
                    report["ticketId"] = (string)mine["ticketId"];
                    JObject peerTicket = null;
                    yield return JsonRequest(lobby.GetLeftPart(UriPartial.Authority) + "/api/match/tickets", peerToken, json => peerTicket = json, "POST", new JObject { ["deckMode"] = "SELECTED", ["serverPings"] = new JArray() });
                    float peerDeadline = Time.realtimeSinceStartup + 60;
                    while (peerTicket == null || !string.Equals((string)peerTicket["state"], "MATCHED", StringComparison.OrdinalIgnoreCase)) {
                        Assert.Less(Time.realtimeSinceStartup, peerDeadline, "The actual lobby did not allocate the second human's ticket.");
                        yield return Delay(0.1f);
                        yield return JsonRequest(lobby.GetLeftPart(UriPartial.Authority) + "/api/match/tickets/active", peerToken, json => peerTicket = json);
                    }
                    var peerMatch = peerTicket["matchInfo"];
                    LocalAddress((string)peerMatch["server"]);
                    Write(peerMatchedPath, new JObject { ["server"] = peerMatch["server"], ["sessionId"] = peerMatch["sessionId"], ["userId"] = peerId, ["simulationVersion"] = peerMatch["lockstepSimulationVersion"] });
                }
                yield return WaitFor(() => context.GetProperty("MatchInfo").GetValue(null) is MatchedInfoDto, 60, "Lobby matching did not publish MatchInfo to the real scene context.");
                var match = (MatchedInfoDto)context.GetProperty("MatchInfo").GetValue(null);
                LocalAddress(match.server);
                Assert.IsTrue(match.IsLockstep); Assert.IsNotEmpty(match.mapType);
                CollectionAssert.AreEquivalent(new long[] { userId, peerId }, new[] { match.leftUser.id, match.rightUser.id });
                report["sessionId"] = match.sessionId; report["userId"] = userId; report["simulationVersion"] = match.lockstepSimulationVersion;
                Progress("matched");
                yield return WaitFor(() => SceneManager.GetActiveScene().name == "GameScene", 60, "Actual lobby matched transition did not load GameScene.");
                Progress("game-scene");
                bool rendered = false, castSent = false;
                float gameDeadline = Time.realtimeSinceStartup + ((float?)config["timeoutSeconds"] ?? 120);
                var servedType = GameType("GameScene.ServedObjectComponent.ServedObject");
                var gameUiType = GameType("GameScene.GameSceneUIController");
                while (SceneManager.GetActiveScene().name != "ResultScene" && Time.realtimeSinceStartup < gameDeadline) {
                    if (!rendered && Camera.main != null && UnityEngine.Object.FindObjectsOfType(servedType).Length >= 2) {
                        rendered = true;
                        yield return ScreenShot((string)config["screenshotPath"]);
                    }
                    if (cast && !castSent) {
                        var gameUi = UnityEngine.Object.FindObjectOfType(gameUiType);
                        var card = gameUi == null ? null : gameUiType.GetMethod("GetCardAt").Invoke(gameUi, new object[] { 0 });
                        var sender = UnityEngine.Object.FindObjectOfType(castType);
                        if (card != null && sender != null && (bool)castType.GetMethod("CanChangeSelection").Invoke(sender, new object[] { card, true })) {
                            card.GetType().GetMethod("OnCardClicked").Invoke(card, null);
                            float targetX = match.leftUser.id == userId ? 17 : 1;
                            castSent = (bool)castType.GetMethod("TrySendInput").Invoke(sender, new object[] { new Vector3(targetX, 0, 5) });
                            Assert.IsTrue(castSent, "The real card UI could not queue its selected hand card.");
                        }
                    }
                    yield return null;
                }
                Assert.AreEqual("ResultScene", SceneManager.GetActiveScene().name, "Lobby-created battle did not reach ResultScene.");
                yield return null;
                Assert.IsTrue(rendered, "No real battle presentation was rendered.");
                Assert.AreEqual(cast ? 1 : 0, casts.Count, "Confirmed card acknowledgement must occur once for the acting player.");
                if (cast) Assert.IsTrue(castSent);
                var resultType = GameType("ResultScene.ResultSceneUIController");
                var resultUi = UnityEngine.Object.FindObjectOfType(resultType);
                Assert.IsNotNull(resultUi);
                var result = resultType.GetField("resultInfo", Private).GetValue(resultUi);
                string ownField = match.leftUser.id == userId ? "leftPlayer" : "rightPlayer";
                string outcome = (string)result.GetType().GetField(ownField).GetValue(result);
                string expected = (string)config["expectedResult"] ?? (cast ? "Win" : "Lose");
                Assert.AreEqual(expected, outcome);
                string title = outcome == "Win" ? "winTitle" : outcome == "Lose" ? "loseTitle" : "drawTitle";
                Assert.IsTrue(((GameObject)resultType.GetField(title, Private).GetValue(resultUi)).activeInHierarchy);
                yield return ScreenShot((string)config["resultScreenshotPath"]);
                report["rendered"] = rendered; report["castSent"] = castSent; report["acceptedCasts"] = casts.Count;
                report["result"] = outcome; report["success"] = true; Progress("result-scene");
            } finally {
                acceptedEvent.RemoveEventHandler(null, accepted);
                Write((string)config["reportPath"], report);
            }
        }

        [UnityTearDown]
        public IEnumerator RestoreLocalFixtureRouting() {
            string path = Environment.GetEnvironmentVariable("AC_LOCKSTEP_UNITY_CONFIG");
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) yield break;
            var saved = JObject.Parse(File.ReadAllText(path));
            if ((bool?)saved["journey"] != true) yield break;
            if (!string.IsNullOrEmpty((string)saved["reportPath"]) && !File.Exists((string)saved["reportPath"]))
                Write((string)saved["reportPath"], report ?? new JObject { ["success"] = false, ["stage"] = "interrupted" });
            JwksService.Reset();
            // PlayerPrefs는 domain reload보다 오래 남으므로 fixture 객체가 살아 있을 때 먼저 복구한다.
            if (preferences != null) foreach (var preference in preferences) {
                if (preference.Value == null) PlayerPrefs.DeleteKey(preference.Key); else PlayerPrefs.SetString(preference.Key, preference.Value);
            }
            PlayerPrefs.Save();
            // 종료까지 로컬 주소를 유지한다. domain reload가 실행되면 Server 정적 인스턴스도 초기화된다.
            if (Application.isPlaying) yield return new ExitPlayMode();
            if (servers != null) foreach (var server in servers) { server.Server.host = server.Host; server.Server.port = server.Port; server.Server.isSecure = server.Secure; }
            servers = null; preferences = null;
        }
    }
}
