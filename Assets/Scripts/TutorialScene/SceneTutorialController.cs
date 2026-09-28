using System;
using Global;
using UnityEngine;

namespace TutorialScene
{
    public abstract class SceneTutorialController<T> : LocalSingletonObject<T> where T : SceneTutorialController<T>
    {
        [SerializeField] private GameObject mask;
        [SerializeField] private GameObject targetClickBlocker;
        [SerializeField] private TutorialPanel panel;

        private readonly TutorialSortingLift lift = new TutorialSortingLift();

        protected override void Awake()
        {
            base.Awake();
            Hide();
        }

        protected void Show(string messageKey, Transform target, Action onNext = null)
        {
            Show(messageKey, target != null ? new[] { target } : null, onNext);
        }

        protected void Show(string messageKey, Transform target, Action onNext, bool blockTargetClick)
        {
            Show(messageKey, target != null ? new[] { target } : null, onNext, TutorialPanelSide.Left, blockTargetClick);
        }

        protected void Show(string messageKey, Transform target, TutorialPanelSide panelSide, Action onNext = null)
        {
            Show(messageKey, target != null ? new[] { target } : null, onNext, panelSide);
        }

        protected void Show(string messageKey, Transform[] targets, Action onNext = null)
        {
            Show(messageKey, targets, onNext, TutorialPanelSide.Left);
        }

        protected void Show(string messageKey, Transform[] targets, Action onNext, TutorialPanelSide panelSide, bool blockTargetClick = false)
        {
            lift.RestoreAll();

            if (mask != null)
            {
                mask.SetActive(true);
                mask.transform.SetAsLastSibling();
            }

            if (targetClickBlocker != null)
            {
                targetClickBlocker.SetActive(blockTargetClick);
            }

            panel?.Show(messageKey, onNext, panelSide);

            LiftAboveMask(targets, blockTargetClick);
        }

        /// <summary>
        /// 마스크가 그려지는 Canvas 순서를 기준으로 짚는 대상, 클릭 차단막, 안내 패널을
        /// 차례로 한 칸씩 위에 올린다. 대상끼리도 한 칸씩 올려 뒤에 준 대상이 앞 대상 위에
        /// 그려진다. 순서가 같은 Canvas 끼리는 무엇이 위인지 보장되지 않으므로, 버튼 뒤의
        /// 배경처럼 겹치는 대상은 아래에 깔 것을 먼저 준다. 차단막은 대상을 보여 주되
        /// 누르지 못하게 하려고 대상 바로 위에 두고, 패널의 다음 버튼은 차단막에 막히지
        /// 않도록 가장 위에 둔다.
        /// </summary>
        private void LiftAboveMask(Transform[] targets, bool blockTargetClick)
        {
            Canvas maskCanvas = mask != null ? mask.GetComponentInParent<Canvas>() : null;
            if (maskCanvas == null)
            {
                return;
            }

            int layer = maskCanvas.sortingLayerID;
            int order = maskCanvas.sortingOrder;

            if (targets != null)
            {
                foreach (Transform target in targets)
                {
                    if (target != null)
                    {
                        lift.Lift(target, layer, ++order);
                    }
                }
            }

            if (targetClickBlocker != null && blockTargetClick)
            {
                lift.Lift(targetClickBlocker.transform, layer, ++order);
            }

            if (panel != null && !SortsAbove(panel.RootRectTransform, layer, order))
            {
                lift.Lift(panel.RootRectTransform, layer, order + 1);
            }
        }

        // 패널은 원래 따로 높은 Canvas에 있다. 이미 차단막보다 위라면 그대로 두어, 패널보다
        // 위에 떠야 하는 시스템 메시지나 로딩 화면 아래로 끌어내리지 않는다.
        private static bool SortsAbove(Transform target, int layer, int order)
        {
            Canvas canvas = target != null ? target.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
            {
                return false;
            }

            Canvas sorting = canvas.overrideSorting || canvas.isRootCanvas ? canvas : canvas.rootCanvas;
            return sorting.sortingLayerID == layer && sorting.sortingOrder > order;
        }

        public void Hide()
        {
            lift.RestoreAll();

            if (mask != null)
            {
                mask.SetActive(false);
            }

            if (targetClickBlocker != null)
            {
                targetClickBlocker.SetActive(false);
            }

            panel?.Hide();
        }
    }
}
