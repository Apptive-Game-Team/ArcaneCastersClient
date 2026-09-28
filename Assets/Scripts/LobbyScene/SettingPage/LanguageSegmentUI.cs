using Data.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace LobbyScene.SettingPage
{
    /// <summary>
    /// 설정 화면 언어 segmented control 에서 지금 선택된 언어의 칸을 주황으로 칠한다.
    /// 칸마다 <see cref="LocaleSetButton"/> 이 붙어 있고, 선택된 칸은 주황 바탕에 흰 외곽선 글자,
    /// 나머지 칸은 투명 바탕에 잉크색 글자다.
    /// </summary>
    public class LanguageSegmentUI : MonoBehaviour
    {
        [SerializeField] private LocaleSetButton[] segments;
        [SerializeField] private Color selectedColor = new Color(1f, 0.6039216f, 0.12156863f, 1f);
        [SerializeField] private Color selectedTextColor = Color.white;
        [SerializeField] private Color idleTextColor = new Color(0.10980392f, 0.101960786f, 0.16862746f, 1f);
        [SerializeField] private Material selectedTextMaterial;
        [SerializeField] private Material idleTextMaterial;

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += Apply;

            var handle = LocalizationSettings.SelectedLocaleAsync;
            if (handle.IsDone)
            {
                Apply(handle.Result);
            }
            else
            {
                handle.Completed += operation => Apply(operation.Result);
            }
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= Apply;
        }

        private void Apply(Locale locale)
        {
            // 초기화가 끝나기 전에 설정 화면이 닫히고 파괴됐을 수 있다.
            if (this == null || segments == null)
            {
                return;
            }

            string code = locale != null ? locale.Identifier.Code : string.Empty;
            foreach (var segment in segments)
            {
                if (segment == null)
                {
                    continue;
                }

                bool selected = segment.localeCode == code;

                var background = segment.GetComponent<Image>();
                if (background != null)
                {
                    background.color = selected ? selectedColor : Color.clear;
                }

                var label = segment.GetComponentInChildren<TMP_Text>(true);
                if (label == null)
                {
                    continue;
                }

                label.color = selected ? selectedTextColor : idleTextColor;
                var material = selected ? selectedTextMaterial : idleTextMaterial;
                if (material != null)
                {
                    label.fontSharedMaterial = material;
                }
            }
        }
    }
}
