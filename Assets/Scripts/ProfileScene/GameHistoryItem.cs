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

        public void Render(UserGameHistoryDto gameHistory, string opponentUsername)
        {
            if (gameHistory == null)
            {
                SetText(opponentName, "");
                SetText(resultText, "");
                return;
            }

            SetText(opponentName, opponentUsername);
            SetResult(gameHistory);
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
    }
}
