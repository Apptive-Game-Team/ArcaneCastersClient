using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Data;
using Data.Magic;
using GameScene;
using GameScene.Card;
using GameScene.Dto;
using GameScene.Object;
using Global;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TutorialScene
{
    public sealed class FactionTutorialBattleDriver : MonoBehaviour
    {
        [SerializeField] private FactionTutorialPanel panel;
        public static FactionTutorialBattleDriver Instance { get; private set; }
        private TutorialReadyDto ready;
        private TutorialStateDto state;
        private TutorialStateDto presentedState;
        private TutorialStateDto pendingTerminal;
        private readonly TutorialPlaybackBuffer playback = new TutorialPlaybackBuffer();
        private double lastRenderRealtime;
        private long owner;
        private string jwt => SceneContext.JwtToken;
        private string matchingServer;
        private int watermark;
        private bool busy;
        private bool commandPending;
        private bool suspended;
        private bool finishing;
        private TutorialPointer pointer;
        private string pointerMode;
        private readonly HashSet<string> guidedCards = new HashSet<string>();

        private void Awake() { Instance = this; }

        private IEnumerator Start()
        {
            if (SceneContext.User == null) { SceneManager.LoadScene("LoginScene"); yield break; }
            owner = SceneContext.UserID;
            matchingServer = ServerList.MatchingServer.url;
            ready = FactionTutorialProgress.Read(owner)?.battle;
            if (ready == null || SceneContext.MatchInfo?.sessionId != ready.battleId)
            {
                SceneManager.LoadScene(FactionTutorialProgress.SelectionScene);
                yield break;
            }
            panel.Show("FactionTutorial.title", "FactionTutorial.connecting");
            while (Current && !finishing)
            {
                float started = Time.realtimeSinceStartup;
                if (!busy && !commandPending && !suspended) yield return Poll();
                float remaining = 0.05f - (Time.realtimeSinceStartup - started);
                if (remaining > 0) yield return new WaitForSecondsRealtime(remaining);
                else yield return null;
            }
        }

        private bool Current => FactionTutorialProgress.IsCurrentOwner(owner, matchingServer);
        private string Api(string suffix) => ready.server.TrimEnd('/') + "/api/tutorial/battles/" + ready.battleId + suffix;

        private IEnumerator Poll()
        {
            busy = true;
            TutorialStateDto result = null;
            long code = 0;
            yield return TutorialBattleApi.Request<TutorialStateDto>("GET", Api("/state?afterFrame=" + watermark), jwt,
                null, (value, status) => { result = value; code = status; });
            busy = false;
            if (!Current || finishing) yield break;
            if (code == 404 || code == 410)
            {
                suspended = true;
                ClearPointer();
                panel.Show("FactionTutorial.title", "FactionTutorial.expired", "FactionTutorial.reconnect",
                    () => SceneManager.LoadScene(FactionTutorialProgress.SelectionScene));
                yield break;
            }
            if (result == null || result.ownerId != owner || result.battleId != ready.battleId || result.faction != ready.faction)
            {
                suspended = true;
                ClearPointer();
                panel.Show("FactionTutorial.title", "FactionTutorial.error", "FactionTutorial.retry", ResumePolling);
                yield break;
            }
            if (result.lastFrame < watermark || (state != null && result.revision < state.revision)) yield break;
            Apply(result);
        }

        private void Apply(TutorialStateDto result)
        {
            if (state == null || state.revision != result.revision || result.status != "RUNNING")
                CardInputSender.Instance?.Cancel();
            state = result;
            if (playback.Receive(result, Time.timeAsDouble))
            {
                pendingTerminal = null;
                ObjectSyncer.Instance.Sync(result.snapshot ?? Array.Empty<SnapshotObjectDto>());
                GameConfig.SetTickRate(playback.TickRate);
            }
            watermark = playback.ReceivedFrame;
            GameSceneUIController.Instance.UpdateMana(result.mana);
            GameScene.UI.TimerController.Instance?.UpdateTimer(result.remainingSeconds);
            SynchronizeCards(result.cards ?? Array.Empty<long>());
            if (result.status != "RUNNING")
            {
                ClearPointer();
                pendingTerminal = presentedState?.revision == result.revision && presentedState.status == result.status
                    ? null : result;
                return;
            }
            pendingTerminal = null;
            ShowState(result);
        }

        private void Update()
        {
            if (state == null || !Current || finishing || suspended) return;
            double realtime = Time.realtimeSinceStartupAsDouble;
            // Time.time은 maximumDeltaTime으로 제한되므로 앱 정지는 실제 시계로 감지한다.
            if (lastRenderRealtime > 0 && realtime - lastRenderRealtime > 0.5)
            {
                playback.Invalidate();
                pendingTerminal = null;
            }
            lastRenderRealtime = realtime;
            double now = Time.timeAsDouble;
            playback.BeginUpdate(now);
            while (playback.TryTake(now, out var frame))
                PresentationFramePlayer.Apply(frame.objects, frame.events);
            if (pendingTerminal != null && pendingTerminal.revision == state.revision
                && playback.IsSettled(now))
            {
                var terminal = pendingTerminal;
                pendingTerminal = null;
                ShowState(terminal);
            }
        }

        private void ShowState(TutorialStateDto result)
        {
            bool changed = presentedState == null || presentedState.revision != result.revision || presentedState.status != result.status
                || presentedState.wave != result.wave || presentedState.recommended != result.recommended;
            presentedState = result;
            if (!changed) return;
            ClearPointer();
            if (result.status == "RUNNING")
            {
                panel.Hide();
                string key = result.stage == "meteor" && result.recommended != "meteor_shower" ? "FactionTutorial.meteor.finish"
                    : result.stage == "final" ? $"FactionTutorial.final.{result.wave}"
                    : result.stage == "formation" ? (result.recommended == FactionTutorialCards.HellPractice[3]
                        || result.recommended == FactionTutorialCards.NaturePractice[3] ? "FactionTutorial.formation.tank" : "FactionTutorial.formation.swarm")
                    : $"FactionTutorial.{result.stage}.{ready.faction}";
                TutorialHint.Show("Onboarding", key);
            }
            else if (result.status == "CLEARED")
            {
                TutorialHint.HideCurrent();
                panel.Show("FactionTutorial.success", $"FactionTutorial.success.{result.stage}",
                    "FactionTutorial.next", () => StartCoroutine(SendAction("NEXT")));
            }
            else if (result.status == "FAILED")
            {
                TutorialHint.HideCurrent();
                panel.Show("FactionTutorial.retryTitle", "FactionTutorial.retryStep", "FactionTutorial.retry",
                    () => StartCoroutine(SendAction("RETRY")));
            }
            else if (result.status == "COMPLETED")
            {
                TutorialHint.HideCurrent();
                FactionTutorialProgress.CompleteBattle(owner);
                panel.Show("FactionTutorial.complete", $"FactionTutorial.reward.{ready.faction}",
                    "FactionTutorial.lobby", () => StartCoroutine(Finish()), rewardFaction: ready.faction);
            }
        }

        private static void SynchronizeCards(long[] cards)
        {
            var ui = GameSceneUIController.Instance;
            var current = ui.GetAllCards();
            foreach (long id in current.Where(id => !cards.Contains(id)).ToArray()) ui.RemoveCard(id);
            foreach (long id in cards.Where(id => !current.Contains(id))) ui.AddCard(id);
        }

        public void SubmitCast(int inputId, string magic, Vector3 position, Action<MagicValidInfo> complete)
        {
            if (state == null || state.status != "RUNNING" || commandPending || suspended)
            {
                complete(new MagicValidInfo { id = inputId, valid = false, resultCode = "FAIL_INVALID_PLACE", message = "" });
                return;
            }
            commandPending = true;
            var body = new { commandId = Guid.NewGuid().ToString(), revision = state.revision, action = "CAST", magic,
                position = new { x = position.x, y = 0, z = position.z } };
            StartCoroutine(Command(body, inputId, complete, state.stage, magic));
        }

        private IEnumerator SendAction(string action)
        {
            if (state == null || commandPending) yield break;
            commandPending = true;
            var body = new { commandId = Guid.NewGuid().ToString(), revision = state.revision, action };
            yield return Command(body, 0, null);
        }

        private IEnumerator Command(object body, int inputId, Action<MagicValidInfo> complete,
            string castStage = null, string castMagic = null)
        {
            while (busy) yield return null;
            busy = true;
            Reply reply = null;
            long code = 0;
            yield return TutorialBattleApi.Request<Reply>("POST", Api("/command"), jwt, body,
                (value, status) => { reply = value; code = status; });
            busy = false;
            if (!Current || finishing) yield break;
            if (reply == null && (code == 0 || code >= 500 || (code >= 200 && code < 300)))
            {
                suspended = true;
                ClearPointer();
                panel.Show("FactionTutorial.title", "FactionTutorial.error", "FactionTutorial.retry",
                    () => StartCoroutine(Command(body, inputId, complete, castStage, castMagic)));
                yield break;
            }
            bool accepted = reply?.accepted == true;
            if (accepted && castStage != null && castMagic != null)
            {
                guidedCards.Add(castStage + ":" + castMagic);
                ClearPointer();
            }
            complete?.Invoke(new MagicValidInfo { id = inputId, valid = accepted,
                resultCode = accepted ? "SUCCESS" : "FAIL_INVALID_PLACE", message = "" });
            commandPending = false;
            suspended = false;
            panel.Hide();
            if (!accepted && code != 409)
            {
                suspended = true;
                panel.Show("FactionTutorial.title", "FactionTutorial.castRetry", "FactionTutorial.retry", ResumePolling);
            }
            else yield return Poll();
        }

        private void ResumePolling()
        {
            panel.Hide();
            suspended = false;
            state = null;
            presentedState = null;
            pendingTerminal = null;
            playback.Invalidate();
        }

        private void LateUpdate()
        {
            if (state?.status != "RUNNING" || state.stage == "final" || suspended || !Current) return;
            if (guidedCards.Contains(state.stage + ":" + state.recommended))
            {
                ClearPointer();
                return;
            }
            var sender = FindObjectOfType<CardInputSender>();
            if (sender == null || string.IsNullOrEmpty(state.recommended)) return;
            string mode = sender.IsFieldSelectMode() ? "field" : "card";
            var card = FindObjectsOfType<CardUI>().FirstOrDefault(candidate => candidate.Magic?.serverName == state.recommended);
            if (mode == "card" && (card == null || !TutorialPointer.IsFullyOnScreen(card.transform as RectTransform))) mode = "bar";
            if (pointerMode == mode) return;
            ClearPointer();
            pointerMode = mode;
            if (mode == "field")
            {
                // explosion은 호환용 stage 이름이며 현재는 공중 유닛 소환 실습이다.
                bool summon = state.stage == "anti_air" || state.stage == "formation" || state.stage == "explosion";
                // 선택 시점의 사용 지점을 유지해 토네이도에 휘말린 적을 따라가지 않는다.
                Vector3 point = summon ? new Vector3(6, 0, 5) : new Vector3(state.targetX, 0, state.targetZ);
                pointer = TutorialPointer.PointAtScreen(() => TutorialPointer.ScreenPointOfWorld(point));
            }
            else if (mode == "card") pointer = TutorialPointer.PointAt(card.transform);
            else
            {
                var bar = FindObjectOfType<BarController>();
                if (bar != null && bar.ManaBarButtonTransform != null) pointer = TutorialPointer.PointAt(bar.ManaBarButtonTransform);
            }
        }

        private IEnumerator Finish()
        {
            finishing = true;
            pendingTerminal = null;
            playback.Invalidate();
            yield return TutorialBattleApi.Request<Reply>("DELETE", Api(""), jwt, null, (_, _) => { });
            if (!Current) yield break;
            SceneContext.MatchInfo = null;
            SceneContext.MatchResult = null;
            SceneManager.LoadScene("LobbyScene");
        }

        private void ClearPointer()
        {
            if (pointer != null) Destroy(pointer.gameObject);
            pointer = null;
            pointerMode = null;
        }

        private void OnDestroy()
        {
            playback.Invalidate();
            pendingTerminal = null;
            ClearPointer();
            TutorialHint.HideCurrent();
            if (Instance == this) Instance = null;
            if (ready != null && SceneContext.MatchInfo?.sessionId == ready.battleId) SceneContext.MatchInfo = null;
        }

        [Serializable, UnityEngine.Scripting.Preserve]
        private sealed class Reply
        {
            [UnityEngine.Scripting.Preserve] public Reply() { }
            public bool accepted;
        }
    }
}
