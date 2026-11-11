using DG.Tweening;
using UnityEngine;
using Sequence = DG.Tweening.Sequence;

namespace GameScene.ServedObjectComponent.Effect
{
    /// <summary>
    /// Moves the staff tip anchor through the body's attack frame swap.
    /// <para>
    /// The player's element auras are the ordinary server-driven effects: selecting a fire card
    /// puts <c>FireIdleAura</c> in the object's effect list, and several selected elements stack.
    /// <see cref="ServedObject"/> parents them to this anchor instead of the body, so they gather
    /// at the staff tip. The tip moves when the attack frame swaps in, and the body sprite carries
    /// no transform of its own to move them, so the anchor is moved here for the same duration.
    /// </para>
    /// </summary>
    public sealed class PlayerStaffAuraController : ServedObjectBehaviour
    {
        [SerializeField] private Transform auraAnchor;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Vector3 idleLocalPosition;
        [SerializeField] private Vector3 attackLocalPosition;
        [SerializeField] private float attackDuration = 0.18f;

        private Sequence sequence;
        private Vector3 currentLocalPosition;
        private bool lastFlipX;
        private bool poseDriven;

        protected override void OnBound()
        {
            if (auraAnchor == null || bodyRenderer == null || Owner == null)
            {
                return;
            }

            lastFlipX = bodyRenderer.flipX;
            ApplyPosition(poseDriven ? currentLocalPosition : idleLocalPosition);
            Owner.OnAttack += Play;
        }

        protected override void OnUnbound()
        {
            if (Owner != null)
            {
                Owner.OnAttack -= Play;
            }

            StopAndRestore();
        }

        private void OnDisable()
        {
            StopAndRestore();
        }

        private void Update()
        {
            if (bodyRenderer == null || bodyRenderer.flipX == lastFlipX)
            {
                return;
            }

            lastFlipX = bodyRenderer.flipX;
            ApplyPosition(currentLocalPosition);
        }

        private void Play()
        {
            if (poseDriven || auraAnchor == null || bodyRenderer == null)
            {
                return;
            }

            StopSequence();
            ApplyPosition(attackLocalPosition);

            Sequence attackSequence = DOTween.Sequence();
            sequence = attackSequence;
            attackSequence.SetLink(gameObject);
            attackSequence
                .AppendInterval(attackDuration)
                .AppendCallback(RestoreIdle)
                .OnComplete(() => ClearSequence(attackSequence))
                .OnKill(() =>
                {
                    ClearSequence(attackSequence);
                    RestoreIdle();
                });
        }

        public void SetPosePosition(Vector2 position)
        {
            // 프레임을 재생하는 외형은 자세를 고르는 컴포넌트와 같은 순간에 위치를 바꾼다.
            if (!poseDriven)
            {
                poseDriven = true;
                StopSequence();
            }

            ApplyPosition(position);
        }

        private void ClearSequence(Sequence finished)
        {
            if (sequence == finished)
            {
                sequence = null;
            }
        }

        private void StopAndRestore()
        {
            StopSequence();
            RestoreIdle();
        }

        private void StopSequence()
        {
            Sequence activeSequence = sequence;
            sequence = null;
            if (activeSequence != null && activeSequence.IsActive())
            {
                activeSequence.Kill(false);
            }
        }

        private void RestoreIdle()
        {
            if (!poseDriven)
            {
                ApplyPosition(idleLocalPosition);
            }
        }

        private void ApplyPosition(Vector3 localPosition)
        {
            currentLocalPosition = localPosition;
            if (auraAnchor == null)
            {
                return;
            }

            Vector3 mirroredPosition = localPosition;
            if (bodyRenderer != null && bodyRenderer.flipX)
            {
                mirroredPosition.x *= -1;
            }

            auraAnchor.localPosition = mirroredPosition;
        }
    }
}
