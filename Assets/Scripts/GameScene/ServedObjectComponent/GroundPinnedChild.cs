using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    /// <summary>
    /// 부모가 공중에 있어도 이 child 를 부모 바로 아래 땅 높이에 둔다. 하늘에서 떨어지는 Drop
    /// object 의 범위 고리가 쓴다. 고리를 root 밑에 그냥 두면 낙하하는 동안 본체와 함께 공중에
    /// 뜨고, 이 component 가 있으면 떨어질 자리의 땅에 먼저 그려진다.
    ///
    /// <see cref="PositionUpdater"/> 가 매 갱신마다 root 를 옮기므로, 그 뒤인 LateUpdate 에서 매
    /// 프레임 높이만 되돌린다. x 와 z 는 부모를 그대로 따른다.
    /// </summary>
    public class GroundPinnedChild : MonoBehaviour
    {
        [SerializeField] private float groundY = 0.02f;

        private void LateUpdate()
        {
            Transform parent = transform.parent;
            if (parent == null)
            {
                return;
            }

            Vector3 parentPosition = parent.position;
            transform.position = new Vector3(parentPosition.x, groundY, parentPosition.z);
        }
    }
}
