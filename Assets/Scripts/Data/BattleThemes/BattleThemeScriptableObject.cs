using UnityEngine;

namespace Data.BattleThemes
{
    /// <summary>
    /// Battle scene environment art for one adventure. A null reference on
    /// <c>AdventureScriptableObject.battleTheme</c> keeps the forest look
    /// <c>GameScene.unity</c> already ships with; <see cref="BattleThemeApplier"/>
    /// applies this theme's fields on top of that default.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/BattleTheme/New")]
    public class BattleThemeScriptableObject : ScriptableObject
    {
        [SerializeField] private Texture2D groundTexture;
        [SerializeField] private Sprite tree1;
        [SerializeField] private Sprite tree2;
        [SerializeField] private Sprite tree3;
        [SerializeField] private Sprite tree4;
        [SerializeField] private Sprite rock;
        [SerializeField] private Sprite grass1;
        [SerializeField] private Sprite grass2;
        [SerializeField] private Color skyColor;

        public Texture2D GroundTexture => groundTexture;
        public Sprite Tree1 => tree1;
        public Sprite Tree2 => tree2;
        public Sprite Tree3 => tree3;
        public Sprite Tree4 => tree4;
        public Sprite Rock => rock;
        public Sprite Grass1 => grass1;
        public Sprite Grass2 => grass2;
        public Color SkyColor => skyColor;
    }
}
