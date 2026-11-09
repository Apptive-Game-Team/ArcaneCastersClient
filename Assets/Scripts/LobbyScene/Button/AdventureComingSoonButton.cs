using Global.Button;
using TutorialScene;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;

namespace LobbyScene.Button
{
    public sealed class AdventureComingSoonButton : ButtonBase, IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler
    {
        private bool hovering;
        private bool selected;
        private TutorialHint hint;

        private void Awake()
        {
            GetComponent<UnityEngine.UI.Button>().interactable = false;
        }

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            if (hint != null)
            {
                hint.HideMessage();
            }

            hovering = false;
            selected = false;
        }

        // 저장된 UnityEvent가 직접 호출해도 모험을 시작하지 않는다.
        public override void ButtonEvent() { }

        protected override void OnClickButton() { }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;
            RefreshHint();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            RefreshHint();
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            RefreshHint();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            RefreshHint();
        }

        private void OnLocaleChanged(UnityEngine.Localization.Locale locale)
        {
            if (hovering || selected)
            {
                RefreshHint();
            }
        }

        private void RefreshHint()
        {
            if (hovering || selected)
            {
                if (hint == null)
                {
                    TutorialHint prefab = Resources.Load<TutorialHint>("UI/Tutorial/TutorialHint");
                    if (prefab == null)
                    {
                        return;
                    }

                    // 튜토리얼 안내를 덮어쓰지 않도록 별도 인스턴스를 사용한다.
                    hint = Instantiate(prefab);
                }

                hint.ShowMessage("LobbyUI", "AdventureComingSoon");
            }
            else if (hint != null)
            {
                hint.HideMessage();
            }
        }

        private void OnDestroy()
        {
            if (hint != null)
            {
                Destroy(hint.gameObject);
            }
        }
    }
}
