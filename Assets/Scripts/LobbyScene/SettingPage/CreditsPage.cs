using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace LobbyScene.SettingPage
{
    /// <summary>
    /// Fills the credits body. The generated-asset notice and the section heading
    /// are localized; the third-party licence text is not, because MIT and the Open
    /// Font License both require their own wording to ship verbatim. Composing all
    /// three into one text object keeps the scroll content a single laid-out block.
    /// </summary>
    public sealed class CreditsPage : MonoBehaviour
    {
        private const string SectionSeparator = "\n\n";

        [SerializeField] private ScrollRect scroll;
        [SerializeField] private TMP_Text body;
        [SerializeField] private TextAsset thirdPartyNotices;
        [SerializeField] private LocalizedString generatedAssetNotice;
        [SerializeField] private LocalizedString thirdPartyNoticeHeading;

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            Render();
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        }

        private void OnSelectedLocaleChanged(Locale locale)
        {
            Render();
        }

        private void Render()
        {
            if (body == null) return;

            body.text = string.Join(
                SectionSeparator,
                Resolve(generatedAssetNotice),
                Resolve(thirdPartyNoticeHeading),
                thirdPartyNotices == null ? string.Empty : thirdPartyNotices.text
            );

            // ContentSizeFitter drives the scroll height off the text's preferred size,
            // which is still stale on the frame the page opens. Without the rebuild the
            // notices are clipped to whatever height the content had before.
            LayoutRebuilder.ForceRebuildLayoutImmediate(body.rectTransform);
        }

        private static string Resolve(LocalizedString value)
        {
            return value == null || value.IsEmpty ? string.Empty : value.GetLocalizedString();
        }
    }
}
