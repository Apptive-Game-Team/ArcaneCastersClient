using Admin.Dto;
using Global;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Admin
{
    public class BotMatchPanelController : MonoBehaviour
    {
        [SerializeField] private AdminViewModel adminViewModel;
        [SerializeField] private UnityEngine.UI.InputField leftBotIdInputField;
        [SerializeField] private UnityEngine.UI.InputField rightBotIdInputField;
        [SerializeField] private Button matchButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private bool enterSpectatingSceneOnMatched = true;

        private void Awake()
        {
            if (adminViewModel == null) adminViewModel = FindObjectOfType<AdminViewModel>();
            if (matchButton != null) matchButton.onClick.AddListener(MatchBots);
        }

        private void OnDestroy()
        {
            if (matchButton != null) matchButton.onClick.RemoveListener(MatchBots);
        }

        private void MatchBots()
        {
            if (!TryReadBotId(leftBotIdInputField, out long leftBotId) ||
                !TryReadBotId(rightBotIdInputField, out long rightBotId))
            {
                SetStatus("Bot IDs must be non-zero numbers.");
                return;
            }

            if (leftBotId == rightBotId)
            {
                SetStatus("Bot IDs must be different.");
                return;
            }

            SetInteractable(false);
            SetStatus("Matching bots...");

            adminViewModel.MatchBots(new BotMatchRequestDto
            {
                leftBotId = leftBotId,
                rightBotId = rightBotId
            }, matchedInfo =>
            {
                SetInteractable(true);

                if (matchedInfo == null)
                {
                    SetStatus("Bot matching failed.");
                    return;
                }

                SetStatus($"Matched: {matchedInfo.sessionId}");

                if (!enterSpectatingSceneOnMatched) return;

                SceneContext.MatchInfo = matchedInfo;
                SceneContext.PrepareMap(matchedInfo.mapType, null);
                SceneManager.LoadScene("Scenes/SpectatingScene");
            });
        }

        private bool TryReadBotId(UnityEngine.UI.InputField inputField, out long botId)
        {
            botId = 0;
            return inputField != null &&
                   long.TryParse(inputField.text, out botId) &&
                   botId != 0;
        }

        private void SetInteractable(bool interactable)
        {
            if (matchButton != null) matchButton.interactable = interactable;
            if (leftBotIdInputField != null) leftBotIdInputField.interactable = interactable;
            if (rightBotIdInputField != null) rightBotIdInputField.interactable = interactable;
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
            WDebug.Log(message);
        }
    }
}
