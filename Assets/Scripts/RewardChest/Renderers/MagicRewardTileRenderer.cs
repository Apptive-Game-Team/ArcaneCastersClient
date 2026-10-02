using Data.Magic;
using UnityEngine;

namespace RewardChest.Renderers
{
    /// <summary><c>MAGIC</c> and the older <c>CARD</c>: the magic's card art and its name from the <c>Magic</c> table.</summary>
    public sealed class MagicRewardTileRenderer : IRewardTileRenderer
    {
        public bool Handles(string rewardType)
        {
            return rewardType == RewardTypes.Magic || rewardType == RewardTypes.Card;
        }

        public RewardTileContent Build(RewardView reward)
        {
            RewardSpriteResolver.TryResolveMagic(reward.Id, out CombinedMagicData magic, out Sprite sprite);
            if (magic == null || string.IsNullOrEmpty(magic.localizationKey))
            {
                return new RewardTileContent(sprite, RewardNameKeys.Table, RewardNameKeys.Magic, RewardTypes.Magic);
            }

            return new RewardTileContent(sprite, RewardNameKeys.MagicTable, magic.localizationKey, magic.serverName);
        }
    }
}
