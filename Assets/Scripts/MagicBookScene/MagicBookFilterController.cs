using System;
using System.Collections.Generic;
using Data;
using Data.Localization;
using Data.Magic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace MagicBookScene
{
    public class MagicBookFilterController : MonoBehaviour
    {
        private const string MagicBookTable = "MagicBook";
        private const string ElementTable = "Element";

        [SerializeField] private MagicInfoFactory magicInfoFactory;
        [SerializeField] private TMP_Text sortLabel;
        [SerializeField] private TMP_Dropdown sortDropdown;
        [SerializeField] private TMP_Text attributeLabel;
        [SerializeField] private TMP_Dropdown attributeDropdown;
        // TODO(#578): 시전 종류 필터는 없어진다. 씬에서 지울 때까지 참조만 남겨 두고 감춘다.
        [SerializeField] private TMP_Text actionTypeLabel;
        [SerializeField] private TMP_Dropdown actionTypeDropdown;
        [SerializeField] private TMP_Dropdown typeDropdown;
        [SerializeField] private TMP_Dropdown manaDropdown;
        [SerializeField] private SortButtonBinding[] sortButtons;
        [SerializeField] private FilterButtonBinding[] attributeButtons;
        [SerializeField] private Color selectedColor = new(0.78f, 0.78f, 0.78f, 1f);

        private readonly MagicBookSortMode[] sortOptions =
        {
            MagicBookSortMode.Name,
            MagicBookSortMode.Attribute,
            MagicBookSortMode.ManaCost,
        };

        private readonly ElementType?[] attributeOptions =
        {
            null,
            ElementType.Fire,
            ElementType.Water,
            ElementType.Nature,
            ElementType.Lightning,
            ElementType.Rock,
            ElementType.Wind,
        };

        private MagicBookSortMode selectedSortMode = MagicBookSortMode.Name;
        private ElementType? selectedAttribute;
        private int localizationRefreshVersion;

        private void Awake()
        {
            if (magicInfoFactory == null)
            {
                magicInfoFactory = FindObjectOfType<MagicInfoFactory>();
            }

            HideActionTypeFilter();
            BindDropdowns();
            BindFallbackButtons();
            RefreshLocalizedText();
        }

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        }

        private void OnDestroy()
        {
            UnbindDropdowns();
            UnbindFallbackButtons();
        }

        private void OnSelectedLocaleChanged(Locale locale)
        {
            RefreshLocalizedText();
        }

        private void BindDropdowns()
        {
            if (sortDropdown != null)
            {
                sortDropdown.onValueChanged.AddListener(OnSortChanged);
            }

            if (attributeDropdown != null)
            {
                attributeDropdown.onValueChanged.AddListener(OnAttributeChanged);
            }

            typeDropdown?.onValueChanged.AddListener(OnTypeChanged);
            manaDropdown?.onValueChanged.AddListener(OnManaChanged);
        }

        /// <summary>
        /// 시전 종류 축이 없어져 이 필터는 아무것도 거르지 않는다. 화면에서 감춰 둔다.
        /// TODO(#578): 도감 화면을 정리할 때 MagicBookScene 에서 이 dropdown 과 label 을 지운다.
        /// </summary>
        private void HideActionTypeFilter()
        {
            if (actionTypeDropdown != null)
            {
                actionTypeDropdown.gameObject.SetActive(false);
            }

            if (actionTypeLabel != null)
            {
                actionTypeLabel.gameObject.SetActive(false);
            }
        }

        private void BindFallbackButtons()
        {
            if (sortDropdown != null || attributeDropdown != null || actionTypeDropdown != null)
            {
                return;
            }

            BindSortButtons();
            BindFilterButtons(attributeButtons, SelectAttribute);
            ApplyFallbackSelectionState();
        }

        private void UnbindFallbackButtons()
        {
            UnbindSortButtons();
            UnbindFilterButtons(attributeButtons);
        }

        private void BindSortButtons()
        {
            if (sortButtons == null)
            {
                return;
            }

            foreach (SortButtonBinding binding in sortButtons)
            {
                if (binding.button == null)
                {
                    continue;
                }

                MagicBookSortMode mode = binding.mode;
                binding.clickAction = () => SelectSortMode(mode);
                binding.button.onClick.AddListener(binding.clickAction);
            }
        }

        private void UnbindSortButtons()
        {
            if (sortButtons == null)
            {
                return;
            }

            foreach (SortButtonBinding binding in sortButtons)
            {
                if (binding.button != null && binding.clickAction != null)
                {
                    binding.button.onClick.RemoveListener(binding.clickAction);
                    binding.clickAction = null;
                }
            }
        }

        private void BindFilterButtons(FilterButtonBinding[] buttons, Action<FilterButtonBinding> onClick)
        {
            if (buttons == null)
            {
                return;
            }

            foreach (FilterButtonBinding binding in buttons)
            {
                if (binding.button == null)
                {
                    continue;
                }

                FilterButtonBinding captured = binding;
                binding.clickAction = () => onClick(captured);
                binding.button.onClick.AddListener(binding.clickAction);
            }
        }

        private void UnbindFilterButtons(FilterButtonBinding[] buttons)
        {
            if (buttons == null)
            {
                return;
            }

            foreach (FilterButtonBinding binding in buttons)
            {
                if (binding.button != null && binding.clickAction != null)
                {
                    binding.button.onClick.RemoveListener(binding.clickAction);
                    binding.clickAction = null;
                }
            }
        }

        private void UnbindDropdowns()
        {
            if (sortDropdown != null)
            {
                sortDropdown.onValueChanged.RemoveListener(OnSortChanged);
            }

            if (attributeDropdown != null)
            {
                attributeDropdown.onValueChanged.RemoveListener(OnAttributeChanged);
            }

            typeDropdown?.onValueChanged.RemoveListener(OnTypeChanged);
            manaDropdown?.onValueChanged.RemoveListener(OnManaChanged);
        }

        private async void RefreshLocalizedText()
        {
            int refreshVersion = ++localizationRefreshVersion;

            if (sortLabel != null)
            {
                sortLabel.text = await GetMagicBookText("filter.sort", "정렬");
            }

            if (refreshVersion != localizationRefreshVersion)
            {
                return;
            }

            if (attributeLabel != null)
            {
                attributeLabel.text = await GetMagicBookText("filter.attribute", "속성");
            }

            if (refreshVersion != localizationRefreshVersion)
            {
                return;
            }

            await PopulateSortDropdown(refreshVersion);
            await PopulateElementDropdown(attributeDropdown, attributeOptions, refreshVersion);

            List<string> typeLabels = await MagicFilterLabels.CastKindLabels();
            List<string> manaLabels = await MagicFilterLabels.ManaBandLabels();
            if (refreshVersion != localizationRefreshVersion)
            {
                return;
            }

            if (typeDropdown != null)
            {
                SetOptions(typeDropdown, typeLabels, typeDropdown.value);
            }

            if (manaDropdown != null)
            {
                SetOptions(manaDropdown, manaLabels, manaDropdown.value);
            }
        }

        private async System.Threading.Tasks.Task PopulateSortDropdown(int refreshVersion)
        {
            if (sortDropdown == null)
            {
                return;
            }

            var options = new List<string>
            {
                await GetMagicBookText("filter.name", "이름"),
                await GetMagicBookText("filter.attributeSort", "속성"),
                await GetMagicBookText("filter.manaCost", "마나"),
            };

            if (refreshVersion == localizationRefreshVersion)
            {
                SetOptions(sortDropdown, options, sortDropdown.value);
            }
        }

        private async System.Threading.Tasks.Task PopulateElementDropdown(TMP_Dropdown dropdown, ElementType?[] elementOptions, int refreshVersion)
        {
            if (dropdown == null)
            {
                return;
            }

            var options = new List<string> { await GetMagicBookText("filter.all", "전체") };
            for (int i = 1; i < elementOptions.Length; i++)
            {
                ElementType element = elementOptions[i].Value;
                options.Add(await GetElementText(element));
            }

            if (refreshVersion == localizationRefreshVersion)
            {
                SetOptions(dropdown, options, dropdown.value);
            }
        }

        private static void SetOptions(TMP_Dropdown dropdown, List<string> options, int selectedIndex)
        {
            selectedIndex = Mathf.Clamp(selectedIndex, 0, options.Count - 1);
            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdown.SetValueWithoutNotify(selectedIndex);
            dropdown.RefreshShownValue();
        }

        private void OnSortChanged(int index)
        {
            if (index < 0 || index >= sortOptions.Length)
            {
                return;
            }

            selectedSortMode = sortOptions[index];
            magicInfoFactory?.SetSortMode(selectedSortMode);
        }

        private void OnTypeChanged(int index)
        {
            if (index >= 0 && index < MagicCastKinds.FilterOptions.Length)
            {
                magicInfoFactory?.SetCastKindFilter(MagicCastKinds.FilterOptions[index]);
            }
        }

        private void OnManaChanged(int index)
        {
            if (index >= 0 && index < ManaBands.FilterOptions.Length)
            {
                magicInfoFactory?.SetManaBandFilter(ManaBands.FilterOptions[index]);
            }
        }

        private void SelectSortMode(MagicBookSortMode mode)
        {
            selectedSortMode = mode;
            magicInfoFactory?.SetSortMode(mode);
            ApplyFallbackSelectionState();
        }

        private void OnAttributeChanged(int index)
        {
            if (index < 0 || index >= attributeOptions.Length)
            {
                return;
            }

            selectedAttribute = attributeOptions[index];
            magicInfoFactory?.SetAttributeFilter(selectedAttribute);
        }

        private void SelectAttribute(FilterButtonBinding binding)
        {
            selectedAttribute = binding.isAll ? null : binding.elementType;
            magicInfoFactory?.SetAttributeFilter(selectedAttribute);
            ApplyFallbackSelectionState();
        }

        private void ApplyFallbackSelectionState()
        {
            if (sortButtons != null)
            {
                foreach (SortButtonBinding binding in sortButtons)
                {
                    SetSelected(binding.button, binding.mode == selectedSortMode);
                }
            }

            ApplyFilterSelectionState(attributeButtons, selectedAttribute);
        }

        private void ApplyFilterSelectionState(FilterButtonBinding[] buttons, ElementType? selectedElement)
        {
            if (buttons == null)
            {
                return;
            }

            foreach (FilterButtonBinding binding in buttons)
            {
                bool selected = binding.isAll ? !selectedElement.HasValue : selectedElement == binding.elementType;
                SetSelected(binding.button, selected);
            }
        }

        private void SetSelected(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            Graphic graphic = button.targetGraphic != null ? button.targetGraphic : button.GetComponent<Graphic>();
            if (graphic == null)
            {
                return;
            }

            graphic.color = selected ? selectedColor : Color.white;
        }

        private static async System.Threading.Tasks.Task<string> GetMagicBookText(string key, string fallback)
        {
            string text = await LocaleUtils.GetStringAsync(MagicBookTable, key);
            return IsMissingLocalization(text, key) ? fallback : text;
        }

        private static async System.Threading.Tasks.Task<string> GetElementText(ElementType element)
        {
            string key = element.ToString();
            string text = await LocaleUtils.GetStringAsync(ElementTable, key);
            return IsMissingLocalization(text, key) ? key : text;
        }

        private static bool IsMissingLocalization(string text, string key)
        {
            return string.IsNullOrWhiteSpace(text) || text == key;
        }
    }

    [Serializable]
    public class SortButtonBinding
    {
        public Button button;
        public MagicBookSortMode mode;
        [NonSerialized] public UnityAction clickAction;
    }

    [Serializable]
    public class FilterButtonBinding
    {
        public Button button;
        public bool isAll;
        public ElementType elementType;
        [NonSerialized] public UnityAction clickAction;
    }
}
