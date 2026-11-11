using UnityEngine;

namespace GameScene.ServedObjectComponent.OnAttack
{
    public sealed class CannonMuzzleFlash : MonoBehaviour
    {
        private const float FrameDuration = 1f / 30f;

        [SerializeField] private SpriteRenderer barrelRenderer;
        [SerializeField] private Vector2 muzzleOffset;
        [SerializeField] private float barrelAngle;
        [SerializeField] private float flashScale = 1f;
        [SerializeField] private Sprite[] frames;

        private SpriteRenderer flash;
        private float startedAt;

        public bool TryPlay(out Vector3 origin)
        {
            origin = default;
            if (!isActiveAndEnabled || barrelRenderer == null || frames == null || frames.Length == 0)
            {
                return false;
            }

            if (flash == null)
            {
                var child = new GameObject("MuzzleFlash");
                child.transform.SetParent(barrelRenderer.transform, false);
                flash = child.AddComponent<SpriteRenderer>();
            }

            float direction = barrelRenderer.flipX ? -1f : 1f;
            flash.transform.localPosition = new Vector3(muzzleOffset.x * direction, muzzleOffset.y, 0f);
            flash.transform.localRotation = Quaternion.Euler(0f, 0f, barrelAngle * direction);
            flash.transform.localScale = Vector3.one * flashScale;
            flash.sortingLayerID = barrelRenderer.sortingLayerID;
            flash.sortingOrder = barrelRenderer.sortingOrder + 1;
            flash.flipX = barrelRenderer.flipX;
            flash.sprite = frames[0];
            flash.enabled = true;
            startedAt = Time.time;
            origin = flash.transform.position;
            return true;
        }

        private void LateUpdate()
        {
            if (flash == null || !flash.enabled) return;

            int index = Mathf.FloorToInt((Time.time - startedAt) / FrameDuration);
            if (index >= frames.Length)
            {
                flash.enabled = false;
                return;
            }

            flash.sprite = frames[index];
        }

        private void OnDisable()
        {
            if (flash != null) flash.enabled = false;
        }
    }
}
