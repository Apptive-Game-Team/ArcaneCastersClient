using System.Collections.Generic;
using Data;
using Data.Localization;
using Data.Magic;
using Global;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckScene
{
    public class HaveCardMagicPopup : MonoBehaviour
    {
        private const float AnchorOffset = 12f;
        private const float PanelWidth = 300f;
        private const float PanelHeight = 260f;
        private const float MagicIconSize = 64f;
        private const float ElementIconSize = 28f;
        private const float ContentPadding = 12f;
        private const float DetailSpacing = 8f;

        // 팝업 배경이 흰 FlatCard 라서 글자는 ink(#1C1A2B)로 쓴다.
        private static readonly Color InkColor = new Color32(0x1C, 0x1A, 0x2B, 0xFF);

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform itemRoot;
        [SerializeField] private TMP_FontAsset detailFont;
        [SerializeField] private TMP_FontAsset descriptionFont;
        [SerializeField] private MagicPreview previewPrefab;
        private float currentPanelHeight = PanelHeight;

        private HoverPopupTransition hoverTransition;
        private int renderVersion;

        private void Awake()
        {
            panelRoot ??= gameObject;
            hoverTransition = new HoverPopupTransition(this);
            Hide();
            Canvas canvas = panelRoot.GetComponentInParent<Canvas>(true);
            if (canvas != null)
            {
                // The popup is taller than its original scroll-view parent.
                // Use the full canvas for both drawing and edge placement.
                panelRoot.transform.SetParent(canvas.rootCanvas.transform, true);
                panelRoot.transform.SetAsLastSibling();
            }
            panelRoot.GetComponent<CanvasGroup>().blocksRaycasts = false;
        }

        public void Show(IReadOnlyList<CombinedMagicData> magics)
        {
            Show(magics, null);
        }

        public void Show(IReadOnlyList<CombinedMagicData> magics, RectTransform anchor)
        {
            panelRoot ??= gameObject;
            Hide();
            ClearItems();
            bool hasPreview = previewPrefab != null && magics != null && magics.Count == 1 && previewPrefab.Supports(magics[0]);
            currentPanelHeight = hasPreview ? PanelHeight + 160f : PanelHeight;
            ConfigureLayout();
            int version = ++renderVersion;

            if (magics == null || magics.Count == 0)
            {
                AddEmptyItem();
            }
            else
            {
                foreach (CombinedMagicData magic in magics)
                {
                    AddMagicItem(magic, version);
                }
            }

            hoverTransition ??= new HoverPopupTransition(this);
            void BeforeShow()
            {
                if (panelRoot.transform is RectTransform panelRect)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
                }

                PlaceNextTo(anchor);
            }

            // A preview should appear while the card is still under the pointer.
            if (hasPreview) hoverTransition.ShowNow(panelRoot, BeforeShow);
            else hoverTransition.ShowAfterDelay(panelRoot, BeforeShow);
        }

        public void Hide()
        {
            renderVersion++;
            GameObject root = panelRoot != null ? panelRoot : gameObject;
            hoverTransition ??= new HoverPopupTransition(this);
            hoverTransition.HideImmediate(root);
        }

        private async void AddMagicItem(CombinedMagicData magic, int version)
        {
            if (itemRoot == null)
            {
                Debug.LogWarning("[HaveCardMagicPopup] itemRoot is not assigned.");
                return;
            }

            if (magic == null)
            {
                return;
            }

            GameObject detailObject = CreateLayoutObject("MagicBookDetail", itemRoot, typeof(VerticalLayoutGroup));
            var detailLayout = detailObject.GetComponent<VerticalLayoutGroup>();
            detailLayout.spacing = DetailSpacing;
            detailLayout.childControlWidth = true;
            detailLayout.childControlHeight = true;
            detailLayout.childForceExpandWidth = true;
            detailLayout.childForceExpandHeight = false;

            var detailElement = detailObject.AddComponent<LayoutElement>();
            detailElement.preferredWidth = PanelWidth - 24f;
            detailElement.preferredHeight = currentPanelHeight - 24f;

            TMP_Text nameText = CreateHeader(detailObject.transform, magic);

            TMP_Text bodyText = CreateText(detailObject.transform, "MagicBookText", 16f, FontStyles.Normal);
            if (descriptionFont != null)
            {
                bodyText.font = descriptionFont;
            }
            bodyText.enableWordWrapping = true;
            bodyText.enableAutoSizing = true;
            bodyText.fontSizeMin = 12f;
            bodyText.fontSizeMax = 16f;
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.overflowMode = TextOverflowModes.Truncate;
            var bodyElement = bodyText.gameObject.AddComponent<LayoutElement>();
            float bodyHeight = PanelHeight - (ContentPadding * 2f) - MagicIconSize - DetailSpacing;
            bodyElement.minHeight = bodyHeight;
            bodyElement.preferredHeight = bodyHeight;
            bodyElement.flexibleHeight = 0f;

            if (currentPanelHeight > PanelHeight && previewPrefab != null && previewPrefab.Supports(magic))
            {
                MagicPreview preview = Instantiate(previewPrefab, detailObject.transform);
                preview.Configure(magic);
                preview.gameObject.SetActive(true);
            }

            string localizedName = await GetLocalizedNameAsync(magic);
            string detailText = await MagicBookDetailText.BuildAsync(magic, compactStats: true);
            if (version != renderVersion || detailObject == null)
            {
                return;
            }

            nameText.text = localizedName;
            bodyText.text = detailText;
            bodyText.gameObject.SetActive(!string.IsNullOrWhiteSpace(detailText));

            if (detailObject.transform is RectTransform detailRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(detailRect);
            }
        }

        private TMP_Text CreateHeader(Transform parent, CombinedMagicData magic)
        {
            GameObject headerObject = CreateLayoutObject("Header", parent, typeof(HorizontalLayoutGroup));
            var headerLayout = headerObject.GetComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;

            var headerElement = headerObject.AddComponent<LayoutElement>();
            headerElement.minHeight = MagicIconSize;
            headerElement.preferredHeight = MagicIconSize;

            Image magicIcon = CreateImage(headerObject.transform, "MagicIcon", MagicIconSize);
            magicIcon.sprite = magic.GetSprite();
            magicIcon.preserveAspect = true;

            GameObject nameRoot = CreateLayoutObject("NameAndElements", headerObject.transform, typeof(VerticalLayoutGroup));
            var nameLayout = nameRoot.GetComponent<VerticalLayoutGroup>();
            nameLayout.spacing = 4f;
            nameLayout.childControlWidth = true;
            nameLayout.childControlHeight = true;
            nameLayout.childForceExpandWidth = true;
            nameLayout.childForceExpandHeight = false;
            var nameElement = nameRoot.AddComponent<LayoutElement>();
            nameElement.preferredWidth = PanelWidth - MagicIconSize - 40f;
            nameElement.preferredHeight = MagicIconSize;

            TMP_Text nameText = CreateText(nameRoot.transform, "MagicName", 21f, FontStyles.Normal);
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            CreateElementIcons(nameRoot.transform, magic);
            return nameText;
        }

        private void CreateElementIcons(Transform parent, CombinedMagicData magic)
        {
            GameObject elementRoot = CreateLayoutObject("Elements", parent, typeof(HorizontalLayoutGroup));
            var layout = elementRoot.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var rootElement = elementRoot.AddComponent<LayoutElement>();
            rootElement.minHeight = ElementIconSize;
            rootElement.preferredHeight = ElementIconSize;

            foreach (ElementType element in magic.elements ?? new List<ElementType>())
            {
                Sprite sprite = DeckCardSpriteResolver.GetElementSprite(element);
                if (sprite == null)
                {
                    continue;
                }

                Image icon = CreateImage(elementRoot.transform, element.ToString(), ElementIconSize);
                icon.sprite = sprite;
                icon.preserveAspect = true;
            }
        }

        private TMP_Text CreateText(Transform parent, string objectName, float fontSize, FontStyles fontStyle)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            if (detailFont != null)
            {
                text.font = detailFont;
            }
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = InkColor;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(Transform parent, string objectName, float size)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            imageObject.transform.SetParent(parent, false);
            var element = imageObject.GetComponent<LayoutElement>();
            element.minWidth = size;
            element.minHeight = size;
            element.preferredWidth = size;
            element.preferredHeight = size;
            return imageObject.GetComponent<Image>();
        }

        private static GameObject CreateLayoutObject(string objectName, Transform parent, System.Type layoutType)
        {
            var layoutObject = new GameObject(objectName, typeof(RectTransform), layoutType);
            layoutObject.transform.SetParent(parent, false);
            return layoutObject;
        }

        private void ConfigureLayout()
        {
            if (panelRoot != null && panelRoot.transform is RectTransform panelRect)
            {
                panelRect.sizeDelta = new Vector2(PanelWidth, currentPanelHeight);
            }

            if (itemRoot == null || !(itemRoot is RectTransform itemRect))
            {
                return;
            }

            var grid = itemRoot.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                // LayoutGroup disallows sibling layout groups, so reuse the scene's
                // existing grid instead of trying to add a VerticalLayoutGroup.
                grid.enabled = true;
                grid.padding = new RectOffset(
                    (int)ContentPadding,
                    (int)ContentPadding,
                    (int)ContentPadding,
                    (int)ContentPadding);
                grid.childAlignment = TextAnchor.UpperLeft;
                grid.cellSize = new Vector2(PanelWidth - 24f, currentPanelHeight - 24f);
                grid.spacing = Vector2.zero;
                grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 1;
            }
            else
            {
                var vertical = itemRoot.GetComponent<VerticalLayoutGroup>() ?? itemRoot.gameObject.AddComponent<VerticalLayoutGroup>();
                vertical.padding = new RectOffset(12, 12, 12, 12);
                vertical.childControlWidth = true;
                vertical.childControlHeight = true;
                vertical.childForceExpandWidth = true;
                vertical.childForceExpandHeight = false;
            }

            itemRect.anchorMin = Vector2.zero;
            itemRect.anchorMax = Vector2.one;
            itemRect.offsetMin = Vector2.zero;
            itemRect.offsetMax = Vector2.zero;
        }

        private static async System.Threading.Tasks.Task<string> GetLocalizedNameAsync(CombinedMagicData magic)
        {
            string key = magic.localizationKey;
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            string localized = await LocaleUtils.GetStringAsync("Magic", key);
            return string.IsNullOrWhiteSpace(localized) || localized == key
                ? key
                : localized;
        }

        private void AddEmptyItem()
        {
            if (itemRoot == null)
            {
                return;
            }

            GameObject textObject = new GameObject("EmptyMagicText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(itemRoot, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = "None";
            text.fontSize = 18f;
            text.color = InkColor;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void ClearItems()
        {
            if (itemRoot == null)
            {
                return;
            }

            foreach (Transform child in itemRoot)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        private void PlaceNextTo(RectTransform anchor)
        {
            if (anchor == null || panelRoot == null)
            {
                return;
            }

            RectTransform panelRect = panelRoot.transform as RectTransform;
            if (panelRect == null)
            {
                return;
            }

            var corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            Vector3 rightCenter = (corners[2] + corners[3]) * 0.5f;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(panelRect), rightCenter);
            screenPoint.x += AnchorOffset;

            panelRect.pivot = new Vector2(0f, 0.5f);
            RectTransform parentRect = panelRect.parent as RectTransform;
            if (parentRect == null)
            {
                panelRect.position = rightCenter + (anchor.right * AnchorOffset);
                return;
            }

            Camera camera = GetCanvasCamera(parentRect);
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, screenPoint, camera, out Vector3 worldPoint))
            {
                panelRect.position = worldPoint;
                Vector3 local = panelRect.localPosition;
                if (local.x + panelRect.rect.width > parentRect.rect.xMax)
                {
                    RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect,
                        RectTransformUtility.WorldToScreenPoint(camera, (corners[0] + corners[1]) * 0.5f),
                        camera, out Vector3 left);
                    local.x = parentRect.InverseTransformPoint(left).x - panelRect.rect.width - AnchorOffset;
                }
                local.x = Mathf.Clamp(local.x, parentRect.rect.xMin,
                    Mathf.Max(parentRect.rect.xMin, parentRect.rect.xMax - panelRect.rect.width));
                float halfHeight = panelRect.rect.height * 0.5f;
                local.y = Mathf.Clamp(local.y, parentRect.rect.yMin + halfHeight,
                    Mathf.Max(parentRect.rect.yMin + halfHeight, parentRect.rect.yMax - halfHeight));
                panelRect.localPosition = local;
            }
        }

        private static Camera GetCanvasCamera(Component component)
        {
            Canvas canvas = component.GetComponentInParent<Canvas>();
            return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
        }
    }
}
