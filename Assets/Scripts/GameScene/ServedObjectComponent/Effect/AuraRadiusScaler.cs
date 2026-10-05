using System;
using System.Collections.Generic;
using Data.GameConfig;
using Global;
using UnityEngine;

namespace GameScene.ServedObjectComponent.Effect
{
    /// <summary>
    /// Scales an aura sprite so the circle it draws has the radius the server actually uses. The
    /// value comes from the shared parameter table that <see cref="ParametersDataSource"/> caches,
    /// which is the same table the server reads, so changing <c>repair_totem.radius</c> in the
    /// database moves this ring without a client change.
    /// <para>
    /// The sprite is a ring standing in the world XY plane while the aura it stands for is a circle
    /// lying on the ground XZ plane. Both project to an ellipse of horizontal radius r and vertical
    /// radius r·cos45, because <c>GameScene.unity</c>'s camera is tilted exactly 45°, where sine and
    /// cosine are equal. The two ellipses differ only by the perspective between the ring's top and
    /// the ground circle's far edge, a few percent at this field size. At any other tilt they would
    /// disagree outright. See <c>.agents/docs/scene-space.md</c>.
    /// </para>
    /// <para>
    /// This runs in <c>Awake</c> so that <see cref="IdleAuraEffect"/> reads the scaled value as its
    /// base scale and pulses around it. Every Awake runs before any Start, so the order
    /// holds even though both components sit on the same object.
    /// </para>
    /// </summary>
    public class AuraRadiusScaler : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        /// <summary>Server-side object name in the parameter table, for example <c>repair_totem</c>.</summary>
        [SerializeField] private string gameObjectName;

        /// <summary>Parameter name to read, for example <c>radius</c>.</summary>
        [SerializeField] private string parameterName = "radius";

        /// <summary>Radius used when the parameter table has not been fetched yet.</summary>
        [SerializeField] private float fallbackRadius = 1f;

        /// <summary>Drawn radius as a fraction of the parameter. 1 draws the true aura edge.</summary>
        [SerializeField] private float radiusMultiplier = 1f;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                WDebug.LogWarning($"AuraRadiusScaler on '{name}' has no sprite to scale.");
                return;
            }

            float radius = ResolveRadius() * radiusMultiplier;
            if (radius <= 0f)
            {
                return;
            }

            // Half the sprite's world size is the radius it draws at scale 1. rect and
            // pixelsPerUnit are used rather than Sprite.bounds because a tight-meshed sprite —
            // nature_aura is imported that way — makes what bounds covers less obvious. Scaling the
            // two axes separately keeps the ring circular in world units even though the source
            // image is 512 x 484 rather than square. It assumes a centred pivot, which every aura
            // sprite under Assets/Art/Images/Effect/Aura has (alignment: 0).
            Sprite sprite = spriteRenderer.sprite;
            float pixelsPerUnit = sprite.pixelsPerUnit;
            if (pixelsPerUnit <= Mathf.Epsilon)
            {
                return;
            }

            float halfWidth = sprite.rect.width * 0.5f / pixelsPerUnit;
            float halfHeight = sprite.rect.height * 0.5f / pixelsPerUnit;
            if (halfWidth <= Mathf.Epsilon || halfHeight <= Mathf.Epsilon)
            {
                return;
            }

            transform.localScale = new Vector3(radius / halfWidth, radius / halfHeight, 1f);
        }

        private float ResolveRadius()
        {
            var world = GameScene.Object.PresentationWorld.For(this);
            if (world != null)
                return world.TryParameter(gameObjectName, parameterName, out float recorded) ? recorded : fallbackRadius;
            IReadOnlyList<GameParameterData> parameters = ParametersDataSource.GetCachedParameters();
            if (parameters != null)
            {
                foreach (GameParameterData parameter in parameters)
                {
                    if (IsSameName(parameter.gameObjectName, gameObjectName) &&
                        IsSameName(parameter.paramName, parameterName))
                    {
                        return parameter.value;
                    }
                }
            }

            WDebug.LogWarning(
                $"Parameter '{gameObjectName}.{parameterName}' not found; " +
                $"drawing the aura at the fallback radius {fallbackRadius}.");
            return fallbackRadius;
        }

        private static bool IsSameName(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
