using System;
using System.Collections.Generic;

namespace RewardChest
{
    /// <summary>
    /// A reward as the presentation sees it. It does not know whether it came from a quest or a chest.
    /// </summary>
    public sealed class RewardView
    {
        /// <summary>Normalized type: upper case, never empty (see <see cref="RewardTypes.Normalize"/>).</summary>
        public string Type { get; }

        /// <summary>Appearance key or chest key; null for types that have none.</summary>
        public string Key { get; }

        public long Id { get; }

        /// <summary>Always at least 1. The server sends 0 for a missing amount.</summary>
        public int Amount { get; }

        public RewardView(string type, string key, long id, int amount)
        {
            Type = RewardTypes.Normalize(type);
            Key = string.IsNullOrWhiteSpace(key) ? null : key.Trim();
            Id = id;
            Amount = Math.Max(1, amount);
        }

        public static RewardView From(RewardEntryDto entry)
        {
            if (entry == null)
            {
                return null;
            }

            return new RewardView(entry.rewardType, entry.rewardKey, entry.rewardId, entry.amount);
        }

        /// <summary>Converts a server list, skipping null entries. A null list gives an empty list.</summary>
        public static List<RewardView> FromEntries(IEnumerable<RewardEntryDto> entries)
        {
            var views = new List<RewardView>();
            if (entries == null)
            {
                return views;
            }

            foreach (RewardEntryDto entry in entries)
            {
                RewardView view = From(entry);
                if (view != null)
                {
                    views.Add(view);
                }
            }

            return views;
        }
    }
}
