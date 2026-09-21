using System.Collections.Generic;
using UnityEngine;

namespace Data.Magic
{
    /// <summary>
    /// 마법 카드 한 장. 카드 한 장이 곧 마법 하나이므로 조합(recipe)도 시전 종류(castType)도 없다.
    /// 마나 비용과 조준 표시는 서버가 <c>/api/data/magics</c> 로 준 값을 그대로 담는다.
    /// </summary>
    public class CombinedMagicData
    {
        public long id;
        public string serverName;
        public string localizationKey;
        public string textLocalizationKey;
        public string resourceName;

        /// <summary>마법이 가진 원소 전체 목록. 항상 최소 한 항목을 담는다.</summary>
        public List<ElementType> elements;

        public int manaCost;

        /// <summary>조준 표시를 적은 document. 서버가 주지 않았으면 null 이다.</summary>
        public MagicIndicatorDocument indicator;

        private const string SpriteResourceRoot = "Game/sprites";

        /// <summary>카드 앞면이자 도감 아이콘. 마법마다 다른 아트를 이름으로 찾는다.</summary>
        public Sprite GetSprite()
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                return null;
            }

            return Resources.Load<Sprite>($"{SpriteResourceRoot}/{resourceName}");
        }
    }
}
