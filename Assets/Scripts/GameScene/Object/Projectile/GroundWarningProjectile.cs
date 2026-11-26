using GameScene.Dto.Projectile;
using UnityEngine;

namespace GameScene.Object.Projectile
{
    /// <summary>
    /// 서버가 보내는 projection의 도착점에 경고를 표시한다. 스프라이트, 크기, 투명도는
    /// 실제 projection의 지속 시간에 맞춰 진행한다.
    ///
    /// The root lies flat like RiverWater (rotated 90 degrees about X) instead of facing the camera,
    /// so a top-down circle sprite reads as a circle on the floor at any camera tilt.
    /// </summary>
    public class GroundWarningProjectile : MonoBehaviour, IProjectile
    {
        private const float MinimumDuration = 0.05f;
        private static readonly Quaternion FlatOnGround = Quaternion.Euler(90f, 0f, 0f);

        [SerializeField] private SpriteRenderer decalRenderer;
        [SerializeField] private Sprite[] warningFrames;
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
            if (warningFrames != null && warningFrames.Length > 0)
            {
                int index = Mathf.Min((int)(progress * warningFrames.Length), warningFrames.Length - 1);
                Sprite frame = warningFrames[index];
                if (decalRenderer.sprite != frame)
                {
                    decalRenderer.sprite = frame;
                }
            }

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
