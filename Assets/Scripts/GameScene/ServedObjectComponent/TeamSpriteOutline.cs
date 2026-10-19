using Global;
using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    /// <summary>
    /// Draws a team-coloured outline on a unit's sprite so allies and enemies read apart at a
    /// glance, not only through the small HP bar.
    /// <para>
    /// The material is loaded from Resources rather than built with <c>Shader.Find</c>: a shader
    /// that no asset references is stripped from the WebGL build, and the Resources material is
    /// that reference. The colour goes through a property block, so every outlined renderer still
    /// shares one material.
    /// </para>
    /// </summary>
    public static class TeamSpriteOutline
    {
        private const string MaterialResourcePath = "Materials/SpriteTeamOutline";
        private const string DefaultSpriteShaderName = "Sprites/Default";
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

        private static Material _material;
        private static bool _materialLoadFailed;
        private static MaterialPropertyBlock _propertyBlock;

        /// <summary>
        /// Swaps the renderer onto the outline material and sets its colour. A renderer whose
        /// prefab already carries its own material is left alone, since replacing it would drop
        /// whatever that material does.
        /// </summary>
        public static void Apply(SpriteRenderer spriteRenderer, Color color)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            Material material = GetMaterial();
            if (material == null)
            {
                return;
            }

            Material current = spriteRenderer.sharedMaterial;
            if (current != material)
            {
                if (current != null && current.shader != null && current.shader.name != DefaultSpriteShaderName)
                {
                    return;
                }

                spriteRenderer.sharedMaterial = material;
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            spriteRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(OutlineColorId, color);
            spriteRenderer.SetPropertyBlock(_propertyBlock);
        }

        private static Material GetMaterial()
        {
            if (_material != null || _materialLoadFailed)
            {
                return _material;
            }

            _material = Resources.Load<Material>(MaterialResourcePath);
            if (_material == null)
            {
                _materialLoadFailed = true;
                WDebug.LogWarning($"Team outline material not found at Resources/{MaterialResourcePath}.");
            }

            return _material;
        }
    }
}
