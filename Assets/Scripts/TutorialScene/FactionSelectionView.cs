using System;
using System.Linq;
using Data.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TutorialScene
{
    public sealed class FactionSelectionView : MonoBehaviour
    {
        [SerializeField] private Image[] backgrounds;
        [SerializeField] private RectTransform[] armies;
        [SerializeField] private Image[] hellUnits;
        [SerializeField] private Image[] natureUnits;
        [SerializeField] private UnityEngine.UI.Button[] regions;
        [SerializeField] private Image[] nameplates;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text[] names;
        private readonly float[] weights = { 1, 0, 0 };
        private int preview;
        private int hovered;
        private int focused;
        private int version;
        private bool choosing;

        public void Show(Action hell, Action nature)
        {
            gameObject.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            choosing = true;
            preview = 0;
            hovered = focused = 0;
            for (int i = 0; i < weights.Length; i++) weights[i] = i == 0 ? 1 : 0;
            Bind(0, hell);
            Bind(1, nature);
            int current = ++version;
            Localize(title, "FactionSelection.title", current);
            Localize(names[0], "FactionSelection.hell", current);
            Localize(names[1], "FactionSelection.nature", current);
            Apply(0);
        }

        public void Hide()
        {
            choosing = false;
            version++;
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
                EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform))
                EventSystem.current.SetSelectedGameObject(null);
            gameObject.SetActive(false);
        }

        public void Preview(int faction)
        {
            if (choosing) preview = Mathf.Clamp(faction, 0, 2);
        }

        public void Hover(int faction)
        {
            hovered = faction;
            Preview(faction);
        }

        public void ClearHover(int faction)
        {
            if (hovered != faction) return;
            hovered = 0;
            Preview(focused);
        }

        public void Focus(int faction)
        {
            focused = faction;
            Preview(faction);
        }

        public void ClearFocus(int faction)
        {
            if (focused != faction) return;
            focused = 0;
            Preview(hovered);
        }

        private void Bind(int index, Action action)
        {
            var button = regions[index];
            button.onClick.RemoveAllListeners();
            button.interactable = action != null;
            button.onClick.AddListener(() => {
                if (!choosing || action == null) return;
                choosing = false;
                foreach (var region in regions) region.interactable = false;
                action();
            });
        }

        private void LateUpdate()
        {
            if (choosing && EventSystem.current != null &&
                (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Tab)) &&
                !regions.Any(region => EventSystem.current.currentSelectedGameObject == region.gameObject))
                EventSystem.current.SetSelectedGameObject(regions[Input.GetKeyDown(KeyCode.RightArrow) ? 1 : 0].gameObject);
            Apply(1 - Mathf.Exp(-12 * Time.unscaledDeltaTime));
        }

        private void Apply(float blend)
        {
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = Mathf.Lerp(weights[i], preview == i ? 1 : 0, blend);
            }
            // 두 세력 사이를 바로 이동할 때 기본 배경이 중간에 비치지 않도록 합성 비율을 맞춘다.
            backgrounds[0].color = Color.white;
            backgrounds[1].color = new Color(1, 1, 1, weights[1] / Mathf.Max(.0001f, 1 - weights[2]));
            backgrounds[2].color = new Color(1, 1, 1, weights[2]);
            float hell = weights[1], nature = weights[2];
            Tint(hellUnits, nature);
            Tint(natureUnits, hell);
            armies[0].localScale = Vector3.one * (1 + .045f * hell);
            armies[1].localScale = Vector3.one * (1 + .045f * nature);
            nameplates[0].color = Color.Lerp(Color.white, new Color32(255, 154, 31, 255), hell);
            nameplates[1].color = Color.Lerp(Color.white, new Color32(170, 215, 95, 255), nature);
            names[0].alpha = 1 - .45f * nature;
            names[1].alpha = 1 - .45f * hell;
        }

        private static void Tint(Image[] units, float dim)
        {
            foreach (var unit in units) unit.color = Color.Lerp(Color.white, new Color(.34f, .36f, .38f, 1), dim);
        }

        private async void Localize(TMP_Text target, string key, int current)
        {
            target.text = "";
            string value = await LocaleUtils.GetStringAsync("Onboarding", key);
            if (this != null && current == version) target.text = value;
        }
    }
}
