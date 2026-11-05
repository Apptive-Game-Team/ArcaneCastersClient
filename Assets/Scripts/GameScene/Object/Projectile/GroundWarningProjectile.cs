using GameScene.Dto.Projectile;
using UnityEngine;

namespace GameScene.Object.Projectile
{
    /// <summary>
    /// A decal laid flat on the ground at the projection's end point that grows and pulses until
    /// the projection's duration runs out. The server sends it before a delayed area hit, with the
    /// same point as start and end, so the decal marks where the hit will land for exactly as long
    /// as the player has to step out.
    ///
    /// The root lies flat like RiverWater (rotated 90 degrees about X) instead of facing the camera,
    /// so a top-down circle sprite reads as a circle on the floor at any camera tilt.
    /// </summary>
    public class GroundWarningProjectile : MonoBehaviour, IProjectile
    {
        private const float MinimumDuration = 0.05f;
        private static readonly Quaternion FlatOnGround = Quaternion.Euler(90f, 0f, 0f);

        [SerializeField] private SpriteRenderer decalRenderer;
        [SerializeField] private float startScale = 0.55f;
        [SerializeField] private float endScale = 1f;
        [SerializeField] private int pulseCount = 3;
        [SerializeField] private float minimumAlpha = 0.45f;

        /// <summary>Height above the ground plane, so the decal does not flicker against it.</summary>
        [SerializeField] private float groundLift = 0.03f;

        private Vector3 decalScale;
        private float duration;
        private float elapsed;

        public void Init(ProjectileDto projectileDto)
        {
            PresentationWorld world = PresentationWorld.For(this);
            transform.position = ProjectileUtil.GetPosition(projectileDto.end, world) + Vector3.up * groundLift;
            transform.rotation = world != null ? world.transform.rotation * FlatOnGround : FlatOnGround;

            duration = Mathf.Max(projectileDto.duration, MinimumDuration);
            decalScale = decalRenderer.transform.localScale;
            ApplyProgress(0f);
        }

        private void Update()
        {
            if (duration <= 0f)
            {
                return;
            }

            elapsed += Time.deltaTime;
            ApplyProgress(Mathf.Clamp01(elapsed / duration));
        }

        private void ApplyProgress(float progress)
        {
            float easedGrowth = 1f - (1f - progress) * (1f - progress);
            decalRenderer.transform.localScale = decalScale * Mathf.Lerp(startScale, endScale, easedGrowth);

            // Starts and ends each pulse at full alpha, so the last frame before the hit is solid.
            float pulse = 0.5f + 0.5f * Mathf.Cos(progress * pulseCount * 2f * Mathf.PI);
            Color color = decalRenderer.color;
            color.a = Mathf.Lerp(minimumAlpha, 1f, pulse);
            decalRenderer.color = color;
        }
    }
}
