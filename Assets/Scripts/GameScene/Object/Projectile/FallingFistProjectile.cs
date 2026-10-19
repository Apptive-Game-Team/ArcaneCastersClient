using GameScene.Dto.Projectile;
using UnityEngine;

namespace GameScene.Object.Projectile
{
    /// <summary>
    /// A fist that drops onto the projection's end point, then a burst of rubble and a ring of
    /// ground cracks at the point of impact. The server sends it at the moment the hit lands, so
    /// the fist reaches the ground at <see cref="impactFraction"/> of the duration and the rest of
    /// the duration is the aftermath.
    ///
    /// The fist and the burst face the camera like every other projection; the cracks lie flat on
    /// the ground. The fist sprite has a bottom-centre pivot and is drawn with flipY, so its
    /// knuckles hang below the pivot: the pivot stops one sprite height above the ground.
    /// </summary>
    public class FallingFistProjectile : MonoBehaviour, IProjectile
    {
        private const float MinimumDuration = 0.05f;
        private static readonly Quaternion FlatOnGround = Quaternion.Euler(90f, 0f, 0f);

        [SerializeField] private SpriteRenderer fistRenderer;
        [SerializeField] private SpriteRenderer burstRenderer;
        [SerializeField] private SpriteRenderer crackRenderer;

        /// <summary>How far above its landing spot the fist starts, along screen-up, in world units.</summary>
        [SerializeField] private float dropHeight = 2.5f;

        /// <summary>Share of the duration spent falling. The remainder plays the impact.</summary>
        [SerializeField] private float impactFraction = 0.4f;

        /// <summary>Height above the ground plane for the cracks, so they do not flicker against it.</summary>
        [SerializeField] private float groundLift = 0.03f;

        private Vector3 impactPoint;
        private Vector3 screenUp;
        private float fistHeight;
        private Vector3 burstScale;
        private float duration;
        private float elapsed;

        public void Init(ProjectileDto projectileDto)
        {
            PresentationWorld world = PresentationWorld.For(this);
            impactPoint = ProjectileUtil.GetPosition(projectileDto.end, world);
            screenUp = ProjectileUtil.GetScreenUp(world);
            Quaternion facingCamera = ProjectileUtil.GetRotation(impactPoint, impactPoint, world);

            transform.position = impactPoint;
            fistRenderer.transform.rotation = facingCamera;
            burstRenderer.transform.rotation = facingCamera;
            burstRenderer.transform.position = impactPoint;
            crackRenderer.transform.position = impactPoint + Vector3.up * groundLift;
            crackRenderer.transform.rotation = world != null ? world.transform.rotation * FlatOnGround : FlatOnGround;

            fistHeight = fistRenderer.sprite != null
                ? fistRenderer.sprite.bounds.size.y * Mathf.Abs(fistRenderer.transform.lossyScale.y)
                : 0f;
            burstScale = burstRenderer.transform.localScale;
            duration = Mathf.Max(projectileDto.duration, MinimumDuration);
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
            float fall = Mathf.Clamp01(progress / impactFraction);
            float aftermath = progress <= impactFraction
                ? 0f
                : Mathf.Clamp01((progress - impactFraction) / (1f - impactFraction));

            // Accelerates into the ground, the way a dropped weight does.
            float height = Mathf.Lerp(dropHeight, 0f, fall * fall);
            fistRenderer.transform.position = impactPoint + screenUp * (fistHeight + height);
            SetAlpha(fistRenderer, 1f - aftermath);

            bool landed = progress > impactFraction;
            burstRenderer.enabled = landed;
            crackRenderer.enabled = landed;
            if (!landed)
            {
                return;
            }

            burstRenderer.transform.localScale = burstScale * Mathf.Lerp(0.5f, 1f, Mathf.Sqrt(aftermath));
            SetAlpha(burstRenderer, 1f - aftermath * aftermath);
            SetAlpha(crackRenderer, 1f - aftermath * aftermath * aftermath);
        }

        private static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }
}
