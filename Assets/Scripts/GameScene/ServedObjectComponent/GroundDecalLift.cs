using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    /// <summary>
    /// 바닥에 눕힌 sprite 를 땅 mesh 위로 조금 띄운다. 서버가 보내는 높이는 0 이라 그대로 두면
    /// 땅 mesh(y = 0)와 같은 깊이에서 깜빡인다. 강 맵의 물과 다리 칸이 쓴다.
    ///
    /// 한 번만 올리면 부족하다. 서버는 오브젝트를 만든 직후 y = 0 인 위치 갱신을 보내고
    /// <see cref="PositionUpdater"/> 가 그 위치로 transform 을 옮겨 올려 둔 높이를 지운다. 그래서
    /// 갱신이 모두 끝난 뒤인 LateUpdate 에서 매 프레임 최소 높이를 보장한다.
    /// </summary>
    public class GroundDecalLift : MonoBehaviour
    {
        [SerializeField] private float height = 0.02f;

        private void LateUpdate()
        {
            Vector3 position = transform.position;
            if (position.y >= height)
            {
                return;
            }

            position.y = height;
            transform.position = position;
        }
    }
}
