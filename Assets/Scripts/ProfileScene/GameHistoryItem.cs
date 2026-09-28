using System;
using Data.Profile;
using TMPro;
using UnityEngine;

namespace ProfileScene
{
    public class GameHistoryItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text opponentName;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Color winColor = new Color(0.2f, 0.7f, 0.2f);
        [SerializeField] private Color loseColor = new Color(0.85f, 0.2f, 0.2f);
        [SerializeField] private Color drawColor = new Color(0.75f, 0.75f, 0.2f);

        // Added for issue #98. The GameItem prefab was not opened (no Unity Editor available),
        // so this object reference is unwired and stays null until a developer drags a TMP_Text
        // onto it in the Editor. SetGameType() falls back to appending onto resultText while it
        // is null, so the game type still reaches the screen in the meantime.
        [SerializeField] private TMP_Text gameTypeText;

        public void Render(UserGameHistoryDto gameHistory, string opponentUsername)
        {
            if (gameHistory == null)
            {
                SetText(opponentName, "");
                SetText(resultText, "");
                SetText(gameTypeText, "");
                return;
            }

            SetText(opponentName, opponentUsername);
            SetResult(gameHistory);
            SetGameType(gameHistory);
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private void SetResult(UserGameHistoryDto gameHistory)
        {
            if (resultText == null)
            {
                return;
            }

            if (gameHistory.IsDraw)
            {
                resultText.text = "Draw";
                resultText.color = drawColor;
                return;
            }

            bool isWin = gameHistory.IsWin;
            resultText.text = isWin ? "Win" : "Loss";
            resultText.color = isWin ? winColor : loseColor;
        }

        private void SetGameType(UserGameHistoryDto gameHistory)
        {
            string label = GameTypeLabel(gameHistory.gameType);
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            if (gameTypeText != null)
            {
                gameTypeText.text = label;
                return;
            }

            // Fallback while gameTypeText is unwired (see the field comment above): append onto
            // resultText instead of dropping the game type on the floor.
            if (resultText != null)
            {
                resultText.text = $"{resultText.text} · {label}";
            }
        }

        // gameType is "PVP", "Practice" or "PVE" (issue #98). Practice and PVE are renamed to
        // reader-facing words instead of printed verbatim: Practice is a bot match, PVE is an
        // adventure scenario, and neither word says that on its own. An unrecognized value is
        // printed as-is rather than hidden, so a new server value is visible instead of blank.
        private static string GameTypeLabel(string gameType)
        {
            if (string.IsNullOrEmpty(gameType))
            {
                return null;
            }

            if (string.Equals(gameType, "PVP", StringComparison.OrdinalIgnoreCase))
            {
                return "PVP";
            }

            if (string.Equals(gameType, "Practice", StringComparison.OrdinalIgnoreCase))
            {
                return "vs Bot";
            }

            if (string.Equals(gameType, "PVE", StringComparison.OrdinalIgnoreCase))
            {
                return "Adventure";
            }

            return gameType;
        }
    }
}
