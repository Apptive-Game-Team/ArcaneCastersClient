using System;
using System.Collections.Generic;
using System.Globalization;

namespace RewardChest
{
    /// <summary>
    /// Finds the chest a claim just added. The claim response names the chest definition (<c>rewardKey</c>),
    /// not the owned row, and the open endpoint takes the owned row id, so the caller reads
    /// <c>GET /api/users/mine/chests</c> and takes the newest unopened chest of that key.
    /// </summary>
    public static class ChestSelection
    {
        /// <summary>
        /// The newest chest whose <c>chestKey</c> equals <paramref name="chestKey"/> (case and surrounding
        /// spaces ignored), or null. Newest is the latest <c>acquiredAt</c>; a missing or unreadable timestamp
        /// counts as oldest, and a tie goes to the higher <c>id</c>. An empty key matches every chest.
        /// </summary>
        public static ChestDto FindNewestUnopened(IEnumerable<ChestDto> chests, string chestKey)
        {
            if (chests == null)
            {
                return null;
            }

            string wantedKey = string.IsNullOrWhiteSpace(chestKey) ? null : chestKey.Trim();
            ChestDto newest = null;
            DateTimeOffset newestAcquiredAt = DateTimeOffset.MinValue;
            foreach (ChestDto chest in chests)
            {
                if (chest == null)
                {
                    continue;
                }

                if (wantedKey != null
                    && !string.Equals(chest.chestKey?.Trim(), wantedKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                DateTimeOffset acquiredAt = ParseAcquiredAt(chest.acquiredAt);
                if (newest == null
                    || acquiredAt > newestAcquiredAt
                    || (acquiredAt == newestAcquiredAt && chest.id > newest.id))
                {
                    newest = chest;
                    newestAcquiredAt = acquiredAt;
                }
            }

            return newest;
        }

        private static DateTimeOffset ParseAcquiredAt(string acquiredAt)
        {
            if (string.IsNullOrWhiteSpace(acquiredAt))
            {
                return DateTimeOffset.MinValue;
            }

            // A timestamp without an offset is read as UTC, so every chest in one list compares the same way.
            return DateTimeOffset.TryParse(acquiredAt, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out DateTimeOffset parsed)
                ? parsed
                : DateTimeOffset.MinValue;
        }
    }
}
