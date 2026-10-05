using System;

namespace RewardChest
{
    /// <summary>
    /// One unopened chest from <c>GET /api/users/mine/chests</c>.
    /// <see cref="rewards"/> is a preview of what opening it gives.
    /// </summary>
    [Serializable]
    public class ChestDto
    {
        /// <summary>The owned chest row. This is the id the open endpoint takes.</summary>
        public long id;

        /// <summary>The chest definition the row was made from.</summary>
        public long chestId;

        public string chestKey;

        /// <summary>ISO-8601 timestamp, kept as the server sent it.</summary>
        public string acquiredAt;

        public RewardEntryDto[] rewards;
    }

    /// <summary>Body of <c>POST /api/users/mine/chests/{id}/open</c> on 200.</summary>
    [Serializable]
    public class ChestOpenResponseDto
    {
        public RewardEntryDto[] rewards;
    }
}
