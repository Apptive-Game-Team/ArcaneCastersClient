using UnityEngine;

namespace GameScene.ServedObjectComponent.Motion
{
    public sealed class BrickGolemPoseController : ServedObjectBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite raisedArmSprite;
        [SerializeField] private Sprite[] sweepFrames;

        private const float RaisedArmDuration = 0.1f;

        private SpriteRenderer resolvedRenderer;
        private Sprite idleSprite;
        private float raisedArmUntil = float.NegativeInfinity;
        private float sweepStarted = float.NegativeInfinity;
        private float sweepDuration;

        protected override void OnBound()
        {
            resolvedRenderer = ResolveRenderer(targetRenderer);
            if (resolvedRenderer == null)
            {
                return;
            }

            idleSprite = resolvedRenderer.sprite;
            Owner.OnAttack += HandleAttack;
        }

        protected override void OnUnbound()
        {
            if (Owner != null)
            {
                Owner.OnAttack -= HandleAttack;
            }

            ResetPose();
        }

        internal void PlaySweep(float duration)
        {
            if (!isActiveAndEnabled || sweepFrames == null || sweepFrames.Length == 0)
            {
                return;
            }

            sweepStarted = Time.time;
            sweepDuration = duration;
            ApplyPose(sweepStarted);
        }

        private void HandleAttack()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            float now = Time.time;
            raisedArmUntil = now + RaisedArmDuration;
            ApplyPose(now);
        }

        private void LateUpdate() => ApplyPose(Time.time);

        private void OnDisable() => ResetPose();

        private void ApplyPose(float now)
        {
            if (resolvedRenderer == null)
            {
                return;
            }

            Sprite pose = idleSprite;
            float elapsed = now - sweepStarted;
            // 같은 서버 프레임의 Attack 상태 갱신이 휩쓸기 자세를 덮지 않게 한다.
            if (elapsed < sweepDuration)
            {
                int index = Mathf.Min((int)(elapsed * sweepFrames.Length / sweepDuration), sweepFrames.Length - 1);
                pose = sweepFrames[index];
            }
            else if (now < raisedArmUntil)
            {
                pose = raisedArmSprite;
            }

            if (pose != null && resolvedRenderer.sprite != pose)
            {
                resolvedRenderer.sprite = pose;
            }
        }

        private void ResetPose()
        {
            raisedArmUntil = sweepStarted = float.NegativeInfinity;
            sweepDuration = 0f;
            if (resolvedRenderer != null && idleSprite != null)
            {
                resolvedRenderer.sprite = idleSprite;
            }
        }
    }
}
