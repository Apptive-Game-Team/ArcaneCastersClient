using Data;
using Data.Localization;
using Data.Magic;
using GameScene.ServedObjectComponent;
using Global.Sound;
using Sound;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameScene.Card
{
    public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI cardManaText;
        [SerializeField] private Image image;
        [SerializeField] private AudioSource cardSound;
        
        [SerializeField] private Outline outline;
        
        public Sprite CardSprite => image.sprite;
        
        private void Awake()
        {
            cardSound = gameObject.GetComponent<AudioSource>();
            if (cardSound == null)
            {
                cardSound = gameObject.AddComponent<AudioSource>();
            }
            cardSound.clip = SoundAssets.CardSelect;
            SoundVolumeSetter.Attach(cardSound, SoundVolumeSetter.SoundType.UI);
        }

        private bool isActive = false;

        public string CardName { get; private set; }

        /// <summary>이 카드가 곧 이 마법이다. 호출자가 이미 id 로 찾아서 넘기므로 null 이 아니어야 한다.</summary>
        public CombinedMagicData Magic { get; private set; }

        public string DisplayName => cardNameText.text;
        public string Mana => cardManaText.text;

        public async void Init(CombinedMagicData magic, Sprite cardSprite)
        {
            Magic = magic;
            CardName = magic?.serverName;
            image.sprite = cardSprite != null ? cardSprite : magic?.GetSprite();
            cardManaText.text = CardManaCost.Of(magic).ToString();
            cardNameText.text = await LocaleUtils.GetStringAsync("Magic", magic?.localizationKey ?? CardName);
        }

        /// <summary>
        /// 고른 카드는 흰 카드를 금색(#FFD23F)으로 칠한다. 회색은 새 디자인에서 비활성처럼 읽힌다.
        /// </summary>
        private static readonly Color SelectedCardColor = new Color32(0xFF, 0xD2, 0x3F, 0xFF);

        /// <summary>지금 고른 카드인가. gamepad 가 손패에서 이어 고를 슬롯을 정하는 데 쓴다.</summary>
        public bool IsSelected => isActive;

        public void SetCardActive(bool isActive)
        {
            this.isActive = isActive;
            GetComponent<Image>().color = isActive ? SelectedCardColor : Color.white;
        }
        
        public void OnCardClicked()
        {
            CardInputSender cardInputSender = CardInputSender.Instance;
            if (cardInputSender.IsWaitingInputResponse())
            {
                return;
            }

            if (isActive)
            {
                cardSound.PlayOneShot(SoundAssets.CardDeselect);
                cardInputSender.CancelUseCard(this);
                SetCardActive(false);
            }
            else
            {
                cardSound.PlayOneShot(SoundAssets.CardSelect);
                PlayerFeedbackController.Instance.PlayCardSelectFeedback();
                cardInputSender.TryUseCard(this);
                SetCardActive(true);
            }
            cardInputSender.SetExpectedMagicUI(); 
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }
        public void SetHighlighted(bool on)
        {
            if (outline != null)
                outline.enabled = on;
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            CardUIZoom.Instance.Show(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CardUIZoom.Instance.Hide();
        }
    }
}
