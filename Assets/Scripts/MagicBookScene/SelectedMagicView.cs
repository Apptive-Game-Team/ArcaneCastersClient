using Data.Localization;
using Data.Magic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace MagicBookScene
{
    /// <summary>
    /// 도감의 왼쪽 페이지. 마법 아이콘 목록과 고른 마법의 그림을 번갈아 보여준다.
    /// 그림과 돌아가기 버튼은 이 컴포넌트가 만들어 붙이므로 씬에는 페이지 배경만 있으면 된다.
    /// </summary>
    public class SelectedMagicView : MonoBehaviour
    {
        private const string MagicBookTable = "MagicBook";
        private const string BackToListKey = "detail.backToList";
        private const string BackToListFallback = "목록으로";

        private const float BackButtonFontSize = 24f;
        private const float BackButtonCornerScale = 6f;

        private static readonly Vector2 CenterAnchor = new(0.5f, 0.5f);
        private static readonly Vector2 TopLeftAnchor = new(0f, 1f);
        private static readonly Vector2 MagicImageSize = new(380f, 380f);
        // 돌아가기 버튼이 페이지 위쪽을 차지하므로 그림은 그 아래로 내려 앉힌다.
        private static readonly Vector2 MagicImagePosition = new(0f, -40f);
        private static readonly Vector2 BackButtonSize = new(190f, 60f);
        private static readonly Vector2 BackButtonPosition = new(16f, -16f);

        private static readonly Color BackButtonColor = new(0.973f, 0.925f, 0.839f);
        private static readonly Color BackButtonTextColor = new(0.478f, 0.384f, 0.282f);

        [SerializeField] private GameObject magicList;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private Sprite backButtonBackground;

        private GameObject selectedMagicRoot;
        private Image magicImage;
        private Button backButton;
        private TMP_Text backButtonLabel;
        private int localizationRefreshVersion;

        private void Awake()
        {
            EnsureSelectedMagicRoot();
        }

        private void OnEnable()
        {
            // element chart 책갈피를 눌렀다 돌아오면 이 페이지가 다시 켜진다. 그때는 목록부터 보여준다.
            ShowList();
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            RefreshBackButtonLabel();
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        }

        private void OnDestroy()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(ShowList);
            }
        }

        public void Show(CombinedMagicData data)
        {
            if (data == null)
            {
                return;
            }

            EnsureSelectedMagicRoot();
            magicImage.sprite = data.GetSprite();
            SetListVisible(false);
        }

        public void ShowList()
        {
            SetListVisible(true);
        }

        private void SetListVisible(bool listVisible)
        {
            if (magicList != null)
            {
                magicList.SetActive(listVisible);
            }

            if (selectedMagicRoot != null)
            {
                selectedMagicRoot.SetActive(!listVisible);
            }
        }

        private void OnSelectedLocaleChanged(Locale locale)
        {
            RefreshBackButtonLabel();
        }

        private async void RefreshBackButtonLabel()
        {
            if (backButtonLabel == null)
            {
                return;
            }

            int refreshVersion = ++localizationRefreshVersion;
            string text = await LocaleUtils.GetStringAsync(MagicBookTable, BackToListKey);

            if (refreshVersion != localizationRefreshVersion || backButtonLabel == null)
            {
                return;
            }

            backButtonLabel.text = string.IsNullOrWhiteSpace(text) || text == BackToListKey
                ? BackToListFallback
                : text;
        }

        private void EnsureSelectedMagicRoot()
        {
            if (selectedMagicRoot != null)
            {
                return;
            }

            selectedMagicRoot = new GameObject("SelectedMagic", typeof(RectTransform));
            var root = (RectTransform)selectedMagicRoot.transform;
            root.SetParent(transform, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            magicImage = CreateMagicImage(root);
            backButton = CreateBackButton(root);
            selectedMagicRoot.SetActive(false);
        }

        private Image CreateMagicImage(RectTransform parent)
        {
            var imageObject = new GameObject("MagicImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            PlaceCentered((RectTransform)imageObject.transform, parent, MagicImageSize, MagicImagePosition);

            var image = imageObject.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private Button CreateBackButton(RectTransform parent)
        {
            var buttonObject = new GameObject("BackToListButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var buttonRect = (RectTransform)buttonObject.transform;
            buttonRect.SetParent(parent, false);
            buttonRect.anchorMin = TopLeftAnchor;
            buttonRect.anchorMax = TopLeftAnchor;
            buttonRect.pivot = TopLeftAnchor;
            buttonRect.sizeDelta = BackButtonSize;
            buttonRect.anchoredPosition = BackButtonPosition;

            var background = buttonObject.GetComponent<Image>();
            // sprite 가 없는 Image 는 흰 사각형을 그린다. element chart 의 칸과 같은 배경을 쓴다.
            background.enabled = backButtonBackground != null;
            background.sprite = backButtonBackground;
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = BackButtonCornerScale;
            background.color = BackButtonColor;

            backButtonLabel = CreateBackButtonLabel(buttonObject.transform);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(ShowList);
            return button;
        }

        private TMP_Text CreateBackButtonLabel(Transform parent)
        {
            var labelObject = new GameObject("Label", typeof(RectTransform));
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.SetParent(parent, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObject.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null)
            {
                label.font = fontAsset;
            }

            label.text = BackToListFallback;
            label.fontSize = BackButtonFontSize;
            label.color = BackButtonTextColor;
            label.alignment = TextAlignmentOptions.Center;
            // 버튼 폭에 못 담아도 두 줄로 접지 않는다.
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            return label;
        }

        private static void PlaceCentered(RectTransform rect, RectTransform parent, Vector2 size, Vector2 position)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = CenterAnchor;
            rect.anchorMax = CenterAnchor;
            rect.pivot = CenterAnchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
