using System;
using System.Collections.Generic;

namespace Data.Quests
{
    /// <summary>
    /// The quests this session claimed with <c>POST /api/users/mine/quests/{questId}/claim</c>. The claim screen
    /// already showed those rewards, so the lobby's <c>POST /api/users/mine/quests/check</c> popup drops any
    /// reward that names one of them instead of showing the same chest a second time.
    /// </summary>
    public sealed class ClaimedQuestLedger
    {
        /// <summary>The ledger for the running client. Cleared on logout by <c>SceneContext.ClearContext</c>.</summary>
        public static ClaimedQuestLedger Session { get; } = new ClaimedQuestLedger();

        private readonly HashSet<long> claimedQuestIds = new HashSet<long>();

        /// <summary>Ignores ids of 0 and below; the check endpoint uses 0 for a reward with no quest.</summary>
        public void Remember(long questId)
        {
            if (questId > 0)
            {
                claimedQuestIds.Add(questId);
            }
        }

        public bool Contains(long questId)
        {
            return claimedQuestIds.Contains(questId);
        }

        public void Clear()
        {
            claimedQuestIds.Clear();
        }

        /// <summary>
        /// <paramref name="rewards"/> without the entries whose <c>questId</c> this session claimed. Entries
        /// without a quest id are kept. A null list gives an empty array.
        /// </summary>
        public LobbyScene.QuestRewardDto[] WithoutClaimed(LobbyScene.QuestRewardDto[] rewards)
        {
            if (rewards == null)
            {
                return Array.Empty<LobbyScene.QuestRewardDto>();
            }

            var kept = new List<LobbyScene.QuestRewardDto>(rewards.Length);
            foreach (LobbyScene.QuestRewardDto reward in rewards)
            {
                if (reward != null && reward.questId > 0 && Contains(reward.questId))
                {
                    continue;
                }

                kept.Add(reward);
            }

            return kept.ToArray();
        }
    }
}
