using System;
using GameScene.Dto.Projectile;
using GameScene.Object;
using GameScene.Object.Projectile;
using GameScene.ServedObjectComponent.Effect;
using UnityEngine;

namespace GameScene.ServedObjectComponent.Motion
{
    /// <summary>이동, 공격, 피격 및 추락 자세를 같은 렌더러에서 선택한다.</summary>
    public class UnitSpriteMotionController : ServedObjectBehaviour
    {
        protected const float FrameDuration = 1f / 12f;
        private const float HurtCooldown = 0.5f;

        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite[] moveFrames = Array.Empty<Sprite>();
        [SerializeField] private float moveFrameInterval = FrameDuration;
        [SerializeField, Min(0f)] private float moveCycleDistance;
        [SerializeField] private float[] moveFootX = Array.Empty<float>();
        [SerializeField] private bool loopMovementAtRest;
        [SerializeField] private Sprite moveStopFrame;
        [SerializeField] private Sprite[] attackFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] contactFrames = Array.Empty<Sprite>();
        [SerializeField] private string projectilePoseType;
        [SerializeField] private Sprite[] projectilePoseFrames = Array.Empty<Sprite>();
        [SerializeField] private ProjectilePose[] projectilePoses = Array.Empty<ProjectilePose>();
        [SerializeField] private Sprite spawnFrame;
        [SerializeField] private float spawnDuration = 0.28f;
        [SerializeField] private Sprite[] hurtFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite dyingSprite;
        [SerializeField] private Sprite[] deathFrames = Array.Empty<Sprite>();

        private SpriteRenderer resolvedRenderer;
        private Sprite restingSprite;
        private float movementUntil = float.NegativeInfinity;
        private float movementStarted;
        private Vector3 lastWalkPosition;
        private float walkDistance;
        private bool walkFacingLeft;
        private float walkSpriteOffset;
        private Vector3 appliedWalkOffset;
        private float attackStarted = float.NegativeInfinity;
        private Sprite[] activeAttackFrames;
        private float attackDuration;
        private float impactFraction;
        private int impactFrame;
        private AttackPoseSource attackPoseSource;
        private float spawnUntil = float.NegativeInfinity;
        private float hurtStarted = float.NegativeInfinity;
        private bool hurtPosePending;
        private bool dying;
        private bool deathPlayed;

        protected virtual Sprite[] HurtFrames => hurtFrames;
        protected virtual Sprite[] DeathFrames => deathFrames;
        private bool HasHurtSequence => (HurtFrames?.Length ?? 0) > 0;
        private bool HasDeathSequence => (DeathFrames?.Length ?? 0) > 0;
        internal bool HasHurtFrames => isActiveAndEnabled && HasHurtSequence;
        internal bool HasDeathFrames => isActiveAndEnabled && HasDeathSequence;

        protected override void OnBound()
        {
            resolvedRenderer = ResolveRenderer(targetRenderer);
            restingSprite = idleSprite != null ? idleSprite : resolvedRenderer?.sprite;
            ResetPose();
            Owner.OnMoved += HandleMoved;
            Owner.OnAttack += HandleAttack;
            if (contactFrames != null && contactFrames.Length > 0) Owner.OnHitTarget += HandleContact;
            if (!string.IsNullOrEmpty(projectilePoseType) || (projectilePoses?.Length ?? 0) > 0)
                Owner.OnProjectilePose += HandleProjectilePose;
            if (spawnFrame != null) Owner.OnSpawned += HandleSpawned;
            if (HasHurtSequence || HasDeathSequence || dyingSprite != null)
            {
                Owner.AuthoredReaction = this;
                Owner.OnHpDecreased += HandleHurt;
                Owner.OnOtherStatus += HandleStatus;
                Owner.OnDestroyed += HandleDeath;
            }
        }

        protected override void OnUnbound()
        {
            if (Owner != null)
            {
                Owner.OnMoved -= HandleMoved;
                Owner.OnAttack -= HandleAttack;
                Owner.OnHitTarget -= HandleContact;
                Owner.OnProjectilePose -= HandleProjectilePose;
                Owner.OnSpawned -= HandleSpawned;
                Owner.OnHpDecreased -= HandleHurt;
                Owner.OnOtherStatus -= HandleStatus;
                Owner.OnDestroyed -= HandleDeath;
                if (Owner.AuthoredReaction == this) Owner.AuthoredReaction = null;
            }
            ResetPose();
        }

        protected virtual void OnDisable() => ResetPose();

        private void HandleHurt()
        {
            float now = Time.time;
            if (!HasHurtFrames || dying || now - hurtStarted < HurtCooldown) return;
            hurtStarted = now;
            hurtPosePending = true;
            ApplyPose(now);
        }

        private void HandleStatus(string status)
        {
            if (status != "Dying" || dyingSprite == null) return;
            dying = true;
            ApplyPose(Time.time);
        }

        private void HandleDeath()
        {
            if (deathPlayed || !HasDeathFrames || resolvedRenderer == null) return;
            deathPlayed = true;
            Sprite[] frames = DeathFrames;
            var effect = DeathVisualFactory.CreateSpriteClone(resolvedRenderer, $"{Owner.name} {Owner.id} DeathFrames");
            effect.AddComponent<SpriteFrameAnimator>().PlayOnce(
                effect.GetComponent<SpriteRenderer>(), frames, FrameDuration, destroyAfterPlayback: true);
        }

        private void HandleMoved()
        {
            if (!isActiveAndEnabled || moveFrames == null || moveFrames.Length == 0) return;
            float now = Time.time;
            // 연속 이동 패킷은 종료 시각만 연장한다. 매번 위상을 초기화하면 첫 발만 보인다.
            if (!loopMovementAtRest && now >= movementUntil) RestartWalk(now);
            movementUntil = now + Mathf.Max(0.08f, 2f * GameConfig.FrameDuration);
            ApplyPose(now);
        }

        private void HandleAttack()
        {
            if (attackPoseSource == AttackPoseSource.ProjectileSequence && Time.time - attackStarted < attackDuration) return;
            StartAttack(attackFrames);
        }

        private void HandleContact()
        {
            StartAttack(contactFrames);
        }

        private void HandleProjectilePose(ProjectileDto projectile)
        {
            if (projectilePoses != null)
            {
                foreach (ProjectilePose pose in projectilePoses)
                {
                    if (pose.type != projectile.type) continue;
                    if (pose.playback == ProjectilePosePlayback.BetweenAttacks && IsAttacking(Time.time)) return;
                    if (pose.faceTarget) FaceProjectileTarget(projectile.end);
                    float duration = 0f;
                    float hitFraction = pose.impactFraction;
                    if (pose.followDuration && projectile.duration > 0f && !float.IsInfinity(projectile.duration))
                    {
                        duration = projectile.duration + Mathf.Max(0f, pose.recoveryDuration);
                        hitFraction = projectile.duration * pose.impactFraction / duration;
                    }
                    var source = pose.playback == ProjectilePosePlayback.Protected
                        ? AttackPoseSource.ProjectileSequence : AttackPoseSource.Projectile;
                    StartAttack(pose.frames, source, duration, hitFraction, pose.impactFrame);
                    return;
                }
            }
            if (projectile.type == projectilePoseType) StartAttack(projectilePoseFrames, AttackPoseSource.Projectile);
        }

        private void FaceProjectileTarget(ProjectileTarget target)
        {
            var world = Owner.PresentationWorld;
            if (target is PositionProjectileTarget position)
                Owner.FaceTowards(ProjectileUtil.GetPosition(position, world));
            else if (target is ReferenceProjectileTarget reference &&
                PresentationWorld.Find(reference.id, world) is ServedObject unit)
                Owner.FaceTowards(unit.transform.position);
        }

        private void StartAttack(Sprite[] frames, AttackPoseSource source = AttackPoseSource.Ordinary, float duration = 0f,
            float hitFraction = 0f, int hitFrame = 0)
        {
            if (!isActiveAndEnabled || frames == null || frames.Length == 0) return;
            activeAttackFrames = frames;
            attackDuration = duration > 0f && !float.IsInfinity(duration) ? duration : frames.Length * FrameDuration;
            attackPoseSource = source;
            impactFraction = hitFraction;
            impactFrame = hitFrame;
            attackStarted = Time.time;
            ApplyPose(attackStarted);
        }

        private void HandleSpawned()
        {
            if (!isActiveAndEnabled) return;
            float now = Time.time;
            spawnUntil = now + spawnDuration;
            ApplyPose(now);
        }

        protected void LateUpdate() => ApplyPose(Time.time);

        protected void ApplyPose(float now)
        {
            if (resolvedRenderer == null) return;
            walkSpriteOffset = 0f;
            Sprite pose = ResolvePose(now) ?? restingSprite;
            ApplyWalkOffset();
            if (pose != resolvedRenderer.sprite) resolvedRenderer.sprite = pose;
        }

        private Sprite ResolvePose(float now)
        {
            if (dying && dyingSprite != null) return dyingSprite;
            float attackElapsed = now - attackStarted;
            bool attacking = IsAttacking(now);
            Sprite priorityPose = GetPriorityPose(now, attacking);
            if (priorityPose != null)
            {
                hurtPosePending = false;
                return priorityPose;
            }
            if (attacking)
            {
                hurtPosePending = false;
                int frame = GetAttackFrame(attackElapsed / attackDuration);
                return activeAttackFrames[frame];
            }
            if (now < spawnUntil)
            {
                hurtPosePending = false;
                return spawnFrame;
            }
            if (activeAttackFrames != null)
            {
                activeAttackFrames = null;
                // 복귀 자세 뒤에는 이동 루프를 첫 자세부터 잇는다.
                RestartWalk(now);
            }
            float moveInterval = moveFrameInterval > 0f ? moveFrameInterval : FrameDuration;
            bool moving = now < movementUntil;
            if (moving) hurtPosePending = false;
            float hurtElapsed = now - hurtStarted;
            Sprite[] reaction = HurtFrames;
            if (hurtPosePending && isActiveAndEnabled && reaction != null && hurtElapsed < reaction.Length * FrameDuration)
            {
                return reaction[Mathf.FloorToInt(hurtElapsed / FrameDuration)];
            }
            if ((moving || loopMovementAtRest) && moveFrames != null && moveFrames.Length > 0)
            {
                int frame = moveCycleDistance > 0f && !loopMovementAtRest
                    ? ResolveDistanceWalkFrame(now)
                    : Mathf.FloorToInt((now - movementStarted) / moveInterval) % moveFrames.Length;
                return moveFrames[frame];
            }
            if (now < movementUntil + Mathf.Max(moveInterval, FrameDuration))
            {
                return moveStopFrame;
            }
            return GetRestingPose(now);
        }

        private Vector3 GetWalkPosition()
        {
            var world = Owner.PresentationWorld;
            Vector3 position = Owner.transform.position;
            return world != null ? world.transform.InverseTransformPoint(position) : position;
        }

        private void RestartWalk(float now)
        {
            movementStarted = now;
            if (moveCycleDistance <= 0f || resolvedRenderer == null) return;
            lastWalkPosition = GetWalkPosition();
            walkFacingLeft = resolvedRenderer.flipX;
            walkDistance = 0f;
        }

        private int ResolveDistanceWalkFrame(float now)
        {
            if (walkFacingLeft != resolvedRenderer.flipX) RestartWalk(now);
            Vector3 position = GetWalkPosition();
            Vector3 delta = position - lastWalkPosition;
            delta.y = 0f;
            lastWalkPosition = position;
            walkDistance = Mathf.Repeat(walkDistance + delta.magnitude, moveCycleDistance);

            int count = moveFrames.Length;
            int frame = Mathf.Min(Mathf.FloorToInt(walkDistance / moveCycleDistance * count), count - 1);
            if (moveFootX.Length != count || (count & 1) != 0 || resolvedRenderer.transform == Owner.transform)
                return frame;

            // 두 발의 접지 지점을 각각 기준으로 삼되 머리와 몸통이 크게 밀리지 않도록 제한한다.
            int stepStart = frame < count / 2 ? 0 : count / 2;
            float stepDistance = walkDistance - (stepStart == 0 ? 0f : moveCycleDistance * 0.5f);
            Transform worldTransform = Owner.PresentationWorld?.transform;
            float worldScale = worldTransform != null ? worldTransform.TransformVector(Vector3.right).magnitude : 1f;
            float spriteScale = resolvedRenderer.transform.TransformVector(Vector3.right).magnitude;
            float offset = (moveFootX[stepStart] - moveFootX[frame]) * spriteScale - stepDistance * worldScale;
            float distanceUntilNextStep = moveCycleDistance * 0.5f - stepDistance;
            float release = Mathf.Clamp01(distanceUntilNextStep / (moveCycleDistance * 0.1f));
            walkSpriteOffset = Mathf.Clamp(offset, -0.12f * worldScale, 0.12f * worldScale) * release;
            return frame;
        }

        private void ApplyWalkOffset()
        {
            Transform visual = resolvedRenderer.transform;
            if (walkSpriteOffset == 0f && appliedWalkOffset == Vector3.zero) return;
            Vector3 worldOffset = visual.right * (resolvedRenderer.flipX ? -walkSpriteOffset : walkSpriteOffset);
            Vector3 localOffset = visual.parent != null ? visual.parent.InverseTransformVector(worldOffset) : worldOffset;
            visual.localPosition += localOffset - appliedWalkOffset;
            appliedWalkOffset = localOffset;
        }

        protected virtual Sprite GetPriorityPose(float now, bool attacking) => null;
        protected virtual Sprite GetRestingPose(float now) => restingSprite;

        private bool IsAttacking(float now) => activeAttackFrames != null && now - attackStarted < attackDuration;

        private int GetAttackFrame(float progress)
        {
            int count = activeAttackFrames.Length;
            float frame = progress * count;
            if (impactFraction > 0f && impactFraction < 1f && impactFrame > 0 && impactFrame < count)
            {
                frame = progress < impactFraction
                    ? progress / impactFraction * impactFrame
                    : impactFrame + (progress - impactFraction) / (1f - impactFraction) * (count - impactFrame);
            }
            return Mathf.Clamp(Mathf.FloorToInt(frame), 0, count - 1);
        }

        private void ResetPose()
        {
            movementUntil = attackStarted = spawnUntil = hurtStarted = float.NegativeInfinity;
            activeAttackFrames = null;
            attackPoseSource = AttackPoseSource.Ordinary;
            hurtPosePending = false;
            dying = false;
            deathPlayed = false;
            movementStarted = Time.time;
            walkDistance = 0f;
            walkSpriteOffset = 0f;
            if (resolvedRenderer != null) ApplyWalkOffset();
            if (resolvedRenderer != null && restingSprite != null) resolvedRenderer.sprite = restingSprite;
        }

        private enum AttackPoseSource
        {
            Ordinary,
            Projectile,
            ProjectileSequence
        }

        private enum ProjectilePosePlayback
        {
            Protected = 0,
            Interruptible = 1,
            BetweenAttacks = 2
        }

        [Serializable]
        private struct ProjectilePose
        {
            public string type;
            public Sprite[] frames;
            public ProjectilePosePlayback playback;
            public bool followDuration;
            public bool faceTarget;
            public float impactFraction;
            public int impactFrame;
            public float recoveryDuration;
        }
    }
}
