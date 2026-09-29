using Global;
using Global.Button;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

namespace ResultScene
{
    /// <summary>
    /// The result screen's single continue button. For an adventure match it goes
    /// back to that adventure's map instead of the lobby, and its label swaps from
    /// "Go to Lobby" to "Back to Adventure" to match — see
    /// <see cref="SceneContext.AdventureId"/>, stamped by
    /// <see cref="Adventures.AdventureMapController"/> right before the match starts.
    /// </summary>
    public class GotoLobbyButton : ButtonBase
    {
        // LobbyUI table key. TableEntryReference converts implicitly from a key string.
        private static readonly TableEntryReference AdventureBackEntry = "AdventureBackButton";

        [SerializeField] private LocalizeStringEvent label;

        private void Start()
        {
            if (SceneContext.AdventureId.HasValue && label != null)
            {
                label.StringReference.TableEntryReference = AdventureBackEntry;
                label.RefreshString();
            }
        }

        protected override void OnClickButton()
        {
            if (SceneContext.AdventureId.HasValue)
            {
                SceneManager.LoadScene("AdventureScene");
                return;
            }

            SceneManager.LoadScene("LobbyScene");
        }
    }
}
