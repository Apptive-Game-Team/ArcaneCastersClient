using System;
using System.Collections.Generic;
using System.Linq;

namespace Data.Appearances
{
    /// <summary>One element of <c>GET /api/users/mine/appearances</c>: the whole catalog, ordered by sortOrder.</summary>
    [Serializable]
    public class AppearanceDto
    {
        public string key;
        public int sortOrder;
        public bool owned;
        public bool selected;
    }

    /// <summary>Body of <c>PUT /api/users/mine/appearance</c> and of its 200 response.</summary>
    [Serializable]
    public class AppearanceSelectionDto
    {
        public string appearance;
    }

    public enum AppearanceSelectionOutcome
    {
        Success,
        NotOwned,
        UnknownAppearance,
        Failed,
    }

    public static class AppearanceCatalog
    {
        /// <summary>By sortOrder, then key, with null entries and blank keys dropped.</summary>
        public static AppearanceDto[] Order(IEnumerable<AppearanceDto> appearances)
        {
            if (appearances == null)
            {
                return Array.Empty<AppearanceDto>();
            }

            return appearances
                .Where(appearance => appearance != null && !string.IsNullOrWhiteSpace(appearance.key))
                .OrderBy(appearance => appearance.sortOrder)
                .ThenBy(appearance => appearance.key, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>The key the server marks selected, else <paramref name="fallbackKey"/>.</summary>
        public static string FindSelectedKey(IEnumerable<AppearanceDto> appearances, string fallbackKey)
        {
            AppearanceDto selected = appearances?.FirstOrDefault(appearance => appearance != null && appearance.selected);
            return selected != null && !string.IsNullOrWhiteSpace(selected.key) ? selected.key : fallbackKey;
        }

        public static bool CanSelect(AppearanceDto appearance)
        {
            return appearance != null && appearance.owned && !string.IsNullOrWhiteSpace(appearance.key);
        }

        /// <summary>Marks <paramref name="key"/> selected and every other entry not selected.</summary>
        public static void MarkSelected(IEnumerable<AppearanceDto> appearances, string key)
        {
            if (appearances == null)
            {
                return;
            }

            foreach (AppearanceDto appearance in appearances)
            {
                if (appearance != null)
                {
                    appearance.selected = string.Equals(appearance.key, key, StringComparison.Ordinal);
                }
            }
        }

        public static AppearanceSelectionOutcome ClassifyResponse(long responseCode, bool transportSucceeded)
        {
            if (transportSucceeded && responseCode >= 200 && responseCode < 300)
            {
                return AppearanceSelectionOutcome.Success;
            }

            switch (responseCode)
            {
                case 403:
                    return AppearanceSelectionOutcome.NotOwned;
                case 404:
                    return AppearanceSelectionOutcome.UnknownAppearance;
                default:
                    return AppearanceSelectionOutcome.Failed;
            }
        }
    }
}
