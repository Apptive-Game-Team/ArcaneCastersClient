using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace WordOnline.Tests {
    public class MagicOwnershipListTests {
        private static Type RuntimeType(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(name)).First(type => type != null);

        private static object EnumValue(string name, string value) => Enum.Parse(RuntimeType(name), value);

        private static object Card(long id, int count, bool unlocked = true) {
            object card = Activator.CreateInstance(RuntimeType("Data.Deck.CardDto"));
            card.GetType().GetField("id").SetValue(card, id);
            card.GetType().GetField("name").SetValue(card, "Magic" + id);
            card.GetType().GetField("count").SetValue(card, count);
            card.GetType().GetField("unlocked").SetValue(card, unlocked);
            return card;
        }

        private static object DeckEntry(object card, string name, int cost = 10) {
            return Activator.CreateInstance(RuntimeType("DeckScene.DeckOwnedCardEntry"),
                card, name, Array.CreateInstance(RuntimeType("Data.ElementType"), 0), cost,
                EnumValue("Data.Magic.MagicCastKind", "Shot"));
        }

        private static object[] QueryDeck(object[] values, string search = "", string sort = "Name") {
            Array entries = Array.CreateInstance(RuntimeType("DeckScene.DeckOwnedCardEntry"), values.Length);
            for (int i = 0; i < values.Length; i++) entries.SetValue(values[i], i);
            return ((IEnumerable)RuntimeType("DeckScene.DeckOwnedCardQuery").GetMethod("Apply")
                .Invoke(null, new object[] {
                    entries, search, EnumValue("DeckScene.DeckOwnedCardSortMode", sort), null, null, null
                })).Cast<object>().ToArray();
        }

        [Test]
        public void DeckHidesMissingAndZeroStockButRetainsOwnedLockedCards() {
            object owned = DeckEntry(Card(1, 3), "Owned");
            object lockedOwned = DeckEntry(Card(2, 1, false), "Locked");
            object[] entries = {
                null, DeckEntry(null, "Missing"), DeckEntry(Card(3, 0), "UnlockedWithoutStock"),
                DeckEntry(Card(4, -1), "InvalidStock"), owned, lockedOwned
            };
            CollectionAssert.AreEqual(new[] { lockedOwned, owned }, QueryDeck(entries));
            CollectionAssert.AreEqual(new[] { owned }, QueryDeck(entries, "owned"));
            Assert.IsEmpty(QueryDeck(new[] { DeckEntry(Card(3, 0), "Empty") }));
        }

        [Test]
        public void DeckUsesTotalStockAndPreservesSearchAndManaOrdering() {
            object expensive = DeckEntry(Card(1, 3), "Fire expensive", 70);
            object cheap = DeckEntry(Card(2, 1), "Fire cheap", 10);
            object other = DeckEntry(Card(3, 1), "Water", 5);
            CollectionAssert.AreEqual(new[] { cheap, expensive },
                QueryDeck(new[] { expensive, other, cheap }, "fire", "ManaCost"));
        }

        [TestCase("Name", new long[] { 3, 2, 1, 4 })]
        [TestCase("Attribute", new long[] { 3, 2, 1, 4 })]
        [TestCase("ManaCost", new long[] { 2, 3, 1, 4 })]
        public void MagicBookKeepsWholeCatalogAndSortsWithinOwnedGroups(string sort, long[] expected) {
            WithMagicBook(factory => {
                SetField(factory, "sortMode", EnumValue("MagicBookScene.MagicBookSortMode", sort));
                CollectionAssert.AreEqual(expected, VisibleIds(factory));
            });
        }

        [Test]
        public void MagicBookOwnershipGroupingPreservesAttributeCastKindAndManaFilters() {
            WithMagicBook(factory => {
                SetField(factory, "selectedAttribute", EnumValue("Data.ElementType", "Fire"));
                SetField(factory, "selectedCastKind", EnumValue("Data.Magic.MagicCastKind", "Shot"));
                SetField(factory, "selectedManaBand", EnumValue("Data.Magic.ManaBand", "Low"));
                CollectionAssert.AreEqual(new long[] { 3, 1 }, VisibleIds(factory));
            });
        }

        private static void WithMagicBook(Action<object> test) {
            var host = new GameObject("MagicOwnershipListTests");
            host.SetActive(false);
            try {
                object factory = host.AddComponent(RuntimeType("MagicBookScene.MagicInfoFactory"));
                var entries = (IList)GetField(factory, "entries");
                entries.Add(BookEntry(1, false, "A", "Fire", 5));
                entries.Add(BookEntry(2, true, "Z", "Water", 5));
                entries.Add(BookEntry(3, true, "Y", "Fire", 10));
                entries.Add(BookEntry(4, false, "B", "Water", 70));
                test(factory);
            } finally {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static object BookEntry(long id, bool owned, string name, string element, int cost) {
            Type magicType = RuntimeType("Data.Magic.CombinedMagicData");
            object magic = Activator.CreateInstance(magicType);
            magicType.GetField("id").SetValue(magic, id);
            magicType.GetField("manaCost").SetValue(magic, cost);
            magicType.GetField("castKind").SetValue(magic, EnumValue("Data.Magic.MagicCastKind", "Shot"));
            var elements = (IList)Activator.CreateInstance(magicType.GetField("elements").FieldType);
            elements.Add(EnumValue("Data.ElementType", element));
            magicType.GetField("elements").SetValue(magic, elements);
            Type entryType = RuntimeType("MagicBookScene.MagicInfoFactory")
                .GetNestedType("MagicBookEntry", BindingFlags.NonPublic);
            return Activator.CreateInstance(entryType, magic, owned, name);
        }

        private static long[] VisibleIds(object factory) {
            var entries = (IEnumerable)factory.GetType().GetMethod("GetVisibleEntries",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(factory, null);
            return entries.Cast<object>().Select(entry => {
                object data = entry.GetType().GetProperty("Data").GetValue(entry);
                return (long)data.GetType().GetField("id").GetValue(data);
            }).ToArray();
        }

        private static object GetField(object target, string name) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        private static void SetField(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
