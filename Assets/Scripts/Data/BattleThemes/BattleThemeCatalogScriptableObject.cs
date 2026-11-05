using Data;
using UnityEngine;

namespace Data.BattleThemes
{
    /// <summary>
    /// The three <see cref="BattleThemeScriptableObject"/> assets a server <c>mapType</c> can ask
    /// for. Lives at <c>Assets/Resources/BattleThemeCatalog.asset</c> so that
    /// <see cref="Global.SceneContext.PrepareMap"/> can reach it from a static method before
    /// `GameScene` loads. `GRASSLAND` and `RIVER` have no entry: they use the scene default.
    /// </summary>
    public class BattleThemeCatalogScriptableObject : ScriptableObject
    {
        public const string ResourcePath = "BattleThemeCatalog";

        [SerializeField] private BattleThemeScriptableObject forest;
        [SerializeField] private BattleThemeScriptableObject fortress;
        [SerializeField] private BattleThemeScriptableObject gate;

        /// <summary>
        /// The theme asset for a choice. Null for <see cref="MapThemeChoice.SceneDefault"/>,
        /// <see cref="MapThemeChoice.KeepFallback"/>, and a slot nobody filled in.
        /// </summary>
        public BattleThemeScriptableObject Find(MapThemeChoice choice)
        {
            switch (choice)
            {
                case MapThemeChoice.Forest:
                    return forest;
                case MapThemeChoice.Fortress:
                    return fortress;
                case MapThemeChoice.Gate:
                    return gate;
                default:
                    return null;
            }
        }
    }
}
