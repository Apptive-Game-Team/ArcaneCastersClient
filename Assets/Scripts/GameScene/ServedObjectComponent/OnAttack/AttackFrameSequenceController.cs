using DG.Tweening;
using UnityEngine;
using Sequence = DG.Tweening.Sequence;

namespace GameScene.ServedObjectComponent.OnAttack
{
    /// <summary>
    /// Plays a short frame sequence on the attack event, then returns to the idle frame.
    /// <para>
    /// <see cref="AttackSpriteSwapController"/> covers attacks that read as one held pose. This
    /// covers attacks that have to grow: the lightning cloud swaps through frames where the bolt
    /// reaches further down below it on every frame. All frames are drawn on the idle frame's
    /// canvas with the same pivot, so only <see cref="SpriteRenderer.sprite"/> changes.
    /// </para>
    /// Leave <see cref="frames"/> empty on prefabs that have no sequence — the component then
    /// costs one length check at bind time and never subscribes.
    /// </summary>
    public sealed class AttackFrameSequenceController : ServedObjectBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float frameInterval = 0.05f;

        private SpriteRenderer resolvedRenderer;
        private Sprite idleSprite;
        private Sequence sequence;

        protected override void OnBound()
        {
            if (frames == null || frames.Length == 0 || frameInterval <= 0f)
            {
                return;
            }

            resolvedRenderer = ResolveRenderer(targetRenderer);
            if (resolvedRenderer == null)
            {
                return;
            }

            idleSprite = resolvedRenderer.sprite;
            Owner.OnAttack += Play;
        }

        protected override void OnUnbound()
        {
            if (Owner != null)
            {
                Owner.OnAttack -= Play;
            }

            StopAndRestore();
        }

        private void OnDisable()
        {
            StopAndRestore();
        }

        /// <summary>Plays the sequence. Safe to call again while a previous one still runs.</summary>
        private void Play() => Play(frameInterval);

        internal void PlayForDuration(float duration)
        {
            if (!isActiveAndEnabled || resolvedRenderer == null || !(duration > 0f) || float.IsInfinity(duration))
            {
                return;
            }

            Play(duration / frames.Length);
            ApplyFrame(frames[0]);
        }

        private void Play(float interval)
        {
            if (resolvedRenderer == null)
            {
                return;
            }

            StopAndRestore();

            Sequence playing = DOTween.Sequence();
            sequence = playing;
            playing.SetLink(gameObject);
            foreach (Sprite frame in frames)
            {
                playing.AppendCallback(() => ApplyFrame(frame));
                playing.AppendInterval(interval);
            }

            playing
                .AppendCallback(RestoreIdle)
                .OnComplete(() => ClearSequence(playing))
                .OnKill(() =>
                {
                    ClearSequence(playing);
                    RestoreIdle();
                });
        }

        private void ApplyFrame(Sprite frame)
        {
            if (resolvedRenderer != null && frame != null)
            {
                resolvedRenderer.sprite = frame;
            }
        }

        private void ClearSequence(Sequence finished)
        {
            if (sequence == finished)
            {
                sequence = null;
            }
        }

        private void StopAndRestore()
        {
            Sequence active = sequence;
            sequence = null;
            if (active != null && active.IsActive())
            {
                active.Kill(false);
            }

            RestoreIdle();
        }

        private void RestoreIdle()
        {
            if (resolvedRenderer != null && idleSprite != null)
            {
                resolvedRenderer.sprite = idleSprite;
            }
        }
    }
}
