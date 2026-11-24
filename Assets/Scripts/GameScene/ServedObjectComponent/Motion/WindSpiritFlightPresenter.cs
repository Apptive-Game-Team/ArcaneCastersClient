using UnityEngine;

namespace GameScene.ServedObjectComponent.Motion
{
    public sealed class WindSpiritFlightPresenter : ServedObjectBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite[] hoverFrames;
        [SerializeField] private Sprite[] glideFrames;
        [SerializeField] private Sprite[] descentFrames;
        [SerializeField, Min(0.01f)] private float hoverFrameInterval = 0.30f;
        [SerializeField, Min(0.01f)] private float glideFrameInterval = 0.26f;
        [SerializeField, Min(0.01f)] private float minimumDescentSpeed = 0.45f;
        [SerializeField, Min(0f)] private float descentPoseDepth;

        private const float MinimumGlideSpeed = 0.20f;
        private const float MinimumAscentSpeed = 0.25f;
        private const float MaximumPositionGap = 0.30f;
        // SelfDestructMob의 차원문 바람 정령이 자폭하는 높이다. 자세의 진행 구간으로 쓴다.
        private const float DescentEndHeight = 1f;

        private enum FlightPose { Hover, Glide, Descent }

        private SpriteRenderer resolvedRenderer;
        private Sprite defaultSprite;
        private FlightPose pose;
        private float poseStartedAt;

        private bool hasPosition;
        private bool wasMovingAcrossGround;
        private bool wasDescending;
        private Vector3 previousPosition;
        private float positionReceivedAt;
        private float glideUntil = float.NegativeInfinity;
        private float descentUntil = float.NegativeInfinity;
        private float descentStartHeight;
        private float lowestDescentHeight;

        protected override void OnBound()
        {
            resolvedRenderer = ResolveRenderer(targetRenderer);
            if (resolvedRenderer == null)
            {
                return;
            }

            defaultSprite = resolvedRenderer.sprite;
            Owner.OnServerPositionUpdated += OnPosition;
            poseStartedAt = Time.time;
            ResetAirflow(poseStartedAt);
        }

        protected override void OnUnbound()
        {
            if (Owner != null)
            {
                Owner.OnServerPositionUpdated -= OnPosition;
            }
        }

        private void OnDisable()
        {
            hasPosition = false;
            if (Owner != null && !Owner.IsDespawning)
            {
                poseStartedAt = Time.time;
                ResetAirflow(poseStartedAt);
            }
        }

        private void OnPosition(Vector3 position)
        {
            if (!isActiveAndEnabled || Owner.IsDespawning)
            {
                return;
            }

            float now = Time.time;
            float gap = now - positionReceivedAt;
            // 녹화에서 여러 서버 프레임을 한 번에 처리할 때도 서버 틱 간격을 쓴다.
            float recordedTick = Owner.PresentationWorld != null ? Owner.PresentationWorld.RecordedFrameDuration : 0f;
            float tick = recordedTick > 0f ? recordedTick : GameConfig.FrameDuration;
            float interval = Mathf.Max(tick, gap);
            Vector3 offset = position - previousPosition;
            float lastHeight = previousPosition.y;
            float correctionLimit = Mathf.Max(0.13f, interval * 8f);
            bool reset = !hasPosition || gap > MaximumPositionGap ||
                offset.sqrMagnitude > correctionLimit * correctionLimit;
            previousPosition = position;
            positionReceivedAt = now;
            hasPosition = true;

            if (reset)
            {
                ResetAirflow(now);
                return;
            }

            float hold = Mathf.Max(0.09f, 2f * tick);
            float minimumTravel = MinimumGlideSpeed * interval;
            bool movingAcrossGround = offset.x * offset.x + offset.z * offset.z >= minimumTravel * minimumTravel;
            if (movingAcrossGround && wasMovingAcrossGround)
            {
                glideUntil = now + hold;
            }
            wasMovingAcrossGround = movingAcrossGround;

            bool descending = offset.y <= -minimumDescentSpeed * interval;
            if (descending)
            {
                if (!wasDescending && now >= descentUntil)
                {
                    descentStartHeight = lastHeight;
                    lowestDescentHeight = position.y;
                }
                else
                {
                    lowestDescentHeight = Mathf.Min(lowestDescentHeight, position.y);
                    descentUntil = now + hold;
                    // 같은 Unity 프레임에 제거가 들어와도 마지막 하강 자세를 보존한다.
                    Apply(DescentFrame());
                }
            }
            else if (offset.y >= MinimumAscentSpeed * interval)
            {
                descentUntil = float.NegativeInfinity;
            }
            wasDescending = descending;
        }

        private void LateUpdate()
        {
            if (resolvedRenderer == null || Owner == null || Owner.IsDespawning)
            {
                return;
            }

            float now = Time.time;
            FlightPose next = now < descentUntil ? FlightPose.Descent
                : now < glideUntil ? FlightPose.Glide : FlightPose.Hover;
            if (pose != next)
            {
                pose = next;
                poseStartedAt = now;
            }

            Sprite sprite = pose switch
            {
                FlightPose.Glide => LoopFrame(glideFrames, now, glideFrameInterval),
                FlightPose.Descent => DescentFrame(),
                _ => LoopFrame(hoverFrames, now, hoverFrameInterval),
            };
            Apply(sprite);
        }

        private Sprite LoopFrame(Sprite[] frames, float now, float interval)
        {
            if (frames == null || frames.Length == 0)
            {
                return defaultSprite;
            }
            int index = (int)((now - poseStartedAt) / interval) % frames.Length;
            return frames[index];
        }

        private Sprite DescentFrame()
        {
            if (descentFrames == null || descentFrames.Length == 0)
            {
                return defaultSprite;
            }
            float span = descentPoseDepth > 0f ? descentPoseDepth : Mathf.Max(0.16f, descentStartHeight - DescentEndHeight);
            float progress = Mathf.Clamp01((descentStartHeight - lowestDescentHeight) / span);
            int index = Mathf.Min((int)(progress * descentFrames.Length), descentFrames.Length - 1);
            return descentFrames[index];
        }

        private void ResetAirflow(float now)
        {
            wasMovingAcrossGround = wasDescending = false;
            glideUntil = descentUntil = float.NegativeInfinity;
            if (pose != FlightPose.Hover)
            {
                pose = FlightPose.Hover;
                poseStartedAt = now;
            }
            Apply(LoopFrame(hoverFrames, now, hoverFrameInterval));
        }

        private void Apply(Sprite sprite)
        {
            if (resolvedRenderer != null && sprite != null && resolvedRenderer.sprite != sprite)
            {
                resolvedRenderer.sprite = sprite;
            }
        }
    }
}
