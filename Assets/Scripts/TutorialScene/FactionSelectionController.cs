using System;
using System.Collections;
using System.Linq;
using Data;
using Data.Magic;
using Data.GameConfig;
using Global;
using LobbyScene;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TutorialScene
{
    public sealed class FactionSelectionController : MonoBehaviour
    {
        [SerializeField] private FactionTutorialPanel panel;
        private long owner;
        private string jwt => SceneContext.JwtToken;
        private string matchingServer;
        private string faction;
        private FactionTutorialCheckpoint checkpoint;
        private bool busy;

        private IEnumerator Start()
        {
            GlobalTutorialManager.Instance?.FinishOnboarding();
            panel.Show("FactionTutorial.title", "FactionTutorial.loading");
            if (SceneContext.User == null) yield return UserInfoGetter.GetUserInfo();
            if (this == null || SceneContext.User == null) yield break;
            owner = SceneContext.UserID;
            matchingServer = ServerList.MatchingServer.url;
            checkpoint = FactionTutorialProgress.Begin(owner);
            yield return PrepareChoices();
        }

        private IEnumerator PrepareChoices()
        {
            panel.Show("FactionTutorial.title", "FactionTutorial.loading");
            yield return GameDataRefresh.Refresh();
            if (!Current) yield break;
            var names = FactionTutorialCards.Hell.Concat(FactionTutorialCards.Nature).Concat(new[] { "meteor_shower", "tornado_strike" });
            if (names.Any(name => !LocalCombinedMagicData.TryGetByName(name, out var magic) || magic.id <= 0
                    || !GameParameterResolver.TryGetMagicParameter(magic, "range", out float range) || range <= 0))
            {
                Error(() => StartCoroutine(PrepareChoices()));
                yield break;
            }
            yield return LoadChoice();
        }

        private IEnumerator LoadChoice()
        {
            PlayerProgressionDto result = null;
            yield return PlayerProgressionApi.Sync(value => result = value);
            if (!Current) yield break;
            if (result == null) { Error(() => StartCoroutine(LoadChoice())); yield break; }
            if (result.faction == "HELL" || result.faction == "NATURE")
            {
                faction = result.faction;
                yield return Connect();
                yield break;
            }
            panel.Show("FactionTutorial.title", "FactionTutorial.choose", null, null,
                () => StartCoroutine(Choose("HELL")), () => StartCoroutine(Choose("NATURE")));
        }

        private IEnumerator Choose(string selected)
        {
            if (busy) yield break;
            busy = true;
            panel.Show("FactionTutorial.title", "FactionTutorial.loading");
            PlayerProgressionDto result = null;
            yield return PlayerProgressionApi.Choose(selected, value => result = value);
            busy = false;
            if (!Current) yield break;
            // 응답 유실이나 이미 선택된 계정은 sync로 확인하고, 선택을 성공했다고 추측하지 않는다.
            if (result == null) { yield return LoadChoice(); yield break; }
            if (result.faction != selected) { Error(() => StartCoroutine(LoadChoice())); yield break; }
            faction = result.faction;
            yield return UserInfoGetter.RefreshGameUser();
            if (!Current) yield break;
            LobbyUIController.ClearCachedDecks();
            DeckScene.DeckManagementViewModel.ClearCachedUserData();
            SceneContext.OwnedCards = null;
            yield return Connect();
        }

        private IEnumerator Connect()
        {
            panel.Show("FactionTutorial.title", "FactionTutorial.connecting");
            if (checkpoint.battle != null)
            {
                if (FactionTutorialProgress.IsValidReady(checkpoint.battle, owner, faction))
                {
                    GameScene.Dto.TutorialStateDto state = null;
                    long code = 0;
                    yield return TutorialBattleApi.Request<GameScene.Dto.TutorialStateDto>("GET",
                        checkpoint.battle.server.TrimEnd('/') + "/api/tutorial/battles/" + checkpoint.battle.battleId + "/state",
                        jwt, null, (value, status) => { state = value; code = status; });
                    if (!Current) yield break;
                    if (state != null && state.ownerId == owner && state.battleId == checkpoint.battle.battleId)
                    {
                        Enter(checkpoint.battle);
                        yield break;
                    }
                    if (code != 404 && code != 410) { Error(() => StartCoroutine(Connect())); yield break; }
                }
                checkpoint.battle = null;
                checkpoint.requestId = Guid.NewGuid().ToString();
                FactionTutorialProgress.Save(owner, checkpoint);
            }
            TutorialReadyDto ready = null;
            yield return TutorialBattleApi.Request<TutorialReadyDto>("POST",
                ServerList.MatchingServer.url.TrimEnd('/') + "/api/tutorial/battles", jwt,
                new { requestId = checkpoint.requestId }, (value, _) => ready = value);
            if (!Current) yield break;
            if (!FactionTutorialProgress.IsValidReady(ready, owner, faction))
            {
                Error(() => StartCoroutine(Connect()));
                yield break;
            }
            checkpoint.battle = ready;
            FactionTutorialProgress.Save(owner, checkpoint);
            Enter(ready);
        }

        private void Enter(TutorialReadyDto ready)
        {
            if (!Current) return;
            SceneContext.ClearAdventureMatch();
            SceneContext.MatchResult = null;
            SceneContext.MatchInfo = new MatchedInfoDto {
                sessionId = ready.battleId, server = ready.server, leftUser = SceneContext.User,
                rightUser = new User(-1, "", "", -1)
            };
            SceneManager.LoadScene(FactionTutorialProgress.BattleScene);
        }

        private bool Current => FactionTutorialProgress.IsCurrentOwner(owner, matchingServer);
        private void Error(Action retry) => panel.Show("FactionTutorial.title", "FactionTutorial.error", "FactionTutorial.retry", retry);
    }
}
