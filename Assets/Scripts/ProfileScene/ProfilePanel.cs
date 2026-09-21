using Data;
using Data.Profile;
using TMPro;
using UnityEngine;

namespace ProfileScene
{
    public class ProfilePanel : MonoBehaviour
    {
        // Shown instead of a stale or blank number when the overview request fails,
        // so the panel never presents old or default data as if it were current.
        private const string UnavailableValue = "-";

        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text emailText;
        [SerializeField] TMP_Text mmrText;
        [SerializeField] TMP_Text totalGamesText;
        [SerializeField] TMP_Text totalWinText;

        private void Render(UserProfileData profile)
        {
            if (profile == null)
            {
                return;
            }

            if (profile.user != null)
            {
                SetUser(profile.user);
            }

            UserStatisticsOverviewDto overview = profile.overview ?? new UserStatisticsOverviewDto();
            SetOverview(overview);
        }

        public void SetOverview(UserStatisticsOverviewDto overview)
        {
            totalGamesText.text = $"{overview.TotalGameNum}";
            totalWinText.text = $"{overview.TotalWinNum}";
        }

        public void SetOverviewUnavailable()
        {
            totalGamesText.text = UnavailableValue;
            totalWinText.text = UnavailableValue;
        }

        public void SetUser(User user)
        {
            nameText.text = user.name;
            emailText.text = user.email;
            mmrText.text = $"{user.mmr}";
        }
    }
}