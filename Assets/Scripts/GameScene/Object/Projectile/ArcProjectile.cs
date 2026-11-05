using DG.Tweening;
using GameScene.Dto.Projectile;
using GameScene.ServedObjectComponent;
using UnityEngine;

namespace GameScene.Object.Projectile
{
    /// <summary>
    /// A projectile that flies in a parabolic arc rather than a straight line.
    /// The root, not actualObject, carries the horizontal move, so Shadow (parented to the root)
    /// keeps tracking the landing point on the ground and reads as the shadow of a rising sprite,
    /// while only actualObject rises above it.
    /// The arc is the same parabola the server's CraterEmber uses: height = 4 * arcHeight * progress * (1 - progress).
    /// </summary>
    public class ArcProjectile : MonoBehaviour, IProjectile
    {
        [SerializeField] private Transform actualObject;
        [SerializeField] private float arcHeight = 2.5f;

        public void Init(ProjectileDto projectileDto)
        {
            var world = PresentationWorld.For(this);
            actualObject.rotation = ProjectileUtil.GetRotation(projectileDto, world);
            transform.position = ProjectileUtil.GetPosition(projectileDto.start, world);

            switch (projectileDto.end)
            {
                case PositionProjectileTarget position:
                    transform.DOMove(ProjectileUtil.GetPosition(position, world), projectileDto.duration)
                        .SetEase(Ease.Linear).SetLink(gameObject);
                    break;
                case ReferenceProjectileTarget reference:
                    ServedObject targetObject = PresentationWorld.Find(reference.id, world);
                    if (targetObject == null)
                    {
                        Destroy(gameObject);
                        return;
                    }
                    MoveTo(targetObject.transform, projectileDto.duration);
                    break;
            }

            AnimateArc(projectileDto.duration);
        }

        private void AnimateArc(float duration)
        {
            float baseLocalY = actualObject.localPosition.y;

            DOTween.To(() => 0f, progress =>
                {
                    Vector3 localPosition = actualObject.localPosition;
                    localPosition.y = baseLocalY + 4f * arcHeight * progress * (1f - progress);
                    actualObject.localPosition = localPosition;
                }, 1f, duration)
                .SetEase(Ease.Linear)
                .SetLink(gameObject);
        }

        private void MoveTo(Transform target, float duration)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = target != null ? target.position : startPos;

            DOTween.To(() => 0f, v =>
                {
                    if (target)
                    {
                        endPos = target.position;
                    }
                    transform.position = Vector3.Lerp(startPos, endPos, v);
                }, 1f, duration)
                .SetEase(Ease.Linear).SetLink(gameObject);
        }
    }
}
