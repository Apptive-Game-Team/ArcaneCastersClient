using Data.BattleThemes;
using Global;
using UnityEngine;

namespace GameScene
{
    /// <summary>
    /// Applies the adventure's battle theme (<see cref="SceneContext.BattleTheme"/>) to the
    /// `Map` GameObject in `GameScene.unity`. When the theme is null the scene keeps whatever
    /// art is already placed in the editor (the forest look).
    /// <para>
    /// Sprites are swapped by child name prefix (`tree_1`..`tree_4`, `rock`, `grass_1`,
    /// `grass_2`) rather than by holding 271 individual references, because the scene places
    /// that many hand-positioned instances. The ground texture is swapped on an instance
    /// material so the shared `Assets/Art/Materials/ground.mat` — used by 9 UI scenes through
    /// `background.png` — is never modified.
    /// </para>
    /// </summary>
    public class BattleThemeApplier : MonoBehaviour
    {
        private const string Tree1Prefix = "tree_1";
        private const string Tree2Prefix = "tree_2";
        private const string Tree3Prefix = "tree_3";
        private const string Tree4Prefix = "tree_4";
        private const string RockPrefix = "rock";
        private const string Grass1Prefix = "grass_1";
        private const string Grass2Prefix = "grass_2";

        [SerializeField] private Renderer groundRenderer;
        [SerializeField] private Camera targetCamera;

        private void Awake()
        {
            Apply(SceneContext.BattleTheme);
        }

        private void Apply(BattleThemeScriptableObject theme)
        {
            if (theme == null)
            {
                return;
            }

            ApplySprites(theme);
            ApplyGroundTexture(theme);
            ApplyCameraClearColor(theme);
        }

        private void ApplySprites(BattleThemeScriptableObject theme)
        {
            foreach (Transform child in transform)
            {
                Sprite replacement = ResolveSpriteForName(theme, child.name);
                if (replacement == null)
                {
                    continue;
                }

                if (child.TryGetComponent(out SpriteRenderer spriteRenderer))
                {
                    spriteRenderer.sprite = replacement;
                }
            }
        }

        private static Sprite ResolveSpriteForName(BattleThemeScriptableObject theme, string childName)
        {
            // Prefix match, not exact match: instances are named e.g. "tree_1" and
            // "tree_1 (3)" by the editor's duplicate-naming, and "rock" must not also
            // match "rock_1" if that ever gets added.
            if (childName.StartsWith(Tree1Prefix)) return theme.Tree1;
            if (childName.StartsWith(Tree2Prefix)) return theme.Tree2;
            if (childName.StartsWith(Tree3Prefix)) return theme.Tree3;
            if (childName.StartsWith(Tree4Prefix)) return theme.Tree4;
            if (childName.StartsWith(Grass1Prefix)) return theme.Grass1;
            if (childName.StartsWith(Grass2Prefix)) return theme.Grass2;
            if (childName.StartsWith(RockPrefix)) return theme.Rock;
            return null;
        }

        private void ApplyGroundTexture(BattleThemeScriptableObject theme)
        {
            if (theme.GroundTexture == null || groundRenderer == null)
            {
                return;
            }

            // `.material` (not `.sharedMaterial`) instantiates a copy the first time it is
            // read, so this never touches the shared `ground.mat` the 9 UI scenes reference.
            groundRenderer.material.mainTexture = theme.GroundTexture;
        }

        private void ApplyCameraClearColor(BattleThemeScriptableObject theme)
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null)
            {
                return;
            }

            cam.backgroundColor = theme.SkyColor;
        }
    }
}
