using System;
using System.Collections;
using Data;
using Global;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace GameScene
{
    public sealed class GamePreparationOverlay : MonoBehaviour
    {
        private const string PrefabPath = "UI/GamePreparationOverlay";
        private const string Table = "GameUI";
        private const string KeyPrefix = "Preparation.";
        private const float StartHoldSeconds = 0.32f;
        private const float FadeSeconds = 0.12f;

        [SerializeField] private Image leftPortrait;
        [SerializeField] private Image rightPortrait;
        [SerializeField] private TMP_Text leftName;
        [SerializeField] private TMP_Text rightName;
        [SerializeField] private TMP_Text leftRating;
        [SerializeField] private TMP_Text rightRating;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text status;
        [SerializeField] private TMP_Text countdownLabel;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private TMP_Text cancelLabel;
        [SerializeField] private TMP_Text retryLabel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject versusContent;
        [SerializeField] private GameObject adventureContent;
        [SerializeField] private TMP_Text adventureName;
        [SerializeField] private TMP_Text adventureStage;
        [SerializeField] private TMP_Text adventureCountdown;
        [SerializeField] private Image adventureArt;
        [SerializeField] private Sprite fortressArt;
        [SerializeField] private Sprite gateArt;

        private MapKind map;
        private string displayedPhase;
        private int displayedCount;
        private int renderVersion;
        private bool focusPending;
        private Coroutine hiding;
        private bool isAdventure;

        public event Action OnCancel;
        public event Action OnRetry;
        public bool IsVisible => isActiveAndEnabled;

        public static GamePreparationOverlay Show(MatchedInfoDto match)
        {
            var prefab = Resources.Load<GamePreparationOverlay>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Missing preparation overlay prefab: {PrefabPath}");
                return null;
            }

            var view = Instantiate(prefab);
            view.map = match?.MapKind ?? MapKind.Unspecified;
            view.isAdventure = match?.rightUser?.id == 0 || (match?.sessionId?.StartsWith("pve-", StringComparison.Ordinal) ?? false);
            view.versusContent.SetActive(!view.isAdventure);
            view.adventureContent.SetActive(view.isAdventure);
            if (view.isAdventure)
            {
                if (view.map == MapKind.Fortress)
                    view.adventureArt.sprite = view.fortressArt;
                else if (view.map == MapKind.Gate)
                    view.adventureArt.sprite = view.gateArt;
            }
            else
            {
                BindPlayer(match?.leftUser, view.leftName, view.leftRating, view.leftPortrait);
                BindPlayer(match?.rightUser, view.rightName, view.rightRating, view.rightPortrait);
            }
            view.Render("connecting");
            return view;
        }

        private void Awake()
        {
            cancelButton.onClick.AddListener(() => OnCancel?.Invoke());
            retryButton.onClick.AddListener(() => OnRetry?.Invoke());
        }

        public void Render(string phase, int countdown = 0)
        {
            if (gameObject.activeSelf && displayedPhase == phase && displayedCount == countdown) return;

            if (hiding != null)
            {
                StopCoroutine(hiding);
                hiding = null;
            }

            canvasGroup.alpha = 1;
            bool wasVisible = gameObject.activeSelf && displayedPhase != null;
            bool hadActions = cancelButton.gameObject.activeSelf || retryButton.gameObject.activeSelf;
            bool failed = phase == "failed" || phase == "timeout" || phase == "handshake_failed"
                || phase == "recovery_failed";
            displayedPhase = phase;
            displayedCount = countdown;
            int version = ++renderVersion;

            if (!wasVisible || hadActions) ClearSelection();
            gameObject.SetActive(true);
            cancelButton.gameObject.SetActive(failed && OnCancel != null);
            retryButton.gameObject.SetActive(failed && OnRetry != null);
            cancelButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnRight = retryButton.gameObject.activeSelf ? retryButton : null
            };
            retryButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = cancelButton.gameObject.activeSelf ? cancelButton : null
            };
            focusPending = cancelButton.gameObject.activeSelf || retryButton.gameObject.activeSelf;

            string titleKey = failed ? "FailedTitle" : phase == "reconnecting" ? "ReconnectTitle"
                : isAdventure ? "AdventureTitle" : "Title";
            SetLocalized(title, titleKey, version);
            SetLocalized(status, StatusKey(phase), version);
            if (isAdventure)
            {
                RenderAdventure(version);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(leftName.text)) SetLocalized(leftName, "Player", version);
                if (string.IsNullOrWhiteSpace(rightName.text)) SetLocalized(rightName, "Player", version);
            }
            if (cancelButton.gameObject.activeSelf) SetLocalized(cancelLabel, "Leave", version);
            if (retryButton.gameObject.activeSelf) SetLocalized(retryLabel, "Retry", version);

            countdownLabel.text = "VS";
            bool counting = phase == "countdown" || phase == "running";
            adventureCountdown.transform.parent.gameObject.SetActive(isAdventure && counting);
            if (counting)
            {
                TMP_Text countLabel = isAdventure ? adventureCountdown : countdownLabel;
                if (countdown >= 1 && countdown <= 3)
                    countLabel.text = countdown.ToString();
                else
                    SetLocalized(countLabel, "Start", version);
            }
        }

        private void RenderAdventure(int version)
        {
            // AdventureMapController는 선택한 화면의 현지화된 이름을 SceneContext에 보관한다.
            string name = SceneContext.AdventureName;
            switch (name)
            {
                case "Forest":
                case "Fortress":
                case "Gate":
                case "Adventure":
                    SetLocalized(adventureName, new LocalizedString("Adventure", name), version);
                    break;
                default:
                    if (string.IsNullOrWhiteSpace(name))
                        SetLocalized(adventureName, new LocalizedString("Adventure", "Adventure"), version);
                    else
                        adventureName.text = name;
                    break;
            }

            int stage = SceneContext.AdventureStageNumber;
            int scenario = SceneContext.AdventureScenarioNumber;
            bool hasStageAndScenario = stage > 0 && scenario > 0;
            adventureStage.gameObject.SetActive(hasStageAndScenario);
            if (hasStageAndScenario)
                SetLocalized(adventureStage, new LocalizedString(Table, KeyPrefix + "AdventureStage"), version, stage, scenario);
        }

        public void HideAfterStarted()
        {
            if (IsVisible && hiding == null) hiding = StartCoroutine(Hide());
        }

        private IEnumerator Hide()
        {
            if (displayedPhase == "running" || (displayedPhase == "countdown" && displayedCount == 0))
                yield return new WaitForSecondsRealtime(StartHoldSeconds);

            float fadeStart = Time.unscaledTime;
            while (Time.unscaledTime - fadeStart < FadeSeconds)
            {
                canvasGroup.alpha = 1 - (Time.unscaledTime - fadeStart) / FadeSeconds;
                yield return null;
            }

            ++renderVersion;
            if (cancelButton.gameObject.activeSelf || retryButton.gameObject.activeSelf) ClearSelection();
            hiding = null;
            focusPending = false;
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!focusPending || EventSystem.current == null) return;

            var initialButton = retryButton.gameObject.activeSelf ? retryButton : cancelButton;
            EventSystem.current.SetSelectedGameObject(initialButton.gameObject);
            focusPending = false;
        }

        private string StatusKey(string phase)
        {
            switch (phase)
            {
                case "waiting": return isAdventure ? "AdventureWaiting" : "Waiting";
                case "loading": return map == MapKind.River ? "River" : "Loading";
                case "applying": return "Applying";
                case "countdown": return isAdventure ? "AdventureReady" : "Ready";
                case "running": return isAdventure ? "AdventureReady" : "Ready";
                case "reconnecting": return "Reconnecting";
                case "failed": return "ConnectionFailed";
                case "timeout": return "TimedOut";
                case "handshake_failed": return "HandshakeFailed";
                case "recovery_failed": return "RecoveryFailed";
                default: return "Connecting";
            }
        }

        private static void BindPlayer(User user, TMP_Text nameLabel, TMP_Text ratingLabel, Image portrait)
        {
            string name = user?.name?.Trim();
            nameLabel.text = string.IsNullOrEmpty(name) || name.IndexOf('@') >= 0 ? string.Empty : name;
            long? rating = user?.matchMmr;
            ratingLabel.gameObject.SetActive(rating.HasValue);
            if (rating.HasValue) ratingLabel.text = $"MMR {rating.Value:N0}";
            if (PlayerAppearanceResolver.Resolve(
                    user?.appearance, Resources.Load<Sprite>, out Sprite idle, out _, out _) != null)
            {
                portrait.sprite = idle;
            }
        }

        private void SetLocalized(TMP_Text label, string key, int version)
        {
            SetLocalized(label, new LocalizedString(Table, KeyPrefix + key), version);
        }

        private void SetLocalized(TMP_Text label, LocalizedString localized, int version, params object[] arguments)
        {
            var handle = localized.GetLocalizedStringAsync(arguments);
            if (handle.IsDone)
            {
                if (handle.Status == AsyncOperationStatus.Succeeded) label.text = handle.Result;
                return;
            }

            handle.Completed += completed =>
            {
                if (this != null && renderVersion == version && completed.Status == AsyncOperationStatus.Succeeded)
                    label.text = completed.Result;
            };
        }

        private static void ClearSelection()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
