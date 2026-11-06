using Global;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace GameScene.UI
{
    public sealed class CardHoverPauseToggle : Selectable, IPointerClickHandler, ISubmitHandler
    {
        [SerializeField] private RectTransform knob;
        [SerializeField] private Image track;
        [SerializeField] private GameObject tooltip;
        [SerializeField] private TMP_Text tooltipText;

        protected override void Start()
        {
            base.Start();
            gameObject.SetActive(SceneContext.MatchInfo?.SupportsCardHoverPause == true);
            Refresh();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) TogglePause();
        }

        public void OnSubmit(BaseEventData eventData) => TogglePause();

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            ShowTooltip();
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            tooltip.SetActive(false);
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            ShowTooltip();
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            tooltip.SetActive(false);
        }

        protected override void OnDisable()
        {
            tooltip.SetActive(false);
            base.OnDisable();
        }

        private void ShowTooltip()
        {
            bool korean = LocalizationSettings.SelectedLocale?.Identifier.Code.StartsWith("ko") == true;
            tooltipText.text = korean
                ? "켜면 카드 설명을 보는 동안 전투가 멈춥니다."
                : "Turn on to pause the battle while viewing a card description.";
            tooltip.SetActive(true);
        }

        private void TogglePause()
        {
            var zoom = Card.CardUIZoom.Instance;
            if (!IsInteractable() || zoom == null) return;
            zoom.SetAutoPause(!zoom.AutoPause);
            Refresh();
        }

        private void Refresh()
        {
            bool enabled = Card.CardUIZoom.Instance?.AutoPause ?? true;
            knob.anchoredPosition = new Vector2(enabled ? 20f : -20f, 0f);
            track.color = enabled ? new Color32(102, 79, 240, 255) : new Color32(91, 98, 117, 255);
        }
    }
}
