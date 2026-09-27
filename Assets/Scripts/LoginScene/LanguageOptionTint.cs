using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace LoginScene
{
    /// <summary>
    /// Tints one segment of the login screen's language switch. The segment whose
    /// <see cref="localeCode"/> matches the current locale gets the orange fill and
    /// white outlined text that LoginButton (the primary action) uses; the other
    /// segment goes transparent with plain ink text, matching the mockup's
    /// "한국어 | English" segmented control.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class LanguageOptionTint : MonoBehaviour
    {
        private static readonly Color SelectedBackground = new Color(1f, 0.6039216f, 0.12156863f, 1f);
        private static readonly Color UnselectedBackground = new Color(1f, 1f, 1f, 0f);
        private static readonly Color SelectedTextColor = Color.white;
        private static readonly Color UnselectedTextColor = new Color(0.10980392f, 0.101960786f, 0.16862746f, 1f);

        [SerializeField] private string localeCode;
        [SerializeField] private Material selectedOutlineMaterial;

        private Image background;
        private TMP_Text label;
        private Material defaultLabelMaterial;

        private void Awake()
        {
            background = GetComponent<Image>();
            label = GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                defaultLabelMaterial = label.fontSharedMaterial;
            }
        }

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
            Apply(SafeSelectedLocale());
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        }

        private void HandleLocaleChanged(Locale locale)
        {
            Apply(locale);
        }

        private static Locale SafeSelectedLocale()
        {
            try
            {
                return LocalizationSettings.SelectedLocale;
            }
            catch
            {
                // Localization has not finished initializing yet; SelectedLocaleChanged
                // will fire once it has and Apply will run again.
                return null;
            }
        }

        private void Apply(Locale locale)
        {
            bool selected = locale != null && locale.Identifier.Code == localeCode;

            background.color = selected ? SelectedBackground : UnselectedBackground;

            if (label == null) return;

            label.color = selected ? SelectedTextColor : UnselectedTextColor;
            Material outlineMaterial = selectedOutlineMaterial != null ? selectedOutlineMaterial : defaultLabelMaterial;
            label.fontSharedMaterial = selected ? outlineMaterial : defaultLabelMaterial;
        }
    }
}
