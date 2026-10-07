using GameScene.Object;
using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    /// <summary>
    /// 강 맵의 칸 하나가 그릴 그림을 긴 띠 그림에서 잘라 쓴다. 띠 그림은 가로 2칸, 세로 10칸이고
    /// 서버가 보내는 강의 칸(열 8 과 9, 행 0 부터 9)과 같은 격자에 맞춰 그렸다. 칸의 중심이
    /// <c>(열 + 0.5, 0, 행 + 0.5)</c> 이므로 생성된 위치에서 열과 행을 구해 그 칸 크기(1 x 1 world unit)의
    /// 조각을 <see cref="Sprite.Create(Texture2D, Rect, Vector2, float)"/> 로 만든다.
    /// 띠 그림 sprite 의 pixels per unit 이 곧 한 칸의 pixel 수이다.
    /// 위치는 <see cref="Awake"/> 에서 읽는다. 생성할 때 위치를 함께 넘기므로 그때는 이미 정해져 있다.
    /// </summary>
    public class RiverCellArt : MonoBehaviour
    {
        /// <summary>서버가 강으로 보내는 첫 열. 서버의 river map 정의와 같아야 한다.</summary>
        public const int FirstColumn = 8;
        public const int ColumnCount = 2;
        public const int RowCount = 10;

        [SerializeField] private SpriteRenderer target;
        [SerializeField] private Sprite strip;

        private Sprite cellSprite;

        private void Awake()
        {
            if (target == null || strip == null)
            {
                return;
            }

            Vector3 position = transform.position;
            PresentationWorld world = PresentationWorld.For(this);
            if (world != null)
            {
                position = world.transform.InverseTransformPoint(position);
            }

            int column = Mathf.Clamp(Mathf.FloorToInt(position.x) - FirstColumn, 0, ColumnCount - 1);
            int row = Mathf.Clamp(Mathf.FloorToInt(position.z), 0, RowCount - 1);

            float cellPixels = strip.pixelsPerUnit;
            Rect stripRect = strip.rect;
            Rect cellRect = new Rect(stripRect.x + column * cellPixels, stripRect.y + row * cellPixels, cellPixels, cellPixels);
            cellSprite = Sprite.Create(strip.texture, cellRect, new Vector2(0.5f, 0.5f), cellPixels, 0, SpriteMeshType.FullRect);
            cellSprite.name = $"{strip.name}_{column}_{row}";
            target.sprite = cellSprite;
        }

        private void OnDestroy()
        {
            if (cellSprite != null)
            {
                Destroy(cellSprite);
            }
        }
    }
}
