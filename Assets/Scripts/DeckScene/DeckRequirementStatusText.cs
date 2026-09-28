using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckScene
{
    public class DeckRequirementStatusText : MonoBehaviour
    {
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button submitButton;

        // 막대 안의 채움. anchorMax.x 를 덱 장수 비율로 맞춘다.
        [SerializeField] private RectTransform progressFill;

        private void Awake()
        {
            statusText ??= GetComponent<TMP_Text>();
        }

        public void Render(DeckRequirementSummary summary, bool canSubmit)
        {
            int required = DeckManagementViewModel.DeckCardCount;
            if (statusText != null)
            {
                statusText.text = $"{summary.CardCount} / {required}";
            }

            if (progressFill != null)
            {
                float ratio = Mathf.Clamp01((float)summary.CardCount / required);
                progressFill.anchorMax = new Vector2(ratio, progressFill.anchorMax.y);
                progressFill.gameObject.SetActive(ratio > 0f);
            }

            if (submitButton != null)
            {
                submitButton.interactable = canSubmit;
            }
        }
    }
}
