using Adventures;
using Data;
using Global;
using Global.Button;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ResultScene
{
    /// <summary>
    /// Replays the scenario the player just finished, straight from the result
    /// screen. <see cref="Adventures.AdventureViewModel"/> is scene-scoped to
    /// AdventureScene / AdventuresScene and is gone by the time this screen loads,
    /// so this calls <see cref="AdventureApiService"/> directly instead of going
    /// through it, and mirrors AdventureViewModel.OnMatched's success path itself.
    /// </summary>
    public class RetryAdventureButton : AsyncButtonBase
    {
        [SerializeField] private AdventureApiService adventureApi;

        protected override void OnClickButton()
        {
            if (!SceneContext.AdventureScenarioId.HasValue || adventureApi == null)
            {
                ResetButton();
                return;
            }

            StartCoroutine(adventureApi.RequestPVE(SceneContext.AdventureScenarioId.Value, OnMatched));
        }

        private void OnMatched(MatchedInfoDto dto)
        {
            if (dto == null)
            {
                WDebug.LogWarning("[RetryAdventureButton] Retry failed to match; staying on the result screen.");
                ResetButton();
                return;
            }

            SceneContext.MatchInfo = dto;
            // The adventure's theme is still in SceneContext from the first run (nothing here
            // clears it), so it is the fallback for a server that sends no mapType.
            SceneContext.PrepareMap(dto.mapType, SceneContext.BattleTheme);
            SceneManager.LoadScene("GameScene");
        }
    }
}
