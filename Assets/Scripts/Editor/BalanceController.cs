using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Data;
using Data.Util;
using GameScene;
using GameScene.Dto;
using GameScene.Object;
using Global;
using Global.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace DevPlayground {
    public sealed class BalanceController {
        internal static BalanceController Active { get; private set; }
        private readonly PlaygroundHost host;
        private readonly List<UnityWebRequest> requests = new();
        private Catalog catalog;
        private State state;
        private string battleId;
        private string baseUrl;
        private string selectedMagic;
        private SkillIndicatorShapeRenderer aim;
        private FieldSelector field;
        private Camera camera;
        private PresentationWorld world;
        private int generation;
        private int cursor;
        private int patchIndex;
        private bool candidate;
        private bool restarting;
        private bool commandPending;
        private bool closing;
        private bool suppressDropdown;
        private Frame[] replay;
        private int replayIndex;
        private double replayTime;
        private bool replayPaused;
        private int replaySpeed = 1;
        private User previousUser;
        private MatchedInfoDto previousMatch;

        public BalanceController(PlaygroundHost host) { this.host = host; }
        public void Bind() {
            host.OnStart = Start;
            host.OnUpdate = Update;
            host.OnDestroyed = Destroy;
        }

        private IEnumerator Start() {
            Active = this;
            previousUser = SceneContext.User;
            previousMatch = SceneContext.MatchInfo;
            var left = new User(1, "Balance Left", "", -1);
            SceneContext.User = left;
            SceneContext.MatchInfo = new MatchedInfoDto { sessionId = "balance-local", leftUser = left,
                rightUser = new User(2, "Balance Right", "", -1) };
            baseUrl = "http://127.0.0.1:" + BalanceLabWindow.Port;
            field = UnityEngine.Object.FindObjectOfType<FieldSelector>();
            camera = Camera.main;
            BalanceLabWindow.SetLabel(host.clearAllButton, "재시작 / 다시 연결");
            BalanceLabWindow.SetLabel(host.closeButton, "테스트 종료");
            host.targetText.enableWordWrapping = false;
            host.targetText.fontSize = 14;
            host.targetText.margin = Vector4.zero;
            host.targetText.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            host.targetText.text = "마법 선택 후 전장을 클릭하세요.";
            world = new GameObject("Balance Presentation").AddComponent<PresentationWorld>();
            world.Camera = camera;
            host.sideDropdown.onValueChanged.AddListener(_ => { if (!suppressDropdown) Restart(); });
            host.clearAllyButton.onClick.AddListener(() => { candidate = !candidate; Restart(); });
            host.clearEnemyButton.onClick.AddListener(() => {
                if (catalog == null || catalog.suite.patches.Length == 0) return;
                patchIndex = (patchIndex + 1) % catalog.suite.patches.Length;
                candidate = true;
                Restart();
            });
            host.clearAllButton.onClick.AddListener(Restart);
            host.immuneAllyButton.onClick.AddListener(() => {
                if (replay != null) { replayPaused = !replayPaused; RefreshLabels(); }
                else if (state != null) Send(new { action = "pause", paused = !state.paused });
            });
            host.immuneEnemyButton.onClick.AddListener(() => {
                if (replay != null) { replaySpeed = replaySpeed == 4 ? 1 : replaySpeed * 2; RefreshLabels(); }
                else if (state != null) Send(new { action = "speed", speed = state.speed == 4 ? 1 : state.speed * 2 });
            });
            host.closeButton.onClick.AddListener(() => EditorApplication.isPlaying = false);
            yield return Begin();
            while (!closing) {
                if (!restarting && battleId != null) {
                    int current = generation;
                    string id = battleId;
                    yield return Request<State>("GET", "/battles/" + id + "?after=" + cursor, null, current, value => {
                        if (value.battleId == battleId) Apply(value);
                    });
                }
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }

        internal void Restart() {
            if (closing || restarting) return;
            host.StartCoroutine(Begin());
        }

        private IEnumerator Begin() {
            restarting = true;
            int current = ++generation;
            commandPending = false;
            replay = null;
            host.sideDropdown.interactable = true;
            state = null;
            selectedMagic = null;
            if (aim != null) aim.gameObject.SetActive(false);
            string old = battleId;
            battleId = null;
            if (old != null) yield return Request<JObject>("DELETE", "/battles/" + old, null, current, _ => {});
            world.Clear();
            foreach (Transform child in host.iconContent) UnityEngine.Object.Destroy(child.gameObject);
            if (catalog == null) {
                host.statusText.text = "로컬 실행기에 연결합니다. 연결 실패 시 재시작을 누르세요.";
                yield return Request<Catalog>("GET", "/catalog", null, current, value => {
                    if (value?.suite?.scenarios == null || value.suite.scenarios.Length == 0 ||
                        value.suite.patches == null || value.suite.patches.Length == 0 || value.values == null)
                        throw new InvalidOperationException("실험 목록이 비어 있거나 지원하지 않는 응답입니다.");
                    catalog = value;
                    suppressDropdown = true;
                    host.sideDropdown.ClearOptions();
                    host.sideDropdown.AddOptions(value.suite.scenarios.Select(s => s.label).ToList());
                    suppressDropdown = false;
                });
            }
            if (catalog == null || catalog.suite.scenarios.Length == 0) { restarting = false; yield break; }
            Scenario scenario = catalog.suite.scenarios[host.sideDropdown.value];
            Dictionary<string, double> overrides;
            try { overrides = candidate ? BalanceLabWindow.Overrides : new Dictionary<string, double>(); }
            catch (Exception e) { host.statusText.text = "수정값 JSON을 확인하세요: " + e.Message; restarting = false; yield break; }
            var body = new { scenarioId = scenario.id,
                patchId = candidate ? catalog.suite.patches[patchIndex].id : "baseline",
                seed = BalanceLabWindow.Seed, humanLeft = !BalanceLabWindow.Automatic, overrides };
            var parameters = new Dictionary<string, float>(catalog.values.ToDictionary(p => p.Key, p => (float)p.Value));
            if (candidate) {
                foreach (var value in catalog.suite.patches[patchIndex].overrides) parameters[value.Key] = (float)value.Value;
                foreach (var value in overrides) parameters[value.Key] = (float)value.Value;
            }
            world.SetParameters(parameters);
            yield return Request<State>("POST", "/battles", body, current, value => {
                battleId = value.battleId;
                cursor = 0;
                Apply(value);
                Populate(scenario.allowedMagics);
                host.statusText.text = (candidate ? catalog.suite.patches[patchIndex].label : "기존값") +
                    "\n시작을 누른 뒤 마법을 선택하고 전장을 클릭하세요.";
            });
            if (battleId == null) catalog = null;
            RefreshLabels();
            restarting = false;
        }

        private void Populate(string[] magics) {
            foreach (string magic in magics) {
                var button = UnityEngine.Object.Instantiate(host.iconTemplate, host.iconContent);
                button.gameObject.SetActive(true);
                var icon = button.transform.Find("Icon").GetComponent<Image>();
                icon.sprite = Resources.Load<Sprite>("Game/sprites/" + StringUtils.ToPascalCase(magic));
                icon.enabled = icon.sprite != null;
                button.GetComponentInChildren<TMPro.TMP_Text>(true).text = magic;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => { selectedMagic = magic; host.targetText.text = magic; });
            }
        }

        private void Apply(State value) {
            if (value?.metrics?.mana?.Length != 2 || value.metrics.playerHp?.Length != 2)
                throw new InvalidOperationException("전투 상태의 마나·HP 정보가 없습니다.");
            state = value;
            foreach (var frame in value.frames ?? Array.Empty<Frame>())
                ApplyFrame(frame);
            Sync(value.objects ?? Array.Empty<SnapshotObjectDto>());
            cursor = value.frameCursor;
            host.timerText.text = $"{value.tick / 20f:0.0}s\n마나 {value.metrics.mana[0]}";
            if (!string.IsNullOrEmpty(value.failure)) host.statusText.text = "전투 실행 실패: " + value.failure;
            else if (value.finished) host.statusText.text = $"전투 종료 · 마스터 HP {value.metrics.playerHp[0]} : {value.metrics.playerHp[1]}\n기록은 Balance Test 창에서 저장할 수 있습니다.";
            RefreshLabels();
        }

        private void Sync(SnapshotObjectDto[] objects) {
            var ids = new HashSet<int>();
            foreach (var item in objects) {
                if (item.prefab == "Wall") continue;
                ids.Add(item.id);
                if (world.FindById(item.id) == null) ObjectSpawner.SpawnShared(new CreatedObjectDto(item), world, false);
                world.FindById(item.id)?.UpdateObject(new UpdatedObjectDto(item));
            }
            foreach (int id in world.Objects.Keys.ToArray()) if (!ids.Contains(id)) world.Unregister(id);
        }

        private void ApplyFrame(Frame frame) {
            // Wall은 서버의 경기장 경계 collider이며 client prefab이 없다.
            if (frame.objects?.create != null)
                frame.objects.create = frame.objects.create.Where(item => item.type != "Wall").ToArray();
            PresentationFramePlayer.Apply(frame.objects, frame.events, world);
        }

        private void RefreshLabels() {
            BalanceLabWindow.SetLabel(host.clearAllyButton, candidate ? "수정안 적용 중" : "기존값 적용 중");
            BalanceLabWindow.SetLabel(host.clearEnemyButton, catalog == null ? "다음 수정안" : catalog.suite.patches[patchIndex].label);
            bool paused = replay != null ? replayPaused : state == null || state.paused;
            BalanceLabWindow.SetLabel(host.immuneAllyButton, paused ? "시작 / 계속" : "일시정지");
            BalanceLabWindow.SetLabel(host.immuneEnemyButton, "배속: " + (replay != null ? replaySpeed : state?.speed ?? 1) + "×");
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.Tab) && !host.sideDropdown.IsExpanded) {
                bool visible = !host.magicPanel.activeSelf;
                host.magicPanel.SetActive(visible);
                host.controlPanel.SetActive(visible);
            }
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) selectedMagic = null;
            if (replay != null) {
                if (!replayPaused) {
                    replayTime += Time.unscaledDeltaTime * replaySpeed;
                    while (replayIndex < replay.Length && replay[replayIndex].time <= replayTime) {
                        var frame = replay[replayIndex++];
                        ApplyFrame(frame);
                    }
                    host.timerText.text = $"재생\n{replayTime:0.0}s";
                    if (replayIndex >= replay.Length) { replayPaused = true; host.statusText.text = "재생이 끝났습니다. 재시작하면 직접 플레이로 돌아갑니다."; RefreshLabels(); }
                }
                return;
            }
            ProcessPointer(Input.mousePosition, Input.GetMouseButtonDown(0));
        }

        internal void ProcessPointer(Vector3 screen, bool pressed) {
            bool valid = !closing && !restarting && state != null && !state.paused && !state.finished &&
                state.failure == null && !BalanceLabWindow.Automatic && selectedMagic != null && field != null && camera != null &&
                camera.pixelRect.Contains(screen) && !PointerInputUtility.IsPointerCapturedByUi &&
                PointerInputUtility.FindUnderPointer<Graphic>(screen) == null;
            Vector3 point = default;
            valid = valid && field.TryGetGroundPosition(screen, out point) && point.x >= 0 && point.x <= 18 && point.z >= 0 && point.z <= 10;
            if (valid && aim == null) {
                var marker = new GameObject("Balance Aim");
                marker.transform.SetParent(host.transform, false);
                aim = marker.AddComponent<SkillIndicatorShapeRenderer>();
            }
            if (aim != null) aim.gameObject.SetActive(valid);
            if (!valid) return;
            aim.SetCircle(new Vector3(point.x, 0, point.z), 0.18f, true, 16, 0f);
            host.targetText.text = selectedMagic;
            if (pressed) Send(new { action = "cast", magic = selectedMagic, position = new { x = point.x, y = 0f, z = point.z } });
        }

        private void Send(object command) {
            if (closing || restarting || commandPending || battleId == null) return;
            commandPending = true;
            host.StartCoroutine(Command(command, generation, battleId));
        }
        private IEnumerator Command(object command, int current, string id) {
            yield return Request<JObject>("POST", "/battles/" + id + "/command", command, current, _ => {});
            if (current == generation) commandPending = false;
        }

        internal void Export() {
            if (battleId == null || closing) return;
            string path = EditorUtility.SaveFilePanel("전투 기록 저장", "", "balance-battle.json", "json");
            if (!string.IsNullOrEmpty(path)) ExportTo(path);
        }

        internal void ExportTo(string path) {
            if (battleId == null || closing) return;
            host.StartCoroutine(Request<JObject>("GET", "/battles/" + battleId + "/report", null, generation,
                value => { File.WriteAllText(path, value.ToString()); host.statusText.text = "전투 기록을 저장했습니다."; }));
        }

        internal void Replay(string json) {
            var envelope = JObject.Parse(json);
            var report = envelope["report"] ?? throw new InvalidOperationException("전투 기록의 report가 없습니다.");
            var frames = JsonCodec.Deserialize<Frame[]>(report["frames"].ToString());
            if (frames == null || frames.Length == 0 || frames.Length > 2401) throw new InvalidOperationException("재생할 프레임이 없습니다.");
            var parameters = JsonCodec.Deserialize<Dictionary<string, float>>(report["parameterReads"].ToString());
            string old = battleId;
            ++generation;
            battleId = null;
            state = null;
            selectedMagic = null;
            if (aim != null) aim.gameObject.SetActive(false);
            if (old != null) host.StartCoroutine(Request<JObject>("DELETE", "/battles/" + old, null, generation, _ => {}));
            world.Clear();
            world.SetParameters(parameters);
            replay = frames;
            replayIndex = 0;
            replayTime = 0;
            replayPaused = false;
            replaySpeed = 1;
            host.sideDropdown.interactable = false;
            host.statusText.text = "전투 기록 재생 · " + report["patchId"];
            RefreshLabels();
        }

        private IEnumerator Request<T>(string method, string path, object body, int current, Action<T> success) {
            using var request = new UnityWebRequest(baseUrl + path, method) { downloadHandler = new DownloadHandlerBuffer(), timeout = 5 };
            if (body != null) {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body)));
                request.SetRequestHeader("Content-Type", "application/json");
            }
            requests.Add(request);
            yield return request.SendWebRequest();
            requests.Remove(request);
            if (closing || current != generation) yield break;
            if (request.result != UnityWebRequest.Result.Success) {
                host.statusText.text = $"연결 또는 요청 실패 ({request.responseCode}): {request.downloadHandler.text}\n재시작으로 다시 연결할 수 있습니다.";
                yield break;
            }
            if (JsonCodec.TryDeserialize(request.downloadHandler.text, out T value, out string error)) {
                try { success(value); }
                catch (Exception e) { host.statusText.text = "응답 처리 실패: " + e.Message + "\n재시작으로 다시 시도할 수 있습니다."; Debug.LogException(e); }
            } else host.statusText.text = "응답을 읽지 못했습니다: " + error;
        }

        private void Destroy() {
            closing = true;
            generation++;
            foreach (var request in requests.ToArray()) request.Abort();
            requests.Clear();
            if (battleId != null) {
                var request = UnityWebRequest.Delete(baseUrl + "/battles/" + battleId);
                request.timeout = 2;
                request.SendWebRequest().completed += _ => request.Dispose();
            }
            SceneContext.User = previousUser;
            SceneContext.MatchInfo = previousMatch;
            if (world != null) UnityEngine.Object.Destroy(world.gameObject);
            if (Active == this) Active = null;
        }

        [Serializable] private sealed class Catalog { public Suite suite; public string source; public string snapshotHash; public Dictionary<string, double> values; }
        [Serializable] private sealed class Suite { public Scenario[] scenarios; public Patch[] patches; }
        [Serializable] private sealed class Scenario { public string id; public string label; public string[] allowedMagics; }
        [Serializable] private sealed class Patch { public string id; public string label; public Dictionary<string, double> overrides; }
        [Serializable] private sealed class Frame { public double time; public ObjectsInfo objects; public List<GameScene.Dto.Event.GameEvent> events; }
        [Serializable] private sealed class Metrics { public int[] mana; public int[] playerHp; }
        [Serializable] private sealed class State {
            public string battleId; public int tick; public bool paused; public int speed;
            public bool finished; public string failure; public int frameCursor;
            public Frame[] frames; public SnapshotObjectDto[] objects; public Metrics metrics;
        }
    }
}
