using System;
using System.Collections.Generic;
using Global.Serialization;
using RewardChest;

namespace Data.Quests
{
    /// <summary>What happened to <c>POST /api/users/mine/quests/{questId}/claim</c>, read from the status code.</summary>
    public enum QuestClaimOutcome
    {
        /// <summary>200. The rewards were granted now.</summary>
        Claimed,

        /// <summary>409. Granted earlier, on another device or by an earlier tap. Refreshing is the whole answer.</summary>
        AlreadyClaimed,

        /// <summary>404. The server does not know this quest.</summary>
        NotFound,

        /// <summary>422. The condition is not met yet.</summary>
        NotClaimableYet,

        /// <summary>Network error, 5xx or any other status.</summary>
        Failed
    }

    /// <summary>Body of the claim endpoint on 200: the rewards that were just granted.</summary>
    [Serializable]
    public class QuestClaimResponseDto
    {
        public RewardEntryDto[] rewards;
    }

    /// <summary>Reads the claim endpoint without throwing.</summary>
    public static class QuestClaimPayloads
    {
        public static QuestClaimOutcome OutcomeFromStatusCode(long statusCode)
        {
            if (statusCode >= 200 && statusCode < 300)
            {
                return QuestClaimOutcome.Claimed;
            }

            switch (statusCode)
            {
                case 404:
                    return QuestClaimOutcome.NotFound;
                case 409:
                    return QuestClaimOutcome.AlreadyClaimed;
                case 422:
                    return QuestClaimOutcome.NotClaimableYet;
                default:
                    return QuestClaimOutcome.Failed;
            }
        }

        /// <summary>
        /// Reads the granted rewards. An empty or unreadable body gives an empty list and false; the rewards
        /// were granted either way, so the caller still treats the claim as done.
        /// </summary>
        public static bool TryParseClaimResponse(string json, out List<RewardView> rewards, out string error)
        {
            if (!JsonCodec.TryDeserialize(json, out QuestClaimResponseDto parsed, out error) || parsed == null)
            {
                rewards = new List<RewardView>();
                error ??= "empty claim response";
                return false;
            }

            rewards = RewardView.FromEntries(parsed.rewards);
            return true;
        }

        /// <summary>The first <c>CHEST</c> reward in <paramref name="rewards"/>, or null.</summary>
        public static RewardView FirstChest(IEnumerable<RewardView> rewards)
        {
            if (rewards == null)
            {
                return null;
            }

            foreach (RewardView reward in rewards)
            {
                if (reward != null && reward.Type == RewardTypes.Chest)
                {
                    return reward;
                }
            }

            return null;
        }
    }
}
