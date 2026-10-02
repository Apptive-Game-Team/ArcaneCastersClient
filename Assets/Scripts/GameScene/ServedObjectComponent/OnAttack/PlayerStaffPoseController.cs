using System;
using Data;
using Global;
using DG.Tweening;
using UnityEngine;
using Sequence = DG.Tweening.Sequence;

namespace GameScene.ServedObjectComponent.OnAttack
{
    /// <summary>
    /// The single owner of the player body's <see cref="SpriteRenderer.sprite"/>.
    /// <para>
    /// The staff is lowered by default, raised while a magic card is selected, and thrust for
    /// <see cref="thrustDuration"/> when an attack fires. This has to be one component: with
    /// <see cref="AttackSpriteSwapController"/> also writing the same renderer, the thrust frame
    /// would be overwritten if the raised/lowered pose changed mid-attack, and that controller's
    /// own restore would put back a pose that is no longer current. This component replaces
    /// <see cref="AttackSpriteSwapController"/> on the player prefab.
    /// </para>
    /// </summary>
    public sealed class PlayerStaffPoseController : ServedObjectBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite staffLoweredSprite;
        [SerializeField] private Sprite staffRaisedSprite;
        [SerializeField] private Sprite staffThrustSprite;
        [SerializeField] private float thrustDuration = 0.3f;
        [SerializeField] private string[] raisingEffects;

        private SpriteRenderer resolvedRenderer;
        private Sequence thrustSequence;
        private bool isThrusting;
        // Updated only when Owner.OnEffectsChanged fires, so applying the current pose never has
        // to re-scan ActiveEffects.
        private bool isRaised;

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

            StopThrustSequence();
            EndThrust();
        }

        private void OnDisable()
        {
            StopThrustSequence();
            EndThrust();
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
        }

        private void HandleEffectsChanged()
        {
            isRaised = HasRaisingEffect();
            if (isThrusting)
            {
                // The thrust keeps playing; this only changes the pose it returns to.
                return;
            }

            ApplyCurrentPose();
        }

        /// <summary>Plays the thrust. Safe to call again while a previous thrust is still running.</summary>
        private void Play()
        {
            if (resolvedRenderer == null)
            {
                return;
            }

            StopThrustSequence();
            isThrusting = true;
            ApplyCurrentPose();

            Sequence attackSequence = DOTween.Sequence();
            thrustSequence = attackSequence;
            attackSequence.SetLink(gameObject);
            attackSequence
                .AppendInterval(thrustDuration)
                .AppendCallback(EndThrust)
                .OnComplete(() => ClearSequence(attackSequence))
                .OnKill(() =>
                {
                    ClearSequence(attackSequence);
                    EndThrust();
                });
        }

        private void ClearSequence(Sequence finished)
        {
            if (thrustSequence == finished)
            {
                thrustSequence = null;
            }
        }

        private void StopThrustSequence()
        {
            Sequence activeSequence = thrustSequence;
            thrustSequence = null;
            if (activeSequence != null && activeSequence.IsActive())
            {
                activeSequence.Kill(false);
            }
        }

        /// <summary>
        /// Ends the thrust and re-applies whatever pose is current right now, not the pose that was
        /// current when the thrust started.
        /// </summary>
        private void EndThrust()
        {
            isThrusting = false;
            ApplyCurrentPose();
        }

        private void ApplyCurrentPose()
        {
            if (resolvedRenderer == null)
            {
                return;
            }

            resolvedRenderer.sprite = ResolvePoseSprite();
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
