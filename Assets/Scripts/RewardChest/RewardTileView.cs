using System.Threading.Tasks;
using Data.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RewardChest
{
    /// <summary>
    /// One reward tile: icon, name and amount. It draws whatever <see cref="RewardTileContent"/> says
    /// and knows nothing about reward types.
    /// </summary>
    public class RewardTileView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text amountText;

        /// <summary>
        /// Shown when a renderer has no sprite for the reward. Left empty, the icon keeps the sprite and
        /// colour the prefab gave it.
        /// </summary>
        [SerializeField] private Sprite placeholderIcon;

        private Sprite authoredIcon;
        private Color authoredIconColor = Color.white;
        private bool authoredIconCaptured;
        private int renderVersion;

        public void Render(RewardView reward, RewardTileContent content)
        {
            CaptureAuthoredIcon();
            renderVersion++;

            SetIcon(content?.Icon);
            if (amountText != null)
            {
                amountText.text = reward == null ? string.Empty : $"x{reward.Amount}";
            }

            _ = ApplyNameAsync(content, renderVersion);
        }

        private void CaptureAuthoredIcon()
        {
            if (authoredIconCaptured || iconImage == null)
            {
                return;
            }

            authoredIcon = iconImage.sprite;
            authoredIconColor = iconImage.color;
            authoredIconCaptured = true;
        }

        private void SetIcon(Sprite sprite)
        {
            if (iconImage == null)
            {
                return;
            }

            if (sprite != null)
            {
                iconImage.sprite = sprite;
                iconImage.color = Color.white;
                iconImage.preserveAspect = true;
                return;
            }

            iconImage.sprite = placeholderIcon != null ? placeholderIcon : authoredIcon;
            iconImage.color = authoredIconColor;
        }

        private async Task ApplyNameAsync(RewardTileContent content, int version)
        {
            if (nameText == null)
            {
                return;
            }

            nameText.text = content?.FallbackName ?? string.Empty;
            if (content == null || !content.HasLocalizedName)
            {
                return;
            }

            string localized = await LocaleUtils.GetStringAsync(content.NameTable, content.NameKey);

            // The tile may have been destroyed or reused for another reward while the lookup ran.
            if (this == null || nameText == null || version != renderVersion || string.IsNullOrEmpty(localized))
            {
                return;
            }

            nameText.text = localized;
        }
    }
}
