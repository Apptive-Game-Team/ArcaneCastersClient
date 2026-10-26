using Data.Localization;
using Data.Magic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameScene.Card
{
    public class ZoomedCardUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text cardNameText;
        [SerializeField] private TMP_Text cardManaText;
        [SerializeField] private Image cardImage;
        [SerializeField] private TMP_Text descriptionText;
        private int renderVersion;
        
        public async void Show(CardUI cardUI)
        {
            int version = ++renderVersion;
            gameObject.SetActive(true);
            cardManaText.text = cardUI.Mana;
            cardImage.sprite = cardUI.CardSprite;
            cardNameText.text = cardUI.DisplayName;
            if (descriptionText == null) {
                Vector3 position = transform.position;
                position.x = cardUI.transform.position.x;
                transform.position = position;
                return;
            }
            descriptionText.text = string.Empty;
            PlaceAbove(cardUI.transform as RectTransform);
            CombinedMagicData magic = cardUI.Magic;
            string name = await LocaleUtils.GetStringAsync("Magic", magic.localizationKey ?? cardUI.CardName);
            string description = await MagicBookDetailText.BuildAsync(magic, compactStats: true);
            if (this == null || cardUI == null || version != renderVersion || !gameObject.activeSelf) return;
            cardNameText.text = name;
            descriptionText.text = description;
            float bodyHeight = Mathf.Clamp(descriptionText.GetPreferredValues(description,
                descriptionText.rectTransform.rect.width, 0f).y, 120f, 432f);
            descriptionText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bodyHeight);
            ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bodyHeight + 168f);
            PlaceAbove(cardUI.transform as RectTransform);
        }
    
        public void Hide()
        {
            renderVersion++;
            gameObject.SetActive(false);
        }

        private void PlaceAbove(RectTransform anchor)
        {
            var panel = transform as RectTransform;
            var parent = panel?.parent as RectTransform;
            if (anchor == null || parent == null) return;
            transform.SetAsLastSibling();
            float scale = Mathf.Min(1f, (parent.rect.width - 24f) / panel.rect.width,
                (parent.rect.height - 24f) / panel.rect.height);
            panel.localScale = Vector3.one * Mathf.Max(0.1f, scale);
            var corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            Vector3 local = parent.InverseTransformPoint((corners[1] + corners[2]) * 0.5f);
            float halfWidth = panel.rect.width * panel.localScale.x * 0.5f;
            float height = panel.rect.height * panel.localScale.y;
            local.x = Mathf.Clamp(local.x, parent.rect.xMin + halfWidth + 12f, parent.rect.xMax - halfWidth - 12f);
            local.y = Mathf.Clamp(local.y + 12f, parent.rect.yMin + 12f, parent.rect.yMax - height - 12f);
            local.z = 0f;
            panel.localPosition = local;
        }
    }
}
