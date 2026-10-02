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
        /// <summary>
        /// Where real chest art goes: <c>Assets/Resources/RewardChest/Chests/&lt;chestKey&gt;.png</c>, with
        /// <c>default.png</c> for every chest that has no art of its own. Neither exists yet, so chest
        /// tiles show their placeholder.
        /// </summary>
        public const string ChestIconRoot = "RewardChest/Chests";

        public const string DefaultChestIconName = "default";

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

        public static bool TryResolveChestIcon(string chestKey, out Sprite sprite)
        {
            sprite = null;
            // ResolveId returns the key unchanged only when it is a safe single path segment.
            if (!string.IsNullOrWhiteSpace(chestKey) && PlayerAppearanceResolver.ResolveId(chestKey) == chestKey)
            {
                sprite = Resources.Load<Sprite>(ChestIconRoot + "/" + chestKey);
            }

            if (sprite == null)
            {
                sprite = Resources.Load<Sprite>(ChestIconRoot + "/" + DefaultChestIconName);
            }

            return sprite != null;
        }
    }
}
