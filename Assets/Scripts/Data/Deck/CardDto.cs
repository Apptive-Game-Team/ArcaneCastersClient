namespace Data.Deck
{
    /// <summary>
    /// <c>GET /api/users/mine/cards</c> 의 항목 하나. 카드 한 장이 마법 하나이므로
    /// <see cref="id"/> 는 magics.id 다.
    /// </summary>
    [System.Serializable]
    public class CardDto
    {
        public long id;
        public string name;

        /// <summary>Fire, Water, Lightning, Rock, Nature, Wind, None 중 하나.</summary>
        public string element;

        public int manaCost;
        public int count;
        public bool unlocked;
        public string unlockText;
        public string progressText;

        /// <summary><c>magics.cast_kind</c>. 옛 lobby 는 보내지 않아 null 이다.</summary>
        public string castKind;
    }
}
