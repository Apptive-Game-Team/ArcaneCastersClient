using Data;
using DeckScene;
using Global;
using UnityEngine.SceneManagement;

namespace LobbyScene
{
    /// <summary>
    /// Applies a matching server change made from the admin dropdown.
    ///
    /// Every matching server owns its own user, deck and card data, so all of it is dropped and
    /// the lobby is rebuilt against the new server. The JWT survives the switch because it is
    /// issued by the account server, not by the matching server.
    /// </summary>
    public static class MatchingServerSwitcher
    {
        public static void SwitchTo(MatchingServerOption option)
        {
            MatchingServerCatalog.Select(option);
            ClearServerScopedData();
            SceneManager.LoadScene("LobbyScene");
        }

        private static void ClearServerScopedData()
        {
            Data.Magic.MagicPreviewDataSource.Clear();
            SceneContext.User = null;
            SceneContext.MatchInfo = null;
            SceneContext.SelectedDeck = null;
            SceneContext.OwnedCards = null;

            LobbyUIController.ClearCachedDecks();
            DeckManagementViewModel.ClearCachedUserData();
            DeckSceneContext.OwnedCards = null;
            DeckSceneContext.CurrentDeck = null;
        }
    }
}
