using UnityEngine;

namespace GameScene.ServedObjectComponent.Motion
{
    /// <summary>실제 지면 이동량에 따라 걷고 공격 이벤트에서 타격 자세를 먼저 표시한다.</summary>
    public sealed class GroundedUnitPoseController : ServedObjectBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private Sprite[] projectileAttackFrames;
        [SerializeField] private Sprite[] spawnFrames;
        [SerializeField] private Vector2 projectileMuzzle;
        [SerializeField] private float walkCycleDistance = 0.54f;
        [SerializeField] private float attackDuration = 0.2f;
        [SerializeField, Min(0f)] private float spawnDuration = 0.28f;
        [SerializeField] private bool attackOnHit;
        [SerializeField] private float maximumGroundSpeed;
        [SerializeField, Min(0f)] private float walkingHeightTolerance;

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
        private float spawnStarted = float.NegativeInfinity;
        private Sprite[] activeAttackFrames;
        private Vector2 lastServerGround;
        private bool hasServerGround;
        private bool correctingServerGround;
        private bool waitingForGround;
        private int precedingServerDirection;
        private int confirmedServerDirection;
        private bool impactFlip;
        private bool lockAttackFacing;

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

            if (spawnFrames is { Length: > 0 } && spawnDuration > 0f)
            {
                Owner.OnSpawned += StartSpawn;
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
                Owner.OnSpawned -= StartSpawn;
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
            if (CanPlayAttack(attackFrames))
            {
                StartAttack(attackFrames, attackOnHit);
            }
        }

        internal bool TryLaunchProjectile(Vector3 destination, out Vector3 start)
        {
            start = default;
            if (!PlayProjectilePose(destination))
            {
                return false;
            }

            float x = impactFlip ? -projectileMuzzle.x : projectileMuzzle.x;
            start = resolvedRenderer.transform.TransformPoint(new Vector3(x, projectileMuzzle.y, 0f));
            return true;
        }

        internal bool PlayProjectilePose(Vector3 destination)
        {
            Sprite[] frames = projectileAttackFrames is { Length: > 0 } ? projectileAttackFrames : attackFrames;
            if (!CanPlayAttack(frames))
            {
                return false;
            }

            Owner.FaceTowards(destination);
            precedingServerDirection = confirmedServerDirection = 0;
            StartAttack(frames, true);
            return true;
        }

        private bool CanPlayAttack(Sprite[] frames) =>
            isActiveAndEnabled && resolvedRenderer != null && Owner != null && !Owner.IsDespawning &&
            frames is { Length: > 0 } && attackDuration > 0f;

        private void StartAttack(Sprite[] frames, bool lockFacing)
        {
            spawnStarted = float.NegativeInfinity;
            activeAttackFrames = frames;
            attackStarted = Time.time;
            impactFlip = resolvedRenderer.flipX;
            lockAttackFacing = lockFacing;
            resolvedRenderer.sprite = frames[0];
        }

        private void StartSpawn()
        {
            if (!isActiveAndEnabled || Owner.IsDespawning)
            {
                return;
            }

            spawnStarted = Time.time;
            resolvedRenderer.sprite = spawnFrames[0];
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
                lastGroundPosition = GroundPosition();
                hasGroundSample = true;
            }

            lastServerGround = ground;
            hasServerGround = true;
            float recordedTick = Owner.PresentationWorld != null ? Owner.PresentationWorld.RecordedFrameDuration : 0f;
            float tick = recordedTick > 0f ? recordedTick : GameConfig.FrameDuration;
            float limit = Mathf.Max(MinimumServerCorrectionDistance, maximumGroundSpeed * tick);
            bool wasWaitingForGround = waitingForGround;
            waitingForGround = walkingHeightTolerance > 0f && Mathf.Abs(position.y) > walkingHeightTolerance;
            // 소환 통로에서 나온 뒤 착지하기까지 움직인 거리는 보행에 합치지 않는다.
            correctingServerGround = waitingForGround || wasWaitingForGround || step.sqrMagnitude > limit * limit;
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
            if (resolvedRenderer == null || Owner == null || Owner.IsDespawning)
            {
                return;
            }

            float now = Time.time;
            float attackElapsed = now - attackStarted;
            float spawnElapsed = now - spawnStarted;
            bool attacking = attackElapsed < attackDuration;
            bool spawning = spawnElapsed < spawnDuration;
            if (attacking && lockAttackFacing)
            {
                resolvedRenderer.flipX = impactFlip;
            }
            else if (confirmedServerDirection != 0)
            {
                resolvedRenderer.flipX = confirmedServerDirection < 0;
            }

            UpdateWalk(now, attacking || spawning);

            Sprite pose = idleSprite;
            if (attacking)
            {
                int frame = Mathf.Min(activeAttackFrames.Length - 1, (int)(attackElapsed * activeAttackFrames.Length / attackDuration));
                pose = activeAttackFrames[frame];
            }
            else if (spawning)
            {
                int frame = Mathf.Min(spawnFrames.Length - 1, (int)(spawnElapsed * spawnFrames.Length / spawnDuration));
                pose = spawnFrames[frame];
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

        private void UpdateWalk(float now, bool playingAction)
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
                if (continuing && !playingAction && walkCycleDistance > 0f)
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
            activeAttackFrames = null;
            hasServerGround = correctingServerGround = waitingForGround = false;
            precedingServerDirection = confirmedServerDirection = 0;
            walkDistance = 0f;
            movingUntil = attackStarted = spawnStarted = float.NegativeInfinity;
            if (resolvedRenderer != null && idleSprite != null)
            {
                resolvedRenderer.sprite = idleSprite;
            }
        }
    }
}
