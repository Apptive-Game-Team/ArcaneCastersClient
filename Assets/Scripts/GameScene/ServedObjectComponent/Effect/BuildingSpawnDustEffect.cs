using DG.Tweening;
using UnityEngine;
using Sequence = DG.Tweening.Sequence;

namespace GameScene.ServedObjectComponent.Effect
{
    [DisallowMultipleComponent]
    public class BuildingSpawnDustEffect : MonoBehaviour
    {
        private const int PuffCount = 6;
        private const int TextureSize = 32;
        private const float EffectDuration = 0.55f;

        private static readonly Color DustColor = new Color(0.72f, 0.65f, 0.52f, 0.5f);
        private static Sprite dustSprite;

        private Sequence sequence;

        public static void Spawn(
            Vector3 worldPosition,
            Quaternion cameraRotation,
            SpriteRenderer referenceRenderer)
        {
            GameObject effectObject = new GameObject(nameof(BuildingSpawnDustEffect));
            effectObject.transform.SetPositionAndRotation(worldPosition, cameraRotation);
            GameScene.Object.PresentationWorld.For(referenceRenderer)?.Own(effectObject);

            BuildingSpawnDustEffect effect = effectObject.AddComponent<BuildingSpawnDustEffect>();
            effect.Play(referenceRenderer);
        }

        private void Play(SpriteRenderer referenceRenderer)
        {
            sequence = DOTween.Sequence();
            sequence.SetLink(gameObject);

            for (int i = 0; i < PuffCount; i++)
            {
                float normalizedIndex = PuffCount == 1 ? 0.5f : i / (float)(PuffCount - 1);
                float horizontalDirection = Mathf.Lerp(-1f, 1f, normalizedIndex);
                float startDelay = i % 2 == 0 ? 0f : 0.035f;

                GameObject puffObject = new GameObject($"DustPuff_{i}");
                Transform puffTransform = puffObject.transform;
                puffTransform.SetParent(transform, false);
                puffTransform.localPosition = new Vector3(horizontalDirection * 0.12f, 0.02f, 0f);

                float startScale = Mathf.Lerp(0.16f, 0.25f, 1f - Mathf.Abs(horizontalDirection));
                puffTransform.localScale = Vector3.one * startScale;

                SpriteRenderer puffRenderer = puffObject.AddComponent<SpriteRenderer>();
                puffRenderer.sprite = GetDustSprite();
                puffRenderer.color = DustColor;

                if (referenceRenderer != null)
                {
                    puffRenderer.sortingLayerID = referenceRenderer.sortingLayerID;
                    puffRenderer.sortingOrder = referenceRenderer.sortingOrder + 1;
                }

                Vector3 endPosition = new Vector3(
                    horizontalDirection * Mathf.Lerp(0.42f, 0.68f, Mathf.Abs(horizontalDirection)),
                    Mathf.Lerp(0.16f, 0.3f, 1f - Mathf.Abs(horizontalDirection)),
                    0f);

                sequence.Insert(
                    startDelay,
                    puffTransform.DOLocalMove(endPosition, EffectDuration).SetEase(Ease.OutQuad));
                sequence.Insert(
                    startDelay,
                    puffTransform.DOScale(startScale * 2.2f, EffectDuration).SetEase(Ease.OutCubic));
                sequence.Insert(
                    startDelay + 0.08f,
                    puffRenderer.DOFade(0f, EffectDuration - 0.08f).SetEase(Ease.InQuad));
            }

            sequence.OnComplete(() => Destroy(gameObject));
        }

        private void OnDestroy()
        {
            sequence?.Kill();
            sequence = null;
        }

        private static Sprite GetDustSprite()
        {
            if (dustSprite != null)
            {
                return dustSprite;
            }

            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                name = "RuntimeDustPuff",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color[] pixels = new Color[TextureSize * TextureSize];
            Vector2 center = new Vector2((TextureSize - 1) * 0.5f, (TextureSize - 1) * 0.5f);
            float radius = TextureSize * 0.5f;

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = 1f - Mathf.SmoothStep(0.45f, 1f, distance);
                    pixels[y * TextureSize + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);

            dustSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f),
                TextureSize);
            dustSprite.name = "RuntimeDustPuff";
            dustSprite.hideFlags = HideFlags.HideAndDontSave;
            return dustSprite;
        }
    }
}
