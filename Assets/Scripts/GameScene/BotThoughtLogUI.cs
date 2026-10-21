using System.Collections;
using Global;
using Global.Util;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace GameScene
{
    public class BotThoughtLogUI : MonoBehaviour
    {
        [SerializeField] private GameObject visibilityRoot;
        [SerializeField] private GameObject panel;
        [SerializeField] private Button toggleButton;
        [SerializeField] private TMP_Text toggleLabel;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private Button latestButton;
        [SerializeField] private TMP_Text latestLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text logText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private BotThoughtLogPointerCapture scrollCapture;

        private bool authorized;
        private bool followLatest = true;
        private bool rendering;
        private bool dirty = true;
        private string checkedToken;
        private bool checkedKeys;

        private bool Korean => LocalizationSettings.SelectedLocale != null &&
            LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("ko");

        private void Awake()
        {
            visibilityRoot.SetActive(false);
            panel.SetActive(false);
            logText.richText = false;
            toggleButton.onClick.AddListener(Toggle);
            latestButton.onClick.AddListener(ShowLatest);
            scrollRect.onValueChanged.AddListener(OnScroll);
            scrollCapture.PressChanged += OnPress;
        }

        private void OnEnable()
        {
            BotThoughtLog.Changed += OnChanged;
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            dirty = true;
        }

        private IEnumerator Start()
        {
            if (!string.IsNullOrEmpty(SceneContext.JwtToken) && !JwksService.IsFetched)
                yield return JwksService.FetchJwks();
            RefreshAuthorization();
        }

        private void OnDisable()
        {
            BotThoughtLog.Changed -= OnChanged;
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        private void Update()
        {
            if (checkedToken != SceneContext.JwtToken || checkedKeys != JwksService.IsFetched)
                RefreshAuthorization();
        }

        private void LateUpdate()
        {
            if (!dirty || !authorized) return;
            dirty = false;
            RefreshLabels();
            if (panel.activeSelf && followLatest && !scrollCapture.IsPressed)
                RenderLatest();
        }

        private void RefreshAuthorization()
        {
            checkedToken = SceneContext.JwtToken;
            checkedKeys = JwksService.IsFetched;
            BotThoughtLog.DiscardOtherOwner();
            authorized = checkedKeys && !string.IsNullOrEmpty(checkedToken) && JwtHelper.IsAdmin(checkedToken);
            visibilityRoot.SetActive(authorized);
            if (!authorized)
            {
                panel.SetActive(false);
                logText.text = "";
            }
            dirty = true;
        }

        public void Toggle()
        {
            RefreshAuthorization();
            if (!authorized) return;
            panel.SetActive(!panel.activeSelf);
            if (panel.activeSelf) ShowLatest();
            RefreshLabels();
        }

        public void ShowLatest()
        {
            RefreshAuthorization();
            if (!authorized) return;
            followLatest = true;
            scrollRect.StopMovement();
            RenderLatest();
            RefreshLabels();
        }

        private void OnChanged()
        {
            if (BotThoughtLog.TotalCount == 0)
            {
                followLatest = true;
                panel.SetActive(false);
                logText.text = "";
            }
            dirty = true;
        }
        private void OnLocaleChanged(Locale locale) => dirty = true;

        private void OnPress(bool pressed)
        {
            if (pressed) followLatest = false;
            else if (scrollRect.verticalNormalizedPosition <= 0.02f) ShowLatest();
            dirty = true;
        }

        private void OnScroll(Vector2 position)
        {
            if (rendering || !panel.activeSelf) return;
            followLatest = !scrollCapture.IsPressed && position.y <= 0.02f;
            dirty = true;
        }

        private void RenderLatest()
        {
            rendering = true;
            logText.text = BotThoughtLog.Entries.Count == 0
                ? (Korean ? "봇의 다음 판단을 기다리는 중…" : "Waiting for the bot's next decision…")
                : string.Join("\n\n", BotThoughtLog.Entries);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            scrollRect.verticalNormalizedPosition = 0f;
            rendering = false;
        }

        private void RefreshLabels()
        {
            string title = Korean ? "봇 생각" : "Bot thoughts";
            titleLabel.text = Korean ? "봇의 판단 기록 · L 왼쪽 / R 오른쪽" : "Decisions · L left / R right";
            toggleLabel.text = $"{title} ({BotThoughtLog.Entries.Count}) {(panel.activeSelf ? "−" : "+")}";
            latestLabel.text = Korean ? "최신으로" : "Latest";
            latestButton.interactable = !followLatest;
            string state = BotThoughtLog.HasEnded ? (Korean ? "경기 종료" : "Match ended") : (Korean ? "기록 중" : "Live");
            string mode = followLatest ? (Korean ? "자동 스크롤" : "Following") : (Korean ? "이전 기록 읽는 중" : "Reading history");
            statusLabel.text = $"{state} · {BotThoughtLog.Entries.Count}/{BotThoughtLog.TotalCount} · {mode}";
        }
    }
}
