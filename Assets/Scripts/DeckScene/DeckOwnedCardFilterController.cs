using System.Collections.Generic;
using Data;
using Data.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace DeckScene
{
    public class DeckOwnedCardFilterController : MonoBehaviour
    {
        private const string MagicBookTable = "MagicBook";
        private const string ElementTable = "Element";

        [SerializeField] private DeckManagementController deckManagementController;
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private TMP_Dropdown sortDropdown;
        [SerializeField] private TMP_Dropdown attributeDropdown;

        private readonly DeckOwnedCardSortMode[] sortOptions =
        {
            DeckOwnedCardSortMode.Name,
            DeckOwnedCardSortMode.Attribute,
            DeckOwnedCardSortMode.ManaCost,
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

        private int localizationRefreshVersion;

        private void Awake()
        {
            deckManagementController ??= FindObjectOfType<DeckManagementController>();
            searchInput ??= transform.Find("SearchInput")?.GetComponent<TMP_InputField>();
            sortDropdown ??= transform.Find("SortDropdown")?.GetComponent<TMP_Dropdown>();
            attributeDropdown ??= transform.Find("AttributeDropdown")?.GetComponent<TMP_Dropdown>();

            BindControls();
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
            UnbindControls();
        }

        private void BindControls()
        {
            searchInput?.onValueChanged.AddListener(OnSearchChanged);
            sortDropdown?.onValueChanged.AddListener(OnSortChanged);
            attributeDropdown?.onValueChanged.AddListener(OnAttributeChanged);
        }

        private void UnbindControls()
        {
            searchInput?.onValueChanged.RemoveListener(OnSearchChanged);
            sortDropdown?.onValueChanged.RemoveListener(OnSortChanged);
            attributeDropdown?.onValueChanged.RemoveListener(OnAttributeChanged);
        }

        private void OnSelectedLocaleChanged(Locale locale)
        {
            RefreshLocalizedText();
        }

        private void OnSearchChanged(string searchText)
        {
            deckManagementController?.SetOwnedCardSearch(searchText);
        }

        private void OnSortChanged(int index)
        {
            if (index >= 0 && index < sortOptions.Length)
            {
                deckManagementController?.SetOwnedCardSortMode(sortOptions[index]);
            }
        }

        private void OnAttributeChanged(int index)
        {
            if (index >= 0 && index < attributeOptions.Length)
            {
                deckManagementController?.SetOwnedCardAttributeFilter(attributeOptions[index]);
            }
        }

        private async void RefreshLocalizedText()
        {
            int refreshVersion = ++localizationRefreshVersion;
            var sortLabels = new List<string>
            {
                await GetMagicBookText("filter.name", "이름"),
                await GetMagicBookText("filter.attributeSort", "속성"),
                await GetMagicBookText("filter.manaCost", "마나"),
            };

            if (refreshVersion != localizationRefreshVersion)
            {
                return;
            }

            var attributeLabels = new List<string> { await GetMagicBookText("filter.all", "전체") };
            for (int i = 1; i < attributeOptions.Length; i++)
            {
                ElementType element = attributeOptions[i].Value;
                string label = await LocaleUtils.GetStringAsync(ElementTable, element.ToString());
                attributeLabels.Add(IsMissingLocalization(label, element.ToString()) ? element.ToString() : label);
            }

            if (refreshVersion != localizationRefreshVersion)
            {
                return;
            }

            SetOptions(sortDropdown, sortLabels);
            SetOptions(attributeDropdown, attributeLabels);

            if (searchInput?.placeholder is TMP_Text placeholder)
            {
                placeholder.text = await GetMagicBookText("filter.search", "검색");
            }
        }

        private static void SetOptions(TMP_Dropdown dropdown, List<string> options)
        {
            if (dropdown == null)
            {
                return;
            }

            int selectedIndex = Mathf.Clamp(dropdown.value, 0, options.Count - 1);
            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdown.SetValueWithoutNotify(selectedIndex);
            dropdown.RefreshShownValue();
        }

        private static async System.Threading.Tasks.Task<string> GetMagicBookText(string key, string fallback)
        {
            string text = await LocaleUtils.GetStringAsync(MagicBookTable, key);
            return IsMissingLocalization(text, key) ? fallback : text;
        }

        private static bool IsMissingLocalization(string text, string key)
        {
            return string.IsNullOrWhiteSpace(text) || text == key;
        }
    }
}
