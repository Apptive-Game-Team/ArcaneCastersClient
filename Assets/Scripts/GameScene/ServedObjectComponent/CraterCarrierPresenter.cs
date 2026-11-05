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

        protected override void OnBound()
        {
            targetRenderer = ResolveRenderer(targetRenderer);
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
            Sprite next = null;
            switch (status)
            {
                case "Deployed":
                    next = deployedSprite;
                    break;
                case "Deploying": // Same held-weapon frame reads as lowering it into place.
                case "Recovering":
                    next = recoveringSprite;
                    break;
                case "Idle":
                case "Move":
                    next = carriedSprite;
                    break;
            }
            if (targetRenderer != null && next != null)
                targetRenderer.sprite = next;
        }
    }
}
