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

        [Tooltip("속성표 탭과 함께 올릴 탭 막대 조각. 아래에 깔 것부터 둔다.")]
        [SerializeField] private RectTransform[] elementChartTabBar;
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
        /// 고른 마법의 그림과 설명은 오른쪽 카드에 다 있으므로 카드만 마스크 위로 올린다.
        /// 왼쪽 페이지는 이제 마법 칸 목록이라, 올리면 이 단계에서도 칸이 눌린다. 대화창은
        /// 왼쪽 위에 떠서 다음 버튼이 목록 윗줄에 겹치고, 다른 마법을 더 눌러 보던 사용자가
        /// 칸 대신 다음 버튼을 눌러 설명을 읽기 전에 속성표 단계로 넘어갔다.
        /// </summary>
        public void ShowMagicInfo(System.Action onNext)
        {
            Show(
                "onboarding.magicBook.readMagicInfo",
                magicDescriptionScrollArea != null ? magicDescriptionScrollArea.transform : null,
                onNext);
        }

        public void ShowElementChart(System.Action onNext)
        {
            Transform target = elementChartButton != null ? elementChartButton.transform : null;
            if (target == null && elementChartButtonBase != null)
            {
                target = elementChartButtonBase.transform;
            }

            Show("onboarding.magicBook.elementChart", WithTabBar(target), onNext);
        }

        /// <summary>
        /// 속성표 탭 버튼은 투명하고 글자만 있다. 흰 알약 배경(TabSegment)과 옆 도감 탭은
        /// 따로 떨어진 형제라, 버튼만 올리면 글자만 마스크 위에 뜨고 탭은 어둡게 남는다.
        /// 탭 막대 조각을 먼저 깔고 버튼을 맨 위에 올린다.
        /// </summary>
        private Transform[] WithTabBar(Transform tabButton)
        {
            var targets = new List<Transform>();
            if (elementChartTabBar != null)
            {
                foreach (RectTransform piece in elementChartTabBar)
                {
                    if (piece != null)
                    {
                        targets.Add(piece);
                    }
                }
            }

            if (tabButton != null)
            {
                targets.Add(tabButton);
            }

            return targets.ToArray();
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
