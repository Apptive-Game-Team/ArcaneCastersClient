using DG.Tweening;
using GameScene.ServedObjectComponent.OnAttack;
using UnityEngine;
using Sequence = DG.Tweening.Sequence;

namespace GameScene.ServedObjectComponent.Effect
{
    /// <summary>
    /// 서버 원소 효과의 앵커를 지팡이 끝에 놓고 플레이어가 뒤집히면 좌우를 바꾼다.
    /// 외형의 자세마다 위치가 지정되어 있으면 자세 컨트롤러가 앵커 위치를 전달한다.
    /// </summary>
    public sealed class PlayerStaffAuraController : ServedObjectBehaviour
    {
        [SerializeField] private Transform auraAnchor;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Vector3 idleLocalPosition;
        [SerializeField] private Vector3 attackLocalPosition;
        [SerializeField] private float attackDuration = 0.18f;

        private Sequence sequence;
        private PlayerStaffPoseController poseController;
        private Vector3 currentLocalPosition;
        private bool lastFlipX;

        internal bool IsConfigured => auraAnchor != null && bodyRenderer != null;

        protected override void OnBound()
        {
            if (!IsConfigured || Owner == null)
            {
                return;
            }

            poseController = GetComponent<PlayerStaffPoseController>();
            if (poseController == null || !poseController.DrivesStaffAura)
            {
                ApplyPosition(idleLocalPosition);
            }
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

        private void OnEnable()
        {
            if (Owner != null && IsConfigured)
            {
                ApplyPosition(currentLocalPosition);
            }
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
            if (!IsConfigured || !isActiveAndEnabled || (poseController != null && poseController.DrivesStaffAura))
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
            if (poseController == null || !poseController.DrivesStaffAura)
            {
                RestoreIdle();
            }
        }

        internal void SetPosePosition(Vector2 tip)
        {
            StopSequence();
            ApplyPosition(new Vector3(tip.x, tip.y, idleLocalPosition.z));
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
            ApplyPosition(idleLocalPosition);
        }

        private void ApplyPosition(Vector3 localPosition)
        {
            currentLocalPosition = localPosition;
            if (auraAnchor == null)
            {
                return;
            }

            Vector3 mirroredPosition = localPosition;
            lastFlipX = bodyRenderer != null && bodyRenderer.flipX;
            if (lastFlipX)
            {
                mirroredPosition.x *= -1;
            }

            auraAnchor.localPosition = mirroredPosition;
        }
    }
}
