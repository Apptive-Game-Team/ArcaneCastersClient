using System;
using Newtonsoft.Json;

namespace Data.Quests
{
    /// <summary>
    /// One element of <c>GET /api/users/mine/quests</c>: one per non-deprecated quest, ordered by questId.
    /// <para>
    /// <c>conditionType</c>, <c>state</c> and every reward's <c>rewardType</c> are plain strings, not enums.
    /// <c>JsonCodec</c> registers <c>StringEnumConverter</c>, so one value the client has not shipped yet
    /// would throw and the caller would discard the whole quest list (see json-payloads.md).
    /// </para>
    /// </summary>
    [Serializable]
    public class QuestDto
    {
        public long questId;
        public string conditionType;
        public long? conditionTargetId;
        public string state;
        public int progress;
        public int requireValue;
        public QuestRewardDto[] rewards;

        /// <summary>
        /// <c>AUTO</c> (granted by <c>POST /api/users/mine/quests/check</c>) or <c>MANUAL</c> (granted only by
        /// <c>POST /api/users/mine/quests/{questId}/claim</c>). Null from a server that predates manual claims.
        /// </summary>
        public string claimMode;

        /// <summary>
        /// True when the condition is met and the rewards were not granted yet, so the claim endpoint would
        /// grant them now. A server that does not send the field reads as false, which hides every claim button.
        /// </summary>
        public bool claimable;

        [JsonIgnore]
        public bool IsManualClaim => string.Equals(claimMode, QuestClaimModes.Manual, StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public bool IsCompleted => string.Equals(state, QuestStates.Completed, StringComparison.OrdinalIgnoreCase);

        /// <summary>Progress clamped to <c>0..requireValue</c>; a completed quest always reads as full.</summary>
        [JsonIgnore]
        public int DisplayProgress
        {
            get
            {
                int total = DisplayRequireValue;
                if (IsCompleted)
                {
                    return total;
                }

                return Math.Max(0, Math.Min(progress, total));
            }
        }

        /// <summary>At least 1, so a bar never divides by zero when the server sends 0.</summary>
        [JsonIgnore]
        public int DisplayRequireValue => Math.Max(1, requireValue);

        [JsonIgnore]
        public QuestRewardDto[] Rewards => rewards ?? Array.Empty<QuestRewardDto>();
    }

    [Serializable]
    public class QuestRewardDto
    {
        public string rewardType;
        public long rewardId;

        /// <summary>The appearance key for APPEARANCE, the chest key for CHEST, otherwise null.</summary>
        public string rewardKey;

        public int amount;
    }

    public static class QuestStates
    {
        public const string Pending = "PENDING";
        public const string InProgress = "IN_PROGRESS";
        public const string Completed = "COMPLETED";
    }

    public static class QuestClaimModes
    {
        public const string Auto = "AUTO";
        public const string Manual = "MANUAL";
    }

    public static class QuestConditionTypes
    {
        public const string StageClear = "STAGE_CLEAR";
        public const string TotalWin = "TOTAL_WIN";
        public const string AdventureClear = "ADVENTURE_CLEAR";
    }

    public static class QuestRewardTypes
    {
        public const string Magic = "MAGIC";

        /// <summary>The old name for a magic reward; a card is one magic, so it points at magics.id too.</summary>
        public const string Card = "CARD";

        public const string Decoration = "DECORATION";
        public const string Appearance = "APPEARANCE";
        public const string Chest = "CHEST";
    }
}
