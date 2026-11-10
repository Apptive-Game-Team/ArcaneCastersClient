using Data;
using Data.Localization;
using Data.Magic;
using GameScene.Card;
using Global;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeckScene
{
    public class CardItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI cardManaText;
        [SerializeField] private TextMeshProUGUI cardCountText;
        [SerializeField] private CardImageMapper cardImageMapper;

        // [SerializeField] private Sprite typeSprite;
        // [SerializeField] private Sprite magicSprite;

        [SerializeField] private GameObject lockRoot;
        [SerializeField] private TextMeshProUGUI unlockConditionText;
        [SerializeField] private TextMeshProUGUI unlockProgressText;
        [SerializeField] private Image cardArtImage;

        // 칸 배경. 비어 있으면 이 GameObject 의 Image 를 쓴다.
        [SerializeField] private Image tileImage;

        // 지금 덱에 든 마법에 켜는 초록 테두리.
        [SerializeField] private GameObject inDeckMarker;

        private static readonly Color LockedColor = new Color(0f, 0f, 0f, 0.85f);
        // 덱에 다 넣어 더 넣을 수 없는 카드의 아트 색.
        private static readonly Color ExhaustedColor = new Color(1f, 1f, 1f, 0.4f);
        private System.Action<CardItemUI> onPointerEnter;
        private System.Action onPointerExit;
        private Color? tileBaseColor;
        private int ownedCount;
        private int? remainingCount;
        private bool countVisible;

        public void Init(string cName, int count)
        {
            Init(cName, count, true, null, null);
        }

        public async void Init(string cName, int count, bool unlocked, string unlockText, string progressText)
        {
            ownedCount = count;
            countVisible = false;
            if (cardArtImage == null)
                cardArtImage = transform.GetChild(2).GetComponent<Image>();

            CombinedMagicData magic = LocalCombinedMagicData.GetCombinedMagicData(cName);

            // 카드 앞면은 마법마다 다른 아트다. cardImageMapper 에는 원소 아이콘만 남아 있다.
            // TODO(#577): 카드에 원소 아이콘을 함께 붙이려면 cardImageMapper.GetElementImage 를 쓴다.
            cardArtImage.sprite = magic != null ? magic.GetSprite() : null;

            if (cardManaText != null)
            {
                cardManaText.text = CardManaCost.Of(magic).ToString();
            }

            Image bg = tileImage != null ? tileImage : GetComponent<Image>();
            if (bg != null)
            {
                tileBaseColor ??= bg.color;
            }

            if (cardNameText != null)
            {
                string localizedName = await LocaleUtils.GetStringAsync("Magic", magic?.localizationKey ?? cName);
                if (this == null)
                {
                    return;
                }

                cardNameText.text = localizedName;
            }

            if (unlocked)
            {
                if (lockRoot != null) lockRoot.SetActive(false);
                if (unlockConditionText != null) unlockConditionText.text = "";
                if (unlockProgressText != null) unlockProgressText.text = "";
                countVisible = true;
                RefreshCount();
                if (bg != null) bg.color = tileBaseColor ?? Color.white;
            }
            else
            {
                if (cardCountText != null) cardCountText.text = "";
                if (lockRoot != null) lockRoot.SetActive(true);
                if (unlockConditionText != null) unlockConditionText.text = unlockText ?? "";
                if (unlockProgressText != null) unlockProgressText.text = progressText ?? "";
                cardArtImage.color = LockedColor;
                if (bg != null) bg.color = LockedColor;
            }
        }

        /// <summary>
        /// 덱에 넣고 남은 장수를 보인다. 이름을 읽어 오는 동안 Init 이 끝나지 않았어도 값을 기억해 두었다가
        /// Init 이 마저 그릴 때 쓴다.
        /// </summary>
        public void SetRemaining(int remaining)
        {
            remainingCount = remaining;
            RefreshCount();
        }

        private void RefreshCount()
        {
            if (!countVisible)
            {
                return;
            }

            int shown = remainingCount ?? ownedCount;
            if (cardCountText != null) cardCountText.text = $"×{shown}";
            if (cardArtImage != null) cardArtImage.color = shown > 0 ? Color.white : ExhaustedColor;
        }

        public void SetInDeck(bool isInDeck)
        {
            if (inDeckMarker != null)
            {
                inDeckMarker.SetActive(isInDeck);
            }
        }

        public void BindHover(System.Action<CardItemUI> onEnter, System.Action onExit)
        {
            onPointerEnter = onEnter;
            onPointerExit = onExit;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            onPointerEnter?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            onPointerExit?.Invoke();
        }

        // gamepad 는 마우스를 올릴 수 없으므로 선택을 hover 로 취급해 같은 팝업을 띄운다.
        public void OnSelect(BaseEventData eventData)
        {
            onPointerEnter?.Invoke(this);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            onPointerExit?.Invoke();
        }
    }
}
