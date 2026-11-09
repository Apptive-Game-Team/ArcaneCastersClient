using TMPro;
using UnityEngine;

namespace TutorialScene
{
    public class ComicPanelView : MonoBehaviour
    {
        [SerializeField] private int page;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private RectTransform artwork;
        [SerializeField] private CanvasGroup[] captionGroups;
        [SerializeField] private TMP_Text[] captionTexts;
        [SerializeField] private Vector2 drift = new Vector2(-8f, 3f);

        private float revealedAt;
        private bool revealed;

        public int Page => page;

        public void ResetView()
        {
            revealed = false;
            panelGroup.alpha = 0f;
            foreach (CanvasGroup caption in captionGroups)
            {
                caption.alpha = 0f;
            }
        }

        public void Reveal()
        {
            revealed = true;
            revealedAt = Time.unscaledTime;
            panelGroup.alpha = 0f;
            artwork.localScale = Vector3.one * 1.025f;
            artwork.anchoredPosition = Vector2.zero;
        }

        public TMP_Text PrepareCaption(int index, string text)
        {
            captionGroups[index].alpha = 0f;
            TMP_Text label = captionTexts[index];
            label.text = text;
            label.maxVisibleCharacters = 0;
            label.ForceMeshUpdate();
            return label;
        }

        public void SetCaptionAlpha(int index, float alpha)
        {
            captionGroups[index].alpha = alpha;
        }

        public void CompleteReveal()
        {
            revealedAt = Mathf.Min(revealedAt, Time.unscaledTime - 0.4f);
            panelGroup.alpha = 1f;
        }

        private void Update()
        {
            if (!revealed)
            {
                return;
            }

            float elapsed = Time.unscaledTime - revealedAt;
            panelGroup.alpha = Mathf.Clamp01(elapsed / 0.4f);
            float motion = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 8f));
            artwork.localScale = Vector3.one * Mathf.Lerp(1.025f, 1.055f, motion);
            artwork.anchoredPosition = drift * motion;
        }
    }
}
