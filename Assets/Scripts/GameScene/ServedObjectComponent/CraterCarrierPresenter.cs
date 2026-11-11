using GameScene.ServedObjectComponent.Motion;
using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    /// <summary>서버가 보낸 설치 상태를 기준으로 운반과 포격 자세를 재생한다.</summary>
    public sealed class CraterCarrierPresenter : UnitSpriteMotionController
    {
        private enum SiegePhase { Carried, Deploying, Deployed, Recovering }

        [SerializeField] private Sprite carriedSprite;
        [SerializeField] private Sprite deployedSprite;
        [SerializeField] private Sprite recoveringSprite;
        [SerializeField] private Sprite[] carryFrames;
        [SerializeField] private Sprite transitionSprite;
        [SerializeField] private Sprite recoilSprite;
        [SerializeField] private Sprite[] deployedHurtFrames;
        [SerializeField] private Sprite[] deployedDeathFrames;

        private SiegePhase phase;
        private float phaseStarted;
        private float moveStarted;
        private float moveUntil = float.NegativeInfinity;
        private float recoilUntil = float.NegativeInfinity;

        protected override Sprite[] HurtFrames => UseDeployedReaction && deployedHurtFrames?.Length > 0
            ? deployedHurtFrames : base.HurtFrames;
        protected override Sprite[] DeathFrames => UseDeployedReaction && deployedDeathFrames?.Length > 0
            ? deployedDeathFrames : base.DeathFrames;

        private bool UseDeployedReaction => phase == SiegePhase.Deployed ||
            (phase == SiegePhase.Deploying && Time.time - phaseStarted >= 2f * FrameDuration) ||
            (phase == SiegePhase.Recovering && Time.time - phaseStarted < 5f * FrameDuration);

        protected override void OnBound()
        {
            phase = SiegePhase.Carried;
            phaseStarted = Time.time;
            moveUntil = recoilUntil = float.NegativeInfinity;
            base.OnBound();
            Owner.OnOtherStatus += ShowStatus;
            Owner.OnAttack += ShowAttack;
            Owner.OnMoved += ShowMove;
            ShowStatus("Idle");
        }

        protected override void OnUnbound()
        {
            if (Owner != null)
            {
                Owner.OnOtherStatus -= ShowStatus;
                Owner.OnAttack -= ShowAttack;
                Owner.OnMoved -= ShowMove;
            }
            base.OnUnbound();
        }

        private void ShowAttack()
        {
            if (!isActiveAndEnabled) return;
            float now = Time.time;
            SetPhase(SiegePhase.Deployed, now);
            recoilUntil = now + 2f * FrameDuration;
            ApplyPose(now);
        }

        private void ShowMove()
        {
            if (!isActiveAndEnabled || carryFrames == null || carryFrames.Length == 0) return;
            float now = Time.time;
            if (now >= moveUntil) moveStarted = now;
            moveUntil = now + 2f * GameConfig.FrameDuration;
            ApplyPose(now);
        }

        private void ShowStatus(string status)
        {
            float now = Time.time;
            switch (status)
            {
                case "Deployed":
                    SetPhase(SiegePhase.Deployed, now);
                    break;
                case "Deploying":
                    SetPhase(SiegePhase.Deploying, now);
                    break;
                case "Recovering":
                    SetPhase(SiegePhase.Recovering, now);
                    break;
                case "Idle":
                case "Move":
                    SetPhase(SiegePhase.Carried, now);
                    break;
                default:
                    return;
            }
            ApplyPose(now);
        }

        private void SetPhase(SiegePhase next, float now)
        {
            if (phase == next) return;
            phase = next;
            phaseStarted = now;
            moveUntil = recoilUntil = float.NegativeInfinity;
        }

        protected override Sprite GetPriorityPose(float now, bool attacking)
        {
            if (phase == SiegePhase.Deploying || phase == SiegePhase.Recovering)
                return TransitionPose(now - phaseStarted);
            return phase == SiegePhase.Deployed && now < recoilUntil ? recoilSprite : null;
        }

        protected override Sprite GetRestingPose(float now)
        {
            if (phase == SiegePhase.Deployed) return deployedSprite;
            if (now >= moveUntil) return carriedSprite;

            int frame = Mathf.Max(0, Mathf.FloorToInt((now - moveStarted) / FrameDuration));
            return carryFrames[frame % carryFrames.Length];
        }

        private Sprite TransitionPose(float elapsed)
        {
            if (transitionSprite == null) return recoveringSprite;

            bool deploying = phase == SiegePhase.Deploying;
            if (elapsed < 2f * FrameDuration) return deploying ? carriedSprite : recoveringSprite;
            if (elapsed < 5f * FrameDuration) return transitionSprite;
            return deploying ? recoveringSprite : carriedSprite;
        }
    }
}
