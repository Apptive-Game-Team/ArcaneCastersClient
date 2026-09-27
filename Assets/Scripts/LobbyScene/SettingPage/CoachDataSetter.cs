using System;
using Data.Coach;
using Data.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LobbyScene.SettingPage
{
    /// <summary>
    /// 로비 설정 페이지의 훈수 on/off 스위치를 연결한다. 볼륨 슬라이더와 달리 드래그 중
    /// 연달아 발생할 일이 없어서, 저장을 모으지 않고 바로 쓴다.
    /// </summary>
    public class CoachDataSetter : MonoBehaviour
    {
        private const string LobbyUiTable = "LobbyUI";
        private const string TitleKey = "CoachHint";
        private const string OnKey = "CoachHintOn";
        private const string OffKey = "CoachHintOff";

        [SerializeField] private Toggle coachToggle;

        [SerializeField] private TMP_Text titleLabel;

        /// <summary>
        /// 현재 상태를 글자로 적는다. 스위치 색도 같이 바뀌지만 색만으로는 애매하고,
        /// 색각 이상 유저는 알 수 없다.
        /// </summary>
        [SerializeField] private TMP_Text stateLabel;

        /// <summary>
        /// 스위치 트랙. 켜지면 초록, 꺼지면 밝은 회색으로 칠한다. <see cref="Toggle"/> 은 체크마크의
        /// 알파만 바꾸므로 트랙 색과 손잡이 위치는 여기서 옮긴다.
        /// </summary>
        [SerializeField] private Image switchTrack;

        /// <summary>트랙 가운데를 기준으로 켜지면 오른쪽, 꺼지면 왼쪽으로 <see cref="knobTravel"/> 만큼 옮긴다.</summary>
        [SerializeField] private RectTransform switchKnob;

        [SerializeField] private float knobTravel = 8.75f;
        [SerializeField] private Color onColor = new Color(0.35686275f, 0.8156863f, 0.29803923f, 1f);
        [SerializeField] private Color offColor = new Color(0.93333334f, 0.9529412f, 0.972549f, 1f);

        public static event Action OnCoachDataChanged;

        private void Awake()
        {
            if (coachToggle == null)
            {
                return;
            }

            coachToggle.SetIsOnWithoutNotify(CoachData.Enabled);
            coachToggle.onValueChanged.AddListener(OnToggleChanged);

            SetLocalizedText(titleLabel, TitleKey);
            RefreshStateLabel(CoachData.Enabled);
        }

        private void OnDestroy()
        {
            if (coachToggle != null)
            {
                coachToggle.onValueChanged.RemoveListener(OnToggleChanged);
            }
        }

        private void OnToggleChanged(bool value)
        {
            CoachData.Enabled = value;
            CoachData.Save();
            RefreshStateLabel(value);
            OnCoachDataChanged?.Invoke();
        }

        private void RefreshStateLabel(bool enabled)
        {
            SetLocalizedText(stateLabel, enabled ? OnKey : OffKey);
            RefreshSwitch(enabled);
        }

        private void RefreshSwitch(bool enabled)
        {
            if (switchTrack != null)
            {
                switchTrack.color = enabled ? onColor : offColor;
            }

            if (switchKnob != null)
            {
                Vector2 position = switchKnob.anchoredPosition;
                position.x = enabled ? knobTravel : -knobTravel;
                switchKnob.anchoredPosition = position;
            }
        }

        private static async void SetLocalizedText(TMP_Text target, string key)
        {
            if (target == null)
            {
                return;
            }

            string localized = await LocaleUtils.GetStringAsync(LobbyUiTable, key);

            // 비동기로 돌아오는 사이에 페이지가 닫혔을 수 있다.
            if (target == null)
            {
                return;
            }

            target.text = string.IsNullOrWhiteSpace(localized) ? key : localized;
        }
    }
}
