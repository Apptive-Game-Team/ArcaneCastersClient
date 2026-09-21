using System.Collections;
using Data.Profile;
using Global;
using LobbyScene;
using UnityEngine;
using UnityEngine.UI;

namespace ProfileScene
{
    public class ProfileSceneUIController : MonoBehaviour
    {
        [SerializeField] private ProfilePanel profilePanel;
        [SerializeField] private UserProfileApiClient apiClient;
        [SerializeField] private GameHistoryItemFactory gameHistoryItemFactory;
        [SerializeField] private ScrollRect gameHistoryScrollRect;
        [SerializeField] private int gameHistoryPageSize = 20;
        [SerializeField] private float loadMoreThreshold = 0.05f;

        private Coroutine gameHistoryRenderCoroutine;
        private int nextGameHistoryPage;
        private bool isLoadingGameHistory;
        private bool hasMoreGameHistory = true;

        private void Awake()
        {
            if (apiClient == null)
            {
                apiClient = GetComponent<UserProfileApiClient>();
            }

            if (gameHistoryItemFactory == null)
            {
                gameHistoryItemFactory = GetComponent<GameHistoryItemFactory>();
            }

            if (gameHistoryItemFactory == null)
            {
                gameHistoryItemFactory = gameObject.AddComponent<GameHistoryItemFactory>();
            }

            if (gameHistoryScrollRect == null)
            {
                gameHistoryScrollRect = GetComponentInChildren<ScrollRect>(true);
            }

            if (gameHistoryScrollRect != null)
            {
                gameHistoryItemFactory.SetItemRoot(gameHistoryScrollRect.content);
                gameHistoryScrollRect.onValueChanged.AddListener(OnGameHistoryScroll);
            }
        }

        private void OnDestroy()
        {
            if (gameHistoryScrollRect != null)
            {
                gameHistoryScrollRect.onValueChanged.RemoveListener(OnGameHistoryScroll);
            }
        }

        private IEnumerator Start()
        {
            if (SceneContext.User == null)
            {
                yield return UserInfoGetter.GetUserInfo();
            }

            Refresh();
        }

        public void Refresh()
        {
            SetLoading();

            if (apiClient == null)
            {
                return;
            }

            apiClient.GetOverview(RenderOverview);
            ResetGameHistory();
        }

        private void SetLoading()
        {
            if (SceneContext.User != null)
            {
                profilePanel.SetUser(SceneContext.User);
            }
        }

        private void RenderOverview(UserStatisticsOverviewDto overview)
        {
            if (overview == null)
            {
                profilePanel.SetOverviewUnavailable();
                return;
            }

            profilePanel.SetOverview(overview);
        }

        private void ResetGameHistory()
        {
            if (gameHistoryRenderCoroutine != null)
            {
                StopCoroutine(gameHistoryRenderCoroutine);
                gameHistoryRenderCoroutine = null;
            }

            nextGameHistoryPage = 0;
            isLoadingGameHistory = false;
            hasMoreGameHistory = true;

            if (gameHistoryScrollRect != null)
            {
                gameHistoryItemFactory.ClearItems();
            }

            LoadNextGameHistoryPage();
        }

        private void OnGameHistoryScroll(Vector2 normalizedPosition)
        {
            if (normalizedPosition.y <= loadMoreThreshold)
            {
                LoadNextGameHistoryPage();
            }
        }

        private void LoadNextGameHistoryPage()
        {
            if (apiClient == null || isLoadingGameHistory || !hasMoreGameHistory)
            {
                return;
            }

            gameHistoryRenderCoroutine = StartCoroutine(LoadGameHistoryPageCoroutine());
        }

        private IEnumerator LoadGameHistoryPageCoroutine()
        {
            isLoadingGameHistory = true;

            bool done = false;
            UserGameHistoryResponseDto response = null;
            apiClient.GetGameHistoryPage(nextGameHistoryPage, gameHistoryPageSize, page =>
            {
                response = page;
                done = true;
            });

            yield return new WaitUntil(() => done);

            UserGameHistoryDto[] games = response?.Items ?? new UserGameHistoryDto[0];
            if (nextGameHistoryPage == 0 && games.Length == 0)
            {
            }
            else
            {
                yield return gameHistoryItemFactory.CreateRange(games);
            }

            hasMoreGameHistory = gameHistoryScrollRect != null && HasMoreGameHistory(response);
            if (hasMoreGameHistory)
            {
                nextGameHistoryPage++;
            }

            isLoadingGameHistory = false;
            gameHistoryRenderCoroutine = null;
        }

        private static bool HasMoreGameHistory(UserGameHistoryResponseDto response)
        {
            return response != null && !response.IsLastPage;
        }
    }
}
