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
            moveTween = transform.DOMove(nextPosition.Value, GameConfig.FRAME_DURATION)
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

            if (Math.Abs(lastX - x) > 0.1 * GameConfig.FRAME_DURATION)
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
        
        private const int FLIP_THRESHOLD = 10;
        private int flipCounter = 0;
        
        private void SetFlipX(bool flipX)
        {
            if (spriteRenderer.flipX != flipX)
            {
                flipCounter++;
                if (flipCounter >= FLIP_THRESHOLD)
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
