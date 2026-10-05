using System;
using System.Collections.Generic;
using RewardChest;

namespace Data.Quests
{
    /// <summary>What the chest at the end of an adventure's stage path shows.</summary>
    public enum AdventureChestState
    {
        /// <summary>No quest for this adventure, the list failed to load, or the adventure is not cleared yet.</summary>
        Hidden,

        /// <summary>The adventure is cleared and the chest was not taken yet. A click claims it.</summary>
        Claimable,

        /// <summary>The chest was taken. It stays on the path, open and empty.</summary>
        Claimed
    }

    /// <summary>
    /// Finds an adventure's clear quest in <c>GET /api/users/mine/quests</c> and reads the chest state from it.
    /// The quest is the element with <c>conditionType</c> <c>ADVENTURE_CLEAR</c> and <c>conditionTargetId</c>
    /// equal to the adventure id (<c>AdventureScriptableObject.adventureId</c>: forest 1, fortress 2).
    /// </summary>
    public static class AdventureChestQuest
    {
        /// <summary>
        /// The adventure's clear quest, or null. When several match, a claimable one wins, then a completed one,
        /// then the lowest <c>questId</c>, so the chest always shows the state the player can act on.
        /// </summary>
        public static QuestDto Select(IEnumerable<QuestDto> quests, long adventureId)
        {
            if (quests == null)
            {
                return null;
            }

            QuestDto selected = null;
            foreach (QuestDto quest in quests)
            {
                if (!IsClearQuestOf(quest, adventureId))
                {
                    continue;
                }

                if (selected == null || Precedes(quest, selected))
                {
                    selected = quest;
                }
            }

            return selected;
        }

        public static bool IsClearQuestOf(QuestDto quest, long adventureId)
        {
            return quest != null
                   && quest.conditionTargetId.HasValue
                   && quest.conditionTargetId.Value == adventureId
                   && quest.conditionType != null
                   && string.Equals(quest.conditionType.Trim(), QuestConditionTypes.AdventureClear,
                       StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Claimable when the server says so; claimed when the rewards were granted (<c>COMPLETED</c>);
        /// hidden otherwise, including a null quest.
        /// </summary>
        public static AdventureChestState StateOf(QuestDto quest)
        {
            if (quest == null)
            {
                return AdventureChestState.Hidden;
            }

            if (quest.claimable)
            {
                return AdventureChestState.Claimable;
            }

            return quest.IsCompleted ? AdventureChestState.Claimed : AdventureChestState.Hidden;
        }

        /// <summary>The first <c>CHEST</c> reward of the quest, or null when it has none.</summary>
        public static RewardView ChestRewardOf(QuestDto quest)
        {
            if (quest == null)
            {
                return null;
            }

            foreach (QuestRewardDto reward in quest.Rewards)
            {
                if (reward != null && RewardTypes.Normalize(reward.rewardType) == RewardTypes.Chest)
                {
                    return new RewardView(reward.rewardType, reward.rewardKey, reward.rewardId, reward.amount);
                }
            }

            return null;
        }

        private static bool Precedes(QuestDto candidate, QuestDto current)
        {
            int candidateRank = Rank(candidate);
            int currentRank = Rank(current);
            if (candidateRank != currentRank)
            {
                return candidateRank < currentRank;
            }

            return candidate.questId < current.questId;
        }

        private static int Rank(QuestDto quest)
        {
            switch (StateOf(quest))
            {
                case AdventureChestState.Claimable:
                    return 0;
                case AdventureChestState.Claimed:
                    return 1;
                default:
                    return 2;
            }
        }
    }
}
