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
        [SerializeField] private bool loopMovementAtRest;
        [SerializeField] private Sprite moveStopFrame;
        [SerializeField] private Sprite[] attackFrames = Array.Empty<Sprite>();
        [SerializeField] private bool keepAttackPoseOnHit;
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
        private float attackStarted = float.NegativeInfinity;
        private Sprite[] activeAttackFrames;
        private float attackDuration;
        private float impactFraction;
        private int impactFrame;
        private AttackPoseSource attackPoseSource;
        private float spawnUntil = float.NegativeInfinity;
        private float hurtStarted = float.NegativeInfinity;
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
            if (keepAttackPoseOnHit && IsAttacking(now)) return;
            hurtStarted = now;
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
            if (!loopMovementAtRest && now >= movementUntil) movementStarted = now;
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
            Sprite pose = ResolvePose(now) ?? restingSprite;
            if (pose != resolvedRenderer.sprite) resolvedRenderer.sprite = pose;
        }

        private Sprite ResolvePose(float now)
        {
            if (dying && dyingSprite != null) return dyingSprite;
            float attackElapsed = now - attackStarted;
            bool attacking = IsAttacking(now);
            Sprite priorityPose = GetPriorityPose(now, attacking);
            if (priorityPose != null) return priorityPose;

            float hurtElapsed = now - hurtStarted;
            Sprite[] reaction = HurtFrames;
            if (isActiveAndEnabled && (!attacking || (attackPoseSource == AttackPoseSource.Ordinary && !keepAttackPoseOnHit)) &&
                reaction != null && hurtElapsed >= 0f && hurtElapsed < reaction.Length * FrameDuration)
            {
                int frame = Mathf.Min(Mathf.FloorToInt(hurtElapsed / FrameDuration), reaction.Length - 1);
                return reaction[frame];
            }
            if (attacking)
            {
                int frame = GetAttackFrame(attackElapsed / attackDuration);
                return activeAttackFrames[frame];
            }
            if (now < spawnUntil)
            {
                return spawnFrame;
            }
            float moveInterval = moveFrameInterval > 0f ? moveFrameInterval : FrameDuration;
            if ((now < movementUntil || loopMovementAtRest) && moveFrames != null && moveFrames.Length > 0)
            {
                int frame = Mathf.FloorToInt((now - movementStarted) / moveInterval) % moveFrames.Length;
                return moveFrames[frame];
            }
            if (now < movementUntil + moveInterval)
            {
                return moveStopFrame;
            }
            return GetRestingPose(now);
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
            dying = false;
            deathPlayed = false;
            movementStarted = Time.time;
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
