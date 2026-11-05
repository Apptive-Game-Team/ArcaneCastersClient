using System;

namespace GameScene.Dto
{
    /// <summary>
    /// Builds the objective HUD line from a <see cref="PveObjectiveInfo"/>. Pure string logic with no
    /// Unity dependency so the EditMode tests can cover it.
    /// </summary>
    public static class PveObjectiveText
    {
        public const string DestroyKey = "pve_objective_destroy";
        public const string SurviveKey = "pve_objective_survive";

        // Used when the Adventure table has no row for the key.
        public const string DestroyFallback = "Destroy the targets {0}/{1}";
        public const string SurviveFallback = "Survive {0}";

        /// <summary>Returns the string table key for the win condition, or null when it is unknown.</summary>
        public static string KeyFor(PveObjectiveInfo info)
        {
            if (info == null)
            {
                return null;
            }

            switch (info.winCondition)
            {
                case PveObjectiveInfo.DestroyObjectives:
                    return DestroyKey;
                case PveObjectiveInfo.Survive:
                    return SurviveKey;
                default:
                    return null;
            }
        }

        public static string FallbackFor(PveObjectiveInfo info)
        {
            return info != null && info.winCondition == PveObjectiveInfo.Survive ? SurviveFallback : DestroyFallback;
        }

        /// <summary>Formats seconds as m:ss. A negative value counts as zero.</summary>
        public static string FormatClock(int seconds)
        {
            int clamped = Math.Max(0, seconds);
            return $"{clamped / 60}:{clamped % 60:00}";
        }

        /// <summary>
        /// Fills <paramref name="template"/> for the info: {0}/{1} are destroyed and total for
        /// DestroyObjectives, {0} is the m:ss clock for Survive. Returns null for an unknown win condition.
        /// </summary>
        public static string Render(PveObjectiveInfo info, string template)
        {
            if (info == null || string.IsNullOrEmpty(template))
            {
                return null;
            }

            switch (info.winCondition)
            {
                case PveObjectiveInfo.DestroyObjectives:
                    int total = Math.Max(0, info.objectivesTotal);
                    int destroyed = Math.Min(total, Math.Max(0, total - info.objectivesRemaining));
                    return template.Replace("{0}", destroyed.ToString()).Replace("{1}", total.ToString());
                case PveObjectiveInfo.Survive:
                    return template.Replace("{0}", FormatClock(info.remainingSeconds));
                default:
                    return null;
            }
        }
    }
}
