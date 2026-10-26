using Data;
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
        private MagicCastKindBadge castKindBadge;

        private void Awake() {
            // 확대 카드는 CardUI를 제거한 프리팹 인스턴스다. 숨긴 배지도 다시 찾는다.
            castKindBadge = GetComponentInChildren<MagicCastKindBadge>(true);
        }
        
        public void Show(CardUI cardUI)
        {
            gameObject.SetActive(true);
            Vector3 position = gameObject.transform.position;
            position.x = cardUI.transform.position.x;
            gameObject.transform.position = position;
            cardManaText.text = cardUI.Mana;
            cardImage.sprite = cardUI.CardSprite;
            cardNameText.text = cardUI.DisplayName;
            castKindBadge.Bind(cardUI.Magic?.castKind ?? MagicCastKind.Unknown);
            gameObject.SetActive(true);
        }
    
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
