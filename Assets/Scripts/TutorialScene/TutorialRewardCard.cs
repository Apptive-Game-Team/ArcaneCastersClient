using Data.Magic;
using GameScene.Card;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TutorialScene
{
    public sealed class TutorialRewardCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private string magicName;
        private ZoomedCardUI details;
        private RectTransform Anchor => transform as RectTransform;

        public void Bind(string name, ZoomedCardUI popup)
        {
            details?.HideFor(Anchor);
            magicName = name;
            details = popup;
        }

        private void Show()
        {
            if (isActiveAndEnabled && details != null && LocalCombinedMagicData.TryGetByName(magicName, out var magic))
                details.ShowMagic(magic, Anchor);
        }

        public void OnPointerEnter(PointerEventData data)
        {
            if (data.pointerId < 0) Show();
        }

        public void OnPointerExit(PointerEventData data)
        {
            // 터치가 끝난 뒤에도 탭한 카드의 설명은 유지한다.
            if (data.pointerId < 0) details?.HideFor(Anchor);
        }

        public void OnPointerClick(PointerEventData data) => Show();
        private void OnDisable() => details?.HideFor(Anchor);
    }
}
