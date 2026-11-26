using UnityEngine;

namespace GameScene.ServedObjectComponent.Motion
{
    public sealed class DropPoseController : ServedObjectBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite[] descentFrames;
        [SerializeField, Min(0.01f)] private float initialHeight = 10f;

        protected override void OnBound()
        {
            targetRenderer = ResolveRenderer(targetRenderer);
            if (targetRenderer == null || descentFrames == null || descentFrames.Length == 0)
            {
                return;
            }

            Vector3 position = Owner.transform.position;
            if (Owner.PresentationWorld != null)
            {
                position = Owner.PresentationWorld.transform.InverseTransformPoint(position);
            }

            SetPose(position);
            Owner.OnServerPositionUpdated += SetPose;
        }

        protected override void OnUnbound()
        {
            if (Owner != null)
            {
                Owner.OnServerPositionUpdated -= SetPose;
            }
        }

        private void SetPose(Vector3 serverPosition)
        {
            float progress = 1f - Mathf.Clamp01(serverPosition.y / initialHeight);
            int index = Mathf.Min((int)(progress * descentFrames.Length), descentFrames.Length - 1);
            if (targetRenderer.sprite != descentFrames[index])
            {
                targetRenderer.sprite = descentFrames[index];
            }
        }
    }
}
