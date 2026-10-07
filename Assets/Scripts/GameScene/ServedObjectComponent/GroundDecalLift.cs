using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    /// <summary>
    /// 바닥에 눕힌 sprite 를 땅 mesh 위로 조금 띄운다. 서버가 보내는 높이는 0 이라 그대로 두면
    /// 땅 mesh(y = 0)와 같은 깊이에서 깜빡인다. 강 맵의 물과 다리 칸이 쓴다.
    /// </summary>
    public class GroundDecalLift : MonoBehaviour
    {
        [SerializeField] private float height = 0.02f;

        private void Start()
        {
            Vector3 position = transform.position;
            position.y += height;
            transform.position = position;
        }
    }
}
