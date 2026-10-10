using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LobbyScene
{
    /// <summary>
    /// Creates the friend manager on first lobby load, owns the F hotkey, and connects the friend
    /// modal that <c>LobbyScene</c> places as an inactive <c>FriendModal.prefab</c> instance.
    /// </summary>
    public class FriendBootstrap : MonoBehaviour
    {
        private static FriendBootstrap instance;
        private FriendModalUIController modal;

        public static void Attach(Transform pill)
        {
            if (pill == null) return;

            if (instance == null)
            {
                GameObject bootstrapGo = new GameObject("FriendBootstrap");
                bootstrapGo.transform.SetParent(pill.root, false);
                instance = bootstrapGo.AddComponent<FriendBootstrap>();
                instance.EnsureFriendManager();
            }

            instance.BindModal();
        }

        // Entry for the lobby menu's friend item (LobbyMenu.OpenFriends). The
        // bootstrap is created by LobbyUserNameUI.Awake before any click.
        public static void ToggleFriendModal()
        {
            if (instance == null)
            {
                Debug.LogWarning("[FriendBootstrap] Friend button pressed before FriendBootstrap.Attach ran.");
                return;
            }

            instance.ToggleModal();
        }

        private void EnsureFriendManager()
        {
            if (FriendManager.Instance == null)
            {
                gameObject.AddComponent<FriendApiClient>();
                gameObject.AddComponent<FriendEventStream>();
                gameObject.AddComponent<FriendManager>();
            }
        }

        // The modal instance is saved inactive, so it is found with includeInactive.
        private void BindModal()
        {
            if (modal == null)
            {
                modal = FindObjectOfType<FriendModalUIController>(true);
            }

            if (modal == null)
            {
                Debug.LogWarning("[FriendBootstrap] LobbyScene has no FriendModal instance.");
                return;
            }

            if (FriendManager.Instance != null)
            {
                FriendManager.Instance.BindFriendModal(modal);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F) && !IsTypingIntoField())
            {
                ToggleModal();
            }
        }

        public void ToggleModal()
        {
            if (modal != null && modal.IsOpen)
            {
                modal.Close();
                return;
            }

            OpenModal();
        }

        public void OpenModal()
        {
            if (modal == null)
            {
                BindModal();
            }

            if (modal != null)
            {
                modal.Open();
            }
        }

        private bool IsTypingIntoField()
        {
            if (modal != null && modal.IsTyping) return true;

            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == null) return false;

            InputField legacyField = selected.GetComponent<InputField>();
            if (legacyField != null && legacyField.isFocused) return true;

            TMPro.TMP_InputField tmpField = selected.GetComponent<TMPro.TMP_InputField>();
            return tmpField != null && tmpField.isFocused;
        }
    }
}
