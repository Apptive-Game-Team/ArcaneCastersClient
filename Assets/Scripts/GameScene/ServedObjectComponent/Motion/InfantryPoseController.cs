using UnityEngine;

namespace GameScene.ServedObjectComponent.Motion
{
    /// <summary>걷기와 공격이 같은 renderer를 덮어쓰지 않도록 비전 보병의 자세를 한 곳에서 선택한다.</summary>
    public sealed class InfantryPoseController : ServedObjectBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite walkLeftSprite;
        [SerializeField] private Sprite walkRightSprite;
        [SerializeField] private Sprite attackSprite;
        [SerializeField] private float stepDuration = 0.16f;
        [SerializeField] private float attackDuration = 0.16f;

        private SpriteRenderer resolvedRenderer;
        private float movementUntil = float.NegativeInfinity;
        private float movementStarted;
        private float attackUntil = float.NegativeInfinity;

        protected override void OnBound()
        {
            resolvedRenderer = ResolveRenderer(targetRenderer);
            ResetPose();
            Owner.OnMoved += HandleMoved;
            Owner.OnAttack += HandleAttack;
        }

        protected override void OnUnbound()
        {
            if (Owner != null)
            {
                Owner.OnMoved -= HandleMoved;
                Owner.OnAttack -= HandleAttack;
            }
            ResetPose();
        }

        private void OnDisable() => ResetPose();

        private void HandleMoved()
        {
            if (!isActiveAndEnabled) return;
            float now = Time.time;
            // 연속 이동 패킷은 종료 시각만 연장한다. 매번 위상을 초기화하면 첫 발만 보인다.
            if (now >= movementUntil) movementStarted = now;
            movementUntil = now + Mathf.Max(0.08f, 2f * GameConfig.FrameDuration);
            ApplyPose(now);
        }

        private void HandleAttack()
        {
            if (!isActiveAndEnabled) return;
            attackUntil = Time.time + Mathf.Max(0.01f, attackDuration);
            ApplyPose(Time.time);
        }

        private void LateUpdate() => ApplyPose(Time.time);

        private void ApplyPose(float now)
        {
            if (resolvedRenderer == null) return;
            Sprite pose = idleSprite;
            if (now < attackUntil)
            {
                pose = attackSprite;
            }
            else if (now < movementUntil)
            {
                int step = Mathf.FloorToInt((now - movementStarted) / Mathf.Max(0.01f, stepDuration));
                pose = (step & 1) == 0 ? walkLeftSprite : walkRightSprite;
            }
            if (pose != null) resolvedRenderer.sprite = pose;
        }

        private void ResetPose()
        {
            movementUntil = attackUntil = float.NegativeInfinity;
            movementStarted = 0f;
            if (resolvedRenderer != null && idleSprite != null) resolvedRenderer.sprite = idleSprite;
        }
    }
}
