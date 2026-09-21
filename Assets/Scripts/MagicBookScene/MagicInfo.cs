using System.Threading.Tasks;
using Data;
using Data.GameConfig;
using Data.Localization;
using Data.Magic;
using GameScene.Card;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MagicBookScene
{
    public class MagicInfo : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Transform cardsParent;
        [SerializeField] private TMP_Text statsText;
        
        [SerializeField] private CardImageMapper mapper;

        public async void Init(CombinedMagicData data)
        {
            foreach (Transform child in cardsParent)
            {
                Destroy(child.gameObject);
            }
            // 조합이 없어졌으므로 재료 카드 줄 대신 마법이 가진 원소 아이콘을 하나씩 그린다.
            // TODO(#578): 도감 화면이 정리되면 원소·마나·사거리 표시를 다시 설계한다.
            if (data.elements != null)
            {
                foreach (ElementType element in data.elements)
                {
                    Sprite elementSprite = mapper != null ? mapper.GetElementImage(element) : null;
                    if (elementSprite == null)
                    {
                        // None 이나 mapper 가 모르는 원소는 자리 없이 건너뛴다.
                        continue;
                    }

                    var cardObj = new GameObject(element.ToString());
                    var img = cardObj.AddComponent<Image>();
                    img.preserveAspect = true;
                    img.sprite = elementSprite;
                    // 크기는 cardsParent 의 GridLayoutGroup cellSize 가 정한다.
                    cardObj.transform.SetParent(cardsParent, false);
                }
            }

            nameText.text = await LocaleUtils.GetStringAsync("Magic", data.localizationKey);
            if (statsText != null)
            {
                string stats = GameParameterResolver.GetMagicDisplayStats(data);
                string description = await GetMagicBookDescriptionAsync(data);
                statsText.text = AppendText(stats, description);
                statsText.gameObject.SetActive(!string.IsNullOrWhiteSpace(statsText.text));
            }
        }

        /// <summary>
        /// 도감 설명. MagicBook 표는 마법의 snake_case 이름 하나로만 키를 잡는다.
        /// 모든 마법이 설명을 갖는 것은 아니므로 없으면 빈 문자열이다.
        /// </summary>
        private static async Task<string> GetMagicBookDescriptionAsync(CombinedMagicData data)
        {
            string key = data.textLocalizationKey;
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            string description = await LocaleUtils.GetStringAsync("MagicBook", key);
            return !string.IsNullOrWhiteSpace(description) && description != key
                ? description
                : string.Empty;
        }

        private static string AppendText(string currentText, string additionalText)
        {
            if (string.IsNullOrWhiteSpace(currentText))
            {
                return additionalText ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(additionalText))
            {
                return currentText;
            }

            return $"{currentText}\n\n{additionalText}";
        }
    }
}
