using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace LobbyScene.SettingPage
{
    /// <summary>
    /// Fills the credits body. Headings and the generated-asset notice are localized;
    /// the team roster and the third-party licence text are not, because names read
    /// the same in both locales and because MIT and the Open Font License require
    /// their own wording to ship verbatim. Composing every section into one text
    /// object keeps the scroll content a single laid-out block.
    /// </summary>
    public sealed class CreditsPage : MonoBehaviour
    {
        private const string SectionSeparator = "\n\n";

        [SerializeField] private ScrollRect scroll;
        [SerializeField] private TMP_Text body;
        [SerializeField] private TextAsset team;
        [SerializeField] private TextAsset thirdPartyNotices;
        [SerializeField] private LocalizedString teamHeading;
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

            List<string> sections = new List<string>();
            Append(sections, Resolve(teamHeading));
            Append(sections, Read(team));
            Append(sections, Resolve(generatedAssetNotice));
            Append(sections, Resolve(thirdPartyNoticeHeading));
            Append(sections, Read(thirdPartyNotices));

            body.text = string.Join(SectionSeparator, sections);

            // ContentSizeFitter drives the scroll height off the text's preferred size,
            // which is still stale on the frame the page opens. Without the rebuild the
            // notices are clipped to whatever height the content had before.
            LayoutRebuilder.ForceRebuildLayoutImmediate(body.rectTransform);
        }

        private static void Append(List<string> sections, string value)
        {
            if (!string.IsNullOrEmpty(value)) sections.Add(value);
        }

        private static string Read(TextAsset asset)
        {
            return asset == null ? string.Empty : asset.text.TrimEnd();
        }

        private static string Resolve(LocalizedString value)
        {
            return value == null || value.IsEmpty ? string.Empty : value.GetLocalizedString().TrimEnd();
        }
    }
}
