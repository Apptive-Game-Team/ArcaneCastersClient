using Data;
using Data.Adventures;
using Data.BattleThemes;
using GameScene.Dto;
using Global;
using LobbyScene.Debugger;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using Global.Serialization;

namespace Adventures
{
    public class AdventureViewModel : LocalSingletonObject<AdventureViewModel>
    {
        [SerializeField] private AdventureApiService _adventureApi;

        public enum AdventureState
        {
            Idle,
            Requesting,
        }

        private static readonly LocalizedString serverDown = new LocalizedString
            { TableReference = "SystemMessageUI", TableEntryReference = "serverDown" };

        public StateEvent<AdventureState> CurrentState = new StateEvent<AdventureState>(AdventureState.Idle);

        public void PlayPVE(long scenarioId)
        {
            WDebug.Log($"Adventure scenario selected: starting PVE session. scenarioId={scenarioId}");
            CurrentState.UpdateData(AdventureState.Requesting);
            StartCoroutine(_adventureApi.RequestPVE(scenarioId, HandleCallback));
        }

        public void PlayPveDebug(long scenarioId)
        {
            WDebug.Log($"Adventure debug requested. scenarioId={scenarioId}");
            CurrentState.UpdateData(AdventureState.Requesting);
            StartCoroutine(_adventureApi.RequestDebugPVE(scenarioId, HandleDebugSessionCreated, HandleDebugFailure));
        }

        private void HandleCallback(MatchedInfoDto dto)
        {
            if (dto == null)
            {
                WDebug.LogWarning("Adventure session request failed: staying on the adventure screen.");
                CurrentState.UpdateData(AdventureState.Idle);
                SystemMessageUI.Instance.ShowMessage(serverDown);
                return;
            }

            WDebug.Log("Adventure session matched: transitioning to game scene.");
            OnMatched(dto);
        }

        private void HandleDebugSessionCreated(string json)
        {
            JsonCodec.TryDeserialize(json, out DebugGameResponse response);
            if (response == null || string.IsNullOrWhiteSpace(response.sessionId))
            {
                HandleDebugFailure("Invalid debug session response.");
                return;
            }

            MatchedInfoDto matchedInfoDto = MatchedInfoDto.CreateDebugSession(
                response.sessionId, "left", SceneContext.UserID, response.mapType);
            OnMatched(matchedInfoDto);
        }

        private void HandleDebugFailure(string message)
        {
            CurrentState.UpdateData(AdventureState.Idle);
            WDebug.LogWarning($"Adventure debug session failed: {message}");
            SystemMessageUI.Instance.ShowMessage("Failed to start adventure debug session. Check the debug server.");
        }

        private void OnMatched(MatchedInfoDto matchedInfoDto)
        {
            SceneContext.MatchInfo = matchedInfoDto;
            string targetSceneName = "GameScene";
            if (SceneManager.GetActiveScene().name.Contains(targetSceneName))
            {
                return;
            }

            // CurrentAdventure is destroyed on this load (it is bound to AdventureScene /
            // AdventuresScene), so the theme has to be copied into SceneContext now or it is
            // gone by the time BattleThemeApplier reads it in GameScene. The server's mapType
            // wins; the adventure's own BattleTheme is only the fallback for a server that
            // sends none.
            BattleThemeScriptableObject adventureTheme = CurrentAdventure.Instance != null
                ? CurrentAdventure.Instance.Adventure?.BattleTheme
                : null;
            SceneContext.PrepareMap(matchedInfoDto.mapType, adventureTheme);

            SceneManager.LoadScene(targetSceneName);
        }
    }
}
