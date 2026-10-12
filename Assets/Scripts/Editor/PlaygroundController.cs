#if UNITY_EDITOR
using System;
using System.Collections;
using System.Text;
using Data;
using Data.Net;
using Data.Util;
using GameScene;
using GameScene.Dto;
using GameScene.Handler;
using GameScene.Object;
using Global;
using Global.Serialization;
using Global.Stomp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace DevPlayground {
    [UnityEditor.InitializeOnLoad]
    public static class PlaygroundEditorBridge {
        static PlaygroundEditorBridge() {
            PlaygroundHost.Configure = host => new PlaygroundController(host).Bind();
        }
    }

    public sealed class PlaygroundController {
        private readonly PlaygroundHost host;
        private GameObject gameObject => host.gameObject;
        private Coroutine StartCoroutine(IEnumerator routine) => host.StartCoroutine(routine);
        public PlaygroundController(PlaygroundHost host) {
            this.host = host;
            sideDropdown = host.sideDropdown;
            statusText = host.statusText;
            timerText = host.timerText;
            targetText = host.targetText;
            iconContent = host.iconContent;
            iconTemplate = host.iconTemplate;
            clearAllyButton = host.clearAllyButton;
            clearEnemyButton = host.clearEnemyButton;
            clearAllButton = host.clearAllButton;
            immuneAllyButton = host.immuneAllyButton;
            immuneEnemyButton = host.immuneEnemyButton;
            closeButton = host.closeButton;
        }
        public void Bind() {
            host.OnStart = Start;
            host.OnUpdate = Update;
            host.OnDestroyed = OnDestroy;
        }
        public TMP_Dropdown sideDropdown;
        public TMP_Text statusText;
        public TMP_Text timerText;
        public TMP_Text targetText;
        public RectTransform iconContent;
        public Button iconTemplate;
        public Button clearAllyButton;
        public Button clearEnemyButton;
        public Button clearAllButton;
        public Button immuneAllyButton;
        public Button immuneEnemyButton;
        public Button closeButton;

        private PlaygroundInfo session;
        private NativeStompTransport transport;
        private readonly GameEventHandler events = new();
        private Vector3 target = new(9f, 0f, 5f);
        private bool leftImmune;
        private bool rightImmune;
        private bool closing;
        private bool connected;
        private int frame = -1;
        private bool immunityPending;
        private bool castPending;
        private MagicEntry selectedMagic;
        private Button selectedButton;
        private Color unselectedColor;
        private FieldSelector field;
        private Camera worldCamera;
        private SkillIndicatorShapeRenderer aim;
        // One Editor-only command seam lets Play Mode verify inputs without a live session.
        internal Func<object, IEnumerator> CastCommandOverride;

        private string Side => sideDropdown.value == 0 ? "LeftPlayer" : "RightPlayer";
        private string OtherSide => sideDropdown.value == 0 ? "RightPlayer" : "LeftPlayer";

        private IEnumerator Start() {
            field = UnityEngine.Object.FindObjectOfType<FieldSelector>();
            worldCamera = Camera.main;
            SetControls(false);
            sideDropdown.onValueChanged.AddListener(_ => RefreshImmunityLabels());
            clearAllyButton.onClick.AddListener(() => Clear(Side));
            clearEnemyButton.onClick.AddListener(() => Clear(OtherSide));
            clearAllButton.onClick.AddListener(() => Clear("None"));
            immuneAllyButton.onClick.AddListener(() => ToggleImmunity(Side));
            immuneEnemyButton.onClick.AddListener(() => ToggleImmunity(OtherSide));
            closeButton.onClick.AddListener(Close);
            RefreshImmunityLabels();
            if (SceneContext.User == null || string.IsNullOrEmpty(SceneContext.JwtToken)) {
                statusText.text = "Log in as a developer administrator, then enter from AdminScene.";
                yield break;
            }

            statusText.text = "Creating playground...";
            yield return Request<PlaygroundInfo>("POST",
                ServerList.MatchingServer.url.TrimEnd('/') + "/api/dev/playgrounds", null, info => session = info);
            if (session == null || closing) yield break;
            if (session.ownerId != SceneContext.UserID || !session.sessionId.StartsWith("playground-") ||
                !DateTimeOffset.TryParse(session.expiresAt, out var expires) ||
                expires <= DateTimeOffset.UtcNow) {
                statusText.text = "Invalid playground ready response.";
                session = null;
                yield break;
            }

            SceneContext.ClearAdventureMatch();
            SceneContext.MatchResult = null;
            SceneContext.MatchInfo = new MatchedInfoDto {
                sessionId = session.sessionId, server = session.server, webSocketUrl = session.webSocketUrl,
                leftUser = SceneContext.User,
                rightUser = new User(-1, "Right player", "", -1)
            };
            yield return Request<MagicEntry[]>("GET", Api("magics"), null, PopulateIcons);
            if (closing) yield break;
            if (!ServerEndpoint.TryOf(session.webSocketUrl, out var endpoint)) {
                statusText.text = "Invalid playground WebSocket address.";
                yield break;
            }
            transport = gameObject.AddComponent<NativeStompTransport>();
            transport.Connected += OnConnected;
            transport.MessageReceived += OnMessage;
            transport.Errored += message => statusText.text = "Connection error: " + message;
            transport.Disconnected += message => {
                connected = false;
                SetControls(false);
                if (!closing) statusText.text = "Disconnected. Close and enter a new playground. " + message;
            };
            transport.Connect(endpoint.AsWebSocket().Query("token", SceneContext.JwtToken), SceneContext.JwtToken);
        }

        private void OnConnected() {
            if (closing) return;
            connected = true;
            transport.Subscribe($"/game/{session.sessionId}/frameInfos/{session.ownerId}", "playground");
            StartCoroutine(Request<SnapshotDto>("GET", Api("snapshot"), null, snapshot => {
                if (snapshot.frame <= frame) return;
                frame = snapshot.frame;
                ObjectSyncer.Instance.Sync(snapshot.objects ?? Array.Empty<SnapshotObjectDto>());
            }));
            statusText.text = "Select magic, then click field · Esc/right-click: cancel · Tab: panels";
            SetControls(true);
        }

        private void OnMessage(string subscriptionId, string json) {
            if (closing || subscriptionId != "playground") return;
            try {
                string type = JObject.Parse(json).Value<string>("type");
                if (type == "playgroundEnded") {
                    statusText.text = "Playground ended: " + JObject.Parse(json).Value<string>("reason");
                    StartCoroutine(Leave());
                    return;
                }
                if (!JsonCodec.TryDeserialize(json, out ServerMessage message)) return;
                if (message is FrameInfoDto delta) {
                    PresentationFramePlayer.Apply(delta.objects, delta.events);
                } else if (message is SyncFrameInfo sync && sync.snapshotResponseDto != null) {
                    if (sync.snapshotResponseDto.frame <= frame) return;
                    frame = sync.snapshotResponseDto.frame;
                    events.Handler(sync.events);
                    if (sync.projectileDtos != null)
                        foreach (var projectile in sync.projectileDtos) ProjectileSpawner.Instance.Spawn(projectile);
                    ObjectSyncer.Instance.Sync(sync.snapshotResponseDto.objects ?? Array.Empty<SnapshotObjectDto>());
                }
            } catch (Exception e) {
                statusText.text = "Frame error: " + e.Message;
                Debug.LogException(e);
            }
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.Tab) && !sideDropdown.IsExpanded) TogglePanels();
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) CancelSelection();
            if (session != null && DateTimeOffset.TryParse(session.expiresAt, out var expiry)) {
                int seconds = Mathf.Max(0, (int)Math.Ceiling((expiry - DateTimeOffset.UtcNow).TotalSeconds));
                timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
                if (seconds == 0 && !closing) StartCoroutine(Leave());
            }
            ProcessPointer(Input.mousePosition, Input.GetMouseButtonDown(0));
        }

        internal void ProcessPointer(Vector3 screenPosition, bool pressed) {
            // FieldSelector falls back to an infinite ground plane: explicitly reject missed field hits.
            bool valid = connected && !closing && selectedMagic != null && field != null && worldCamera != null &&
                worldCamera.pixelRect.Contains(screenPosition) && !PointerInputUtility.IsPointerCapturedByUi &&
                PointerInputUtility.FindUnderPointer<Graphic>(screenPosition) == null;
            Vector3 point = default;
            valid = valid && field.TryGetGroundPosition(screenPosition, out point) &&
                point.x >= 0 && point.x <= 18 && point.z >= 0 && point.z <= 10;
            if (aim != null) aim.gameObject.SetActive(valid);
            if (!valid) return;
            target = new Vector3(point.x, 0, point.z);
            aim.SetCircle(target, 0.18f, true, 16, 0f);
            targetText.text = $"Target: X {target.x:0.0}, Z {target.z:0.0}";
            if (pressed && !castPending) {
                // Capture the spell, faction and location before starting the asynchronous request.
                var payload = new { magicId = selectedMagic.id, master = Side,
                    position = new { x = target.x, y = target.y, z = target.z } };
                castPending = true;
                StartCoroutine(Cast(payload));
            }
        }

        internal void TogglePanels() {
            bool visible = !host.magicPanel.activeSelf;
            host.magicPanel.SetActive(visible);
            host.controlPanel.SetActive(visible);
        }

        internal void CancelSelection() {
            if (selectedMagic != null && connected && !closing)
                statusText.text = "Select magic, then click field · Esc/right-click: cancel · Tab: panels";
            if (selectedButton != null) selectedButton.image.color = unselectedColor;
            selectedButton = null;
            selectedMagic = null;
            if (aim != null) aim.gameObject.SetActive(false);
            targetText.text = "Select a magic to aim";
        }

        internal void PopulateIcons(MagicEntry[] magics) {
            foreach (var magic in magics ?? Array.Empty<MagicEntry>()) {
                Button button = UnityEngine.Object.Instantiate(iconTemplate, iconContent);
                button.gameObject.SetActive(true);
                button.name = "Magic " + magic.id + " " + magic.name;
                var icon = button.transform.Find("Icon").GetComponent<Image>();
                icon.sprite = Resources.Load<Sprite>("Game/sprites/" + StringUtils.ToPascalCase(magic.name));
                icon.enabled = icon.sprite != null;
                button.GetComponentInChildren<TMP_Text>(true).text = magic.name;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectMagic(magic, button));
                button.interactable = connected;
            }
        }

        private void SelectMagic(MagicEntry magic, Button button) {
            if (!connected || closing) return;
            CancelSelection();
            selectedMagic = magic;
            selectedButton = button;
            unselectedColor = button.image.color;
            button.image.color = new Color(1f, 0.82f, 0.25f);
            if (aim == null) {
                var marker = new GameObject("Playground Aim");
                marker.transform.SetParent(host.transform, false);
                aim = marker.AddComponent<SkillIndicatorShapeRenderer>();
                marker.SetActive(false);
            }
            statusText.text = $"{magic.name}: click field to cast · Esc/right-click: cancel · Tab: panels";
        }

        private IEnumerator Cast(object payload) {
            try {
                yield return CastCommandOverride != null ? CastCommandOverride(payload) : Command("cast", payload);
            } finally { castPending = false; }
        }
        private void Clear(string master) {
            if (!connected || closing) return;
            StartCoroutine(Command("clear", new { master }));
        }
        private void ToggleImmunity(string master) {
            if (!connected || closing || immunityPending) return;
            immunityPending = true;
            RefreshImmunityLabels();
            bool enabled = !(master == "LeftPlayer" ? leftImmune : rightImmune);
            StartCoroutine(Command("immunity", new { master, enabled }));
        }

        private IEnumerator Command(string action, object payload) {
            yield return Request<CommandReply>("POST", Api(action), payload, reply => {
                if (action == "immunity") {
                    leftImmune = reply.leftImmune;
                    rightImmune = reply.rightImmune;
                }
                statusText.text = reply.message;
            });
            if (action == "immunity") immunityPending = false;
            RefreshImmunityLabels();
        }

        private void RefreshImmunityLabels() {
            bool ally = Side == "LeftPlayer" ? leftImmune : rightImmune;
            bool enemy = Side == "LeftPlayer" ? rightImmune : leftImmune;
            immuneAllyButton.GetComponentInChildren<TMP_Text>(true).text = "Ally invincible: " + (ally ? "ON" : "OFF");
            immuneEnemyButton.GetComponentInChildren<TMP_Text>(true).text = "Enemy invincible: " + (enemy ? "ON" : "OFF");
            immuneAllyButton.interactable = connected && !immunityPending && !closing;
            immuneEnemyButton.interactable = connected && !immunityPending && !closing;
        }

        private string Api(string action = "") =>
            session.server.TrimEnd('/') + "/api/dev/playgrounds/" + Uri.EscapeDataString(session.sessionId) +
            (string.IsNullOrEmpty(action) ? "" : "/" + action);

        private IEnumerator Request<T>(string method, string url, object body, Action<T> onSuccess) {
            using var request = new UnityWebRequest(url, method) {
                downloadHandler = new DownloadHandlerBuffer(), timeout = 8
            };
            if (body != null) {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body)));
                request.SetRequestHeader("Content-Type", "application/json");
            }
            request.SetRequestHeader("Authorization", "Bearer " + SceneContext.JwtToken);
            yield return request.SendWebRequest();
            if (closing && method != "DELETE") yield break;
            if (request.result != UnityWebRequest.Result.Success) {
                statusText.text = $"Request failed ({request.responseCode}): {request.error}";
                yield break;
            }
            if (JsonCodec.TryDeserialize(request.downloadHandler.text, out T value, out string error)) onSuccess?.Invoke(value);
            else statusText.text = "Response error: " + error;
        }

        private void SetControls(bool enabled) {
            if (!enabled) CancelSelection();
            foreach (var button in iconContent.GetComponentsInChildren<Button>(true)) button.interactable = enabled;
            clearAllyButton.interactable = enabled;
            clearEnemyButton.interactable = enabled;
            clearAllButton.interactable = enabled;
            RefreshImmunityLabels();
        }

        private void Close() {
            if (!closing) StartCoroutine(CloseAndLeave());
        }
        private IEnumerator CloseAndLeave() {
            if (session != null) yield return Request<CommandReply>("DELETE", Api(), null, _ => {});
            yield return Leave();
        }
        private IEnumerator Leave() {
            if (closing) yield break;
            closing = true;
            connected = false;
            SetControls(false);
            transport?.Unsubscribe("playground");
            transport?.Disconnect();
            SceneContext.MatchInfo = null;
            SceneContext.MatchResult = null;
            yield return null;
            SceneManager.LoadScene("AdminScene");
        }
        private void OnDestroy() {
            closing = true;
            transport?.Unsubscribe("playground");
            transport?.Disconnect();
            SceneContext.MatchInfo = null;
        }

        [Serializable] private sealed class PlaygroundInfo {
            public string sessionId;
            public string server;
            public string webSocketUrl;
            public long ownerId;
            public string expiresAt;
        }
        [Serializable] internal sealed class MagicEntry { public long id; public string name; }
        [Serializable] private sealed class CommandReply {
            public bool success; public string message; public bool leftImmune; public bool rightImmune;
        }
    }
}
#endif
