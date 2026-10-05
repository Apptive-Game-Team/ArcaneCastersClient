namespace RewardChest.Renderers
{
    /// <summary>Keys of the reward names in the <c>LobbyUI</c> string table.</summary>
    public static class RewardNameKeys
    {
        public const string Table = "LobbyUI";

        public const string Magic = "ChestRewardMagic";
        public const string Decoration = "ChestRewardDecoration";
        public const string Appearance = "ChestRewardAppearance";
        public const string Chest = "ChestRewardChest";
        public const string Unknown = "ChestRewardUnknown";

        /// <summary>The magic display names live in their own table, keyed by camelCase name.</summary>
        public const string MagicTable = "Magic";
    }
}
