using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    public sealed class ShockTrapArmingPresentation : ServedObjectBehaviour
    {
        private const string ArmingEffect = "ShockTrapArming";

        [SerializeField] private SpriteFrameAnimator armingAnimation;

        protected override void OnBound()
        {
            Owner.OnEffectsChanged += StartArming;
            StartArming();
        }

        protected override void OnUnbound()
        {
            if (Owner != null)
                Owner.OnEffectsChanged -= StartArming;
        }

        private void StartArming()
        {
            if (armingAnimation != null && !armingAnimation.enabled && Owner.HasEffect(ArmingEffect))
                armingAnimation.enabled = true;
        }
    }
}
