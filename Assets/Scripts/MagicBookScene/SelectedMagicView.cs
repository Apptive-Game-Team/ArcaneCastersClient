using Data.Magic;
using Global;
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
        [SerializeField] private FireShotPreview previewPrefab;
        private FireShotPreview preview;

        public void Show(CombinedMagicData data)
        {
            if (preview != null) preview.gameObject.SetActive(false);
            if (data == null || magicImage == null)
            {
                return;
            }

            Sprite sprite = data.GetSprite();
            magicImage.sprite = sprite;
            bool showPreview = previewPrefab != null && FireShotPreview.Supports(data);
            if (showPreview)
            {
                if (preview == null)
                {
                    preview = Instantiate(previewPrefab, magicImage.transform);
                    RectTransform rect = (RectTransform)preview.transform;
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                }
                preview.gameObject.SetActive(true);
            }
            magicImage.enabled = !showPreview && sprite != null;
        }
    }
}
