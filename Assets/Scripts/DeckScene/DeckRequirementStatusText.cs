using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckScene
{
    public class DeckRequirementStatusText : MonoBehaviour
    {
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button submitButton;

        private void Awake()
        {
            statusText ??= GetComponent<TMP_Text>();
        }

        public void Render(DeckRequirementSummary summary, bool canSubmit)
        {
            if (statusText != null)
            {
                statusText.text = $"카드 : {summary.CardCount} / {DeckManagementViewModel.DeckCardCount}";
            }

            if (submitButton != null)
            {
                submitButton.interactable = canSubmit;
            }
        }
    }
}
