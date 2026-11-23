using GameScene.Dto.Projectile;
using GameScene.ServedObjectComponent;
using GameScene.ServedObjectComponent.Motion;
using UnityEngine;

namespace GameScene.Object.Projectile
{
    public sealed class BrickGolemSweepPoseProjectile : MonoBehaviour, IProjectile
    {
        public void Init(ProjectileDto projectileDto)
        {
            if (!(projectileDto.start is ReferenceProjectileTarget source) ||
                !(projectileDto.duration > 0f) || float.IsInfinity(projectileDto.duration))
            {
                return;
            }

            PresentationWorld world = PresentationWorld.For(this);
            ServedObject owner = PresentationWorld.Find(source.id, world);
            if (owner == null || !owner.TryGetComponent(out BrickGolemPoseController pose) || !pose.isActiveAndEnabled)
            {
                return;
            }

            if (projectileDto.end is PositionProjectileTarget destination)
            {
                owner.FaceTowards(ProjectileUtil.GetPosition(destination, world));
            }

            pose.PlaySweep(projectileDto.duration);
        }
    }
}
