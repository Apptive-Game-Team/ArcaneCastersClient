using System;
using Data.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TutorialScene
{
    public sealed class FactionTutorialPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;
        [SerializeField] private UnityEngine.UI.Button hell;
        [SerializeField] private UnityEngine.UI.Button nature;
        [SerializeField] private UnityEngine.UI.Button next;
        [SerializeField] private Image[] hellIcons;
        [SerializeField] private Image[] natureIcons;
        private int version;

        public void Show(string titleKey, string bodyKey, string nextKey = null, Action action = null,
            Action chooseHell = null, Action chooseNature = null, string rewardFaction = null)
        {
            gameObject.SetActive(true);
            int current = ++version;
            SetText(title, titleKey, current);
            SetText(body, bodyKey, current);
            Bind(hell, "FactionTutorial.hell", chooseHell, current);
            Bind(nature, "FactionTutorial.nature", chooseNature, current);
            Bind(next, nextKey, action, current);
            bool choosing = chooseHell != null || chooseNature != null;
            SetIcons(hellIcons, FactionTutorialCards.Hell, choosing || rewardFaction == "HELL", choosing ? -490 : -210);
            SetIcons(natureIcons, FactionTutorialCards.Nature, choosing || rewardFaction == "NATURE", choosing ? 70 : -210);
        }

        public void Hide() { version++; gameObject.SetActive(false); }

        private void Bind(UnityEngine.UI.Button button, string key, Action action, int current)
        {
            button.onClick.RemoveAllListeners();
            button.gameObject.SetActive(action != null);
            if (action == null) return;
            button.interactable = true;
            SetText(button.GetComponentInChildren<TMP_Text>(true), key, current);
            button.onClick.AddListener(() => {
                button.interactable = false;
                action();
            });
        }

        private async void SetText(TMP_Text target, string key, int current)
        {
            if (target == null) return;
            target.text = "";
            string text = await LocaleUtils.GetStringAsync("Onboarding", key);
            if (this != null && current == version && target != null) target.text = text;
        }

        private static void SetIcons(Image[] icons, string[] names, bool visible, float start)
        {
            if (icons == null) return;
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i].transform.parent.gameObject.SetActive(visible);
                ((RectTransform)icons[i].transform.parent).anchoredPosition = new Vector2(start + i * 105, -40);
                if (visible && i < names.Length)
                    icons[i].sprite = DeckScene.DeckCardSpriteResolver.GetMagicSprite(names[i]);
            }
        }
    }

    public static class FactionTutorialCards
    {
        public static readonly string[] Hell = { "fire_shot", "ember_spirit_swarm", "towerback", "rock_golem", "magma_explosion" };
        public static readonly string[] Nature = { "chain_lightning", "water_slime_swarm", "vine_spirit", "tree_golem", "water_explosion" };
    }
}
