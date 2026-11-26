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
    /// 플레이어의 조준과 시전 자세를 관리한다. 시전 중 카드 선택 변화는 시전 뒤 복귀할 자세에 반영한다.
    /// 기본 외형의 자세마다 원소 효과의 위치를 지팡이 끝에 맞춘다.
    /// </summary>
    public sealed class PlayerStaffPoseController : ServedObjectBehaviour
    {
        [Serializable]
        private struct StaffFrame
        {
            public Sprite sprite;
            public Vector2 tip;
        }

        private const float AimFrameDuration = 0.08f;

        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite staffLoweredSprite;
        [SerializeField] private Sprite staffRaisedSprite;
        [SerializeField] private Sprite staffThrustSprite;
        [SerializeField] private float thrustDuration = 0.3f;
        [SerializeField] private string[] raisingEffects;
        [SerializeField] private PlayerStaffAuraController staffAura;
        [SerializeField] private StaffFrame[] aimFrames;
        [SerializeField] private StaffFrame[] castFrames;

        private SpriteRenderer resolvedRenderer;
        private Sequence motion;
        private bool hasAuthoredFrames;
        private int aimFrame;
        private bool isThrusting;
        // 효과가 바뀌면 선택 상태를 갱신해 시전 뒤에도 현재 카드에 맞는 자세로 복귀한다.
        private bool isRaised;

        internal bool DrivesStaffAura => hasAuthoredFrames && enabled;

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
            if (enabled)
            {
                ApplyRestPose();
            }
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
            ApplyRestPose();
        }

        private void OnEnable()
        {
            if (resolvedRenderer != null && Owner != null)
            {
                isRaised = HasRaisingEffect();
                ApplyRestPose();
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
            hasAuthoredFrames = loadedId == PlayerAppearanceResolver.DefaultId && staffAura != null && staffAura.IsConfigured
                && HasAllFrames(aimFrames, 4) && HasAllFrames(castFrames, 3)
                && aimFrames[0].sprite == idle && aimFrames[3].sprite == raised && castFrames[0].sprite == attacking;
        }

        private static bool HasAllFrames(StaffFrame[] frames, int count)
        {
            if (frames == null || frames.Length != count)
            {
                return false;
            }

            foreach (StaffFrame frame in frames)
            {
                if (frame.sprite == null)
                {
                    return false;
                }
            }

            return true;
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
                // The thrust keeps playing; this only changes the pose it returns to.
                return;
            }

            if (hasAuthoredFrames)
            {
                AnimateAim();
                return;
            }

            ApplyRestPose();
        }

        private void AnimateAim()
        {
            StopMotion();
            int targetFrame = isRaised ? aimFrames.Length - 1 : 0;
            if (aimFrame == targetFrame)
            {
                return;
            }

            int direction = targetFrame > aimFrame ? 1 : -1;
            Sequence sequence = StartMotion();
            for (int frame = aimFrame; frame != targetFrame; frame += direction)
            {
                int nextFrame = frame + direction;
                sequence.AppendInterval(AimFrameDuration).AppendCallback(() => ApplyAimFrame(nextFrame));
            }
        }

        /// <summary>공격 신호를 받으면 이전 동작을 끝내고 발사 자세를 표시한다.</summary>
        private void Play()
        {
            if (resolvedRenderer == null || !isActiveAndEnabled)
            {
                return;
            }

            StopMotion();
            isThrusting = true;
            Sequence sequence = StartMotion().AppendInterval(thrustDuration);
            if (hasAuthoredFrames)
            {
                ApplyFrame(castFrames[0]);
                sequence.InsertCallback(thrustDuration * 0.4f, () => ApplyFrame(castFrames[1]));
                sequence.InsertCallback(thrustDuration * (2f / 3f), () => ApplyFrame(castFrames[2]));
                return;
            }

            resolvedRenderer.sprite = ResolvePoseSprite();
        }

        private Sequence StartMotion()
        {
            motion = DOTween.Sequence().SetLink(gameObject).OnComplete(() =>
            {
                motion = null;
                isThrusting = false;
                ApplyRestPose();
            });
            return motion;
        }

        private void StopMotion()
        {
            motion?.Kill(false);
            motion = null;
            isThrusting = false;
        }

        private void ApplyRestPose()
        {
            if (resolvedRenderer == null)
            {
                return;
            }

            if (hasAuthoredFrames)
            {
                ApplyAimFrame(isRaised ? aimFrames.Length - 1 : 0);
                return;
            }

            resolvedRenderer.sprite = ResolvePoseSprite();
        }

        private void ApplyAimFrame(int index)
        {
            aimFrame = index;
            ApplyFrame(aimFrames[index]);
        }

        private void ApplyFrame(StaffFrame frame)
        {
            resolvedRenderer.sprite = frame.sprite;
            staffAura.SetPosePosition(frame.tip);
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
