using TMPro;
using UnityEngine;

namespace ResultScene
{
    public static class ResultMmrDeltaPresenter
    {
        public static void SetMmrDelta(TMP_Text mmrDeltaText, int? delta, Color gainColor, Color lossColor)
        {
            if (mmrDeltaText == null) return;

            if (delta == null)
            {
                mmrDeltaText.text = string.Empty;
                return;
            }

            mmrDeltaText.text = delta.Value >= 0 ? $"+{delta.Value}" : delta.Value.ToString();
            mmrDeltaText.color = delta.Value >= 0 ? gainColor : lossColor;
        }
    }
}
