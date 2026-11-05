using DG.Tweening;
using GameScene.Dto.Projectile;
using GameScene.ServedObjectComponent;
using UnityEngine;

namespace GameScene.Object.Projectile
{
    /// <summary>
    /// An arm that erupts from the caster, reaches the target, holds, then retracts.
    /// The middle segment is a Tiled SpriteRenderer whose size.x carries the arm length, so the
    /// bark repeats at a constant density instead of smearing as the arm grows. The tip rides at
    /// the far end.
    ///
    /// ProjectileSpawner schedules Destroy(gameObject, dto.duration) before it calls Init, so the
    /// whole extend + recoil + hold + retract has to fit inside dto.duration. The arm reaches full
    /// extension at the style's extend fraction of that duration, and the server mirrors that
    /// fraction when it schedules the damage, so the hit lands as the tip arrives. The two are
    /// EvilEntMob.PUNCH_IMPACT_FRACTION and friends; keep them in sync.
    /// </summary>
    public class StretchProjectile : MonoBehaviour, IProjectile
    {
        /// <summary>
        /// Which motion curve the arm uses. The curves live here rather than on the prefab because
        /// the prefabs are authored by hand: a float survives that, a DOTween Ease ordinal does not.
        /// </summary>
        public enum ArmStyle
        {
            Punch = 0,
            Grab = 1,
            FireFist = 2,
        }

        private readonly struct Motion
        {
            public readonly float Extend;
            public readonly float Recoil;
            public readonly float Retract;
            public readonly float RecoilAmount;
            public readonly Ease ExtendEase;
            public readonly Ease RecoilEase;
            public readonly Ease RetractEase;

            public Motion(float extend, float recoil, float retract, float recoilAmount,
                Ease extendEase, Ease recoilEase, Ease retractEase)
            {
                Extend = extend;
                Recoil = recoil;
                Retract = retract;
                RecoilAmount = recoilAmount;
                ExtendEase = extendEase;
                RecoilEase = recoilEase;
                RetractEase = retractEase;
            }
        }

        private const float MinimumDuration = 0.05f;
        private const float VisibleLength = 0.01f;

        // Grab bursts out and then hangs on, so it reaches early and holds for the whole drag.
        // Punch leans back and slams, so most of its time is the wind-up. FireFist is the punch
        // with a harder acceleration and a bigger yank back off the impact.
        private static readonly Motion PunchMotion =
            new Motion(0.55f, 0.08f, 0.20f, 0.10f, Ease.InQuart, Ease.OutQuad, Ease.InCubic);
        // OutExpo, not OutBack: Back overshoots its target by 10%, which on an arm means the hand
        // is thrown a tenth of its reach past the enemy. The line runs downhill from the shoulder
        // to the enemy's body, so that extra length also drives the hand toward the floor. OutExpo
        // gives the same burst-and-settle read without ever exceeding the aim point.
        private static readonly Motion GrabMotion =
            new Motion(0.22f, 0f, 0.15f, 0f, Ease.OutExpo, Ease.OutQuad, Ease.InCubic);
        private static readonly Motion FireFistMotion =
            new Motion(0.45f, 0.10f, 0.20f, 0.14f, Ease.InExpo, Ease.OutQuad, Ease.InBack);

        [SerializeField] private SpriteRenderer armRenderer;
        [SerializeField] private SpriteRenderer tipRenderer;
        [SerializeField] private Transform tipTransform;
        [SerializeField] private ArmStyle style = ArmStyle.Punch;

        /// <summary>Extra reach past the target, in world units, so the tip bites in.</summary>
        [SerializeField] private float overshoot;

        /// <summary>
        /// Height above the caster's own position where the arm leaves the body, in world units.
        /// A ServedObject sits on the ground, so without this the arm would sprout from the feet.
        /// Measured off EvilEnt's sprite: the body is 2.56 units tall on a bottom-centre pivot and
        /// the shoulders sit a little above the middle.
        /// </summary>
        [SerializeField] private float shoulderHeight = 1.55f;

        /// <summary>
        /// Fallback aim height, used only when the victim has no sprite to measure. Normally the
        /// arm aims at the middle of the victim's sprite, which suits a slime and a golem alike.
        /// </summary>
        [SerializeField] private float aimHeight = 0.6f;

        private Transform startFollow;
        private Transform endFollow;
        private ServedObject endObject;
        private Vector3 origin;
        private Vector3 destination;
        private float length;
        private float progress;
        private PresentationWorld world;

        public void Init(ProjectileDto projectileDto)
        {
            world = PresentationWorld.For(this);
            startFollow = Follow(projectileDto.start);
            endObject = Resolve(projectileDto.end);
            endFollow = endObject != null ? endObject.transform : Follow(projectileDto.end);
            origin = ProjectileUtil.GetPosition(projectileDto.start, world);
            destination = ProjectileUtil.GetPosition(projectileDto.end, world);

            // A Simple renderer silently ignores size, which is the whole mechanism here.
            if (armRenderer != null && armRenderer.drawMode == SpriteDrawMode.Simple)
            {
                armRenderer.drawMode = SpriteDrawMode.Tiled;
            }

            Aim();
            ApplyProgress(0f);

            Motion motion = MotionFor(style);
            float total = Mathf.Max(projectileDto.duration, MinimumDuration);
            float extend = total * motion.Extend;
            float recoil = total * motion.Recoil;
            float retract = total * motion.Retract;
            float hold = Mathf.Max(0f, total - extend - recoil - retract);

            Sequence sequence = DOTween.Sequence()
                .Append(DOTween.To(() => progress, value => progress = value, 1f, extend)
                    .SetEase(motion.ExtendEase));

            if (recoil > 0f && motion.RecoilAmount > 0f)
            {
                sequence.Append(DOTween
                    .To(() => progress, value => progress = value, 1f - motion.RecoilAmount, recoil)
                    .SetEase(motion.RecoilEase));
            }

            sequence.AppendInterval(hold)
                .Append(DOTween.To(() => progress, value => progress = value, 0f, retract)
                    .SetEase(motion.RetractEase))
                .SetLink(gameObject)
                .SetTarget(this);
        }

        // LateUpdate, so this frame's movement of the caster and the target is already applied.
        private void LateUpdate()
        {
            Aim();
            ApplyProgress(progress);
        }

        private void Aim()
        {
            // A followed object that died mid-animation leaves its last known point in place, so
            // the arm finishes its swing at empty air instead of collapsing back to the origin.
            if (startFollow != null)
            {
                origin = startFollow.position;
            }
            if (endFollow != null)
            {
                destination = endFollow.position;
            }

            // Both ends arrive as ground positions. Lift them before aiming, so the arm leaves the
            // shoulder and lands on the target's body rather than crawling along the floor.
            // The lift runs along screen-up, not world up: the sprites these heights were measured
            // off are billboarded to the tilted camera, so world up would raise the point by only
            // its cosine on screen and push the rest into depth, dropping the arm toward the floor.
            Vector3 screenUp = ProjectileUtil.GetScreenUp(world);
            Vector3 from = origin + screenUp * shoulderHeight;

            // Aim at the middle of the victim's sprite rather than a fixed height, so the arm
            // strikes a slime and a golem in the body instead of the feet or the head. Passing an
            // edge bias of zero returns the sprite centre, measured in the renderer's own space.
            Vector3 to = endObject != null
                ? endObject.GetEdgeWorldPositionTowards(from, 0f)
                : destination + screenUp * aimHeight;

            transform.position = from;
            transform.rotation = ProjectileUtil.GetRotation(from, to, world);
            length = Mathf.Max(0f, ProjectileUtil.GetCameraPlaneLength(from, to, world) + overshoot);
        }

        private void ApplyProgress(float value)
        {
            // Clamped, not just floored: several DOTween eases leave [0,1] by design — Back
            // overshoots above 1 and undershoots below 0 — and anything above 1 throws the hand
            // past the enemy and down toward the floor. Reach past the target is the `overshoot`
            // field's job, in world units, where it does not scale with the length of the arm.
            float drawn = Mathf.Clamp01(value) * length;

            if (armRenderer != null)
            {
                // size is applied before scale, so the arm child has to stay at scale 1.
                armRenderer.size = new Vector2(drawn, armRenderer.size.y);
            }
            if (tipTransform != null)
            {
                tipTransform.localPosition = new Vector3(drawn, 0f, 0f);
            }
            if (tipRenderer != null)
            {
                // Otherwise the hand sits on the shoulder for a frame before the arm exists.
                tipRenderer.enabled = drawn > VisibleLength;
            }
        }

        private static Motion MotionFor(ArmStyle armStyle)
        {
            switch (armStyle)
            {
                case ArmStyle.Grab:
                    return GrabMotion;
                case ArmStyle.FireFist:
                    return FireFistMotion;
                default:
                    return PunchMotion;
            }
        }

        private Transform Follow(ProjectileTarget target)
        {
            ServedObject servedObject = Resolve(target);
            return servedObject != null ? servedObject.transform : null;
        }

        private ServedObject Resolve(ProjectileTarget target)
        {
            if (!(target is ReferenceProjectileTarget reference))
            {
                return null;
            }

            return PresentationWorld.Find(reference.id, world);
        }
    }
}
