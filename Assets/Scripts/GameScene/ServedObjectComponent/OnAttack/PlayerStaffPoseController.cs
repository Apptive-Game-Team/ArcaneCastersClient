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
    /// 프레임이 있는 외형은 자세마다 원소 효과의 위치를 지팡이 끝에 맞춘다.
    /// </summary>
    public sealed class PlayerStaffPoseController : ServedObjectBehaviour
    {
        [Serializable]
        private struct StaffFrame
        {
            public Sprite sprite;
            public Vector2 tip;
        }

        [Serializable]
        private sealed class StaffMotion
        {
            public string appearanceId;
            public StaffFrame[] aimFrames;
            public StaffFrame[] castFrames;
            public float[] castCueFractions;
        }

        private const float AimFrameDuration = 0.08f;

        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite staffLoweredSprite;
        [SerializeField] private Sprite staffRaisedSprite;
        [SerializeField] private Sprite staffThrustSprite;
        [SerializeField] private float thrustDuration = 0.3f;
        [SerializeField] private string[] raisingEffects;
        [SerializeField] private PlayerStaffAuraController staffAura;
        [SerializeField] private StaffMotion[] appearanceMotions;

        private SpriteRenderer resolvedRenderer;
        private Sequence motion;
        private StaffMotion appearanceMotion;
        private int aimFrame;
        private bool isThrusting;
        // 효과가 바뀌면 선택 상태를 갱신해 시전 뒤에도 현재 카드에 맞는 자세로 복귀한다.
        private bool isRaised;

        internal bool DrivesStaffAura => appearanceMotion != null && enabled;

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
            appearanceMotion = FindMotion(loadedId, idle, raised, attacking);
        }

        private StaffMotion FindMotion(string appearanceId, Sprite idle, Sprite raised, Sprite attacking)
        {
            if (staffAura == null || !staffAura.IsConfigured || appearanceMotions == null)
            {
                return null;
            }

            foreach (StaffMotion entry in appearanceMotions)
            {
                if (entry == null || entry.appearanceId != appearanceId
                    || !HasAllFrames(entry.aimFrames) || !HasAllFrames(entry.castFrames)
                    || entry.aimFrames[0].sprite != idle
                    || entry.aimFrames[entry.aimFrames.Length - 1].sprite != raised
                    || entry.castFrames[0].sprite != attacking
                    || !HasValidCastCues(entry.castCueFractions, entry.castFrames.Length - 1))
                {
                    continue;
                }

                return entry;
            }

            return null;
        }

        private static bool HasAllFrames(StaffFrame[] frames)
        {
            if (frames == null || frames.Length < 2)
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

        private static bool HasValidCastCues(float[] cues, int count)
        {
            if (cues == null || cues.Length != count)
            {
                return false;
            }

            float previous = 0f;
            foreach (float fraction in cues)
            {
                if (!(fraction > previous && fraction < 1f))
                {
                    return false;
                }

                previous = fraction;
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

            if (appearanceMotion != null)
            {
                AnimateAim();
                return;
            }

            ApplyRestPose();
        }

        private void AnimateAim()
        {
            StopMotion();
            int targetFrame = isRaised ? appearanceMotion.aimFrames.Length - 1 : 0;
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
            if (appearanceMotion != null)
            {
                StaffFrame[] castFrames = appearanceMotion.castFrames;
                ApplyFrame(castFrames[0]);
                for (int frame = 1; frame < castFrames.Length; frame++)
                {
                    StaffFrame nextFrame = castFrames[frame];
                    sequence.InsertCallback(thrustDuration * appearanceMotion.castCueFractions[frame - 1],
                        () => ApplyFrame(nextFrame));
                }
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

            if (appearanceMotion != null)
            {
                ApplyAimFrame(isRaised ? appearanceMotion.aimFrames.Length - 1 : 0);
                return;
            }

            resolvedRenderer.sprite = ResolvePoseSprite();
        }

        private void ApplyAimFrame(int index)
        {
            aimFrame = index;
            ApplyFrame(appearanceMotion.aimFrames[index]);
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
