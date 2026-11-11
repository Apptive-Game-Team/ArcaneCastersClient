using System;
using Data;
using Global;
using DG.Tweening;
using GameScene.ServedObjectComponent.Effect;
using UnityEngine;
using Sequence = DG.Tweening.Sequence;

namespace GameScene.ServedObjectComponent.OnAttack
{
    /// <summary>
    /// 카드 상태와 시전이 같은 렌더러를 쓰므로 여기서 자세의 우선순위를 정한다.
    /// </summary>
    public sealed class PlayerStaffPoseController : ServedObjectBehaviour
    {
        [Serializable]
        private struct StaffFrame
        {
            public Sprite sprite;
            public Vector2 tip;
        }

        private const float FrameDuration = 1f / 12f;

        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite staffLoweredSprite;
        [SerializeField] private Sprite staffRaisedSprite;
        [SerializeField] private Sprite staffThrustSprite;
        [SerializeField] private float thrustDuration = 0.3f;
        [SerializeField] private string[] raisingEffects;
        [SerializeField] private PlayerStaffAuraController staffAura;
        [SerializeField] private Vector2 loweredTip;
        [SerializeField] private Vector2 raisedTip;
        [SerializeField] private Vector2 thrustTip;
        [SerializeField] private StaffFrame aimLiftStart;
        [SerializeField] private StaffFrame aimLiftEnd;
        [SerializeField] private StaffFrame castRecoil;
        [SerializeField] private StaffFrame castRecover;

        private SpriteRenderer resolvedRenderer;
        private Sequence motion;
        private bool isThrusting;
        private bool isRaised;
        private bool hasFrameAnimation;

        public bool UsesFrameAnimation => isActiveAndEnabled && hasFrameAnimation;

        protected override void OnBound()
        {
            resolvedRenderer = ResolveRenderer(targetRenderer);
            if (resolvedRenderer == null)
            {
                return;
            }

            ApplyAppearance();
            isRaised = HasRaisingEffect();
            Owner.OnAttack += Play;
            Owner.OnEffectsChanged += HandleEffectsChanged;
            ApplyCurrentPose();
        }

        protected override void OnUnbound()
        {
            if (Owner != null)
            {
                Owner.OnAttack -= Play;
                Owner.OnEffectsChanged -= HandleEffectsChanged;
            }

            StopMotion();
        }

        private void OnDisable()
        {
            StopMotion();
            ApplyCurrentPose();
        }

        private void OnEnable()
        {
            if (resolvedRenderer != null && Owner != null)
            {
                isRaised = HasRaisingEffect();
                ApplyCurrentPose();
            }
        }

        /// <summary>
        /// Replaces the three serialized sprites with the set for this player's <c>appearance</c>.
        /// When no complete set exists, including the default set, the serialized sprites stay.
        /// </summary>
        private void ApplyAppearance()
        {
            string appearance = SceneContext.MatchInfo?.FindUserByMaster(Owner.GetMaster())?.appearance;
            string loadedId = PlayerAppearanceResolver.Resolve(
                appearance,
                Resources.Load<Sprite>,
                out Sprite idle,
                out Sprite raised,
                out Sprite attacking);
            if (loadedId == null)
            {
                return;
            }

            staffLoweredSprite = idle;
            staffRaisedSprite = raised;
            staffThrustSprite = attacking;
            hasFrameAnimation = loadedId == PlayerAppearanceResolver.DefaultId && staffAura != null
                && aimLiftStart.sprite != null && aimLiftEnd.sprite != null
                && castRecoil.sprite != null && castRecover.sprite != null;
        }

        private void HandleEffectsChanged()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            bool wasRaised = isRaised;
            isRaised = HasRaisingEffect();
            if (isThrusting || wasRaised == isRaised)
            {
                return;
            }

            StopMotion();
            if (hasFrameAnimation)
            {
                StaffFrame first = isRaised ? aimLiftStart : aimLiftEnd;
                StaffFrame last = isRaised ? aimLiftEnd : aimLiftStart;
                Sequence transition = StartMotion();
                if (resolvedRenderer.sprite != last.sprite)
                {
                    ApplyFrame(first);
                    transition.AppendInterval(FrameDuration)
                        .AppendCallback(() => ApplyFrame(last));
                }
                transition.AppendInterval(FrameDuration);
                return;
            }

            ApplyCurrentPose();
        }

        private void Play()
        {
            if (!isActiveAndEnabled || resolvedRenderer == null)
            {
                return;
            }

            // 공격 이벤트가 오면 조준 중이어도 발사 자세를 즉시 표시한다.
            StopMotion();
            isThrusting = true;
            ApplyCurrentPose();

            Sequence cast = StartMotion();
            if (!hasFrameAnimation)
            {
                cast.AppendInterval(thrustDuration);
                return;
            }

            cast.AppendInterval(2f * FrameDuration)
                .AppendCallback(() => ApplyFrame(castRecoil))
                .AppendInterval(FrameDuration)
                .AppendCallback(() => ApplyFrame(castRecover))
                .AppendInterval(FrameDuration)
                .AppendCallback(() => ApplyFrame(isRaised ? aimLiftEnd : aimLiftStart))
                .AppendInterval(FrameDuration);
        }

        private Sequence StartMotion()
        {
            motion = DOTween.Sequence();
            motion.SetLink(gameObject);
            motion.OnComplete(() =>
            {
                motion = null;
                isThrusting = false;
                ApplyCurrentPose();
            });
            return motion;
        }

        private void StopMotion()
        {
            motion?.Kill(false);
            motion = null;
            isThrusting = false;
        }

        private void ApplyFrame(StaffFrame frame)
        {
            resolvedRenderer.sprite = frame.sprite;
            staffAura.SetPosePosition(frame.tip);
        }

        private void ApplyCurrentPose()
        {
            if (resolvedRenderer == null)
            {
                return;
            }

            resolvedRenderer.sprite = ResolvePoseSprite();
            if (hasFrameAnimation)
            {
                Vector2 tip = isThrusting ? thrustTip : isRaised ? raisedTip : loweredTip;
                staffAura.SetPosePosition(tip);
            }
        }

        /// <summary>
        /// Sprite for the current pose, falling back to whichever staff sprite is actually
        /// assigned so an empty field never throws and never blanks the renderer needlessly.
        /// </summary>
        private Sprite ResolvePoseSprite()
        {
            Sprite preferredSprite = isThrusting
                ? staffThrustSprite
                : isRaised ? staffRaisedSprite : staffLoweredSprite;

            if (preferredSprite != null)
            {
                return preferredSprite;
            }

            if (staffLoweredSprite != null)
            {
                return staffLoweredSprite;
            }

            if (staffRaisedSprite != null)
            {
                return staffRaisedSprite;
            }

            return staffThrustSprite;
        }

        /// <summary>
        /// Whether any name in <see cref="raisingEffects"/> is currently active on the owner. The
        /// element idle auras belong here; player status effects such as <c>Burn</c> or
        /// <c>Panic</c> must not be listed, since those do not mean a card is selected.
        /// </summary>
        private bool HasRaisingEffect()
        {
            if (raisingEffects == null || raisingEffects.Length == 0)
            {
                return false;
            }

            foreach (string activeEffect in Owner.ActiveEffects)
            {
                for (int i = 0; i < raisingEffects.Length; i++)
                {
                    if (string.Equals(activeEffect, raisingEffects[i], StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
