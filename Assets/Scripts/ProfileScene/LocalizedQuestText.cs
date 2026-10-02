using System;
using System.Collections.Generic;
using Data.Adventures.Local;
using Data.Quests;
using UnityEngine.Localization.Tables;

namespace ProfileScene
{
    /// <summary>
    /// <see cref="IQuestText"/> over an already loaded <c>LobbyUI</c> string table, so the rows can be
    /// filled synchronously. A missing table or key, or a value whose placeholders do not match, falls
    /// back to the English text the presenter passed in.
    /// </summary>
    public sealed class LocalizedQuestText : IQuestText
    {
        private readonly StringTable table;
        private readonly IReadOnlyList<AdventureScriptableObject> adventures;

        public LocalizedQuestText(StringTable table, IReadOnlyList<AdventureScriptableObject> adventures)
        {
            this.table = table;
            this.adventures = adventures ?? Array.Empty<AdventureScriptableObject>();
        }

        public string Localize(string key, string fallback, params object[] arguments)
        {
            StringTableEntry entry = table != null && !string.IsNullOrEmpty(key) ? table.GetEntry(key) : null;
            if (entry != null)
            {
                try
                {
                    string localized = arguments == null || arguments.Length == 0
                        ? entry.GetLocalizedString()
                        : entry.GetLocalizedString(arguments);
                    if (!string.IsNullOrEmpty(localized))
                    {
                        return localized;
                    }
                }
                catch (FormatException)
                {
                    // A translated value with the wrong placeholders; the English fallback still reads.
                }
            }

            return QuestTextFormat.Format(fallback, arguments);
        }

        public string FindAdventureName(long adventureId)
        {
            foreach (AdventureScriptableObject adventure in adventures)
            {
                if (adventure != null && adventure.AdventureId == adventureId)
                {
                    return adventure.AdventureName;
                }
            }

            return null;
        }
    }
}
