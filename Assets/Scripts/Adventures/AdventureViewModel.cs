using Data;
using Data.Adventures;
using GameScene.Dto;
using Global;
using LobbyScene.Debugger;
using UnityEngine;
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
            WDebug.Log("Adventure session matched: transitioning to game scene.");
            if (dto == null)
            {
                CurrentState.UpdateData(AdventureState.Idle);
            }
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

            MatchedInfoDto matchedInfoDto = MatchedInfoDto.CreateDebugSession(response.sessionId, "left", SceneContext.UserID);
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
            // gone by the time BattleThemeApplier reads it in GameScene.
            SceneContext.BattleTheme = CurrentAdventure.Instance != null
                ? CurrentAdventure.Instance.Adventure?.BattleTheme
                : null;

            SceneManager.LoadScene(targetSceneName);
        }
    }
}
