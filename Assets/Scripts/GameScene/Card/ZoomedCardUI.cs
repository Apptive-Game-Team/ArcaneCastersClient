using System.Threading.Tasks;
using Data.Localization;
using Data.Magic;
using Data.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

namespace GameScene.Card
{
    public class ZoomedCardUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text cardNameText;
        [SerializeField] private TMP_Text cardManaText;
        [SerializeField] private Image cardImage;
        [SerializeField] private TMP_Text descriptionText;
        private int renderVersion;
        private MagicCastKindBadge castKindBadge;
        private Transform currentAnchor;

        private void Awake() {
            // 확대 카드는 CardUI를 제거한 프리팹 인스턴스다. 숨긴 배지도 다시 찾는다.
            castKindBadge = GetComponentInChildren<MagicCastKindBadge>(true);
        }
        
        public void Show(CardUI cardUI)
        {
            if (cardUI == null) return;
            ShowDetails(cardUI.Magic, cardUI.CardSprite, cardUI.DisplayName, cardUI.Mana,
                cardUI.transform);
        }

        public void ShowMagic(CombinedMagicData magic, RectTransform anchor)
        {
            if (magic == null || anchor == null) return;
            ShowDetails(magic, magic.GetSprite(), string.Empty, magic.manaCost.ToString(), anchor);
        }

        private async void ShowDetails(CombinedMagicData magic, Sprite sprite, string displayName,
            string mana, Transform anchor)
        {
            int version = ++renderVersion;
            currentAnchor = anchor;
            gameObject.SetActive(true);
            cardManaText.text = mana;
            cardImage.sprite = sprite;
            cardNameText.text = displayName;
            // 새 설명 패널에는 배지가 없고 기존 확대 카드에는 배지가 있다.
            if (castKindBadge != null) {
                castKindBadge.Bind(magic?.castKind ?? MagicCastKind.Unknown);
            }
            if (descriptionText == null) {
                Vector3 position = transform.position;
                position.x = anchor.position.x;
                transform.position = position;
                return;
            }
            descriptionText.text = string.Empty;
            PlaceAbove(anchor as RectTransform);
            string name = await LocaleUtils.GetStringAsync("Magic", magic.localizationKey ?? magic.serverName);
            string description = await GetSummaryAsync(magic);
            if (this == null || anchor == null || version != renderVersion || !gameObject.activeInHierarchy) return;
            cardNameText.text = name;
            descriptionText.text = description;
            float bodyHeight = Mathf.Clamp(descriptionText.GetPreferredValues(description,
                descriptionText.rectTransform.rect.width, 0f).y, 48f, 168f);
            descriptionText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bodyHeight);
            ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bodyHeight + 168f);
            PlaceAbove(anchor as RectTransform);
        }

        private static async Task<string> GetSummaryAsync(CombinedMagicData magic)
        {
            if (string.IsNullOrWhiteSpace(magic?.serverName)) return string.Empty;
            try {
                await LocalizationSettings.InitializationOperation.Task;
                var table = await LocalizationSettings.StringDatabase.GetTableAsync("MagicBook").Task;
                string key = "summary." + StringUtils.ToSnakeCase(magic.serverName);
                return table?.GetEntry(key)?.GetLocalizedString() ?? string.Empty;
            } catch (System.Exception exception) {
                Debug.LogException(exception);
                return string.Empty;
            }
        }
    
        public void Hide()
        {
            renderVersion++;
            gameObject.SetActive(false);
        }

        public void HideFor(RectTransform anchor)
        {
            if (currentAnchor == anchor) Hide();
        }

        private void OnDisable()
        {
            renderVersion++;
            currentAnchor = null;
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
