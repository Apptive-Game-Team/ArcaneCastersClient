using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    /// <summary>
    /// HP 와 Collider gizmo 가 없어도 땅에 놓는 소환을 막는 몸에 붙인다. 고정 바위 장애물이 쓴다.
    /// <see cref="PlacementPreview"/> 만 읽으며, 투사체와 폭발과 낙하는 이 component 를 보지 않는다.
    /// </summary>
    public class PlacementObstacle : MonoBehaviour
    {
        [SerializeField] private float radius = GameScene.Dto.PlacementOverlapRule.RockObstacleRadius;

        public float Radius => radius;
    }
}
