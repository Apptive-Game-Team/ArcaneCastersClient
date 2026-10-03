using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Data.Quests
{
    /// <summary>
    /// Holds the presenters in the order they are asked. To support a new <c>conditionType</c>, write an
    /// <see cref="IQuestProgressPresenter"/> and add it to <see cref="CreateDefault"/>; until then the
    /// quest still shows through <see cref="GenericQuestProgressPresenter"/>.
    /// </summary>
    public sealed class QuestProgressPresenterRegistry
    {
        private readonly IReadOnlyList<IQuestProgressPresenter> presenters;
        private readonly IQuestProgressPresenter fallback;

        public QuestProgressPresenterRegistry(IReadOnlyList<IQuestProgressPresenter> presenters, IQuestProgressPresenter fallback)
        {
            this.presenters = presenters ?? Array.Empty<IQuestProgressPresenter>();
            this.fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        }

        public static QuestProgressPresenterRegistry CreateDefault()
        {
            return new QuestProgressPresenterRegistry(
                new IQuestProgressPresenter[]
                {
                    new StageClearQuestProgressPresenter(),
                    new TotalWinQuestProgressPresenter(),
                    new AdventureClearQuestProgressPresenter(),
                },
                new GenericQuestProgressPresenter());
        }

        public IQuestProgressPresenter Select(string conditionType)
        {
            foreach (IQuestProgressPresenter presenter in presenters)
            {
                if (presenter != null && presenter.Handles(conditionType))
                {
                    return presenter;
                }
            }

            return fallback;
        }

        public void Render(QuestDto quest, IQuestRowView row, IQuestText text)
        {
            if (quest == null || row == null)
            {
                return;
            }

            Select(quest.conditionType).Render(quest, row, text);
        }
    }

    /// <summary>Base for presenters bound to one exact <c>conditionType</c>.</summary>
    public abstract class ConditionTypeQuestProgressPresenter : IQuestProgressPresenter
    {
        protected abstract string ConditionType { get; }

        public bool Handles(string conditionType)
        {
            return string.Equals(conditionType, ConditionType, StringComparison.OrdinalIgnoreCase);
        }

        public abstract void Render(QuestDto quest, IQuestRowView row, IQuestText text);

        protected static string Localize(IQuestText text, string key, string fallback, params object[] arguments)
        {
            return QuestTextFormat.Localize(text, key, fallback, arguments);
        }

        protected static string ProgressLabel(QuestDto quest, IQuestText text)
        {
            return QuestTextFormat.ProgressLabel(quest, text);
        }
    }

    /// <summary>"Clear N stages": one segment per stage, so the count reads at a glance.</summary>
    public sealed class StageClearQuestProgressPresenter : ConditionTypeQuestProgressPresenter
    {
        /// <summary>Above this many segments each one is too thin to read, so a plain bar is drawn.</summary>
        public const int MaximumSegments = 10;

        protected override string ConditionType => QuestConditionTypes.StageClear;

        public override void Render(QuestDto quest, IQuestRowView row, IQuestText text)
        {
            row.SetTitle(Localize(text, QuestLocalizationKeys.StageClearTitle, "Clear {0} stages", quest.DisplayRequireValue));
            row.SetProgressLabel(ProgressLabel(quest, text));
            if (quest.DisplayRequireValue <= MaximumSegments)
            {
                row.ShowSegments(quest.DisplayProgress, quest.DisplayRequireValue);
            }
            else
            {
                row.ShowBar(quest.DisplayProgress, quest.DisplayRequireValue);
            }
        }
    }

    /// <summary>"Win N matches": a plain bar, since win targets are usually too large to segment.</summary>
    public sealed class TotalWinQuestProgressPresenter : ConditionTypeQuestProgressPresenter
    {
        protected override string ConditionType => QuestConditionTypes.TotalWin;

        public override void Render(QuestDto quest, IQuestRowView row, IQuestText text)
        {
            row.SetTitle(Localize(text, QuestLocalizationKeys.TotalWinTitle, "Win {0} matches", quest.DisplayRequireValue));
            row.SetProgressLabel(ProgressLabel(quest, text));
            row.ShowBar(quest.DisplayProgress, quest.DisplayRequireValue);
        }
    }

    /// <summary>
    /// "Clear &lt;adventure&gt;": one pip per stage of the adventure named by <c>conditionTargetId</c>.
    /// The name comes from the client's adventure data, falling back to "Adventure N".
    /// </summary>
    public sealed class AdventureClearQuestProgressPresenter : ConditionTypeQuestProgressPresenter
    {
        protected override string ConditionType => QuestConditionTypes.AdventureClear;

        public override void Render(QuestDto quest, IQuestRowView row, IQuestText text)
        {
            row.SetTitle(Localize(text, QuestLocalizationKeys.AdventureClearTitle, "Clear {0}", AdventureName(quest, text)));
            row.SetProgressLabel(Localize(
                text,
                QuestLocalizationKeys.AdventureStageFormat,
                "Stage {0}/{1}",
                quest.DisplayProgress,
                quest.DisplayRequireValue));
            row.ShowPips(quest.DisplayProgress, quest.DisplayRequireValue);
        }

        public static string AdventureName(QuestDto quest, IQuestText text)
        {
            if (quest.conditionTargetId.HasValue)
            {
                long adventureId = quest.conditionTargetId.Value;
                string name = text?.FindAdventureName(adventureId);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }

                return Localize(text, QuestLocalizationKeys.AdventureFallbackName, "Adventure {0}", adventureId);
            }

            return Localize(text, QuestLocalizationKeys.AdventureAnyName, "an adventure");
        }
    }

    /// <summary>
    /// Any <c>conditionType</c> no other presenter claims, including ones added on the server after this
    /// client shipped: the type itself as a title and a plain <c>progress/requireValue</c> bar.
    /// </summary>
    public sealed class GenericQuestProgressPresenter : IQuestProgressPresenter
    {
        public bool Handles(string conditionType)
        {
            return true;
        }

        public void Render(QuestDto quest, IQuestRowView row, IQuestText text)
        {
            row.SetTitle(QuestTextFormat.Humanize(quest.conditionType));
            row.SetProgressLabel(QuestTextFormat.ProgressLabel(quest, text));
            row.ShowBar(quest.DisplayProgress, quest.DisplayRequireValue);
        }
    }

    /// <summary>Keys in the <c>LobbyUI</c> string table.</summary>
    public static class QuestLocalizationKeys
    {
        public const string StageClearTitle = "QuestStageClearTitle";
        public const string TotalWinTitle = "QuestTotalWinTitle";
        public const string AdventureClearTitle = "QuestAdventureClearTitle";
        public const string AdventureFallbackName = "QuestAdventureFallbackName";
        public const string AdventureAnyName = "QuestAdventureAnyName";
        public const string AdventureStageFormat = "QuestAdventureStageFormat";
        public const string ProgressFormat = "QuestProgressFormat";
        public const string Completed = "QuestCompleted";
        public const string Empty = "QuestEmpty";
        public const string LoadFailed = "QuestLoadFailed";
    }

    public static class QuestTextFormat
    {
        public static string Localize(IQuestText text, string key, string fallback, params object[] arguments)
        {
            if (text != null)
            {
                return text.Localize(key, fallback, arguments);
            }

            return Format(fallback, arguments);
        }

        public static string ProgressLabel(QuestDto quest, IQuestText text)
        {
            return Localize(text, QuestLocalizationKeys.ProgressFormat, "{0}/{1}", quest.DisplayProgress, quest.DisplayRequireValue);
        }

        public static string Format(string format, params object[] arguments)
        {
            if (string.IsNullOrEmpty(format))
            {
                return string.Empty;
            }

            if (arguments == null || arguments.Length == 0)
            {
                return format;
            }

            try
            {
                return string.Format(CultureInfo.InvariantCulture, format, arguments);
            }
            catch (FormatException)
            {
                return format;
            }
        }

        /// <summary><c>WEEKLY_PVP_WIN</c> becomes <c>Weekly Pvp Win</c>; null or blank becomes <c>Quest</c>.</summary>
        public static string Humanize(string conditionType)
        {
            if (string.IsNullOrWhiteSpace(conditionType))
            {
                return "Quest";
            }

            var builder = new StringBuilder(conditionType.Length);
            bool startOfWord = true;
            foreach (char character in conditionType.Trim())
            {
                if (character == '_' || character == '-' || char.IsWhiteSpace(character))
                {
                    if (builder.Length > 0 && builder[builder.Length - 1] != ' ')
                    {
                        builder.Append(' ');
                    }

                    startOfWord = true;
                    continue;
                }

                builder.Append(startOfWord ? char.ToUpperInvariant(character) : char.ToLowerInvariant(character));
                startOfWord = false;
            }

            return builder.ToString().TrimEnd();
        }
    }
}
