using System;
using System.Collections.Generic;
using Global.Serialization;
using RewardChest;

namespace LobbyScene
{
    /// <summary>
    /// One entry of <c>POST /api/users/mine/quests/check</c>. The reward fields match
    /// <see cref="RewardEntryDto"/>; <see cref="questId"/> says which quest paid it.
    /// </summary>
    [Serializable]
    public class QuestRewardDto
    {
        public string rewardType;
        public long rewardId;
        public string rewardKey;
        public int amount;
        public long questId;

        // Compatibility fields for temporary backend naming differences.
        public string type;
        public long id;
        public int value;
    }

    [Serializable]
    public class QuestRewardResponseDto
    {
        public QuestRewardDto[] rewards;
    }

    /// <summary>
    /// Reads the quest check body. The lobby has sent both a bare array and <c>{"rewards":[...]}</c>,
    /// and older builds used <c>type</c>/<c>id</c>/<c>value</c>, so all of them are accepted.
    /// </summary>
    public static class QuestRewardPayload
    {
        /// <summary>Throws on malformed JSON; the caller logs the body and shows nothing.</summary>
        public static QuestRewardDto[] Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<QuestRewardDto>();
            }

            if (json.TrimStart().StartsWith("["))
            {
                return JsonCodec.Deserialize<QuestRewardDto[]>(json) ?? Array.Empty<QuestRewardDto>();
            }

            var response = JsonCodec.Deserialize<QuestRewardResponseDto>(json);
            return response?.rewards ?? Array.Empty<QuestRewardDto>();
        }

        public static string GetRewardType(QuestRewardDto reward)
        {
            if (!string.IsNullOrEmpty(reward.rewardType))
            {
                return reward.rewardType;
            }

            if (!string.IsNullOrEmpty(reward.type))
            {
                return reward.type;
            }

            return RewardTypes.Unknown;
        }

        public static long GetRewardId(QuestRewardDto reward)
        {
            if (reward.rewardId > 0)
            {
                return reward.rewardId;
            }

            return reward.id;
        }

        public static int GetAmount(QuestRewardDto reward)
        {
            if (reward.amount > 0)
            {
                return reward.amount;
            }

            if (reward.value > 0)
            {
                return reward.value;
            }

            return 1;
        }

        public static RewardView ToRewardView(QuestRewardDto reward)
        {
            if (reward == null)
            {
                return null;
            }

            return new RewardView(GetRewardType(reward), reward.rewardKey, GetRewardId(reward), GetAmount(reward));
        }

        public static List<RewardView> ToRewardViews(IEnumerable<QuestRewardDto> rewards)
        {
            var views = new List<RewardView>();
            if (rewards == null)
            {
                return views;
            }

            foreach (QuestRewardDto reward in rewards)
            {
                RewardView view = ToRewardView(reward);
                if (view != null)
                {
                    views.Add(view);
                }
            }

            return views;
        }
    }
}
