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
        
        // 반대 방향 이동이 이 시간 동안 이어져야 방향을 바꾼다. 갱신 횟수로 세면 tick rate 마다 달라진다.
        private const float FLIP_THRESHOLD_SECONDS = 0.5f;
        private float flipPendingSince = -1f;
        
        private void SetFlipX(bool flipX)
        {
            if (spriteRenderer.flipX != flipX)
            {
                if (flipPendingSince < 0f)
                {
                    flipPendingSince = Time.time;
                }
                if (Time.time - flipPendingSince >= FLIP_THRESHOLD_SECONDS)
                {
                    spriteRenderer.flipX = flipX;
                    flipPendingSince = -1f;
                }
            }
            else
            {
                flipPendingSince = -1f;
            }
        }
    }
}
