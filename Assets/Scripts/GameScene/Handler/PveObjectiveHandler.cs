using GameScene.Dto;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GameScene.Handler
{
    /// <summary>
    /// Turns each <c>pveObjective</c> message into the objective HUD line. The client keeps no clock of its
    /// own: it shows what the latest message says.
    /// </summary>
    public class PveObjectiveHandler : IFrameInfoHandler<PveObjectiveInfo>
    {
        private const string ObjectiveTable = "Adventure";

        public void Handler(PveObjectiveInfo objective)
        {
            string key = PveObjectiveText.KeyFor(objective);
            if (key == null)
            {
                return;
            }

            string fallback = PveObjectiveText.FallbackFor(objective);

            // Look the entry up in the table: GetLocalizedStringAsync reports a missing key as a successful
            // "No translation found" string (same reason as PveScriptEventHandler).
            LocalizationSettings.StringDatabase.GetTableAsync(ObjectiveTable).Completed += handle =>
            {
                StringTableEntry entry = handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null
                    ? handle.Result.GetEntry(key)
                    : null;
                string template = entry != null ? entry.GetLocalizedString() : fallback;
                string text = PveObjectiveText.Render(objective, template);
                if (!string.IsNullOrEmpty(text))
                {
                    PveObjectiveHud.ShowObjective(text);
                }
            };
        }
    }
}
