using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using GameScene.Dto;
using GameScene.Object;
using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    public class PositionUpdater
    {
        private readonly Transform transform;
        private readonly SpriteRenderer spriteRenderer;
        
        private TweenerCore<Vector3, Vector3, VectorOptions> moveTween;
        private Vector3? nextPosition = null;
        private double lastX;
        private readonly Action onMoved;
        private readonly Func<PresentationWorld> world;
        
        public PositionUpdater(Transform transform, SpriteRenderer spriteRenderer, Action onMoved = null, Func<PresentationWorld> world = null)
        {
            this.transform = transform;
            this.spriteRenderer = spriteRenderer;
            this.onMoved = onMoved;
            this.world = world;
            lastX = this.transform.position.x;
        }

        internal void UpdatePosition(UpdatedObjectDto updatedObjectDto)
        {
            if (moveTween != null && moveTween.IsActive())
            {
                moveTween.Kill();
            }
            if (nextPosition.HasValue)
            {
                transform.position = nextPosition.Value;
            }
            PresentationWorld presentation = world?.Invoke();
            nextPosition = presentation != null ? presentation.ToWorld(updatedObjectDto.position) : updatedObjectDto.position;
            if ((nextPosition.Value - transform.position).sqrMagnitude > 0.0001f)
            {
                onMoved?.Invoke();
            }
            moveTween = transform.DOMove(nextPosition.Value, GameConfig.FrameDuration)
                .SetEase(Ease.Linear)
                .SetLink(transform.gameObject);
            
            UpdateFlip();
        }

        private void UpdateFlip()
        {
            if (nextPosition?.x == null)
            {
                return;
            }

            double x = (double) nextPosition?.x;

            // 갱신 한 번당 x 이동량 기준이다. 0.1 * FrameDuration 은 초속 0.1 칸이라
            // tick rate 가 20 이든 60 이든 같은 속도에서 방향이 바뀐다.
            if (Math.Abs(lastX - x) > 0.1 * GameConfig.FrameDuration)
            {
                if (lastX < x)
                {
                    SetFlipX(false);
                }
                else if (x < lastX)
                {
                    SetFlipX(true);
                }
            } 

            lastX = x;
        }
        
        // 반대 방향 갱신이 이 시간만큼 연속으로 와야 방향을 바꾼다. 횟수로 세되 기준을 tick rate 에 맞춰
        // 20 FPS 에서는 예전과 같은 10번이 된다. 시각으로 재면 멈췄다가 한 번 튄 갱신에도 바로 뒤집힌다.
        private const float FLIP_THRESHOLD_SECONDS = 0.5f;
        private int flipCounter = 0;

        private void SetFlipX(bool flipX)
        {
            if (spriteRenderer.flipX != flipX)
            {
                flipCounter++;
                if (flipCounter >= Mathf.CeilToInt(FLIP_THRESHOLD_SECONDS * GameConfig.TickRate))
                {
                    spriteRenderer.flipX = flipX;
                    flipCounter = 0;
                }
            }
            else
            {
                flipCounter = 0;
            }
        }
    }
}
