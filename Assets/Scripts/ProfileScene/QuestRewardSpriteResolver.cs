using Data;
using Data.Magic;
using Data.Quests;
using UnityEngine;

namespace ProfileScene
{
    /// <summary>
    /// Picks the icon for one quest reward. MAGIC and CARD resolve the same way
    /// <c>LobbyUIController.QuestRewardTracker</c> does, through the cached magic list by magics.id;
    /// APPEARANCE loads the idle pose under <c>Resources/PlayerAppearances/&lt;rewardKey&gt;</c>.
    /// DECORATION has no sprite source in the client yet, so it shares the placeholder with CHEST and
    /// every unknown type.
    /// </summary>
    public static class QuestRewardSpriteResolver
    {
        public static Sprite Resolve(QuestRewardDto reward, Sprite placeholder)
        {
            Sprite sprite = null;
            switch (QuestRewardIconRule.Classify(reward))
            {
                case QuestRewardIconKind.Magic:
                    sprite = ResolveMagicSprite(reward.rewardId);
                    break;
                case QuestRewardIconKind.Appearance:
                    sprite = ResolveAppearanceSprite(reward.rewardKey);
                    break;
            }

            return sprite != null ? sprite : placeholder;
        }

        public static Sprite ResolveMagicSprite(long magicId)
        {
            return LocalCombinedMagicData.TryGetById(magicId, out CombinedMagicData magic) ? magic.GetSprite() : null;
        }

        /// <summary>
        /// The idle pose of <paramref name="appearanceKey"/>, or of the default set when that key has no
        /// complete sprite set; null when even the default set is missing.
        /// </summary>
        public static Sprite ResolveAppearanceSprite(string appearanceKey)
        {
            string loadedId = PlayerAppearanceResolver.Resolve(
                appearanceKey,
                Resources.Load<Sprite>,
                out Sprite idle,
                out Sprite _,
                out Sprite _);
            return loadedId != null ? idle : null;
        }
    }
}
