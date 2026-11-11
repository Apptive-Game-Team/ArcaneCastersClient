using GameScene.ServedObjectComponent.Motion;
using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    public sealed class ChickenCommandoSpriteSwap : UnitSpriteMotionController
    {
        [SerializeField] private Sprite elevatedSprite;
        [SerializeField] private Sprite landingSprite;
        [SerializeField] private float heightThreshold = 0.5f;

        private bool wasElevated;
        private float landingUntil = float.NegativeInfinity;

        protected override Sprite GetPriorityPose(float now, bool attacking)
        {
            if (transform.position.y > heightThreshold)
            {
                wasElevated = true;
                return elevatedSprite;
            }

            if (wasElevated)
            {
                wasElevated = false;
                landingUntil = now + 3f * FrameDuration;
            }

            return !attacking && now < landingUntil ? landingSprite : null;
        }

        protected override void OnDisable()
        {
            wasElevated = false;
            landingUntil = float.NegativeInfinity;
            base.OnDisable();
        }
    }
}
