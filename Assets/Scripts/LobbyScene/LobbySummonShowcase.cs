using System.Collections.Generic;
using Data.Deck;
using Data.Magic;
using UnityEngine;
using UnityEngine.UI;

namespace LobbyScene
{
    /// <summary>
    /// 로비에서 플레이어 곁에 선 소환수 두 마리를 고른 덱의 유닛 카드로 바꿔 보인다. 덱에 유닛 카드가
    /// 모자라거나 랜덤 덱이면 모자란 자리는 씬에 놓인 기본 소환수를 그대로 둔다.
    /// </summary>
    public class LobbySummonShowcase : MonoBehaviour
    {
        [SerializeField] private Image[] summonImages;

        private Sprite[] defaultSprites;

        private void Awake()
        {
            CaptureDefaults();
        }

        public void Show(DeckResponseDto deck)
        {
            CaptureDefaults();
            List<Sprite> deckSprites = PickUnitSprites(deck, summonImages.Length);
            for (int i = 0; i < summonImages.Length; i++)
            {
                Image image = summonImages[i];
                if (image == null)
                {
                    continue;
                }

                image.sprite = i < deckSprites.Count ? deckSprites[i] : defaultSprites[i];
                image.preserveAspect = true;
            }
        }

        private void CaptureDefaults()
        {
            if (defaultSprites != null || summonImages == null)
            {
                return;
            }

            defaultSprites = new Sprite[summonImages.Length];
            for (int i = 0; i < summonImages.Length; i++)
            {
                defaultSprites[i] = summonImages[i] != null ? summonImages[i].sprite : null;
            }
        }

        // 덱에 넣은 순서대로 서로 다른 유닛 카드를 고른다. 유닛 카드의 앞면이 곧 그 유닛의 모습이다.
        private static List<Sprite> PickUnitSprites(DeckResponseDto deck, int count)
        {
            var sprites = new List<Sprite>(count);
            if (deck?.cards == null)
            {
                return sprites;
            }

            var seen = new HashSet<long>();
            foreach (CardDto card in deck.cards)
            {
                if (sprites.Count >= count)
                {
                    break;
                }

                if (card == null || !seen.Add(card.id))
                {
                    continue;
                }

                CombinedMagicData magic = LocalCombinedMagicData.GetCombinedMagicData(card.name);
                MagicCastKind castKind = magic != null && magic.castKind != MagicCastKind.Unknown
                    ? magic.castKind
                    : MagicCastKinds.Parse(card.castKind);
                if (castKind != MagicCastKind.Unit)
                {
                    continue;
                }

                Sprite sprite = magic?.GetSprite();
                if (sprite != null)
                {
                    sprites.Add(sprite);
                }
            }

            return sprites;
        }
    }
}
