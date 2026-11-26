using GameScene.Dto.Projectile;
using GameScene.ServedObjectComponent;
using UnityEngine;

namespace GameScene.Object.Projectile
{
    public class StaticProjectile : MonoBehaviour, IProjectile
    {
        [SerializeField] private SpriteFrameAnimator projectileFrames;

        public void Init(ProjectileDto projectileDto)
        {
            var world = PresentationWorld.For(this);
            transform.position = ProjectileUtil.GetPosition(projectileDto.start, world);
            transform.rotation = ProjectileUtil.GetRotation(projectileDto, world);
            projectileFrames?.PlayOnce(projectileDto.duration);
            
            Destroy(gameObject, projectileDto.duration);
        }
    }
}
