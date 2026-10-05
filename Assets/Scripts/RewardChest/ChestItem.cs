using System;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RewardChest
{
    /// <summary>One row of the chest screen: the chest, when it was earned, what it holds, and an Open button.</summary>
    public class ChestItem : MonoBehaviour
    {
        [SerializeField] private Image chestIcon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text acquiredText;
        [SerializeField] private RectTransform previewRoot;
        [SerializeField] private RewardTileView previewTilePrefab;
        [SerializeField] private Button openButton;

        private Action<ChestItem> openRequested;

        public ChestDto Chest { get; private set; }

        private void Awake()
        {
            if (openButton == null)
            {
                openButton = GetComponentInChildren<Button>(true);
            }

            if (openButton != null)
            {
                openButton.onClick.AddListener(OnOpenButtonClicked);
            }
        }

        public void Bind(ChestDto chest, RewardTileRendererSelector selector, Action<ChestItem> onOpenRequested)
        {
            Chest = chest;
            openRequested = onOpenRequested;

            if (nameText != null)
            {
                nameText.text = FormatChestName(chest?.chestKey);
            }

            if (acquiredText != null)
            {
                acquiredText.text = FormatAcquiredAt(chest?.acquiredAt);
            }

            if (chestIcon != null && RewardSpriteResolver.TryResolveChestIcon(chest?.chestKey, out Sprite sprite))
            {
                chestIcon.sprite = sprite;
                chestIcon.color = Color.white;
                chestIcon.preserveAspect = true;
            }

            RenderPreview(chest, selector);
            SetInteractable(true);
        }

        public void SetInteractable(bool interactable)
        {
            if (openButton != null)
            {
                openButton.interactable = interactable;
            }
        }

        private void RenderPreview(ChestDto chest, RewardTileRendererSelector selector)
        {
            if (previewRoot == null || previewTilePrefab == null || chest?.rewards == null)
            {
                return;
            }

            for (int index = previewRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(previewRoot.GetChild(index).gameObject);
            }

            foreach (RewardView reward in RewardView.FromEntries(chest.rewards))
            {
                RewardTileView tile = Instantiate(previewTilePrefab, previewRoot);
                tile.gameObject.SetActive(true);
                tile.Render(reward, selector.Build(reward));
            }
        }

        private void OnOpenButtonClicked()
        {
            openRequested?.Invoke(this);
        }

        /// <summary>
        /// <c>forest_chest</c> becomes <c>Forest Chest</c>. Chest names are not in a string table yet;
        /// when they are, look the key up here instead.
        /// </summary>
        public static string FormatChestName(string chestKey)
        {
            if (string.IsNullOrWhiteSpace(chestKey))
            {
                return "Chest";
            }

            var builder = new StringBuilder(chestKey.Length);
            bool startOfWord = true;
            foreach (char character in chestKey.Trim())
            {
                if (character == '_' || character == '-' || character == ' ')
                {
                    if (builder.Length > 0 && builder[builder.Length - 1] != ' ')
                    {
                        builder.Append(' ');
                    }

                    startOfWord = true;
                    continue;
                }

                builder.Append(startOfWord ? char.ToUpperInvariant(character) : character);
                startOfWord = false;
            }

            return builder.ToString().TrimEnd();
        }

        /// <summary>Local calendar date of an ISO-8601 timestamp; an unreadable value shows nothing.</summary>
        public static string FormatAcquiredAt(string acquiredAt)
        {
            if (string.IsNullOrWhiteSpace(acquiredAt))
            {
                return string.Empty;
            }

            if (!DateTimeOffset.TryParse(acquiredAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset parsed))
            {
                return string.Empty;
            }

            return parsed.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }
}
