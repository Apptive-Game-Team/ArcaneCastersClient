using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Data;
using Data.Deck;

namespace DeckScene
{
    public enum DeckOwnedCardSortMode
    {
        Name,
        Attribute,
        ManaCost,
    }

    public sealed class DeckOwnedCardEntry
    {
        public DeckOwnedCardEntry(
            CardDto card,
            string localizedName,
            IReadOnlyCollection<ElementType> elements,
            int manaCost)
        {
            Card = card;
            LocalizedName = localizedName ?? string.Empty;
            Elements = elements ?? Array.Empty<ElementType>();
            ManaCost = manaCost;
        }

        public CardDto Card { get; }
        public string LocalizedName { get; }
        public IReadOnlyCollection<ElementType> Elements { get; }
        public int ManaCost { get; }
    }

    public static class DeckOwnedCardQuery
    {
        public static IEnumerable<DeckOwnedCardEntry> Apply(
            IEnumerable<DeckOwnedCardEntry> entries,
            string searchText,
            DeckOwnedCardSortMode sortMode,
            ElementType? selectedAttribute)
        {
            IEnumerable<DeckOwnedCardEntry> visibleEntries = (entries ?? Array.Empty<DeckOwnedCardEntry>())
                .Where(entry => PassesSearch(entry, searchText))
                .Where(entry => PassesAttribute(entry, selectedAttribute));

            CompareInfo compareInfo = CultureInfo.CurrentCulture.CompareInfo;
            var nameComparer = Comparer<string>.Create((left, right) =>
                compareInfo.Compare(left, right, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace));

            return sortMode switch
            {
                DeckOwnedCardSortMode.Attribute => visibleEntries
                    .OrderBy(GetPrimaryAttributeSortValue)
                    .ThenBy(entry => entry.LocalizedName, nameComparer),
                DeckOwnedCardSortMode.ManaCost => visibleEntries
                    .OrderBy(entry => entry.ManaCost)
                    .ThenBy(entry => entry.LocalizedName, nameComparer),
                _ => visibleEntries.OrderBy(entry => entry.LocalizedName, nameComparer),
            };
        }

        private static bool PassesSearch(DeckOwnedCardEntry entry, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            string normalizedSearchText = searchText.Trim();
            return Contains(entry.LocalizedName, normalizedSearchText) ||
                   Contains(entry.Card?.name, normalizedSearchText);
        }

        private static bool PassesAttribute(DeckOwnedCardEntry entry, ElementType? selectedAttribute)
        {
            return !selectedAttribute.HasValue || entry.Elements.Contains(selectedAttribute.Value);
        }

        private static int GetPrimaryAttributeSortValue(DeckOwnedCardEntry entry)
        {
            return entry.Elements.Count == 0 ? int.MaxValue : entry.Elements.Min(element => (int)element);
        }

        private static bool Contains(string source, string value)
        {
            return !string.IsNullOrEmpty(source) &&
                   CultureInfo.CurrentCulture.CompareInfo.IndexOf(
                       source,
                       value,
                       CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
        }
    }
}
