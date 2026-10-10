using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace LobbyScene
{
    /// <summary>
    /// The dropdown list under the lobby hamburger button. Sits on the full-screen, transparent
    /// <c>MenuDropdown</c> object in <c>LobbyScene</c>, whose Image catches every click outside the list
    /// so the lobby buttons beneath it are not pressed while the list is open.
    /// <para>
    /// The hamburger button's persistent onClick calls <see cref="Toggle"/>. The object starts inactive,
    /// so the call reaches this component while its GameObject is off; a UnityEvent invokes it anyway.
    /// </para>
    /// </summary>
    public class LobbyMenu : MonoBehaviour, IPointerClickHandler
    {
        private const string ChestSceneName = "ChestScene";
        private const string SettingPageName = "Setting";

        [SerializeField] private LobbyScene.SettingPage.SettingPage settingPage;

        public void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        // A click on the list panel's own background bubbles up to here too; only a click on the
        // full-screen catcher closes the list.
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.pointerCurrentRaycast.gameObject != gameObject)
            {
                return;
            }

            Close();
        }

        public void OpenProfile()
        {
            Close();
            SceneManager.LoadScene(ProfileEntryButton.ProfileSceneName);
        }

        public void OpenFriends()
        {
            Close();
            FriendBootstrap.ToggleFriendModal();
        }

        public void OpenAppearance()
        {
            Close();
            ProfileScene.ProfileSceneUIController.RequestAppearanceOnNextLoad();
            SceneManager.LoadScene(ProfileEntryButton.ProfileSceneName);
        }

        public void OpenChests()
        {
            Close();
            SceneManager.LoadScene(ChestSceneName);
        }

        public void OpenSettings()
        {
            Close();
            if (settingPage == null)
            {
                Debug.LogWarning("[LobbyMenu] SettingPage is not assigned.");
                return;
            }

            settingPage.OpenPage(SettingPageName);
        }
    }
}
