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
        [SerializeField] private bool attackOnHit;
        [SerializeField] private float maximumGroundSpeed;

        private const float MinimumGroundStep = 0.00025f;
        private const float MinimumMovementGrace = 0.08f;
        private const float MinimumCorrectionDistance = 0.25f;
        private const float CorrectionSpeed = 2f;
        private const float MinimumServerCorrectionDistance = 0.15f;
        private const float MinimumFacingSpeed = 0.1f;

        private SpriteRenderer resolvedRenderer;
        private Sprite idleSprite;
        private Vector2 lastGroundPosition;
        private bool hasGroundSample;
        private bool lastFlip;
        private float walkDistance;
        private float movingUntil = float.NegativeInfinity;
        private float attackStarted = float.NegativeInfinity;
        private Vector2 lastServerGround;
        private bool hasServerGround;
        private bool correctingServerGround;
        private int precedingServerDirection;
        private int confirmedServerDirection;
        private bool impactFlip;

        protected override void OnBound()
        {
            resolvedRenderer = ResolveRenderer(targetRenderer);
            if (resolvedRenderer == null)
            {
                return;
            }

            idleSprite = resolvedRenderer.sprite;
            if (!attackOnHit)
            {
                Owner.OnAttack += HandleAttack;
            }

            if (maximumGroundSpeed > 0f)
            {
                Owner.OnServerPositionUpdated += HandleServerGround;
            }
        }

        protected override void OnUnbound()
        {
            if (Owner != null)
            {
                Owner.OnAttack -= HandleAttack;
                Owner.OnServerPositionUpdated -= HandleServerGround;
            }

            ResetPose();
        }

        private void OnDisable() => ResetPose();

        internal void PlayHitPose()
        {
            if (attackOnHit)
            {
                HandleAttack();
            }
        }

        private void HandleAttack()
        {
            if (!isActiveAndEnabled || resolvedRenderer == null || attackFrames == null ||
                attackFrames.Length == 0 || attackDuration <= 0f)
            {
                return;
            }

            attackStarted = Time.time;
            impactFlip = resolvedRenderer.flipX;
            resolvedRenderer.sprite = attackFrames[0];
        }

        private void HandleServerGround(Vector3 position)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            var ground = new Vector2(position.x, position.z);
            Vector2 step = ground - (hasServerGround ? lastServerGround : GroundPosition());
            if (correctingServerGround)
            {
                // PositionUpdater가 새 보간 전에 직전 목표 좌표에 맞춘다.
                lastGroundPosition = lastServerGround;
                hasGroundSample = true;
            }

            lastServerGround = ground;
            hasServerGround = true;
            float tick = GameConfig.FrameDuration;
            float limit = Mathf.Max(MinimumServerCorrectionDistance, maximumGroundSpeed * tick);
            correctingServerGround = step.sqrMagnitude > limit * limit;
            if (correctingServerGround)
            {
                precedingServerDirection = confirmedServerDirection = 0;
                movingUntil = float.NegativeInfinity;
                walkDistance = 0f;
                return;
            }

            int direction = Mathf.Abs(step.x) >= MinimumFacingSpeed * tick ? (step.x < 0f ? -1 : 1) : 0;
            if (direction != 0 && direction == precedingServerDirection)
            {
                confirmedServerDirection = direction;
            }

            precedingServerDirection = direction;
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
            if (attackOnHit && attacking)
            {
                resolvedRenderer.flipX = impactFlip;
            }
            else if (confirmedServerDirection != 0)
            {
                resolvedRenderer.flipX = confirmedServerDirection < 0;
            }

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
            Vector2 groundPosition = GroundPosition();
            bool flipped = resolvedRenderer.flipX;
            bool changedDirection = hasGroundSample && flipped != lastFlip;
            float distance = hasGroundSample ? Vector2.Distance(lastGroundPosition, groundPosition) : 0f;
            lastGroundPosition = groundPosition;
            lastFlip = flipped;
            hasGroundSample = true;

            // 빠른 이동은 서버 좌표에서 보정을 판별하고 해당 보간 구간을 모두 건너뛴다.
            bool correcting = maximumGroundSpeed > 0f
                ? correctingServerGround
                : distance > Mathf.Max(MinimumCorrectionDistance, CorrectionSpeed * Time.deltaTime);
            if (correcting)
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

        private Vector2 GroundPosition()
        {
            Vector3 rootPosition = Owner.transform.position;
            if (Owner.PresentationWorld != null)
            {
                rootPosition = Owner.PresentationWorld.transform.InverseTransformPoint(rootPosition);
            }

            return new Vector2(rootPosition.x, rootPosition.z);
        }

        private void ResetPose()
        {
            hasGroundSample = false;
            hasServerGround = correctingServerGround = false;
            precedingServerDirection = confirmedServerDirection = 0;
            walkDistance = 0f;
            movingUntil = attackStarted = float.NegativeInfinity;
            if (resolvedRenderer != null && idleSprite != null)
            {
                resolvedRenderer.sprite = idleSprite;
            }
        }
    }
}
