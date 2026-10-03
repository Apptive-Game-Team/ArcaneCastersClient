using System;
using System.Collections.Generic;
using Global.Serialization;

namespace RewardChest
{
    /// <summary>What happened to an open request, read from the HTTP status code.</summary>
    public enum ChestOpenOutcome
    {
        Opened,

        /// <summary>409. Another device or an earlier tap opened it; refreshing the list is enough.</summary>
        AlreadyOpened,

        /// <summary>404. The chest does not exist or belongs to someone else.</summary>
        NotFound,

        Failed
    }

    /// <summary>Parses the chest endpoints' bodies without throwing.</summary>
    public static class ChestPayloads
    {
        public static ChestOpenOutcome OutcomeFromStatusCode(long statusCode)
        {
            if (statusCode >= 200 && statusCode < 300)
            {
                return ChestOpenOutcome.Opened;
            }

            switch (statusCode)
            {
                case 404:
                    return ChestOpenOutcome.NotFound;
                case 409:
                    return ChestOpenOutcome.AlreadyOpened;
                default:
                    return ChestOpenOutcome.Failed;
            }
        }

        /// <summary>
        /// Reads the chest list. Null entries are dropped and a null <c>rewards</c> becomes empty, so the
        /// caller never checks either.
        /// </summary>
        public static bool TryParseChestList(string json, out ChestDto[] chests, out string error)
        {
            if (!JsonCodec.TryDeserialize(json, out ChestDto[] parsed, out error))
            {
                chests = Array.Empty<ChestDto>();
                return false;
            }

            var kept = new List<ChestDto>(parsed.Length);
            foreach (ChestDto chest in parsed)
            {
                if (chest == null)
                {
                    continue;
                }

                chest.rewards ??= Array.Empty<RewardEntryDto>();
                kept.Add(chest);
            }

            chests = kept.ToArray();
            return true;
        }

        /// <summary>Reads the open response. An empty or unreadable body gives an empty reward list.</summary>
        public static bool TryParseOpenResponse(string json, out List<RewardView> rewards, out string error)
        {
            if (!JsonCodec.TryDeserialize(json, out ChestOpenResponseDto parsed, out error))
            {
                rewards = new List<RewardView>();
                return false;
            }

            rewards = RewardView.FromEntries(parsed.rewards);
            return true;
        }
    }
}
