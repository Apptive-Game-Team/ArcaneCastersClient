using System;

namespace Data
{
    /// <summary>
    /// Map kind the game server picked for a match. Never serialized: DTOs carry the raw
    /// <c>mapType</c> string and parse it with <see cref="MapKinds.Parse"/>, because the client
    /// registers `StringEnumConverter` and an enum-typed DTO field would throw on a name this
    /// client has not shipped yet and drop the whole response.
    /// </summary>
    public enum MapKind
    {
        /// <summary>The server sent no <c>mapType</c> (an older server).</summary>
        Unspecified,

        /// <summary>The server sent a <c>mapType</c> this client does not know.</summary>
        Unknown,

        Grassland,
        River,
        Fortress,
        Gate,
        Forest,
    }

    /// <summary>Which battle look a map kind asks for.</summary>
    public enum MapThemeChoice
    {
        /// <summary>
        /// The server decided nothing usable, so the caller keeps its own old decision
        /// (the adventure's theme, or no theme).
        /// </summary>
        KeepFallback,

        /// <summary>No theme asset: the look `GameScene.unity` ships with.</summary>
        SceneDefault,

        Forest,
        Fortress,
        Gate,
    }

    /// <summary>Pure functions over <see cref="MapKind"/>; no Unity types, so they run in tests.</summary>
    public static class MapKinds
    {
        /// <summary>
        /// Parses the server's <c>mapType</c> spelling. Null or blank is
        /// <see cref="MapKind.Unspecified"/>; a name this client does not know is
        /// <see cref="MapKind.Unknown"/>. Never throws.
        /// </summary>
        public static MapKind Parse(string mapType)
        {
            if (string.IsNullOrWhiteSpace(mapType)) return MapKind.Unspecified;

            string name = mapType.Trim();
            if (name.Equals("GRASSLAND", StringComparison.OrdinalIgnoreCase)) return MapKind.Grassland;
            if (name.Equals("RIVER", StringComparison.OrdinalIgnoreCase)) return MapKind.River;
            if (name.Equals("FORTRESS", StringComparison.OrdinalIgnoreCase)) return MapKind.Fortress;
            if (name.Equals("GATE", StringComparison.OrdinalIgnoreCase)) return MapKind.Gate;
            if (name.Equals("FOREST", StringComparison.OrdinalIgnoreCase)) return MapKind.Forest;
            return MapKind.Unknown;
        }

        /// <summary>
        /// The battle look for a map kind. <c>GRASSLAND</c> and <c>RIVER</c> use the scene's
        /// default look; <c>Unspecified</c> and <c>Unknown</c> hand the decision back to the caller.
        /// </summary>
        public static MapThemeChoice ThemeFor(MapKind kind)
        {
            switch (kind)
            {
                case MapKind.Grassland:
                case MapKind.River:
                    return MapThemeChoice.SceneDefault;
                case MapKind.Forest:
                    return MapThemeChoice.Forest;
                case MapKind.Fortress:
                    return MapThemeChoice.Fortress;
                case MapKind.Gate:
                    return MapThemeChoice.Gate;
                default:
                    return MapThemeChoice.KeepFallback;
            }
        }
    }
}
