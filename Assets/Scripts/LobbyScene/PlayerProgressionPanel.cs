using System.Collections;
using Data;
using Global;
using Data.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;

namespace LobbyScene
{
    public class PlayerProgressionPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private PlayerProgressionView view;
        private PlayerProgressionDto state;
        public bool IsSelectingFaction { get; private set; }

        public IEnumerator Initialize()
        {
            if (view == null) view = Instantiate(Resources.Load<GameObject>("UI/Progression/PlayerProgressionView")).GetComponent<PlayerProgressionView>();
            if (statusText != null) statusText.gameObject.SetActive(false);
            foreach (var localized in GetComponentsInChildren<LocalizeStringEvent>(true)) localized.enabled = false;
            root.SetActive(false);
            yield return PlayerProgressionApi.Sync(result => state = result);
            if (state == null)
            {
                StartCoroutine(InitializeWhenAvailable());
                yield break;
            }
            yield return CompleteInitialization();
        }

        private IEnumerator InitializeWhenAvailable()
        {
            while (state == null)
            {
                yield return new WaitForSecondsRealtime(30);
                yield return PlayerProgressionApi.Sync(result => state = result);
            }
            yield return CompleteInitialization();
            if (IsSelectingFaction) yield break;
            var lobby = FindObjectOfType<LobbyUIController>();
            if (lobby != null) yield return lobby.FetchDecks();
        }

        private IEnumerator CompleteInitialization()
        {
            if (state == null) yield break;
            if (string.IsNullOrEmpty(state.faction))
            {
                IsSelectingFaction = true;
                SceneManager.LoadScene(TutorialScene.FactionTutorialProgress.SelectionScene);
                yield break;
            }
            root.SetActive(false);
            ShowStatus();
            if (view != null) StartCoroutine(view.RefreshChests());
            GetComponent<Canvas>().sortingOrder = 19;
            yield return UserInfoGetter.RefreshGameUser();
            LobbyUIController.ClearCachedDecks();
            DeckScene.DeckManagementViewModel.ClearCachedUserData();
            SceneContext.OwnedCards = null;
            StartCoroutine(RefreshPeriodically());
        }

        private IEnumerator RefreshPeriodically()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(30);
                yield return PlayerProgressionApi.Sync(result => { if (result != null) state = result; });
                if (state != null && state.gainedXp > 0)
                {
                    DeckScene.DeckManagementViewModel.ClearCachedUserData();
                    SceneContext.OwnedCards = null;
                }
                ShowStatus();
                if (view != null) yield return view.RefreshChests();
            }
        }

        private async void ShowStatus()
        {
            var current = state;
            if (view != null) view.Bind(current);
            string gold = await LocaleUtils.GetStringAsync("LobbyUI", "ProgressionGold");
            if (this == null || current == null) return;
            string xp = current.nextLevelXp == 0 ? "MAX" : $"{current.levelXp}/{current.nextLevelXp} XP";
            if (statusText != null) statusText.text = $"Lv. {current.level} · {xp} · {gold} {current.currency}";
            if (current.gainedXp > 0 || current.grantedChests > 0)
            {
                string reward = await LocaleUtils.GetStringAsync("LobbyUI", "ProgressionReceived");
                if (this != null && SystemMessageUI.Instance != null)
                    SystemMessageUI.Instance.ShowMessage(string.Format(reward, current.gainedXp, current.grantedChests));
                current.gainedXp = 0;
                current.grantedChests = 0;
            }
        }

    }
}
