using Data.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProfileScene
{
    /// <summary>
    /// One quest row. The prefab holds the title, the progress label and two empty areas; the
    /// progress widget and the reward icons are built here, because their count depends on the quest.
    /// What goes into the title and the widget is decided by an <see cref="IQuestProgressPresenter"/>.
    /// </summary>
    public class QuestItem : MonoBehaviour, IQuestRowView
    {
        /// <summary>Above this many pips each one is too small to read, so a bar is drawn instead.</summary>
        private const int MaximumPips = 12;

        private const float SegmentSpacing = 3f;
        private const float RewardIconSize = 40f;
        private const float RewardIconSpacing = 6f;

        // Thin bar pieces need a larger multiplier than the 4 DESIGN.md gives for an 800 canvas, or the
        // 9-slice borders of FlatChip (about 10 units each) are taller than the 14-unit bar.
        private const float BarPixelsPerUnitMultiplier = 12f;

        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform progressRoot;
        [SerializeField] private RectTransform rewardRoot;
        [SerializeField] private Sprite barSprite;
        [SerializeField] private Sprite pipSprite;
        [SerializeField] private Sprite placeholderRewardSprite;
        [SerializeField] private Color trackColor = new Color(0.835f, 0.863f, 0.902f, 1f);
        [SerializeField] private Color fillColor = new Color(0.357f, 0.816f, 0.298f, 1f);
        [SerializeField] private Color placeholderRewardColor = new Color(1f, 0.824f, 0.247f, 1f);
        [SerializeField] private Color inkColor = new Color(0.110f, 0.102f, 0.169f, 1f);
        [SerializeField] private Color completedColor = new Color(0.180f, 0.580f, 0.141f, 1f);

        public void Render(QuestDto quest, QuestProgressPresenterRegistry registry, IQuestText text)
        {
            ClearChildren(progressRoot);
            ClearChildren(rewardRoot);

            if (quest == null || registry == null)
            {
                SetTitle(string.Empty);
                SetProgressLabel(string.Empty);
                return;
            }

            registry.Render(quest, this, text);
            ApplyCompletedState(quest, text);
            RenderRewards(quest);
        }

        public void SetTitle(string title)
        {
            if (titleText != null)
            {
                titleText.text = title ?? string.Empty;
            }
        }

        public void SetProgressLabel(string label)
        {
            if (progressText != null)
            {
                progressText.text = label ?? string.Empty;
                progressText.color = inkColor;
            }
        }

        public void ShowBar(int current, int total)
        {
            if (progressRoot == null)
            {
                return;
            }

            ClearChildren(progressRoot);
            Image track = CreateImage("Track", progressRoot, barSprite, trackColor, Image.Type.Sliced);
            Stretch(track.rectTransform, 0f, 1f);

            float ratio = total <= 0 ? 0f : Mathf.Clamp01((float)current / total);
            if (ratio <= 0f)
            {
                return;
            }

            Image fill = CreateImage("Fill", track.rectTransform, barSprite, fillColor, Image.Type.Sliced);
            Stretch(fill.rectTransform, 0f, ratio);
        }

        public void ShowSegments(int current, int total)
        {
            if (progressRoot == null)
            {
                return;
            }

            ClearChildren(progressRoot);
            if (total <= 0)
            {
                ShowBar(current, total);
                return;
            }

            float width = progressRoot.rect.width;
            float segmentWidth = (width - SegmentSpacing * (total - 1)) / total;
            if (width <= 0f || segmentWidth <= 2f)
            {
                ShowBar(current, total);
                return;
            }

            for (int index = 0; index < total; index++)
            {
                Image segment = CreateImage(
                    $"Segment{index + 1}",
                    progressRoot,
                    barSprite,
                    index < current ? fillColor : trackColor,
                    Image.Type.Sliced);
                RectTransform rect = segment.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.sizeDelta = new Vector2(segmentWidth, 0f);
                rect.anchoredPosition = new Vector2(index * (segmentWidth + SegmentSpacing), 0f);
            }
        }

        public void ShowPips(int current, int total)
        {
            if (progressRoot == null)
            {
                return;
            }

            ClearChildren(progressRoot);
            if (total <= 0 || total > MaximumPips)
            {
                ShowBar(current, total);
                return;
            }

            float size = progressRoot.rect.height > 0f ? progressRoot.rect.height : 14f;
            for (int index = 0; index < total; index++)
            {
                Image pip = CreateImage(
                    $"Pip{index + 1}",
                    progressRoot,
                    pipSprite,
                    index < current ? fillColor : trackColor,
                    Image.Type.Simple);
                pip.preserveAspect = true;
                RectTransform rect = pip.rectTransform;
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.sizeDelta = new Vector2(size, size);
                rect.anchoredPosition = new Vector2(index * (size + SegmentSpacing * 2f), 0f);
            }
        }

        private void ApplyCompletedState(QuestDto quest, IQuestText text)
        {
            if (!quest.IsCompleted || progressText == null)
            {
                return;
            }

            progressText.text = QuestTextFormat.Localize(text, QuestLocalizationKeys.Completed, "Completed");
            progressText.color = completedColor;
        }

        private void RenderRewards(QuestDto quest)
        {
            if (rewardRoot == null)
            {
                return;
            }

            QuestRewardDto[] rewards = quest.Rewards;
            for (int index = 0; index < rewards.Length; index++)
            {
                QuestRewardDto reward = rewards[index];
                if (reward == null)
                {
                    continue;
                }

                CreateRewardIcon(reward, rewards.Length - 1 - index);
            }
        }

        /// <summary>Icons are right-aligned: <paramref name="slotFromRight"/> 0 is the rightmost one.</summary>
        private void CreateRewardIcon(QuestRewardDto reward, int slotFromRight)
        {
            Sprite resolved = QuestRewardSpriteResolver.Resolve(reward, null);
            bool isPlaceholder = resolved == null;
            Image icon = CreateImage(
                $"Reward{slotFromRight}_{reward.rewardType}",
                rewardRoot,
                isPlaceholder ? placeholderRewardSprite : resolved,
                isPlaceholder ? placeholderRewardColor : Color.white,
                Image.Type.Simple);
            icon.preserveAspect = true;

            RectTransform rect = icon.rectTransform;
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(RewardIconSize, RewardIconSize);
            rect.anchoredPosition = new Vector2(-slotFromRight * (RewardIconSize + RewardIconSpacing), 0f);

            string amountLabel = QuestRewardIconRule.AmountLabel(reward);
            if (string.IsNullOrEmpty(amountLabel) || titleText == null)
            {
                return;
            }

            var amountObject = new GameObject("Amount", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var amountRect = amountObject.GetComponent<RectTransform>();
            amountRect.SetParent(rect, false);
            amountRect.anchorMin = new Vector2(0f, 0f);
            amountRect.anchorMax = new Vector2(1f, 0f);
            amountRect.pivot = new Vector2(1f, 0f);
            amountRect.sizeDelta = new Vector2(0f, 16f);
            amountRect.anchoredPosition = Vector2.zero;

            var amount = amountObject.GetComponent<TextMeshProUGUI>();
            amount.font = titleText.font;
            amount.fontSharedMaterial = titleText.fontSharedMaterial;
            amount.fontSize = 13f;
            amount.color = inkColor;
            amount.alignment = TextAlignmentOptions.BottomRight;
            amount.raycastTarget = false;
            amount.text = amountLabel;
        }

        private Image CreateImage(string objectName, Transform parent, Sprite sprite, Color color, Image.Type type)
        {
            var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sprite != null ? type : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = BarPixelsPerUnitMultiplier;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect, float minimumX, float maximumX)
        {
            rect.anchorMin = new Vector2(minimumX, 0f);
            rect.anchorMax = new Vector2(maximumX, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ClearChildren(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (int index = root.childCount - 1; index >= 0; index--)
            {
                Destroy(root.GetChild(index).gameObject);
            }
        }
    }
}
