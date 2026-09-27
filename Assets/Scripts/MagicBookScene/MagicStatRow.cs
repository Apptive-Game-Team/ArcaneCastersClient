using TMPro;
using UnityEngine;

namespace MagicBookScene
{
    /// <summary>
    /// 도감 카드의 능력치 한 줄. 왼쪽에 이름, 오른쪽에 값을 쓴다.
    /// </summary>
    public class MagicStatRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text value;

        public void Set(string labelText, string valueText)
        {
            if (label != null)
            {
                label.text = labelText;
            }

            if (value != null)
            {
                value.text = valueText;
            }
        }
    }
}
