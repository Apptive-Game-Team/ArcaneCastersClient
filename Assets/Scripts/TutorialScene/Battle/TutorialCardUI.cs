using Data;
using Data.Localization;
using Data.Magic;
using Global.Sound;
using Sound;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TutorialScene
{
    public class TutorialCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI cardManaText;
        [SerializeField] private AudioSource cardSound;
        [SerializeField] private Image cardImage;

        [SerializeField] private Outline outline;
        
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
    
        public string CardName;

        /// <summary>이 카드가 곧 이 마법이다.</summary>
        public CombinedMagicData Magic { get; private set; }

        public string DisplayName => cardNameText.text;
        public string Mana => cardManaText.text;

        /// <summary>카드 앞면은 마법마다 다른 아트다. 배치가 바뀌어도 art Image를 직접
        /// 가리키도록, 위치에 의존하는 transform.GetChild 대신 이 메서드를 쓴다.</summary>
        public void SetArt(Sprite sprite)
        {
            if (cardImage != null)
            {
                cardImage.sprite = sprite;
            }
        }

        public async void Init(string magicName)
        {
            CardName = magicName;
            Magic = LocalCombinedMagicData.GetCombinedMagicData(magicName);
            cardManaText.text = CardManaCost.Of(Magic).ToString();
            cardNameText.text = await LocaleUtils.GetStringAsync("Magic", Magic?.localizationKey ?? magicName);
        }

        public void SetCardActive(bool isActive)
        {
            this.isActive = isActive;
            GetComponent<Image>().color = isActive ? Color.gray : Color.white;
        }
        
        public void OnCardClicked()
        {
            TutorialCardSender cardInputSender = FindObjectOfType<TutorialCardSender>();
            if (isActive)
            {
                cardSound.PlayOneShot(SoundAssets.CardDeselect);
                cardInputSender.CancelUseCard(this);
                SetCardActive(false);
            }
            else
            {
                cardSound.PlayOneShot(SoundAssets.CardSelect);
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
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }
    }
}
