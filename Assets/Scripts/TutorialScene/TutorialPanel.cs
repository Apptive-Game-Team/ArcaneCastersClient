using Data.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TutorialScene
{
    public enum TutorialPanelSide
    {
        Left,
        Right
    }

    public class TutorialPanel : MonoBehaviour
    {
        private const string OnboardingTable = "Onboarding";

        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text message;
        [SerializeField] private Button nextButton;
        private Vector2 leftAnchoredPosition = new Vector2(480, -360);
        private Vector2 rightAnchoredPosition = new Vector2(-480, -360);

        private System.Action nextAction;

        public void Show(string messageKey, System.Action onNext, TutorialPanelSide side = TutorialPanelSide.Left)
        {
            ResolveChildren();
            Root.SetActive(true);
            Root.transform.SetAsLastSibling();
            SetSide(side);

            SetMessage(messageKey);
            BindNext(onNext);
        }

        public void Hide()
        {
            Root.SetActive(false);

            BindNext(null);
        }

        private async void SetMessage(string key)
        {
            if (message == null)
            {
                return;
            }

            string localized = await LocaleUtils.GetStringAsync(OnboardingTable, key);
            message.text = string.IsNullOrWhiteSpace(localized) ? key : localized;
        }

        private void BindNext(System.Action onNext)
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(OnNextClicked);
                nextButton.gameObject.SetActive(onNext != null);
            }

            nextAction = onNext;

            if (nextButton != null && nextAction != null)
            {
                nextButton.onClick.AddListener(OnNextClicked);
            }
        }

        // 콜백을 꺼내고 비운 뒤에 부른다. 한 스텝은 한 번만 진행해야 하는데, 진행 처리 도중에
        // 패널이 아직 살아 있으면 두 번째 클릭이 같은 콜백을 다시 실행한다. 온보딩 마지막
        // 스텝에서는 그 두 번째 실행이 이미 파괴된 GlobalTutorialManager를 건드렸다.
        private void OnNextClicked()
        {
            System.Action action = nextAction;
            nextAction = null;
            action?.Invoke();
        }

        private void ResolveChildren()
        {
            if (message == null)
            {
                message = Root.GetComponentInChildren<TMP_Text>(true);
            }

            if (nextButton == null)
            {
                nextButton = Root.GetComponentInChildren<Button>(true);
            }
        }

        private void SetSide(TutorialPanelSide side)
        {
            RectTransform rectTransform = Root.transform as RectTransform;
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = side == TutorialPanelSide.Right
                ? Vector2.one
                : new Vector2(0, 1);
            
            rectTransform.anchorMax = side == TutorialPanelSide.Right
                ? Vector2.one
                : new Vector2(0, 1);

            rectTransform.anchoredPosition = side == TutorialPanelSide.Right
                ? rightAnchoredPosition
                : leftAnchoredPosition;
        }

        private GameObject Root => root != null ? root : gameObject;

        /// <summary>
        /// 좌우 고정 위치가 맞지 않는 화면에서 배치를 직접 잡을 수 있게 연다.
        /// 훈수 힌트는 화면을 가리면 안 되므로 Show 뒤에 이 값을 덮어쓴다.
        /// </summary>
        public RectTransform RootRectTransform => Root.transform as RectTransform;

        /// <summary>다음 버튼. 다음 버튼으로 넘어가는 단계에서 손가락이 이것을 가리킨다.</summary>
        public Transform NextButtonTransform
        {
            get
            {
                ResolveChildren();
                return nextButton != null ? nextButton.transform : null;
            }
        }
    }
}
