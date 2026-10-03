using Data;
using Data.Magic;
using UnityEngine;

namespace RewardChest
{
    /// <summary>
    /// Finds the sprite a reward is drawn with. Quest rewards in the lobby and chest tiles both read
    /// from here, so a reward looks the same wherever it appears.
    /// </summary>
    public static class RewardSpriteResolver
    {
        /// <summary>Chest art lives under <c>Assets/Resources/RewardChest/Chests/&lt;chestKey&gt;/closed.png</c> and <c>open.png</c>; see <see cref="ChestSpritePaths"/>.</summary>
        public const string ChestIconRoot = ChestSpritePaths.Root;

        public const string DefaultChestIconName = ChestSpritePaths.DefaultKey;

        /// <summary>
        /// The magic a <c>MAGIC</c> (or older <c>CARD</c>) reward points at, by <c>magics.id</c>.
        /// False until the magic list has been fetched, or when the id is not in it.
        /// </summary>
        public static bool TryResolveMagic(long magicId, out CombinedMagicData magic, out Sprite sprite)
        {
            sprite = null;
            if (!LocalCombinedMagicData.TryGetById(magicId, out magic))
            {
                return false;
            }

            sprite = magic.GetSprite();
            return sprite != null;
        }

        /// <summary>
        /// The idle pose of an appearance set, which is the same sprite the lobby avatar uses.
        /// An unsafe or unknown key returns false instead of falling back to the default set, because
        /// showing the default body as a reward would tell the player they got something else.
        /// </summary>
        public static bool TryResolveAppearanceIcon(string appearanceKey, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrWhiteSpace(appearanceKey))
            {
                return false;
            }

            string id = PlayerAppearanceResolver.ResolveId(appearanceKey);
            if (id != appearanceKey)
            {
                return false;
            }

            sprite = Resources.Load<Sprite>(PlayerAppearanceResolver.BuildPath(id, PlayerAppearanceResolver.IdlePose));
            return sprite != null;
        }

        /// <summary>
        /// The closed chest of <paramref name="chestKey"/>, then the default chest's closed art. False only when
        /// neither exists, and the caller keeps its placeholder. An unsafe key never throws; it loads the default.
        /// </summary>
        public static bool TryResolveChestIcon(string chestKey, out Sprite sprite)
        {
            sprite = ChestSpritePaths.Load(chestKey, ChestSpritePaths.Closed, Resources.Load<Sprite>);
            return sprite != null;
        }

        /// <summary>The opened chest of <paramref name="chestKey"/>, with the same fallback as <see cref="TryResolveChestIcon"/>.</summary>
        public static bool TryResolveChestOpenIcon(string chestKey, out Sprite sprite)
        {
            sprite = ChestSpritePaths.Load(chestKey, ChestSpritePaths.Open, Resources.Load<Sprite>);
            return sprite != null;
        }
    }
}
