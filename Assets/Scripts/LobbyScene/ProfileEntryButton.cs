using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LobbyScene
{
    /// <summary>
    /// Makes the top-left profile pill open <c>ProfileScene</c> when clicked. Called at runtime by
    /// <see cref="LobbyUserNameUI"/>, so the lobby scene needs no extra serialized reference.
    /// Uses a <see cref="UnityEngine.UI.Button"/> on the pill's own Image, which gives the color tint on
    /// hover and press; the click sound comes from <c>GlobalButtonSoundPlayer</c>.
    /// </summary>
    public static class ProfileEntryButton
    {
        public const string ProfileSceneName = "ProfileScene";

        public static void Attach(Transform pill)
        {
            if (pill == null)
            {
                return;
            }

            Image background = pill.GetComponent<Image>();
            if (background == null)
            {
                return;
            }

            // The pill image does not receive raycasts in the scene, and a Button needs a raycast target to get clicks.
            background.raycastTarget = true;

            UnityEngine.UI.Button button = pill.GetComponent<UnityEngine.UI.Button>();
            if (button == null)
            {
                button = pill.gameObject.AddComponent<UnityEngine.UI.Button>();
            }

            button.targetGraphic = background;
            button.onClick.AddListener(() => SceneManager.LoadScene(ProfileSceneName));
        }
    }
}
