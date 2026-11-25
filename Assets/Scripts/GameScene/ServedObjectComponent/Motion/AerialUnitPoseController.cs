using System;
using UnityEngine;

namespace GameScene.ServedObjectComponent.Motion
{
    public sealed class AerialUnitPoseController : ServedObjectBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite[] hoverFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] releaseFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] approachFrames = Array.Empty<Sprite>();

        private const float HoverFrameInterval = 0.17f;
        private const float ReleaseFrameInterval = 0.08f;
        private const float MinimumApproachStep = 0.00025f;
        private const float PositionCorrectionStep = 0.15f;
        private const float MinimumApproachGrace = 0.08f;

        private SpriteRenderer body;
        private Sprite fallbackSprite;
        private float releaseStartedAt;
        private float releaseEndsAt = float.NegativeInfinity;
        private float hoverStartedAt;
        private Vector2 lastHorizontalPosition;
        private bool hasHorizontalPosition;
        private Vector2 previousApproachStep;
        private float previousApproachStepAt;
        private float approachStartedAt;
        private float approachUntil = float.NegativeInfinity;

        protected override void OnBound()
        {
            body = ResolveRenderer(targetRenderer);
            if (body == null)
            {
                return;
            }

            fallbackSprite = body.sprite;
            Owner.OnAttack += PlayRelease;
            if (approachFrames.Length > 0)
            {
                Owner.OnServerPositionUpdated += TrackApproach;
            }

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
                Owner.OnServerPositionUpdated -= TrackApproach;
            }
        }

        private void OnEnable()
        {
            hoverStartedAt = Time.time;
            releaseEndsAt = float.NegativeInfinity;
            hasHorizontalPosition = false;
        }

        private void OnDisable()
        {
            previousApproachStep = Vector2.zero;
            approachUntil = float.NegativeInfinity;
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
            releaseEndsAt = releaseStartedAt + ReleaseFrameInterval * releaseFrames.Length;
            hoverStartedAt = releaseEndsAt;
            Show(releaseFrames[0]);
        }

        private void TrackApproach(Vector3 serverPosition)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            var horizontalPosition = new Vector2(serverPosition.x, serverPosition.z);

            // 첫 서버 좌표를 기준으로 삼아 보간과 렌더링 좌표의 오차를 이동량에서 제외한다.
            if (!hasHorizontalPosition)
            {
                lastHorizontalPosition = horizontalPosition;
                hasHorizontalPosition = true;
                return;
            }

            Vector2 step = horizontalPosition - lastHorizontalPosition;
            lastHorizontalPosition = horizontalPosition;
            float stepSquared = step.sqrMagnitude;
            float now = Time.time;

            if (stepSquared > PositionCorrectionStep * PositionCorrectionStep)
            {
                approachUntil = Mathf.Min(approachUntil, now);
                previousApproachStep = Vector2.zero;
                return;
            }

            if (stepSquared < MinimumApproachStep * MinimumApproachStep)
            {
                previousApproachStep = Vector2.zero;
                return;
            }

            float recordedTick = Owner.PresentationWorld != null ? Owner.PresentationWorld.RecordedFrameDuration : 0f;
            float tick = recordedTick > 0f ? recordedTick : GameConfig.FrameDuration;
            float grace = Mathf.Max(MinimumApproachGrace, 2f * tick);
            // 자식 소환처럼 한 틱만 밀리는 경우는 접근으로 표시하지 않는다.
            if (now - previousApproachStepAt <= grace && Vector2.Dot(previousApproachStep, step) > 0f)
            {
                if (now >= approachUntil)
                {
                    approachStartedAt = now;
                }

                approachUntil = now + grace;
            }

            previousApproachStep = step;
            previousApproachStepAt = now;
        }

        private void LateUpdate()
        {
            if (body == null || Owner == null || Owner.IsDespawning)
            {
                return;
            }

            float now = Time.time;
            if (now < releaseEndsAt)
            {
                int frame = Mathf.Min((int)((now - releaseStartedAt) / ReleaseFrameInterval), releaseFrames.Length - 1);
                Show(releaseFrames[frame]);
                return;
            }

            if (now < approachUntil)
            {
                ShowLoop(approachFrames, now - approachStartedAt);
                return;
            }

            if (approachUntil > float.NegativeInfinity)
            {
                approachUntil = float.NegativeInfinity;
                hoverStartedAt = now;
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

            ShowLoop(hoverFrames, now - hoverStartedAt);
        }

        private void ShowLoop(Sprite[] frames, float elapsed)
        {
            int frame = (int)(elapsed / HoverFrameInterval) % frames.Length;
            Show(frames[frame]);
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
