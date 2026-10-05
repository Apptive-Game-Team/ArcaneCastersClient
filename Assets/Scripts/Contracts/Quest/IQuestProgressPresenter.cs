namespace Data.Quests
{
    /// <summary>
    /// Writes one quest's title and progress widget into a row. One implementation per
    /// <c>conditionType</c>; <see cref="QuestProgressPresenterRegistry"/> picks the first one whose
    /// <see cref="Handles"/> returns true and falls back to <see cref="GenericQuestProgressPresenter"/>.
    /// </summary>
    public interface IQuestProgressPresenter
    {
        bool Handles(string conditionType);

        void Render(QuestDto quest, IQuestRowView row, IQuestText text);
    }

    /// <summary>
    /// What a presenter may draw. The row prefab owns the visuals; a presenter only chooses the title,
    /// the label and which widget carries <c>current</c> of <c>total</c>.
    /// </summary>
    public interface IQuestRowView
    {
        void SetTitle(string title);

        void SetProgressLabel(string label);

        /// <summary>One continuous bar filled to <c>current / total</c>.</summary>
        void ShowBar(int current, int total);

        /// <summary>A bar cut into <c>total</c> equal segments, <c>current</c> of them filled.</summary>
        void ShowSegments(int current, int total);

        /// <summary>One dot per step, <c>current</c> of them filled.</summary>
        void ShowPips(int current, int total);
    }

    /// <summary>
    /// Text a presenter needs from outside the contracts assembly: localized strings, and adventure names,
    /// which live on <c>AdventureScriptableObject</c> assets.
    /// </summary>
    public interface IQuestText
    {
        /// <summary>
        /// The localized string for <paramref name="key"/> formatted with <paramref name="arguments"/>, or
        /// <paramref name="fallback"/> formatted the same way when the key has no entry.
        /// </summary>
        string Localize(string key, string fallback, params object[] arguments);

        /// <summary>The display name of an adventure, or null when the client has no data for that id.</summary>
        string FindAdventureName(long adventureId);
    }
}
