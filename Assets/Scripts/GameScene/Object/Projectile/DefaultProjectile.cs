using DG.Tweening;
using GameScene.Dto.Projectile;
using GameScene.ServedObjectComponent;
using UnityEngine;

namespace GameScene.Object.Projectile
{
    public class DefaultProjectile : MonoBehaviour, IProjectile
    {

        [SerializeField] private Transform actualObject;
        [SerializeField] private SpriteFrameAnimator flightFrames;
        
        public void Init(ProjectileDto projectileDto) => InitCore(projectileDto, null);

        internal void InitFromMuzzle(ProjectileDto projectileDto, Vector3 muzzle) => InitCore(projectileDto, muzzle);

        private void InitCore(ProjectileDto projectileDto, Vector3? authoredStart)
        {
            var world = PresentationWorld.For(this);
            Vector3 destination = ProjectileUtil.GetPosition(projectileDto.end, world);
            Vector3 start = authoredStart ?? VisualStart(projectileDto, destination, world);
            actualObject.rotation = ProjectileUtil.GetRotation(start, destination, world);
            transform.position = start;
            flightFrames?.PlayOnce(projectileDto.duration);
            
            switch (projectileDto.end)
            {
                case PositionProjectileTarget:
                    transform.DOMove(destination, projectileDto.duration)
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
        }

        private static Vector3 VisualStart(ProjectileDto projectile, Vector3 destination, PresentationWorld world)
        {
            var launcher = ProjectileSpawner.FindReferencedLauncher(projectile, world);
            if (launcher != null && launcher.TryLaunchProjectile(destination, out Vector3 muzzle))
            {
                return muzzle;
            }

            if (projectile.type == "WaterShot" && projectile.start is ReferenceProjectileTarget reference)
            {
                ServedObject source = PresentationWorld.Find(reference.id, world);
                if (source != null && source.PrefabType == "AquaArcher")
                {
                    var body = source.GetActualTransform().GetComponentInChildren<SpriteRenderer>();
                    if (body != null)
                    {
                        source.FaceTowards(destination);
                        // 200 PPU 발사 그림의 활목은 하단 중앙 피벗에서 오른쪽 74px, 위 113px에 있다.
                        return body.transform.TransformPoint(new Vector3(body.flipX ? -.37f : .37f, .565f, 0f));
                    }
                }
            }

            return ProjectileUtil.GetPosition(projectile.start, world);
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
                .SetEase(Ease.Linear)
                .SetLink(gameObject);
        }
    }
}
