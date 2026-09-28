using UnityEngine;
using UnityEngine.UI;

namespace GameScene
{
    /// <summary>
    /// 마나 바 위에 "지금 준비한 조합이 먹을 마나"를 덧그린다.
    /// 구간은 언제나 파란 채움의 오른쪽 끝에서 왼쪽으로 자란다. 소모량이 현재 마나 이하면
    /// 그만큼만 주황으로 덮고, 현재 마나보다 크면 시전할 수 없다는 뜻이므로 파란 채움 전체를
    /// 빨갛게 덮는다.
    /// 씬 계층에 오브젝트를 미리 심어 두지 않아도 되도록 덧그리는 이미지는 런타임에 만든다.
    /// </summary>
    public class ManaCostPreview
    {
        /// <summary>소모 가능. "이만큼 빠진다".</summary>
        private static readonly Color AffordableColor = new Color32(0xFF, 0x9A, 0x1F, 0xFF);

        /// <summary>마나 부족. "지금은 못 쓴다".</summary>
        private static readonly Color InsufficientColor = new Color32(0xF0, 0x44, 0x3A, 0xFF);

        private const string ClipObjectName = "ManaCostOverlayClip";
        private const string OverlayObjectName = "ManaCostOverlay";

        private readonly Slider slider;

        /// <summary>소모 구간만 남기고 잘라 내는 창.</summary>
        private RectTransform clip;

        /// <summary>파란 채움과 똑같은 사각형. 잘린 뒤 남은 부분만 보인다.</summary>
        private RectTransform overlay;

        private Image overlayImage;

        public ManaCostPreview(Slider slider)
        {
            this.slider = slider;
        }

        /// <param name="currentMana">지금 가진 마나. 슬라이더 값과 같은 단위다.</param>
        /// <param name="cost">준비한 조합이 먹을 마나. 0 이하면 덧그리지 않는다.</param>
        public void Render(float currentMana, int cost)
        {
            if (slider == null || slider.fillRect == null || cost <= 0)
            {
                Hide();
                return;
            }

            float valueRange = slider.maxValue - slider.minValue;
            if (valueRange <= 0f)
            {
                Hide();
                return;
            }

            float filledEnd = Mathf.Clamp01((currentMana - slider.minValue) / valueRange);
            bool insufficient = cost > currentMana;
            float costStart = insufficient
                ? 0f
                : Mathf.Clamp01((currentMana - cost - slider.minValue) / valueRange);

            // 마나가 0이면 덮을 파란 채움이 없다.
            float clipWidth = filledEnd - costStart;
            if (clipWidth <= 0f)
            {
                Hide();
                return;
            }

            if (!TryEnsureOverlay())
            {
                return;
            }

            overlayImage.color = insufficient ? InsufficientColor : AffordableColor;

            // 덧그리는 사각형은 파란 채움과 완전히 같은 자리(Fill Area의 0..filledEnd)에 놓고,
            // 창으로 소모 구간만 남긴다. 그래야 오른쪽 꼭지의 라운드가 파란 채움과 정확히 같다.
            // 좁은 구간에 9-slice를 직접 씌우면 테두리가 폭에 맞춰 줄어들어 라운드가 어긋난다.
            StretchHorizontally(clip, costStart, filledEnd);
            StretchHorizontally(overlay, -costStart / clipWidth, 1f);

            // Render는 마나가 갱신될 때마다, 즉 매 프레임 불린다. 이미 맞는 상태면 건드리지 않는다.
            if (!clip.gameObject.activeSelf)
            {
                clip.gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            if (clip != null && clip.gameObject.activeSelf)
            {
                clip.gameObject.SetActive(false);
            }
        }

        /// <summary>부모 폭의 <paramref name="min"/>..<paramref name="max"/> 구간에 세로로 가득 채워 붙인다.</summary>
        private static void StretchHorizontally(RectTransform rectTransform, float min, float max)
        {
            rectTransform.anchorMin = new Vector2(min, 0f);
            rectTransform.anchorMax = new Vector2(max, 1f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private bool TryEnsureOverlay()
        {
            if (overlay != null)
            {
                return true;
            }

            // 슬라이더는 채움을 이 부모 안에서 0..normalizedValue 앵커로 늘린다. 같은 부모에
            // 같은 방식으로 앵커를 잡으면 덧그린 구간이 마나 값과 정확히 같은 자리에 놓인다.
            RectTransform fillArea = slider.fillRect.parent as RectTransform;
            if (fillArea == null)
            {
                return false;
            }

            // RectMask2D는 스텐실을 쓰지 않아 마스크보다 싸고, 사각형 자르기에는 이것으로 충분하다.
            GameObject clipObject = new GameObject(ClipObjectName, typeof(RectTransform), typeof(RectMask2D));
            clipObject.layer = fillArea.gameObject.layer;
            clip = (RectTransform)clipObject.transform;
            clip.SetParent(fillArea, false);
            clip.pivot = new Vector2(0.5f, 0.5f);

            GameObject overlayObject = new GameObject(OverlayObjectName, typeof(RectTransform), typeof(Image));
            overlayObject.layer = fillArea.gameObject.layer;
            overlay = (RectTransform)overlayObject.transform;
            overlay.SetParent(clip, false);
            overlay.pivot = new Vector2(0.5f, 0.5f);

            overlayImage = overlayObject.GetComponent<Image>();
            overlayImage.raycastTarget = false;
            CopyFillLook(overlayImage);

            // 슬라이더가 채움을 다시 그려도 덧그린 구간이 그 아래로 내려가지 않게 마지막에 둔다.
            // 이후 이 부모의 자식 순서를 바꾸는 코드가 없으므로 만들 때 한 번이면 된다.
            clip.SetAsLastSibling();
            return true;
        }

        /// <summary>모서리 라운드가 파란 채움과 같도록 채움의 스프라이트 설정을 그대로 가져온다.</summary>
        private void CopyFillLook(Image target)
        {
            Image fillImage = slider.fillRect.GetComponent<Image>();
            if (fillImage == null)
            {
                return;
            }

            target.sprite = fillImage.sprite;
            target.fillCenter = fillImage.fillCenter;
            target.preserveAspect = fillImage.preserveAspect;
            target.pixelsPerUnitMultiplier = fillImage.pixelsPerUnitMultiplier;

            // 채움이 Filled면 슬라이더가 fillAmount로 그린다는 뜻이라 앵커 계산 자체가 성립하지
            // 않는다. 이 프로젝트의 마나 바는 Sliced다. 그런 채움을 만나도 모양만은 맞춰 둔다.
            target.type = fillImage.type == Image.Type.Filled ? Image.Type.Sliced : fillImage.type;
        }
    }
}
