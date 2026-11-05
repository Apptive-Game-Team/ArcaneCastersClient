using System;
using Data;
using Data.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LobbyScene {
    public class ProgressionRewardRow : MonoBehaviour {
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private Image background;
        [SerializeField] private Image magicIcon;
        [SerializeField] private Image chestIcon;
        [SerializeField] private UnityEngine.UI.Button button;
        public int Level { get; private set; }

        public void Bind(ProgressionLevelRewardDto reward, Action<int> select) {
            Level = reward.level;
            levelText.text = $"Lv. {Level}";
            chestIcon.gameObject.SetActive(reward.chestCount > 0);
            var name = reward.cards != null && reward.cards.Length > 0 ? reward.cards[0].magicName : null;
            magicIcon.sprite = string.IsNullOrEmpty(name) ? null : Resources.Load<Sprite>($"Game/sprites/{StringUtils.ToPascalCase(name)}");
            magicIcon.gameObject.SetActive(magicIcon.sprite != null);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => select(Level));
        }

        public void Refresh(int current, int selected, string paid, string locked) {
            background.color = Level == selected ? new Color32(255, 154, 31, 255) : Color.white;
            stateText.text = Level <= current ? paid : locked;
            stateText.color = Level <= current ? new Color32(47, 133, 42, 255) : new Color32(91, 98, 117, 255);
        }

        public void BindPending(string title, string status, Action select) {
            Level = 0;
            levelText.text = title;
            levelText.rectTransform.sizeDelta = new Vector2(240, 30);
            magicIcon.gameObject.SetActive(false);
            chestIcon.gameObject.SetActive(false);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => select());
            RefreshStatus(-1, status, true);
        }

        public void RefreshStatus(int selected, string status, bool reached) {
            background.color = Level == selected ? new Color32(255, 154, 31, 255) : Color.white;
            stateText.text = status;
            stateText.color = reached ? new Color32(47, 133, 42, 255) : new Color32(91, 98, 117, 255);
        }
    }
}
