using System;
using Data;
using GameScene.Dto;
using Global;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;

namespace ResultScene
{
    public class ResultSceneUIController : MonoBehaviour
    {
        public static ResultSceneUIController Instance;
        private ResultInfo resultInfo;

        [SerializeField] TextMeshProUGUI resultText;
        [SerializeField] TextMeshProUGUI mmrDeltaText;
        [SerializeField] GameObject winTitle;
        [SerializeField] GameObject loseTitle;
        [SerializeField] GameObject drawTitle;
        [SerializeField] Color mmrGainColor = new Color(0.35686f, 0.81569f, 0.29804f, 1f);
        [SerializeField] Color mmrLossColor = new Color(0.94118f, 0.26667f, 0.22745f, 1f);

        // Adventure matches replace the MMR result with cleared/failed and a stage
        // caption instead. AdventureMapController stamps the adventure/stage context
        // onto SceneContext right before starting the match (that scene and
        // CurrentAdventure are both gone by the time the match ends), so this reads
        // it back rather than re-fetching anything.
        [SerializeField] private LocalizeStringEvent winTitleLabel;
        [SerializeField] private LocalizeStringEvent loseTitleLabel;
        [SerializeField] private GameObject mmrLabel;
        [SerializeField] private GameObject retryButton;

        // LobbyUI table keys. TableEntryReference has no public constructor; it converts
        // implicitly from a key string or an entry id.
        private static readonly TableEntryReference AdventureClearedEntry = "AdventureCleared";
        private static readonly TableEntryReference AdventureFailedEntry = "AdventureFailed";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            // 결과 화면에 들어왔다는 것은 세션이 닫혔다는 뜻이다. 신고하지 않으면 로비 티켓이
            // MATCHED로 남아 다음 매칭이 조용히 무시된다. 결과 표시와는 독립적으로 진행한다.
            StartCoroutine(SessionLossReporter.ReportMatchCompleted());

            resultInfo = SceneContext.MatchResult;
            SceneContext.MatchResult = null;

            if (resultInfo == null)
            {
                WDebug.LogError("ResultInfo is null. Cannot display result.");
                ShowTitle(null);
                SetResultText("No result available.");
                SetMmrDelta(null);
                return;
            }

            if (SceneContext.Me == "LeftPlayer")
            {
                ShowResult(resultInfo.leftPlayer, resultInfo.lastLeftPlayerMmr, resultInfo.newLeftPlayerMmr);
            }
            else if (SceneContext.Me == "RightPlayer")
            {
                ShowResult(resultInfo.rightPlayer, resultInfo.lastRightPlayerMmr, resultInfo.newRightPlayerMmr);
            }
            else
            {
                ShowTitle(null);
                SetResultText("You are not part of this match.");
                SetMmrDelta(null);
            }
        }

        // outcome 은 서버 ResultType 의 이름(Win, Lose, Draw)이다.
        private void ShowResult(string outcome, short lastMmr, short newMmr)
        {
            if (SceneContext.AdventureId.HasValue)
            {
                ShowAdventureResult(outcome);
                return;
            }

            ShowTitle(outcome);
            SetResultText(newMmr.ToString());
            SetMmrDelta(newMmr - lastMmr);
        }

        /// <summary>
        /// Adventure result: cleared/failed instead of an MMR delta, plus the
        /// adventure/stage caption (e.g. "Forest · Stage 2 · 2/3"). Cleared count
        /// assumes the linear stage progression AdventureMapController already relies
        /// on for NextScenario, so no re-fetch is needed here.
        /// </summary>
        private void ShowAdventureResult(string outcome)
        {
            bool cleared = string.Equals(outcome, "Win", StringComparison.OrdinalIgnoreCase);

            SetActive(winTitle, cleared);
            SetActive(loseTitle, !cleared);
            SetActive(drawTitle, false);
            SwapLabel(winTitleLabel, AdventureClearedEntry);
            SwapLabel(loseTitleLabel, AdventureFailedEntry);

            SetActive(mmrLabel, false);
            if (mmrDeltaText != null) mmrDeltaText.gameObject.SetActive(false);

            int clearedCount = Mathf.Clamp(
                SceneContext.AdventureStageClearedBeforeMatch + (cleared ? 1 : 0),
                0,
                SceneContext.AdventureStageScenarioCount);

            string caption = $"{SceneContext.AdventureName} · Stage {SceneContext.AdventureStageNumber}";
            if (SceneContext.AdventureStageScenarioCount > 1)
            {
                caption += $" · {clearedCount}/{SceneContext.AdventureStageScenarioCount}";
            }
            SetResultText(caption);

            SetActive(retryButton, true);
        }

        private static void SwapLabel(LocalizeStringEvent label, TableEntryReference entry)
        {
            if (label == null) return;
            label.StringReference.TableEntryReference = entry;
            label.RefreshString();
        }

        private void ShowTitle(string outcome)
        {
            SetActive(winTitle, string.Equals(outcome, "Win", StringComparison.OrdinalIgnoreCase));
            SetActive(loseTitle, string.Equals(outcome, "Lose", StringComparison.OrdinalIgnoreCase));
            SetActive(drawTitle, string.Equals(outcome, "Draw", StringComparison.OrdinalIgnoreCase));
        }

        private void SetMmrDelta(int? delta)
        {
            if (mmrDeltaText == null) return;

            if (delta == null)
            {
                mmrDeltaText.text = string.Empty;
                return;
            }

            mmrDeltaText.text = delta.Value >= 0 ? $"+{delta.Value}" : delta.Value.ToString();
            mmrDeltaText.color = delta.Value >= 0 ? mmrGainColor : mmrLossColor;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null) target.SetActive(active);
        }

        private void SetResultText(string text)
        {
            resultText.text = text;
        }
    }
}
