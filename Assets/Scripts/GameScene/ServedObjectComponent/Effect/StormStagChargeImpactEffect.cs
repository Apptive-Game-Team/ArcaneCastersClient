using DG.Tweening;
using UnityEngine;

namespace GameScene.ServedObjectComponent.Effect
{
    public class StormStagChargeImpactEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer impactRenderer;
        [SerializeField] private float duration = 0.24f;
        [SerializeField] private float burstScale = 1.12f;
        [SerializeField] private float dissipateScale = 1.35f;

        private Sequence sequence;
        private SpriteRenderer flashRenderer;
        private SpriteRenderer afterimageRenderer;

        private void Start()
        {
            if (impactRenderer == null)
            {
                impactRenderer = GetComponent<SpriteRenderer>();
            }

            if (impactRenderer == null || impactRenderer.sprite == null)
            {
                Destroy(gameObject);
                return;
            }

            Play();
        }

        private void OnDestroy()
        {
            sequence?.Kill();
            sequence = null;
        }

        private void Play()
        {
            Vector3 baseScale = transform.localScale;
            float flashDuration = duration * 0.3f;
            float burstStart = duration * 0.16f;
            float fadeStart = duration * 0.42f;
            float burstDuration = fadeStart - burstStart;
            float fadeDuration = duration - fadeStart;

            flashRenderer = CreateLayer(
                "CompressedFlash",
                new Color(0.78f, 1f, 1f, 0f),
                impactRenderer.sortingOrder + 1);
            afterimageRenderer = CreateLayer(
                "GoldenAfterimage",
                new Color(1f, 0.82f, 0.18f, 0f),
                impactRenderer.sortingOrder - 1);

            transform.localScale = Vector3.Scale(baseScale, new Vector3(0.22f, 0.55f, 1f));
            SetAlpha(impactRenderer, 0f);
            flashRenderer.transform.localScale = Vector3.one * 0.24f;
            afterimageRenderer.transform.localScale = Vector3.one * 0.58f;

            sequence = DOTween.Sequence();
            sequence.SetLink(gameObject);
            sequence.Insert(0f, impactRenderer.DOFade(1f, duration * 0.16f).SetEase(Ease.OutQuad));
            sequence.Insert(0f, transform.DOScale(Vector3.Scale(baseScale, new Vector3(0.76f, 0.86f, 1f)), duration * 0.16f)
                .SetEase(Ease.OutQuad));
            sequence.Insert(0f, flashRenderer.DOFade(0.95f, duration * 0.08f).SetEase(Ease.OutQuad));
            sequence.Insert(0f, flashRenderer.transform.DOScale(0.92f, flashDuration).SetEase(Ease.OutCubic));
            sequence.Insert(duration * 0.08f, flashRenderer.DOFade(0f, flashDuration).SetEase(Ease.InQuad));
            sequence.Insert(burstStart, transform.DOScale(baseScale * burstScale, burstDuration)
                .SetEase(Ease.OutBack));
            sequence.Insert(burstStart, afterimageRenderer.DOFade(0.48f, duration * 0.2f)
                .SetEase(Ease.OutQuad));
            sequence.Insert(burstStart, afterimageRenderer.transform.DOScale(1.08f, burstDuration)
                .SetEase(Ease.OutCubic));
            sequence.Insert(fadeStart, impactRenderer.DOFade(0f, fadeDuration).SetEase(Ease.InQuad));
            sequence.Insert(fadeStart, transform.DOScale(baseScale * dissipateScale, fadeDuration)
                .SetEase(Ease.OutCubic));
            sequence.Insert(fadeStart, afterimageRenderer.DOFade(0f, fadeDuration).SetEase(Ease.InQuad));
            sequence.Insert(fadeStart, afterimageRenderer.transform.DOScale(1.5f, fadeDuration)
                .SetEase(Ease.OutCubic));
            sequence.OnComplete(() => Destroy(gameObject));
        }

        private SpriteRenderer CreateLayer(string layerName, Color color, int sortingOrder)
        {
            GameObject layerObject = new GameObject(layerName);
            Transform layerTransform = layerObject.transform;
            layerTransform.SetParent(transform, false);

            SpriteRenderer layerRenderer = layerObject.AddComponent<SpriteRenderer>();
            layerRenderer.sprite = impactRenderer.sprite;
            layerRenderer.sharedMaterial = impactRenderer.sharedMaterial;
            layerRenderer.sortingLayerID = impactRenderer.sortingLayerID;
            layerRenderer.sortingOrder = sortingOrder;
            layerRenderer.flipX = impactRenderer.flipX;
            layerRenderer.flipY = impactRenderer.flipY;
            layerRenderer.color = color;
            return layerRenderer;
        }

        private static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }
}
