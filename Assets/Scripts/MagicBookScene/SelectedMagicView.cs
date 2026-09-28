using Data.Magic;
using UnityEngine;
using UnityEngine.UI;

namespace MagicBookScene
{
    /// <summary>
    /// 오른쪽 마법 카드 맨 위의 큰 아이콘 칸. 고른 마법의 그림을 보여준다.
    /// 마법을 고르기 전에는 sprite 가 없는 Image 가 흰 사각형을 그리지 않도록 꺼 둔다.
    /// </summary>
    public class SelectedMagicView : MonoBehaviour
    {
        [SerializeField] private Image magicImage;

        public void Show(CombinedMagicData data)
        {
            if (data == null || magicImage == null)
            {
                return;
            }

            Sprite sprite = data.GetSprite();
            magicImage.sprite = sprite;
            magicImage.enabled = sprite != null;
        }
    }
}
