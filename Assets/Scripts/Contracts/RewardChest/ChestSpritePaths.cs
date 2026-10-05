using System;
using Data;

namespace RewardChest
{
    /// <summary>
    /// Where a chest's art lives under <c>Resources</c>: <c>RewardChest/Chests/&lt;chestKey&gt;/closed</c> and
    /// <c>.../open</c>, with the <c>default</c> folder for every chest that has none of its own.
    /// Plain C# with an injected loader, so the fallback order runs in an edit mode test without the Editor.
    /// </summary>
    public static class ChestSpritePaths
    {
        public const string Root = "RewardChest/Chests";
        public const string DefaultKey = "default";
        public const string Closed = "closed";
        public const string Open = "open";

        /// <summary>
        /// <c>RewardChest/Chests/&lt;key&gt;/&lt;state&gt;</c>. An empty key, or one that is not a single safe path
        /// segment, builds the default chest's path, so a hostile key never reaches <c>Resources.Load</c>.
        /// </summary>
        public static string Build(string chestKey, string state)
        {
            return Root + "/" + PlayerAppearanceResolver.ResolveId(chestKey) + "/" + state;
        }

        /// <summary>
        /// Loads the art of <paramref name="chestKey"/> in <paramref name="state"/> (<see cref="Closed"/> or
        /// <see cref="Open"/>), then the default chest's art in the same state, then returns null. Never throws on
        /// a bad key.
        /// </summary>
        public static T Load<T>(string chestKey, string state, Func<string, T> load)
            where T : class
        {
            T found = load(Build(chestKey, state));
            if (found == null && PlayerAppearanceResolver.ResolveId(chestKey) != DefaultKey)
            {
                found = load(Build(DefaultKey, state));
            }

            return found;
        }
    }
}
