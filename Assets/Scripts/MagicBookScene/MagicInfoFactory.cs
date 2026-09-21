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

        // TODO(#578): 조합이 없어져 카드 수 정렬은 의미가 없다. 도감 화면 정리 때 마나 순으로 바꾼다.
        CardCount,
    }

    public class MagicInfoFactory : MonoBehaviour
    {
        [SerializeField] private Transform magicInfoParent;
        [SerializeField] private GameObject magicInfoPrefab;
        [SerializeField] private MagicInfo magicInfo;
        [SerializeField] private SelectedMagicView selectedMagicView;
        
        [SerializeField] private UserMagicApiClient userMagicApiClient;

        public event Action MagicSelected;

        private readonly List<MagicBookEntry> entries = new();
        private MagicBookSortMode sortMode = MagicBookSortMode.Name;
        private ElementType? selectedAttribute;
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
            magicInfo.Init(data);
            selectedMagicView?.Show(data);
            MagicSelected?.Invoke();
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

            foreach (MagicBookEntry entry in GetVisibleEntries())
            {
                CreateMagicInfo(entry.Data, entry.IsOwned);
            }
        }

        private IEnumerable<MagicBookEntry> GetVisibleEntries()
        {
            IEnumerable<MagicBookEntry> visibleEntries = entries.Where(PassesFilters);

            return sortMode switch
            {
                MagicBookSortMode.Attribute => visibleEntries
                    .OrderBy(GetPrimaryAttributeSortValue)
                    .ThenBy(entry => entry.LocalizedName, StringComparer.Create(CultureInfo.CurrentCulture, true)),
                MagicBookSortMode.CardCount => visibleEntries
                    .OrderBy(entry => entry.CardCount)
                    .ThenBy(entry => entry.LocalizedName, StringComparer.Create(CultureInfo.CurrentCulture, true)),
                _ => visibleEntries
                    .OrderBy(entry => entry.LocalizedName, StringComparer.Create(CultureInfo.CurrentCulture, true)),
            };
        }

        private bool PassesFilters(MagicBookEntry entry)
        {
            return !selectedAttribute.HasValue ||
                   (entry.Data.elements != null && entry.Data.elements.Contains(selectedAttribute.Value));
        }

        /// <summary>
        /// 원소가 여럿인 마법은 <see cref="ElementType"/> 선언 순서가 가장 앞인 원소로 묶는다.
        /// <c>magics.element</c> 는 목록에 없는 값일 수 있어 정렬 기준으로 쓰지 않는다.
        /// </summary>
        private static int GetPrimaryAttributeSortValue(MagicBookEntry entry)
        {
            List<ElementType> elements = entry.Data.elements;
            if (elements == null || elements.Count == 0)
            {
                return (int)entry.Data.element;
            }

            int lowest = int.MaxValue;
            foreach (ElementType element in elements)
            {
                lowest = Math.Min(lowest, (int)element);
            }

            return lowest;
        }

        private void ClearMagicInfo()
        {
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

            // TODO(#578): 카드 수 정렬을 마나 순으로 바꿀 때까지 마나 비용으로 대신 정렬한다.
            public int CardCount => Data.manaCost;
        }
    }
}
