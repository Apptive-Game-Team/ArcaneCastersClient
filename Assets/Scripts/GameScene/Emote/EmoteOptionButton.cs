using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameScene.Emote
{
    /// <summary>
    /// 부채꼴로 펼쳐지는 emote 하나. 누름은 전부 <see cref="EmotePickerUI"/> 가 처리하고,
    /// 이 component 는 어떤 emote 인지와 강조 표시만 맡는다.
    /// </summary>
    public class EmoteOptionButton : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const float HighlightScale = 1.18f;

        [SerializeField] private EmotePickerUI picker;
        [SerializeField] private EmoteType emote;

        private Button button;
        private Vector3 baseScale = Vector3.one;

        public EmoteType Emote => emote;

        private void Awake()
        {
            EnsureButton();
            baseScale = transform.localScale;
        }

        /// <summary>
        /// 아이콘은 판이 닫혀 있는 동안 꺼져 있어 Awake 가 아직 돌지 않았을 수 있다.
        /// 잠금 표시는 그때도 들어오므로 필요한 순간에 다시 찾는다.
        /// </summary>
        private void EnsureButton()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }

        public void SetHighlighted(bool highlighted)
        {
            transform.localScale = highlighted ? baseScale * HighlightScale : baseScale;
        }

        public void SetInteractable(bool interactable)
        {
            EnsureButton();
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (picker != null)
            {
                picker.BeginPress(eventData, false);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (picker != null)
            {
                picker.ContinuePress(eventData);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (picker != null)
            {
                picker.EndPress(eventData, false);
            }
        }
    }
}
