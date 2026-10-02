using Data;
using Data.BattleThemes;
using GameScene.Dto;

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
            JwtToken = null;
            _user = null;
            MatchInfo = null;
            MatchResult = null;
            SelectedDeck = null;
            OwnedCards = null;
            ClearAdventureMatch();
            GuestContext.ClearGuestInfo();
        }

        /// <summary>
        /// Forgets the adventure match: call it on every non-adventure entry into GameScene,
        /// or a PVP match after an adventure would still get the adventure's battle theme and
        /// its result screen would route back to the adventure map.
        /// </summary>
        public static void ClearAdventureMatch()
        {
            BattleTheme = null;
            AdventureId = null;
            AdventureScenarioId = null;
            AdventureName = null;
            AdventureStageNumber = 0;
            AdventureScenarioNumber = 0;
        }
    }
}
