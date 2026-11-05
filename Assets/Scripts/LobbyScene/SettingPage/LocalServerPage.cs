using System.Collections.Generic;
using Global.LocalHosting;
using LocalServer;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace LobbyScene.SettingPage
{
    /// <summary>
    /// 설정 화면의 "로컬 서버" 페이지. "내 PC 에서 경기 실행" 스위치, 설치·업데이트 버튼(받는 중에는 취소 버튼),
    /// 상태 문구, tick rate 선택(자동, 20, 30, 60, 120, 144)을 <see cref="LocalServerService"/> 에 연결한다.
    ///
    /// 화면 구성은 <c>Assets/Prefabs/UI/Lobby/LocalServerPanel.prefab</c> 에 있고, 이 스크립트는 값과 문구만 바꾼다.
    /// desktop 이 아니면 설정 화면의 진입 버튼이 <see cref="DesktopOnlyUI"/> 로 숨어서 이 페이지가 열리지 않는다.
    /// </summary>
    public class LocalServerPage : MonoBehaviour
    {
        private const string LobbyUiTable = "LobbyUI";

        [SerializeField] private Toggle enabledToggle;
        [SerializeField] private TMP_Text enabledLabel;

        /// <summary>스위치 트랙과 손잡이. <see cref="CoachDataSetter"/> 와 같은 방식으로 칠하고 옮긴다.</summary>
        [SerializeField] private Image switchTrack;

        [SerializeField] private RectTransform switchKnob;
        [SerializeField] private float knobTravel = 8.75f;
        [SerializeField] private Color onColor = new Color(0.35686275f, 0.8156863f, 0.29803923f, 1f);
        [SerializeField] private Color offColor = new Color(0.93333334f, 0.9529412f, 0.972549f, 1f);

        [SerializeField] private TMP_Text statusText;
        [SerializeField] private UnityEngine.UI.Button actionButton;
        [SerializeField] private TMP_Text actionLabel;
        [SerializeField] private TMP_Text tickRateLabel;

        /// <summary><see cref="LocalTickRatePolicy.ManualChoices"/> 와 같은 순서(자동, 20, 30, 60, 120, 144)다.</summary>
        [SerializeField] private UnityEngine.UI.Button[] tickRateButtons;

        [SerializeField] private Color selectedColor = new Color(1f, 0.6039216f, 0.12156863f, 1f);
        [SerializeField] private Color selectedTextColor = Color.white;
        [SerializeField] private Color idleTextColor = new Color(0.10980392f, 0.101960786f, 0.16862746f, 1f);

        private readonly List<int> buttonTickRates = new List<int>();
        private bool listenersWired;

        /// <summary>마지막으로 그릴 때 찾은 설치 묶음. 받는 중에 매 frame 폴더를 훑지 않으려고 둔다.</summary>
        private InstalledBundle cachedInstalled;

        private LocalServerService Service => LocalServerService.Instance;

        private void OnEnable()
        {
            WireListeners();

            if (Service != null) Service.Changed += Render;
            LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
            Render();
        }

        private void OnDisable()
        {
            if (Service != null) Service.Changed -= Render;
            LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        }

        private void Update()
        {
            // 진행률은 이벤트 없이 매 frame 바뀐다. 받는 중일 때만 상태 문구를 다시 쓴다.
            LocalServerService service = Service;
            if (service == null) return;

            bool installing = service.PackStore.Installing;
            if (installing) RenderStatus(service, cachedInstalled);
        }

        private void WireListeners()
        {
            if (listenersWired) return;
            listenersWired = true;

            if (enabledToggle != null) enabledToggle.onValueChanged.AddListener(HandleToggle);
            if (actionButton != null) actionButton.onClick.AddListener(HandleAction);

            buttonTickRates.Clear();
            if (tickRateButtons == null) return;

            for (int i = 0; i < tickRateButtons.Length; i++)
            {
                int tickRate = i < LocalTickRatePolicy.ManualChoices.Count ? LocalTickRatePolicy.ManualChoices[i] : 0;
                buttonTickRates.Add(tickRate);
                if (tickRateButtons[i] == null) continue;
                tickRateButtons[i].onClick.AddListener(() => LocalServerSettings.SetManualTickRate(tickRate));
            }
        }

        private void HandleToggle(bool value)
        {
            LocalServerSettings.Enabled = value;
            RenderSwitch(value);
        }

        private void HandleAction()
        {
            LocalServerService service = Service;
            if (service == null) return;

            if (service.PackStore.Installing) service.PackStore.CancelInstall();
            else service.PackStore.BeginInstall();
        }

        private void HandleLocaleChanged(UnityEngine.Localization.Locale locale)
        {
            Render();
        }

        private void Render()
        {
            if (this == null) return;

            bool enabled = LocalServerSettings.Enabled;
            if (enabledToggle != null) enabledToggle.SetIsOnWithoutNotify(enabled);
            RenderSwitch(enabled);
            if (enabledLabel != null) enabledLabel.text = Text("LocalServerRunOnPc");

            LocalServerService service = Service;
            cachedInstalled = service != null ? service.Installed : null;
            RenderStatus(service, cachedInstalled);
            RenderTickRate();
        }

        private void RenderStatus(LocalServerService service, InstalledBundle installed)
        {
            if (service == null)
            {
                if (statusText != null) statusText.text = string.Empty;
                if (actionButton != null) actionButton.gameObject.SetActive(false);
                return;
            }

            LocalServerPackStore store = service.PackStore;
            LocalServerHost host = service.Host;
            bool updateAvailable = store.IsUpdateAvailable(installed);
            string latest = store.Latest?.localServer?.version;

            string status;
            if (store.Installing)
            {
                status = Format("LocalServerStatusDownloading", Mathf.FloorToInt(store.InstallProgress * 100f));
            }
            else if (!string.IsNullOrEmpty(store.InstallError))
            {
                status = Format("LocalServerStatusInstallFailed", store.InstallError);
            }
            else if (installed == null)
            {
                status = updateAvailable ? Text("LocalServerStatusNotInstalled") : Text("LocalServerStatusNoRelease");
            }
            else if (host.State == LocalServerState.Failed)
            {
                status = Format("LocalServerStatusFailed", host.FailureReason);
            }
            else if (host.State == LocalServerState.Ready)
            {
                status = Format("LocalServerStatusRunning", installed.Version);
            }
            else if (host.State == LocalServerState.Starting)
            {
                status = Format("LocalServerStatusStarting", installed.Version);
            }
            else if (updateAvailable)
            {
                status = Format("LocalServerStatusUpdateAvailable", installed.Version, latest);
            }
            else
            {
                status = Format("LocalServerStatusInstalled", installed.Version);
            }

            if (installed != null && updateAvailable && !store.Installing && host.State != LocalServerState.Failed &&
                string.IsNullOrEmpty(store.InstallError) && (host.State == LocalServerState.Ready || host.State == LocalServerState.Starting))
            {
                // 실행 중이어도 새 version 이 있다는 것은 알린다.
                status += "\n" + Format("LocalServerStatusUpdateAvailable", installed.Version, latest);
            }

            if (statusText != null) statusText.text = status;

            if (actionButton == null) return;

            bool showButton = store.Installing || updateAvailable;
            actionButton.gameObject.SetActive(showButton);
            if (actionLabel != null)
            {
                actionLabel.text = store.Installing
                    ? Text("LocalServerCancel")
                    : Text(installed == null ? "LocalServerInstall" : "LocalServerUpdate");
            }
        }

        private void RenderTickRate()
        {
            LocalTickRatePolicy policy = LocalServerSettings.TickRatePolicy;

            if (tickRateLabel != null) tickRateLabel.text = Format("LocalServerTickRateNext", policy.TickRate);
            if (tickRateButtons == null) return;

            int selected = policy.IsManual ? policy.State.Manual : 0;
            for (int i = 0; i < tickRateButtons.Length; i++)
            {
                UnityEngine.UI.Button button = tickRateButtons[i];
                if (button == null) continue;

                int tickRate = i < buttonTickRates.Count ? buttonTickRates[i] : 0;
                bool isSelected = tickRate == selected;

                var background = button.GetComponent<Image>();
                if (background != null) background.color = isSelected ? selectedColor : Color.clear;

                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (label == null) continue;

                label.text = tickRate == 0 ? Text("LocalServerTickRateAuto") : tickRate.ToString();
                label.color = isSelected ? selectedTextColor : idleTextColor;
            }
        }

        private void RenderSwitch(bool enabled)
        {
            if (switchTrack != null) switchTrack.color = enabled ? onColor : offColor;

            if (switchKnob != null)
            {
                Vector2 position = switchKnob.anchoredPosition;
                position.x = enabled ? knobTravel : -knobTravel;
                switchKnob.anchoredPosition = position;
            }
        }

        private static string Text(string key)
        {
            string localized = LocalizationSettings.StringDatabase.GetLocalizedString(LobbyUiTable, key);
            return string.IsNullOrWhiteSpace(localized) ? key : localized;
        }

        private static string Format(string key, params object[] arguments)
        {
            // Localization formats placeholders during lookup, so pass arguments in that call.
            string localized = LocalizationSettings.StringDatabase.GetLocalizedString(
                LobbyUiTable, key, arguments: arguments);
            return string.IsNullOrWhiteSpace(localized) ? key : localized;
        }
    }
}
