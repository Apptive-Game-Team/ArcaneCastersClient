using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LobbyScene
{
    /// <summary>
    /// Battle 버튼에 마우스를 올리면 로비의 플레이어가 지팡이를 앞으로 내미는 자세로 바꾸고, 플레이어와
    /// 곁의 소환수가 제자리에서 통통 뛴다. 마우스가 떠나면 원래 자세와 자리로 돌아온다.
    /// 터치 화면에는 hover 가 없어서 누르는 동안에만 보인다.
    /// </summary>
    public class BattleHoverPresenter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image playerImage;
        [SerializeField] private Sprite readyPoseSprite;
        [SerializeField] private RectTransform[] bouncers;
        [SerializeField] private float bounceHeight = 12f;
        [SerializeField] private float bounceDuration = 0.28f;

        private readonly Dictionary<RectTransform, Vector2> restPositions = new();
        private readonly List<Tween> tweens = new();
        private Sprite restPoseSprite;
        private bool hovering;

        private void Awake()
        {
            if (playerImage != null)
            {
                restPoseSprite = playerImage.sprite;
            }

            foreach (RectTransform bouncer in bouncers ?? System.Array.Empty<RectTransform>())
            {
                if (bouncer != null)
                {
                    restPositions[bouncer] = bouncer.anchoredPosition;
                }
            }
        }

        private void OnDisable()
        {
            StopHover();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hovering)
            {
                return;
            }

            hovering = true;
            if (playerImage != null && readyPoseSprite != null)
            {
                playerImage.sprite = readyPoseSprite;
            }

            int index = 0;
            foreach ((RectTransform bouncer, Vector2 rest) in restPositions)
            {
                // 모두 같은 박자로 뛰면 딱딱해 보여 하나씩 조금 늦게 뛴다.
                Tween tween = bouncer
                    .DOAnchorPosY(rest.y + bounceHeight, bounceDuration)
                    .SetEase(Ease.OutQuad)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetDelay(index * bounceDuration * 0.35f);
                tweens.Add(tween);
                index++;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StopHover();
        }

        private void StopHover()
        {
            if (!hovering)
            {
                return;
            }

            hovering = false;
            foreach (Tween tween in tweens)
            {
                tween?.Kill();
            }

            tweens.Clear();

            foreach ((RectTransform bouncer, Vector2 rest) in restPositions)
            {
                if (bouncer != null)
                {
                    bouncer.anchoredPosition = rest;
                }
            }

            if (playerImage != null && restPoseSprite != null)
            {
                playerImage.sprite = restPoseSprite;
            }
        }
    }
}
