using System;
using Data.Localization;
using GameScene.Card;
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
        [SerializeField] private ZoomedCardUI rewardDetails;
        [SerializeField] private GameObject compactRoot;
        [SerializeField] private GameObject background;
        [SerializeField] private TMP_Text compactTitle;
        [SerializeField] private TMP_Text compactBody;
        [SerializeField] private UnityEngine.UI.Button compactNext;
        private int version;

        public void Show(string titleKey, string bodyKey, string nextKey = null, Action action = null,
            Action chooseHell = null, Action chooseNature = null, string rewardFaction = null)
        {
            gameObject.SetActive(true);
            if (rewardDetails != null) rewardDetails.Hide();
            int current = ++version;
            bool choosing = chooseHell != null || chooseNature != null;
            bool compact = !choosing && rewardFaction == null && compactRoot != null;
            title.transform.parent.gameObject.SetActive(!compact);
            if (background != null) background.SetActive(!compact);
            if (compactRoot != null) compactRoot.SetActive(compact);
            SetText(compact ? compactTitle : title, titleKey, current);
            SetText(compact ? compactBody : body, bodyKey, current);
            Bind(hell, "FactionTutorial.hell", chooseHell, current);
            Bind(nature, "FactionTutorial.nature", chooseNature, current);
            Bind(next, nextKey, compact ? null : action, current);
            if (compactNext != null) Bind(compactNext, nextKey, compact ? action : null, current);
            SetIcons(hellIcons, FactionTutorialCards.Hell, choosing || rewardFaction == "HELL", choosing ? -490 : -210, rewardFaction == "HELL");
            SetIcons(natureIcons, FactionTutorialCards.Nature, choosing || rewardFaction == "NATURE", choosing ? 70 : -210, rewardFaction == "NATURE");
        }

        public void Hide() { version++; if (rewardDetails != null) rewardDetails.Hide(); gameObject.SetActive(false); }

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

        private void SetIcons(Image[] icons, string[] names, bool visible, float start, bool describe)
        {
            if (icons == null) return;
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i].transform.parent.gameObject.SetActive(visible);
                ((RectTransform)icons[i].transform.parent).anchoredPosition = new Vector2(start + i * 105, -40);
                if (visible && i < names.Length)
                    icons[i].sprite = DeckScene.DeckCardSpriteResolver.GetMagicSprite(names[i]);
                var card = icons[i].transform.parent.GetComponent<TutorialRewardCard>();
                if (card != null)
                {
                    card.Bind(i < names.Length ? names[i] : null, rewardDetails);
                    card.enabled = visible && describe;
                }
            }
        }
    }

    public static class FactionTutorialCards
    {
        // 시작 보상과 실습은 같은 5종이다. DB V125의 1레벨 지급표와 맞춘다.
        public static readonly string[] Hell = { "fire_shot", "rock_golem", "rock_mage", "wall_golem", "stone_gargoyle" };
        public static readonly string[] Nature = { "chain_lightning", "electric_spearman", "aqua_archer", "tree_golem", "thunder_spirit" };
        // 실습 역할 순서: 슛, 근접, 원거리, 탱커, 공중.
        public static readonly string[] HellPractice = { "fire_shot", "rock_golem", "rock_mage", "wall_golem", "stone_gargoyle" };
        public static readonly string[] NaturePractice = { "chain_lightning", "electric_spearman", "aqua_archer", "tree_golem", "thunder_spirit" };
    }
}
