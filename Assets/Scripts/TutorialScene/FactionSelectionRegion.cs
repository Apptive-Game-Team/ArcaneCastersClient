using UnityEngine;
using UnityEngine.EventSystems;

namespace TutorialScene
{
    public sealed class FactionSelectionRegion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler
    {
        [SerializeField] private FactionSelectionView view;
        [SerializeField] private int faction;
        public void OnPointerEnter(PointerEventData data) => view.Hover(faction);
        public void OnPointerExit(PointerEventData data) => view.ClearHover(faction);
        public void OnSelect(BaseEventData data) => view.Focus(faction);
        public void OnDeselect(BaseEventData data) => view.ClearFocus(faction);
    }
}
