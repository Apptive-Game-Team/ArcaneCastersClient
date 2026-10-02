using System;

namespace Data
{
    /// <summary>
    /// Decides which sprite set a player body uses from the <c>appearance</c> string the lobby sends.
    /// Plain C# with an injected loader, so the decision runs in an edit mode test without the Editor.
    /// <para>
    /// Order: the set named by <c>appearance</c>, then the <c>default</c> set, then nothing, in which
    /// case the caller keeps the sprites serialized on its component. A set is used as a whole; poses
    /// from two different ids are never mixed.
    /// </para>
    /// </summary>
    public static class PlayerAppearanceResolver
    {
        public const string DefaultId = "default";
        public const string RootPath = "PlayerAppearances";
        public const string IdlePose = "idle";
        public const string RaisedPose = "raised";
        public const string AttackingPose = "attacking";

        /// <summary>The id to try first: <paramref name="appearance"/> when usable, otherwise <see cref="DefaultId"/>.</summary>
        public static string ResolveId(string appearance)
        {
            if (string.IsNullOrWhiteSpace(appearance) || !IsSafeId(appearance))
            {
                return DefaultId;
            }

            return appearance;
        }

        public static string BuildPath(string id, string pose)
        {
            return RootPath + "/" + id + "/" + pose;
        }

        /// <summary>
        /// Loads the three poses. Returns the id whose set was loaded, or null when neither the requested
        /// set nor the default set is complete (all three outputs are then default values).
        /// </summary>
        public static string Resolve<T>(string appearance, Func<string, T> load, out T idle, out T raised, out T attacking)
            where T : class
        {
            string id = ResolveId(appearance);
            if (TryLoadSet(id, load, out idle, out raised, out attacking))
            {
                return id;
            }

            if (id != DefaultId && TryLoadSet(DefaultId, load, out idle, out raised, out attacking))
            {
                return DefaultId;
            }

            idle = null;
            raised = null;
            attacking = null;
            return null;
        }

        private static bool TryLoadSet<T>(string id, Func<string, T> load, out T idle, out T raised, out T attacking)
            where T : class
        {
            idle = load(BuildPath(id, IdlePose));
            raised = load(BuildPath(id, RaisedPose));
            attacking = load(BuildPath(id, AttackingPose));
            return idle != null && raised != null && attacking != null;
        }

        private static bool IsSafeId(string id)
        {
            return id.IndexOf('/') < 0
                && id.IndexOf('\\') < 0
                && id.IndexOf("..", StringComparison.Ordinal) < 0;
        }
    }
}
