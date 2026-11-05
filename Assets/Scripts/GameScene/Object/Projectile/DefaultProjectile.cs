using DG.Tweening;
using GameScene.Dto.Projectile;
using GameScene.ServedObjectComponent;
using UnityEngine;

namespace GameScene.Object.Projectile
{
    public class DefaultProjectile : MonoBehaviour, IProjectile
    {

        [SerializeField] private Transform actualObject;
        
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
