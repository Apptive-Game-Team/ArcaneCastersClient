using GameScene.ServedObjectComponent.Motion;
using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    /// <summary>Server statuses select the siege pose in matches, reconnects and previews.</summary>
    public sealed class CraterCarrierPresenter : ServedObjectBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite carriedSprite;
        [SerializeField] private Sprite deployedSprite;
        [SerializeField] private Sprite recoveringSprite;
        [SerializeField] private GroundedUnitPoseController walkPose;

        protected override void OnBound()
        {
            if (walkPose == null)
            {
                targetRenderer = ResolveRenderer(targetRenderer);
            }
            Owner.OnOtherStatus += ShowStatus;
            Owner.OnAttack += ShowAttack;
            ShowStatus("Idle");
        }

        protected override void OnUnbound()
        {
            if (Owner == null) return;
            Owner.OnOtherStatus -= ShowStatus;
            Owner.OnAttack -= ShowAttack;
        }

        private void ShowAttack()
        {
            ShowStatus("Deployed");
        }

        private void ShowStatus(string status)
        {
            Sprite next = status switch
            {
                "Deployed" => deployedSprite,
                "Deploying" or "Recovering" => recoveringSprite,
                "Idle" or "Move" => carriedSprite,
                _ => null
            };
            if (next == null || Owner.IsDespawning)
            {
                return;
            }

            if (walkPose != null)
            {
                walkPose.SetStationaryPose(status is "Idle" or "Move" ? null : next);
            }
            else if (targetRenderer != null)
            {
                targetRenderer.sprite = next;
            }
        }
    }
}
