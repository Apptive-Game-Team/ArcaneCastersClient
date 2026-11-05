using GameScene.Object;
using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    /// <summary>
    /// 이 child sprite 를 부모 바로 아래 땅에 눕혀 둔다. 하늘에서 떨어지는 Drop object 의 범위
    /// 고리가 쓴다.
    ///
    /// 높이: 고리를 root 밑에 그냥 두면 낙하하는 동안 본체와 함께 공중에 뜬다. 이 component 가
    /// 있으면 떨어질 자리의 땅에 먼저 그려진다. x 와 z 는 부모를 그대로 따른다.
    ///
    /// 회전: `GroundWarningProjectile` 과 같이 X 축으로 90도 돌려 땅 XZ 평면에 눕힌다.
    /// 45도 카메라에서는 세운 원과 눕힌 원이 같은 타원으로 맺히지만(<c>.agents/docs/scene-space.md</c>),
    /// 원근 때문에 세운 고리는 위쪽이 더 커 보여 서 있는 판처럼 읽힌다. 고리 그림도 위에서 내려다본
    /// 원으로 그렸다. 같은 transform 의 <c>IdleAuraEffect</c> 가 localRotation 을 매 프레임 덮어쓰므로
    /// 그쪽의 <c>rotationAngle</c> 은 0 으로 둔다.
    ///
    /// <see cref="PositionUpdater"/> 가 매 갱신마다 root 를 옮기므로, 그 뒤인 LateUpdate 에서 매
    /// 프레임 다시 맞춘다. magic preview 처럼 <see cref="PresentationWorld"/> 안에 있으면 그
    /// world 의 땅과 회전을 기준으로 한다.
    /// </summary>
    public class GroundPinnedChild : MonoBehaviour
    {
        private static readonly Quaternion FlatOnGround = Quaternion.Euler(90f, 0f, 0f);

        [SerializeField] private float groundY = 0.02f;

        private PresentationWorld world;

        private void Start()
        {
            world = PresentationWorld.For(this);
        }

        private void LateUpdate()
        {
            Transform parent = transform.parent;
            if (parent == null)
            {
                return;
            }

            if (world == null)
            {
                Vector3 parentPosition = parent.position;
                transform.position = new Vector3(parentPosition.x, groundY, parentPosition.z);
                transform.rotation = FlatOnGround;
                return;
            }

            Transform worldTransform = world.transform;
            Vector3 local = worldTransform.InverseTransformPoint(parent.position);
            local.y = groundY;
            transform.position = worldTransform.TransformPoint(local);
            transform.rotation = worldTransform.rotation * FlatOnGround;
        }
    }
}
