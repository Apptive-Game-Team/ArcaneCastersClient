using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    public sealed class ChickenCommandoSpriteSwap : ServedObjectBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite defaultSprite;
        [SerializeField] private Sprite[] descentFrames;
        [SerializeField] private Sprite[] landingFrames;
        [SerializeField] private Sprite[] runFrames;
        [SerializeField] private Sprite[] attackFrames;

        private const float GroundedHeight = 0.05f;
        private const float LandingDuration = 0.22f;
        private const float AttackDuration = 0.20f;
        private const float RunCycleDistance = 1.28f;
        private const float MinGroundStep = 0.00025f;

        private Transform presentationTransform;
        private Vector3 lastServerPosition;
        private Vector3 lastRenderedPosition;
        private bool awaitingLanding;
        private bool correcting;
        private bool wasFlipped;
        private float landingStarted = float.NegativeInfinity;
        private float attackStarted = float.NegativeInfinity;
        private float pendingAttackUntil = float.NegativeInfinity;
        private float movingUntil = float.NegativeInfinity;
        private float runDistance;

        protected override void OnBound()
        {
            presentationTransform = Owner.PresentationWorld?.transform;
            lastServerPosition = lastRenderedPosition = LocalPosition();
            awaitingLanding = lastRenderedPosition.y > GroundedHeight;
            wasFlipped = spriteRenderer.flipX;
            SetPose(awaitingLanding ? DescentPose(lastRenderedPosition.y) : defaultSprite);
            Owner.OnServerPositionUpdated += HandleServerPosition;
            Owner.OnAttack += HandleAttack;
        }

        protected override void OnUnbound()
        {
            if (Owner != null)
            {
                Owner.OnServerPositionUpdated -= HandleServerPosition;
                Owner.OnAttack -= HandleAttack;
            }
        }

        private void HandleServerPosition(Vector3 position)
        {
            if (correcting)
            {
                // PositionUpdater는 새 보간을 시작하기 전에 직전 목표 좌표에 맞춘다.
                lastRenderedPosition = lastServerPosition;
            }

            Vector2 delta = GroundDelta(position, lastServerPosition);
            float correctionDistance = Mathf.Max(0.55f, 10f * GameConfig.FrameDuration);
            correcting = delta.sqrMagnitude > correctionDistance * correctionDistance;
            lastServerPosition = position;
        }

        private void HandleAttack()
        {
            if (!isActiveAndEnabled || Owner.IsDespawning)
            {
                return;
            }

            if (LocalPosition().y > GroundedHeight)
            {
                // 착지 좌표와 타격이 함께 오면 보간된 발이 닿을 때 타격을 표시한다.
                pendingAttackUntil = Time.time + Mathf.Max(0.08f, 2f * GameConfig.FrameDuration);
                return;
            }

            BeginAttack(Time.time);
        }

        private void BeginAttack(float now)
        {
            attackStarted = now;
            pendingAttackUntil = landingStarted = float.NegativeInfinity;
            SetPose(attackFrames[0]);
        }

        private void LateUpdate()
        {
            if (Owner == null || Owner.IsDespawning)
            {
                return;
            }

            float now = Time.time;
            Vector3 position = LocalPosition();
            float distance = GroundDelta(position, lastRenderedPosition).magnitude;
            lastRenderedPosition = position;
            if (position.y > GroundedHeight)
            {
                ResetRun();
                SetPose(awaitingLanding ? DescentPose(position.y) : defaultSprite);
                return;
            }

            if (awaitingLanding)
            {
                awaitingLanding = false;
                if (now >= attackStarted + AttackDuration)
                {
                    landingStarted = now;
                }
            }
            if (now <= pendingAttackUntil)
            {
                BeginAttack(now);
            }

            if (now < attackStarted + AttackDuration)
            {
                ResetRun();
                SetPose(TimedPose(attackFrames, now - attackStarted, AttackDuration));
            }
            else if (now < landingStarted + LandingDuration)
            {
                ResetRun();
                SetPose(TimedPose(landingFrames, now - landingStarted, LandingDuration));
            }
            else
            {
                SetPose(RunPose(now, distance));
            }
        }

        private Sprite RunPose(float now, float distance)
        {
            bool flipped = spriteRenderer.flipX;
            if (correcting || flipped != wasFlipped || now >= movingUntil)
            {
                ResetRun();
            }
            wasFlipped = flipped;
            if (!correcting && distance >= MinGroundStep)
            {
                if (now < movingUntil)
                {
                    runDistance = Mathf.Repeat(runDistance + distance, RunCycleDistance);
                }
                movingUntil = now + Mathf.Max(0.08f, 2f * GameConfig.FrameDuration);
            }

            int frame = (int)(runDistance / RunCycleDistance * runFrames.Length);
            return now < movingUntil ? runFrames[Mathf.Min(frame, runFrames.Length - 1)] : defaultSprite;
        }

        private void ResetRun()
        {
            movingUntil = float.NegativeInfinity;
            runDistance = 0f;
        }

        private Vector3 LocalPosition()
        {
            Vector3 position = Owner.transform.position;
            return presentationTransform != null ? presentationTransform.InverseTransformPoint(position) : position;
        }

        private Sprite DescentPose(float height)
        {
            if (height > 3.75f) return descentFrames[0];
            if (height > 2f) return descentFrames[1];
            if (height > 0.65f) return descentFrames[2];
            return descentFrames[3];
        }

        private static Vector2 GroundDelta(Vector3 current, Vector3 previous) =>
            new Vector2(current.x - previous.x, current.z - previous.z);

        private static Sprite TimedPose(Sprite[] frames, float elapsed, float duration) =>
            frames[Mathf.Min(frames.Length - 1, (int)(elapsed / duration * frames.Length))];

        private void SetPose(Sprite pose)
        {
            if (spriteRenderer.sprite != pose)
            {
                spriteRenderer.sprite = pose;
            }
        }
    }
}
