using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Global
{
    /// <summary>
    /// Row of element buttons that drives a hidden element <see cref="TMP_Dropdown"/>.
    /// The Magic Book and the deck screen already filter through that dropdown's
    /// onValueChanged, so the bar only writes the dropdown's value and repaints the
    /// selected button; the filter code stays untouched.
    /// </summary>
    public class ElementFilterBar : MonoBehaviour
    {
        [Serializable]
        private class Entry
        {
            public UnityEngine.UI.Button button;
            public Image background;
            public int optionIndex;
        }

        [SerializeField] private TMP_Dropdown targetDropdown;
        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        [SerializeField] private Color selectedColor = new Color32(0xFF, 0xD2, 0x3F, 0xFF);
        [SerializeField] private Color normalColor = Color.white;

        private void Awake()
        {
            foreach (Entry entry in entries)
            {
                int index = entry.optionIndex;
                entry.button?.onClick.AddListener(() => Select(index));
            }
        }

        private void OnEnable()
        {
            targetDropdown?.onValueChanged.AddListener(Repaint);
            Repaint(targetDropdown != null ? targetDropdown.value : 0);
        }

        private void OnDisable()
        {
            targetDropdown?.onValueChanged.RemoveListener(Repaint);
        }

        private void Select(int optionIndex)
        {
            if (targetDropdown == null)
            {
                return;
            }

            // Setting value fires onValueChanged, which runs the existing filter and Repaint.
            targetDropdown.value = optionIndex;
        }

        private void Repaint(int selectedIndex)
        {
            foreach (Entry entry in entries)
            {
                if (entry.background != null)
                {
                    entry.background.color = entry.optionIndex == selectedIndex ? selectedColor : normalColor;
                }
            }
        }
    }
}
