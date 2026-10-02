namespace RewardChest
{
    /// <summary>
    /// The <c>rewardType</c> strings the client draws with a dedicated tile.
    /// Any other value still renders through the fallback tile.
    /// </summary>
    public static class RewardTypes
    {
        public const string Magic = "MAGIC";

        /// <summary>Older quest rewards. One card is one magic, so it points at <c>magics.id</c> like <see cref="Magic"/>.</summary>
        public const string Card = "CARD";

        public const string Decoration = "DECORATION";
        public const string Appearance = "APPEARANCE";
        public const string Chest = "CHEST";

        /// <summary>Stands in for a reward whose type the server left empty.</summary>
        public const string Unknown = "UNKNOWN";

        /// <summary>Upper-cases and trims a server value; empty becomes <see cref="Unknown"/>.</summary>
        public static string Normalize(string rewardType)
        {
            if (string.IsNullOrWhiteSpace(rewardType))
            {
                return Unknown;
            }

            return rewardType.Trim().ToUpperInvariant();
        }
    }
}
