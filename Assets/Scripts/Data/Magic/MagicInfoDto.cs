using System;
using System.Collections.Generic;

namespace Data.Magic
{
    /// <summary>
    /// <c>GET /api/data/magics</c> 의 magics 항목 하나.
    /// </summary>
    [Serializable]
    public class MagicInfoDto
    {
        public long id;
        public string name;
        public string text;

        /// <summary>
        /// 마법이 가진 원소 전체 목록. Fire, Water, Lightning, Rock, Nature, Wind, None 중
        /// 하나씩 담기고, 마법 하나는 항상 최소 한 항목을 받는다.
        /// </summary>
        public List<string> elements;

        /// <summary>
        /// 원소 목록이 생기기 전의 서버가 보내던 단일 원소. deploy 의 lobby 는 아직 이것만
        /// 보내므로, elements 가 비어 있을 때만 대신 읽는다.
        /// </summary>
        public string element;

        public int manaCost;

        /// <summary>
        /// 조준 표시를 적은 jsonb document. 서버가 항목 안에 그대로 실어 보낸다.
        /// 옛 서버나 이 필드가 없는 마법에서는 null 이다.
        /// </summary>
        public MagicIndicatorDocument indicator;

        /// <summary>
        /// <c>magics.cast_kind</c>. Spawn(유닛), Summon(건물), Drop, Explosion, Shot 중 하나.
        /// 옛 lobby 는 보내지 않아 null 이다.
        /// </summary>
        public string castKind;
    }
}
