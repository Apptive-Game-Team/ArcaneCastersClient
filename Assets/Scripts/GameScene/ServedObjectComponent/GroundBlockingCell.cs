using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    /// <summary>
    /// 땅에 놓는 소환을 막는 정사각형 칸에 붙인다. 강 맵의 물 칸이 쓴다.
    /// <see cref="PlacementPreview"/> 만 읽으며, 투사체와 폭발과 낙하와 공중 소환은 이 component 를 보지 않는다.
    /// </summary>
    public class GroundBlockingCell : MonoBehaviour
    {
        [SerializeField] private float halfSize = GameScene.Dto.PlacementSquareRule.CellHalfSize;

        public float HalfSize => halfSize;
    }
}
