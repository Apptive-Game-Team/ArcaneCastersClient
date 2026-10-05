using UnityEngine;

namespace RewardChest
{
    /// <summary>
    /// Turns one reward type into what its tile shows. Add a reward type by writing one of these and
    /// adding it to <c>RewardTileRenderers.CreateDefaultSelector</c>; the presenter does not change.
    /// </summary>
    public interface IRewardTileRenderer
    {
        /// <summary>Called with a normalized type (<see cref="RewardTypes.Normalize"/>). Must not throw.</summary>
        bool Handles(string rewardType);

        RewardTileContent Build(RewardView reward);
    }

    /// <summary>
    /// What a tile shows apart from the amount. A null <see cref="Icon"/> means the tile keeps its
    /// placeholder icon.
    /// </summary>
    public sealed class RewardTileContent
    {
        public Sprite Icon { get; }

        /// <summary>String table to read the name from; null shows <see cref="FallbackName"/> as is.</summary>
        public string NameTable { get; }

        public string NameKey { get; }

        /// <summary>Shown while the localized name loads, and instead of it when the lookup returns nothing.</summary>
        public string FallbackName { get; }

        public RewardTileContent(Sprite icon, string nameTable, string nameKey, string fallbackName)
        {
            Icon = icon;
            NameTable = nameTable;
            NameKey = nameKey;
            FallbackName = fallbackName ?? string.Empty;
        }

        public bool HasLocalizedName => !string.IsNullOrEmpty(NameTable) && !string.IsNullOrEmpty(NameKey);
    }
}
