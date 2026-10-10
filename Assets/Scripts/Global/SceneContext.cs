using Data;
using Data.BattleThemes;
using GameScene.Dto;
using UnityEngine;

namespace Global
{
    public class SceneContext : SingletonObject<SceneContext>
    {
        static SceneContext()
        {
            Server.JwtTokenProvider = () => JwtToken;
        }

        public static string JwtToken
        {
            get; set;
        }
    
        public static long UserID => _user.id;

        private static User _user;

        public static User User
        {
            get => _user;
            set
            {
                // null clears the cached user, e.g. when the matching server changes.
                WDebug.Log(value == null
                    ? "Clearing User"
                    : "Setting User: " + value.name + ", ID: " + value.id);
                _user = value;
            }
        }

        public static string Me
        {
            get
            {
                // Debug-panel default. A missing user or match should fall back to
                // "None" instead of throwing, same as "not one of the two players".
                if (User == null || MatchInfo == null) return "None";
                if (UserID == MatchInfo.leftUser.id)
                    return "LeftPlayer";
                else if (UserID == MatchInfo.rightUser.id)
                    return "RightPlayer";
                return "None";
            }
        }

        public static MatchedInfoDto MatchInfo
        {
            get; set;
        }

        public static ResultInfo MatchResult
        {
            get; set;
        }
    
        public static string SelectedDeck
        {
            get; set;
        }
        public static string OwnedCards
        {
            get; set;
        }

        /// <summary>
        /// Battle scene environment art for the PVE match about to start, read by
        /// `BattleThemeApplier` when GameScene loads. Set right before that load for an
        /// adventure match; null (the default, and the value on every non-adventure entry
        /// point) keeps GameScene's forest look.
        /// </summary>
        public static BattleThemeScriptableObject BattleTheme
        {
            get; set;
        }

        /// <summary>
        /// Map kind the game server picked for the match being entered, set by
        /// <see cref="PrepareMap"/> and left in place after `GameScene` loads so a later change
        /// can ask which map is on. <see cref="MapKind.Unspecified"/> when the server sent none.
        /// </summary>
        public static MapKind MapKind
        {
            get; private set;
        }

        /// <summary>
        /// Decides the match's map kind and battle theme from the server's <c>mapType</c>. Call it
        /// right before loading `GameScene` or `SpectatingScene`, after
        /// <see cref="ClearAdventureMatch"/> if that is called too.
        /// <para>
        /// A usable <paramref name="mapType"/> wins: <c>FOREST</c>, <c>FORTRESS</c>, <c>GATE</c> pick
        /// their theme asset, <c>GRASSLAND</c> and <c>RIVER</c> pick none (the scene default). An
        /// absent or unknown value keeps the old decision, <paramref name="fallbackTheme"/>, so an
        /// older server still works with this client.
        /// </para>
        /// </summary>
        public static void PrepareMap(string mapType, BattleThemeScriptableObject fallbackTheme)
        {
            MapKind kind = MapKinds.Parse(mapType);
            MapKind = kind;
            if (kind == MapKind.Unknown)
            {
                WDebug.LogWarning("Unknown mapType from server, keeping the old battle theme decision: " + mapType);
            }

            MapThemeChoice choice = MapKinds.ThemeFor(kind);
            BattleTheme = choice == MapThemeChoice.KeepFallback
                ? fallbackTheme
                : FindTheme(choice);
        }

        private static BattleThemeScriptableObject FindTheme(MapThemeChoice choice)
        {
            if (choice == MapThemeChoice.SceneDefault) return null;

            var catalog = Resources.Load<BattleThemeCatalogScriptableObject>(
                BattleThemeCatalogScriptableObject.ResourcePath);
            BattleThemeScriptableObject theme = catalog != null ? catalog.Find(choice) : null;
            if (theme == null)
            {
                WDebug.LogWarning("No battle theme asset for " + choice + ", using the scene default.");
            }

            return theme;
        }

        /// <summary>
        /// Adventure being played, carried the same way as <see cref="BattleTheme"/>:
        /// `Data.Adventures.CurrentAdventure` is destroyed when GameScene loads (it is
        /// bound to AdventureScene / AdventuresScene), so `AdventureMapController`
        /// stamps these fields right before starting the match and `ResultScene`
        /// reads them to route back to the right adventure and stage instead of the
        /// lobby. Null on every non-adventure match.
        /// </summary>
        public static long? AdventureId
        {
            get; set;
        }

        /// <summary>Scenario the player just started; used by the result screen's Retry button.</summary>
        public static long? AdventureScenarioId
        {
            get; set;
        }

        public static string AdventureName
        {
            get; set;
        }

        /// <summary>1-based stage index within <see cref="AdventureName"/>, for the result caption.</summary>
        public static int AdventureStageNumber
        {
            get; set;
        }

        /// <summary>1-based position of the scenario inside its stage, for the "1-2" caption.</summary>
        public static int AdventureScenarioNumber
        {
            get; set;
        }

        public static void ClearContext()
        {
            Data.Magic.MagicPreviewDataSource.Clear();
            JwtToken = null;
            _user = null;
            MatchInfo = null;
            MatchResult = null;
            SelectedDeck = null;
            OwnedCards = null;
            ClearAdventureMatch();
            GuestContext.ClearGuestInfo();
            Data.Quests.ClaimedQuestLedger.Session.Clear();
        }

        /// <summary>
        /// Forgets the adventure match: call it on every non-adventure entry into GameScene,
        /// or a PVP match after an adventure would still get the adventure's battle theme and
        /// its result screen would route back to the adventure map.
        /// </summary>
        public static void ClearAdventureMatch()
        {
            BattleTheme = null;
            MapKind = MapKind.Unspecified;
            AdventureId = null;
            AdventureScenarioId = null;
            AdventureName = null;
            AdventureStageNumber = 0;
            AdventureScenarioNumber = 0;
        }
    }
}
