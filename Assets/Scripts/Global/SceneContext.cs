using Data;
using Data.BattleThemes;
using GameScene.Dto;

namespace Global
{
    public class SceneContext : SingletonObject<SceneContext>
    {
    
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

        public static void ClearContext()
        {
            JwtToken = null;
            _user = null;
            MatchInfo = null;
            MatchResult = null;
            SelectedDeck = null;
            OwnedCards = null;
            BattleTheme = null;
            GuestContext.ClearGuestInfo();
        }
    }
}
