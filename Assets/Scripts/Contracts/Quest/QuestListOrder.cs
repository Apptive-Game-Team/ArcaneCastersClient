using System;
using System.Collections.Generic;
using System.Linq;

namespace Data.Quests
{
    /// <summary>
    /// Display order of the quest list: quests still open first, completed ones after, each group by
    /// questId (the order the server sends). Null entries are dropped.
    /// </summary>
    public static class QuestListOrder
    {
        public static QuestDto[] Order(IEnumerable<QuestDto> quests)
        {
            if (quests == null)
            {
                return Array.Empty<QuestDto>();
            }

            return quests
                .Where(quest => quest != null)
                .OrderBy(quest => quest.IsCompleted ? 1 : 0)
                .ThenBy(quest => quest.questId)
                .ToArray();
        }
    }

    public enum QuestRewardIconKind
    {
        Placeholder,
        Magic,
        Decoration,
        Appearance,
    }

    /// <summary>
    /// Which sprite source a reward icon comes from. Not serialized anywhere, so the enum order is free.
    /// CHEST and every type this client does not know get <see cref="QuestRewardIconKind.Placeholder"/>.
    /// </summary>
    public static class QuestRewardIconRule
    {
        public static QuestRewardIconKind Classify(QuestRewardDto reward)
        {
            if (reward == null || string.IsNullOrWhiteSpace(reward.rewardType))
            {
                return QuestRewardIconKind.Placeholder;
            }

            string rewardType = reward.rewardType.Trim();
            if (Is(rewardType, QuestRewardTypes.Magic) || Is(rewardType, QuestRewardTypes.Card))
            {
                return QuestRewardIconKind.Magic;
            }

            if (Is(rewardType, QuestRewardTypes.Decoration))
            {
                return QuestRewardIconKind.Decoration;
            }

            if (Is(rewardType, QuestRewardTypes.Appearance) && !string.IsNullOrWhiteSpace(reward.rewardKey))
            {
                return QuestRewardIconKind.Appearance;
            }

            return QuestRewardIconKind.Placeholder;
        }

        /// <summary>"x2" for more than one, empty for one or a missing amount.</summary>
        public static string AmountLabel(QuestRewardDto reward)
        {
            return reward != null && reward.amount > 1 ? "x" + reward.amount : string.Empty;
        }

        private static bool Is(string rewardType, string expected)
        {
            return string.Equals(rewardType, expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
