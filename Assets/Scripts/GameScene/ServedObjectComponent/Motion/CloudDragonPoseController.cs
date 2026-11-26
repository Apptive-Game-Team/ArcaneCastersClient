using System;
using UnityEngine;

namespace GameScene.ServedObjectComponent.Motion
{
    public sealed class CloudDragonPoseController : ServedObjectBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite[] hoverFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] releaseFrames = Array.Empty<Sprite>();

        private const float HoverFrameInterval = 0.17f;
        private const float ReleaseFrameInterval = 0.08f;

        private SpriteRenderer body;
        private Sprite fallbackSprite;
        private float releaseStartedAt;
        private float hoverStartedAt;

        protected override void OnBound()
        {
            body = ResolveRenderer(targetRenderer);
            if (body == null)
            {
                return;
            }

            fallbackSprite = body.sprite;
            Owner.OnAttack += PlayRelease;

            if (isActiveAndEnabled)
            {
                ShowHover(Time.time);
            }
        }

        protected override void OnUnbound()
        {
            if (Owner != null)
            {
                Owner.OnAttack -= PlayRelease;
            }
        }

        private void OnEnable() => hoverStartedAt = Time.time;

        private void OnDisable()
        {
            if (body != null && Owner != null && !Owner.IsDespawning)
            {
                Show(fallbackSprite);
            }
        }

        private void PlayRelease()
        {
            if (!isActiveAndEnabled || Owner.IsDespawning || releaseFrames.Length == 0)
            {
                return;
            }

            releaseStartedAt = Time.time;
            hoverStartedAt = releaseStartedAt + ReleaseFrameInterval * releaseFrames.Length;
            Show(releaseFrames[0]);
        }

        private void LateUpdate()
        {
            if (body == null || Owner == null || Owner.IsDespawning)
            {
                return;
            }

            float now = Time.time;
            if (now < hoverStartedAt)
            {
                int frame = Mathf.Min((int)((now - releaseStartedAt) / ReleaseFrameInterval), releaseFrames.Length - 1);
                Show(releaseFrames[frame]);
                return;
            }

            ShowHover(now);
        }

        private void ShowHover(float now)
        {
            if (hoverFrames.Length == 0)
            {
                Show(fallbackSprite);
                return;
            }

            int frame = (int)((now - hoverStartedAt) / HoverFrameInterval) % hoverFrames.Length;
            Show(hoverFrames[frame]);
        }

        private void Show(Sprite sprite)
        {
            Sprite next = sprite != null ? sprite : fallbackSprite;
            if (body.sprite != next)
            {
                body.sprite = next;
            }
        }
    }
}
