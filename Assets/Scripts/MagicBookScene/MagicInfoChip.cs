using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MagicBookScene
{
    /// <summary>
    /// 도감 카드의 원소 칩 하나. 칩 바탕색, 원소 아이콘, 원소 이름을 채운다.
    /// </summary>
    public class MagicInfoChip : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;

        public void Set(Sprite iconSprite, string text, Color color)
        {
            if (background != null)
            {
                background.color = color;
            }

            if (icon != null)
            {
                icon.sprite = iconSprite;
                icon.gameObject.SetActive(iconSprite != null);
            }

            if (label != null)
            {
                label.text = text;
            }
        }
    }
}
