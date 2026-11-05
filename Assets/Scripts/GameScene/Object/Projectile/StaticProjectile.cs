using GameScene.Dto.Projectile;
using UnityEngine;

namespace GameScene.Object.Projectile
{
    public class StaticProjectile : MonoBehaviour, IProjectile
    {
        public void Init(ProjectileDto projectileDto)
        {
            var world = PresentationWorld.For(this);
            transform.position = ProjectileUtil.GetPosition(projectileDto.start, world);
            transform.rotation = ProjectileUtil.GetRotation(projectileDto, world);
            
            Destroy(gameObject, projectileDto.duration);
        }
    }
}
