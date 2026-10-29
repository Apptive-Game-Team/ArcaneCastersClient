using System.Collections;
using Data;
using Global;
using Data.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace LobbyScene
{
    public class PlayerProgressionPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private UnityEngine.UI.Button confirmButton;
        [SerializeField] private UnityEngine.UI.Button cancelButton;
        [SerializeField] private TMP_Text statusText;
        private PlayerProgressionDto state;
        private string selection;

        public IEnumerator Initialize()
        {
            foreach (var localized in GetComponentsInChildren<LocalizeStringEvent>(true)) localized.enabled = false;
            root.SetActive(false);
            confirmButton.onClick.RemoveAllListeners();
            cancelButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() => selection = "HELL");
            cancelButton.onClick.AddListener(() => selection = "NATURE");
            while (state == null || string.IsNullOrEmpty(state.faction))
            {
                yield return PlayerProgressionApi.Sync(result => state = result);
                if (state == null)
                {
                    SetText(messageText, "ProgressionRetry", "Could not load rewards. Retrying...");
                    root.SetActive(true);
                    SetButtons(false);
                    yield return new WaitForSecondsRealtime(3);
                    continue;
                }
                if (!string.IsNullOrEmpty(state.faction)) break;
                selection = null;
                SetText(messageText, "ProgressionChoose", "Choose your starting faction. Receive 5 spells, 3 copies each. Both factions can be collected later.");
                SetText(confirmButton.GetComponentInChildren<TMP_Text>(), "ProgressionHell", "Inferno & Stone");
                SetText(cancelButton.GetComponentInChildren<TMP_Text>(), "ProgressionNature", "Nature & Water");
                SetButtons(true);
                root.SetActive(true);
                yield return new WaitUntil(() => selection != null);
                SetButtons(false);
                yield return PlayerProgressionApi.Choose(selection, result => state = result);
            }
            root.SetActive(false);
            ShowStatus();
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
            }
        }

        private async void ShowStatus()
        {
            var current = state;
            string gold = await LocaleUtils.GetStringAsync("LobbyUI", "ProgressionGold");
            if (this == null || statusText == null || current == null) return;
            string xp = current.nextLevelXp == 0 ? "MAX" : $"{current.levelXp}/{current.nextLevelXp} XP";
            statusText.text = $"Lv. {current.level} · {xp} · {gold} {current.currency}";
            if (current.gainedXp > 0 || current.grantedChests > 0)
            {
                string reward = await LocaleUtils.GetStringAsync("LobbyUI", "ProgressionReceived");
                if (this != null && SystemMessageUI.Instance != null)
                    SystemMessageUI.Instance.ShowMessage(string.Format(reward, current.gainedXp, current.grantedChests));
                current.gainedXp = 0;
                current.grantedChests = 0;
            }
        }

        private void SetButtons(bool enabled)
        {
            confirmButton.interactable = enabled;
            cancelButton.interactable = enabled;
        }

        private static async void SetText(TMP_Text target, string key, string fallback)
        {
            if (target == null) return;
            target.text = fallback;
            string value = await LocaleUtils.GetStringAsync("LobbyUI", key);
            if (target != null && !string.IsNullOrEmpty(value)) target.text = value;
        }
    }
}
