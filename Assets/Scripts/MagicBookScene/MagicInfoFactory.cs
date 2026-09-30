using System.Collections.Generic;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Data;
using Data.Localization;
using Data.Magic;
using Global;
using UnityEngine;

namespace MagicBookScene
{
    public enum MagicBookSortMode
    {
        Name,
        Attribute,
        ManaCost,
    }

    public class MagicInfoFactory : MonoBehaviour
    {
        [SerializeField] private Transform magicInfoParent;
        [SerializeField] private GameObject magicInfoPrefab;
        [SerializeField] private MagicInfo magicInfo;
        [SerializeField] private SelectedMagicView selectedMagicView;
        
        [SerializeField] private UserMagicApiClient userMagicApiClient;

        public event Action MagicSelected;

        private static readonly List<ElementType> EmptyElements = new();

        private readonly List<MagicBookEntry> entries = new();
        private readonly List<MagicButton> magicButtons = new();
        private long? selectedMagicId;
        private MagicBookSortMode sortMode = MagicBookSortMode.Name;
        private ElementType? selectedAttribute;
        private MagicCastKind? selectedCastKind;
        private ManaBand? selectedManaBand;
        private System.Threading.SynchronizationContext unityContext;
        
        private void Awake()
        {
            unityContext = System.Threading.SynchronizationContext.Current;
            var savedMagicJson = PlayerPrefs.GetString(MagicInfoDataSource.PlayerPrefsKeyName, string.Empty);
            WDebug.Log($"[MagicInfoFactory] Saved magic json: {savedMagicJson}");

            MagicInfoDataSource.Instance.GetMagics(_ =>
            {
                userMagicApiClient.GetUserMagic(response =>
                {
                    LoadEntries(response?.magicIds);
                });
            });
        }

        public void SetSortMode(MagicBookSortMode mode)
        {
            sortMode = mode;
            RenderCurrentView();
        }

        public void SetAttributeFilter(ElementType? attribute)
        {
            selectedAttribute = attribute;
            RenderCurrentView();
        }

        public void SetCastKindFilter(MagicCastKind? castKind)
        {
            selectedCastKind = castKind;
            RenderCurrentView();
        }

        public void SetManaBandFilter(ManaBand? manaBand)
        {
            selectedManaBand = manaBand;
            RenderCurrentView();
        }
        
        private async void LoadEntries(List<long> userMagicIds = null)
        {
            userMagicIds ??= new List<long>();
            List<MagicBookEntry> loadedEntries = await BuildEntriesAsync(userMagicIds);

            RunOnUnityThread(() =>
            {
                entries.Clear();
                entries.AddRange(loadedEntries);
                RenderCurrentView();
            });
        }
        
        private void OnClickMagicButton(CombinedMagicData data)
        {
            ShowMagic(data);
            MagicSelected?.Invoke();
        }

        /// <summary>
        /// 오른쪽 카드에 마법을 채우고 그 칸에 금색 테두리를 켠다.
        /// 튜토리얼은 <see cref="MagicSelected"/> 를 사용자가 고른 신호로 읽으므로 여기서는 부르지 않는다.
        /// </summary>
        private void ShowMagic(CombinedMagicData data)
        {
            selectedMagicId = data.id;
            magicInfo.Init(data);
            selectedMagicView?.Show(data);
            RefreshSelectedRing();
        }

        private void RefreshSelectedRing()
        {
            foreach (MagicButton magicButton in magicButtons)
            {
                magicButton.SetSelected(selectedMagicId.HasValue &&
                                        magicButton.Data != null &&
                                        magicButton.Data.id == selectedMagicId.Value);
            }
        }

        private async Task<List<MagicBookEntry>> BuildEntriesAsync(List<long> userMagicIds)
        {
            var loadedEntries = new List<MagicBookEntry>();
            foreach (CombinedMagicData data in LocalCombinedMagicData.GetEffectiveDataList())
            {
                string localizedName = await LocaleUtils.GetStringAsync("Magic", data.localizationKey);
                if (string.IsNullOrWhiteSpace(localizedName) || localizedName == data.localizationKey)
                {
                    localizedName = data.serverName;
                }

                loadedEntries.Add(new MagicBookEntry(
                    data,
                    userMagicIds.Contains(data.id),
                    localizedName));
            }

            return loadedEntries;
        }

        private void RenderCurrentView()
        {
            ClearMagicInfo();

            MagicBookEntry firstOwned = null;
            foreach (MagicBookEntry entry in GetVisibleEntries())
            {
                CreateMagicInfo(entry.Data, entry.IsOwned);
                if (firstOwned == null && entry.IsOwned)
                {
                    firstOwned = entry;
                }
            }

            // 카드가 빈 채로 열리지 않도록 처음에는 보이는 첫 보유 마법을 고른다.
            if (!selectedMagicId.HasValue && firstOwned != null)
            {
                ShowMagic(firstOwned.Data);
                return;
            }

            RefreshSelectedRing();
        }

        private IEnumerable<MagicBookEntry> GetVisibleEntries()
        {
            IEnumerable<MagicBookEntry> visibleEntries = entries.Where(PassesFilters);

            return sortMode switch
            {
                MagicBookSortMode.Attribute => visibleEntries
                    .OrderBy(GetPrimaryAttributeSortValue)
                    .ThenBy(entry => entry.LocalizedName, StringComparer.Create(CultureInfo.CurrentCulture, true)),
                MagicBookSortMode.ManaCost => visibleEntries
                    .OrderBy(entry => entry.ManaCost)
                    .ThenBy(entry => entry.LocalizedName, StringComparer.Create(CultureInfo.CurrentCulture, true)),
                _ => visibleEntries
                    .OrderBy(entry => entry.LocalizedName, StringComparer.Create(CultureInfo.CurrentCulture, true)),
            };
        }

        private bool PassesFilters(MagicBookEntry entry)
        {
            bool passesAttribute = !selectedAttribute.HasValue ||
                   (entry.Data.elements != null && entry.Data.elements.Contains(selectedAttribute.Value));
            bool passesCastKind = !selectedCastKind.HasValue || entry.Data.castKind == selectedCastKind.Value;
            return passesAttribute && passesCastKind && ManaBands.Contains(selectedManaBand, entry.ManaCost);
        }

        /// <summary>
        /// 원소가 여럿인 마법은 <see cref="ElementType"/> 선언 순서가 가장 앞인 원소로 묶는다.
        /// 원소가 하나도 없는 마법은 맨 뒤로 보낸다.
        /// </summary>
        private static int GetPrimaryAttributeSortValue(MagicBookEntry entry)
        {
            int lowest = int.MaxValue;
            foreach (ElementType element in entry.Data.elements ?? EmptyElements)
            {
                lowest = Math.Min(lowest, (int)element);
            }

            return lowest;
        }

        private void ClearMagicInfo()
        {
            magicButtons.Clear();
            foreach (Transform child in magicInfoParent)
            {
                Destroy(child.gameObject);
            }
        }

        private void RunOnUnityThread(Action action)
        {
            if (unityContext == null || System.Threading.SynchronizationContext.Current == unityContext)
            {
                action();
                return;
            }

            unityContext.Post(_ => action(), null);
        }
        
        private void CreateMagicInfo(CombinedMagicData data, bool active = true)
        {
            var magicInfoObj = Instantiate(magicInfoPrefab, magicInfoParent);
            
            var magicButton = magicInfoObj.GetComponent<MagicButton>();
            magicButton.Init(data);
            magicButton.SetActive(active);
            magicButtons.Add(magicButton);
            
            if (active)
            {
                magicButton.OnClick += OnClickMagicButton;
            }
        }

        private sealed class MagicBookEntry
        {
            public MagicBookEntry(
                CombinedMagicData data,
                bool isOwned,
                string localizedName)
            {
                Data = data;
                IsOwned = isOwned;
                LocalizedName = localizedName ?? string.Empty;
            }

            public CombinedMagicData Data { get; }
            public bool IsOwned { get; }
            public string LocalizedName { get; }

            public int ManaCost => CardManaCost.Of(Data);
        }
    }
}
