using UnityEngine;

namespace GameScene.ServedObjectComponent.Motion
{
    /// <summary>실제 지면 이동량에 따라 걷고 공격 이벤트에서 타격 자세를 먼저 표시한다.</summary>
    public sealed class GroundedUnitPoseController : ServedObjectBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private float walkCycleDistance = 0.54f;
        [SerializeField] private float attackDuration = 0.2f;

        private const float MinimumGroundStep = 0.00025f;
        private const float MinimumMovementGrace = 0.08f;
        private const float MinimumCorrectionDistance = 0.25f;
        private const float CorrectionSpeed = 2f;

        private SpriteRenderer resolvedRenderer;
        private Sprite idleSprite;
        private Vector2 lastGroundPosition;
        private bool hasGroundSample;
        private bool lastFlip;
        private float walkDistance;
        private float movingUntil = float.NegativeInfinity;
        private float attackStarted = float.NegativeInfinity;

        protected override void OnBound()
        {
            resolvedRenderer = ResolveRenderer(targetRenderer);
            if (resolvedRenderer == null)
            {
                return;
            }

            idleSprite = resolvedRenderer.sprite;
            Owner.OnAttack += HandleAttack;
        }

        protected override void OnUnbound()
        {
            if (Owner != null)
            {
                Owner.OnAttack -= HandleAttack;
            }

            ResetPose();
        }

        private void OnDisable() => ResetPose();

        private void HandleAttack()
        {
            if (!isActiveAndEnabled || attackFrames == null || attackFrames.Length == 0 || attackDuration <= 0f)
            {
                return;
            }

            attackStarted = Time.time;
            resolvedRenderer.sprite = attackFrames[0];
        }

        private void LateUpdate()
        {
            if (resolvedRenderer == null || Owner == null)
            {
                return;
            }

            float now = Time.time;
            float attackElapsed = now - attackStarted;
            bool attacking = attackElapsed < attackDuration;
            UpdateWalk(now, attacking);

            Sprite pose = idleSprite;
            if (attacking)
            {
                int frame = Mathf.Min(attackFrames.Length - 1, (int)(attackElapsed * attackFrames.Length / attackDuration));
                pose = attackFrames[frame];
            }
            else if (now < movingUntil && walkFrames != null && walkFrames.Length > 0 && walkCycleDistance > 0f)
            {
                int frame = Mathf.Min(walkFrames.Length - 1, (int)(walkDistance * walkFrames.Length / walkCycleDistance));
                pose = walkFrames[frame];
            }

            if (pose != null && resolvedRenderer.sprite != pose)
            {
                resolvedRenderer.sprite = pose;
            }
        }

        private void UpdateWalk(float now, bool attacking)
        {
            Vector3 rootPosition = Owner.transform.position;
            if (Owner.PresentationWorld != null)
            {
                rootPosition = Owner.PresentationWorld.transform.InverseTransformPoint(rootPosition);
            }

            var groundPosition = new Vector2(rootPosition.x, rootPosition.z);
            bool flipped = resolvedRenderer.flipX;
            bool changedDirection = hasGroundSample && flipped != lastFlip;
            float distance = hasGroundSample ? Vector2.Distance(lastGroundPosition, groundPosition) : 0f;
            lastGroundPosition = groundPosition;
            lastFlip = flipped;
            hasGroundSample = true;

            // 큰 좌표 보정은 걸음으로 처리하지 않는다. 매 프레임 새 좌표를 기준으로 삼는다.
            if (distance > Mathf.Max(MinimumCorrectionDistance, CorrectionSpeed * Time.deltaTime))
            {
                movingUntil = float.NegativeInfinity;
                walkDistance = 0f;
                return;
            }

            bool continuing = now < movingUntil && !changedDirection;
            if (!continuing)
            {
                walkDistance = 0f;
            }

            // 60Hz의 느린 이동도 쓰기 위해 OnMoved의 패킷당 변위 임계값과 분리한다.
            if (distance >= MinimumGroundStep)
            {
                movingUntil = now + Mathf.Max(MinimumMovementGrace, 2f * GameConfig.FrameDuration);
                if (continuing && !attacking && walkCycleDistance > 0f)
                {
                    walkDistance = Mathf.Repeat(walkDistance + distance, walkCycleDistance);
                }
            }
        }

        private void ResetPose()
        {
            hasGroundSample = false;
            walkDistance = 0f;
            movingUntil = attackStarted = float.NegativeInfinity;
            if (resolvedRenderer != null && idleSprite != null)
            {
                resolvedRenderer.sprite = idleSprite;
            }
        }
    }
}
