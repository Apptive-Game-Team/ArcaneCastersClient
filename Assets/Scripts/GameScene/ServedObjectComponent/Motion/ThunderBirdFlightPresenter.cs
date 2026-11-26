using UnityEngine;

namespace GameScene.ServedObjectComponent.Motion
{
    public sealed class ThunderBirdFlightPresenter : ServedObjectBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite[] flapFrames;
        [SerializeField] private Sprite[] diveFrames;
        [SerializeField] private Sprite[] strikeFrames;
        [SerializeField] private Sprite[] climbFrames;
        [SerializeField] private Sprite[] fallFrames;

        private const float FlapFrameInterval = 0.115f;
        private const float StrikeFrameInterval = 0.08f;
        private const float ClimbFrameInterval = 0.125f;
        private const float MinimumVerticalSpeed = 0.18f;
        private const float MaximumPositionGap = 0.3f;
        private const float MaximumFlightSpeed = 7.5f;
        private const float DivePoseDepth = 1.6f;

        private enum FlightPose { Flap, Dive, Climb }

        private SpriteRenderer bodyRenderer;
        private Sprite defaultSprite;
        private Vector3 lastPosition;
        private float positionReceivedAt;
        private bool hasPosition;
        private int pendingDirection;
        private float pendingOriginHeight;

        private FlightPose flightPose;
        private float flightPoseStartedAt;
        private float directionalPoseUntil = float.NegativeInfinity;
        private float diveOriginHeight;
        private float strikeStartedAt = float.NegativeInfinity;
        private bool strikeFlip;

        private bool dying;
        private float fallOriginHeight;

        protected override void OnBound()
        {
            bodyRenderer = ResolveRenderer(targetRenderer);
            if (bodyRenderer == null)
            {
                return;
            }

            defaultSprite = bodyRenderer.sprite;
            flightPoseStartedAt = Time.time;
            Owner.OnServerPositionUpdated += HandlePosition;
            Owner.OnOtherStatus += HandleStatus;
        }

        protected override void OnUnbound()
        {
            if (Owner != null)
            {
                Owner.OnServerPositionUpdated -= HandlePosition;
                Owner.OnOtherStatus -= HandleStatus;
            }
        }

        private void OnDisable()
        {
            hasPosition = false;
            pendingDirection = 0;
            directionalPoseUntil = float.NegativeInfinity;
            strikeStartedAt = float.NegativeInfinity;
        }

        internal void PlayHitPose()
        {
            if (!isActiveAndEnabled || bodyRenderer == null || Owner.IsDespawning || dying ||
                strikeFrames == null || strikeFrames.Length == 0)
            {
                return;
            }

            strikeFlip = bodyRenderer.flipX;
            strikeStartedAt = Time.time;
            Apply(strikeFrames[0]);
        }

        private void HandleStatus(string status)
        {
            if (status != "Dying" || dying)
            {
                return;
            }

            dying = true;
            fallOriginHeight = lastPosition.y;
            Apply(ProgressFrame(fallFrames, 0f));
        }

        private void HandlePosition(Vector3 position)
        {
            float now = Time.time;
            Vector3 offset = position - lastPosition;
            float previousHeight = lastPosition.y;
            float gap = now - positionReceivedAt;
            bool hadPosition = hasPosition;
            lastPosition = position;
            positionReceivedAt = now;
            hasPosition = true;

            if (dying)
            {
                if (!hadPosition)
                {
                    fallOriginHeight = position.y;
                }
                return;
            }

            float recordedTick = Owner.PresentationWorld != null ? Owner.PresentationWorld.RecordedFrameDuration : 0f;
            float tick = recordedTick > 0f ? recordedTick : GameConfig.FrameDuration;
            float correctionLimit = Mathf.Max(0.15f, MaximumFlightSpeed * tick);
            if (!hadPosition || gap > MaximumPositionGap || offset.sqrMagnitude > correctionLimit * correctionLimit)
            {
                pendingDirection = 0;
                directionalPoseUntil = float.NegativeInfinity;
                SetFlightPose(FlightPose.Flap, now);
                return;
            }

            // 연속 두 틱에서 높이가 같은 방향으로 바뀌면 하강이나 상승 자세로 넘어간다.
            int direction = Mathf.Abs(offset.y) >= MinimumVerticalSpeed * tick ? (offset.y < 0f ? -1 : 1) : 0;
            if (direction != pendingDirection)
            {
                pendingDirection = direction;
                pendingOriginHeight = previousHeight;
                return;
            }
            if (direction == 0)
            {
                return;
            }

            FlightPose next = direction < 0 ? FlightPose.Dive : FlightPose.Climb;
            if (next == FlightPose.Dive && (flightPose != next || now >= directionalPoseUntil))
            {
                diveOriginHeight = pendingOriginHeight;
            }
            SetFlightPose(next, now);
            directionalPoseUntil = now + Mathf.Max(0.085f, 2.5f * tick);
        }

        private void LateUpdate()
        {
            if (bodyRenderer == null || Owner == null || Owner.IsDespawning)
            {
                return;
            }

            if (dying)
            {
                float span = Mathf.Max(0.01f, fallOriginHeight);
                float progress = (fallOriginHeight - lastPosition.y) / span;
                Apply(ProgressFrame(fallFrames, progress));
                return;
            }

            float now = Time.time;
            if (strikeFrames is { Length: > 0 } &&
                now - strikeStartedAt < strikeFrames.Length * StrikeFrameInterval)
            {
                bodyRenderer.flipX = strikeFlip;
                Apply(strikeFrames[(int)((now - strikeStartedAt) / StrikeFrameInterval)]);
                return;
            }

            if (now >= directionalPoseUntil)
            {
                SetFlightPose(FlightPose.Flap, now);
            }

            Sprite sprite = flightPose switch
            {
                FlightPose.Dive => ProgressFrame(diveFrames, (diveOriginHeight - lastPosition.y) / DivePoseDepth),
                FlightPose.Climb => LoopFrame(climbFrames, now, ClimbFrameInterval),
                _ => LoopFrame(flapFrames, now, FlapFrameInterval)
            };
            Apply(sprite);
        }

        private void SetFlightPose(FlightPose next, float now)
        {
            if (flightPose != next)
            {
                flightPose = next;
                flightPoseStartedAt = now;
            }
        }

        private Sprite LoopFrame(Sprite[] frames, float now, float interval)
        {
            if (frames == null || frames.Length == 0)
            {
                return defaultSprite;
            }

            int index = (int)((now - flightPoseStartedAt) / interval) % frames.Length;
            return frames[index];
        }

        private Sprite ProgressFrame(Sprite[] frames, float progress)
        {
            if (frames == null || frames.Length == 0)
            {
                return defaultSprite;
            }

            int index = Mathf.Min((int)(Mathf.Clamp01(progress) * frames.Length), frames.Length - 1);
            return frames[index];
        }

        private void Apply(Sprite sprite)
        {
            if (bodyRenderer != null && sprite != null && bodyRenderer.sprite != sprite)
            {
                bodyRenderer.sprite = sprite;
            }
        }
    }
}
