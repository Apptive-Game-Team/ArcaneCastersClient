using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using UiButton = UnityEngine.UI.Button;

namespace LobbyScene
{
    public sealed class PracticeDifficultyPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text summary;
        [SerializeField] private ScrollRect list;
        [SerializeField] private UiButton rowPrefab;
        [SerializeField] private UiButton startButton;
        [SerializeField] private UiButton quickButton;
        [SerializeField] private UiButton backButton;

        private readonly List<UiButton> rows = new();
        private StringTable strings;
        private PracticeDifficultiesDto data;
        private PracticeTierDto selected;
        private bool retry;

        private void Awake()
        {
            startButton.onClick.AddListener(StartOrRetry);
            quickButton.onClick.AddListener(() => StartMatch(null));
            backButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void OnEnable()
        {
            LobbySceneViewModel.Instance.CurrentState.OnStateChange += LobbyChanged;
            StartCoroutine(Load());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (LobbySceneViewModel.Instance != null)
                LobbySceneViewModel.Instance.CurrentState.OnStateChange -= LobbyChanged;
        }

        private void LobbyChanged(LobbySceneViewModel.LobbyState state)
        {
            if (state != LobbySceneViewModel.LobbyState.Idle)
                gameObject.SetActive(false);
        }

        private IEnumerator Load()
        {
            Clear();
            retry = false;
            startButton.interactable = false;
            var table = LocalizationSettings.StringDatabase.GetTableAsync("LobbyUI");
            yield return table;
            strings = table.Status == AsyncOperationStatus.Succeeded ? table.Result : null;
            title.text = Text("Title", "Computer Battle");
            Label(backButton, Text("Back", "Back"));
            Label(quickButton, Text("Quick", "Quick practice"));
            Label(startButton, Text("Start", "Start battle"));
            summary.text = Text("Loading", "Loading results...");
            yield return LobbySceneViewModel.Instance.LoadPracticeDifficulties(Render);
        }

        internal void Render(PracticeDifficultiesDto response)
        {
            Clear();
            if (response?.tiers == null)
            {
                summary.text = Text("Error", "Could not load difficulty levels.");
                retry = true;
                Label(startButton, Text("Retry", "Retry"));
                startButton.interactable = true;
                return;
            }

            data = response;
            PracticeTierDto recommended = null;
            bool hasResults = false;
            foreach (var tier in data.tiers)
            {
                if (tier == null || string.IsNullOrEmpty(tier.id)) continue;
                var row = Instantiate(rowPrefab, list.content);
                row.interactable = tier.available;
                row.onClick.AddListener(() => Select(tier));
                string availability = tier.available ? string.Empty : "  " + Text("Unavailable", "Unavailable");
                Label(row, NameOf(tier) + "<pos=46%>" + Record(tier) + availability);
                rows.Add(row);
                hasResults |= tier.wins + tier.losses + tier.draws > 0;
                if (tier.available && selected == null) selected = tier;
                if (tier.available && tier.id == data.recommendedId) recommended = tier;
            }

            if (recommended != null)
            {
                selected = recommended;
                int games = recommended.wins + recommended.losses + recommended.draws;
                summary.text = (hasResults
                    ? string.Format(Text("Recommended", "Recommended: {0}"), NameOf(recommended))
                    : string.Format(Text("Begin", "Start with {0}"), NameOf(recommended)))
                    + "\n" + (games < 5
                        ? string.Format(Text("Progress", "Recent battles: {0}/5"), games)
                        : Record(recommended));
            }
            else
            {
                summary.text = selected == null
                    ? Text("NoneAvailable", "No opponents are available right now.")
                    : Text("NoRecommendation", "Choose a difficulty level.");
            }
            Select(selected);
            Canvas.ForceUpdateCanvases();
            list.verticalNormalizedPosition = 1;
        }

        private void Select(PracticeTierDto tier)
        {
            selected = tier;
            int index = 0;
            foreach (var entry in data.tiers)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id)) continue;
                rows[index++].image.color = entry == selected ? new Color32(255, 196, 105, 255) : Color.white;
            }
            startButton.interactable = selected != null;
        }

        private string Record(PracticeTierDto tier)
        {
            if (tier.wins + tier.losses + tier.draws == 0) return Text("NoRecord", "No results");
            return string.Format(Text("Record", "{0}W  {1}L"), tier.wins, tier.losses)
                + (tier.draws > 0 ? string.Format(Text("Draws", "  {0}D"), tier.draws) : string.Empty);
        }

        private string NameOf(PracticeTierDto tier) => Text("Tier." + tier.id, tier.id);

        private string Text(string key, string fallback) =>
            strings?.GetEntry("ComputerBattle." + key)?.LocalizedValue ?? fallback;

        private static void Label(UiButton button, string value) =>
            button.GetComponentInChildren<TMP_Text>(true).text = value;

        private void Clear()
        {
            foreach (var row in rows)
            {
                row.gameObject.SetActive(false);
                Destroy(row.gameObject);
            }
            rows.Clear();
            selected = null;
            data = null;
        }

        private void StartOrRetry()
        {
            if (retry) StartCoroutine(Load());
            else if (selected != null) StartMatch(selected.id);
        }

        private void StartMatch(string difficultyId)
        {
            var lobby = LobbySceneViewModel.Instance;
            if (lobby.CurrentState.Data != LobbySceneViewModel.LobbyState.Idle) return;
            gameObject.SetActive(false);
            if (difficultyId == null) lobby.PlayPracticeMatch();
            else lobby.PlayPracticeMatch(difficultyId);
        }
    }
}
