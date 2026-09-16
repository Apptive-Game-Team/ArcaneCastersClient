namespace Coach
{
    /// <summary>훈수 규칙 식별자. PlayerPrefs 키에도 이 이름을 그대로 쓴다.</summary>
    public enum CoachRuleId
    {
        /// <summary>시전 위치를 고르지 않고 멈춰 있다.</summary>
        FieldSelectIdle,

        /// <summary>마법 시전이 연속으로 실패한다.</summary>
        MagicFailing,

        /// <summary>한동안 마법을 아예 쓰지 않는다.</summary>
        MagicUnused,

        /// <summary>마나 바를 한 번도 띄우지 않았다.</summary>
        ManaBarUnopened,

        /// <summary>로비에서 아무것도 하지 않고 오래 머문다.</summary>
        LobbyIdle
    }
}
