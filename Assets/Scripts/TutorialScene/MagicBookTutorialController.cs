using System.Collections.Generic;
using Global.Button;
using MagicBookScene;
using UnityEngine;
using UnityEngine.UI;

namespace TutorialScene
{
    public class MagicBookTutorialController : SceneTutorialController<MagicBookTutorialController>
    {
        [Header("Magic Book Targets")]
        [SerializeField] private MagicInfoFactory magicInfoFactory;
        [SerializeField] private GameObject magicBookArea;
        [SerializeField] private GameObject magicPageArea;
        [SerializeField] private GameObject magicDescriptionScrollArea;
        [SerializeField] private GameObject elementChartArea;
        [SerializeField] private Button elementChartButton;
        [SerializeField] private ButtonBase elementChartButtonBase;
        [SerializeField] private ButtonBase returnToLobbyButton;

        public event System.Action MagicSelected;
        public event System.Action ElementChartOpened;
        public event System.Action ReturnToLobbySelected;

        private bool isSubscribedToMagicBookScene;

        private void OnEnable()
        {
            // This controller lives in the regular magic book scene, so it must stay inert
            // unless the magic book onboarding step is the one currently running.
            if (!GlobalTutorialManager.IsMagicBookOnboardingActive())
            {
                return;
            }

            if (magicInfoFactory == null)
            {
                magicInfoFactory = GetComponent<MagicInfoFactory>();
            }

            if (magicInfoFactory != null)
            {
                magicInfoFactory.MagicSelected += NotifyMagicSelected;
            }

            if (elementChartButton != null)
            {
                elementChartButton.onClick.AddListener(NotifyElementChartOpened);
            }

            if (elementChartButtonBase != null)
            {
                elementChartButtonBase.OnClick += NotifyElementChartOpened;
            }

            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.OnClick += NotifyReturnToLobbySelected;
            }

            isSubscribedToMagicBookScene = true;
        }

        private void OnDisable()
        {
            if (!isSubscribedToMagicBookScene)
            {
                return;
            }

            isSubscribedToMagicBookScene = false;

            if (magicInfoFactory != null)
            {
                magicInfoFactory.MagicSelected -= NotifyMagicSelected;
            }

            if (elementChartButton != null)
            {
                elementChartButton.onClick.RemoveListener(NotifyElementChartOpened);
            }

            if (elementChartButtonBase != null)
            {
                elementChartButtonBase.OnClick -= NotifyElementChartOpened;
            }

            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.OnClick -= NotifyReturnToLobbySelected;
            }
        }

        public void ShowMagicBook(System.Action onNext)
        {
            Show("onboarding.magicBook.description", magicBookArea != null ? magicBookArea.transform : null, onNext);
        }

        public void ShowMagicSelection()
        {
            Show("onboarding.magicBook.selectAnyMagic", magicPageArea != null ? magicPageArea.transform : null);
        }

        /// <summary>
        /// 마법을 고르면 왼쪽 페이지가 목록 대신 그 마법의 그림을 보여준다. 안내는 짚는 것을
        /// 마스크 위로 올리므로, 왼쪽 페이지도 함께 넘기지 않으면 그림이 마스크에 덮인다.
        /// </summary>
        public void ShowMagicInfo(System.Action onNext)
        {
            var targets = new List<Transform>();
            if (magicPageArea != null)
            {
                targets.Add(magicPageArea.transform);
            }

            if (magicDescriptionScrollArea != null)
            {
                targets.Add(magicDescriptionScrollArea.transform);
            }

            Show("onboarding.magicBook.readMagicInfo", targets.ToArray(), onNext);
        }

        public void ShowElementChart(System.Action onNext)
        {
            Transform target = elementChartButton != null ? elementChartButton.transform : null;
            if (target == null && elementChartButtonBase != null)
            {
                target = elementChartButtonBase.transform;
            }

            Show("onboarding.magicBook.elementChart", target, onNext);
        }

        public void ShowOpenedElementChart(System.Action onNext)
        {
            Show("onboarding.magicBook.chartDescription", elementChartArea != null ? elementChartArea.transform : null, onNext);
        }

        public void ShowReturnToLobby()
        {
            Show(
                "onboarding.magicBook.returnToLobby",
                returnToLobbyButton != null ? returnToLobbyButton.transform : null,
                TutorialPanelSide.Right);
        }

        private void NotifyMagicSelected()
        {
            MagicSelected?.Invoke();
        }

        private void NotifyElementChartOpened()
        {
            ElementChartOpened?.Invoke();
        }

        private void NotifyReturnToLobbySelected()
        {
            ReturnToLobbySelected?.Invoke();
        }
    }
}
