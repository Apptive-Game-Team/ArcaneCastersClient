using UnityEngine;
using UnityEngine.UI;

namespace LobbyScene
{
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class FriendEntryButton : MonoBehaviour
    {
        private UnityEngine.UI.Button button;

        private void Awake()
        {
            button = GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }
        }

        private void OnClick()
        {
            if (FriendManager.Instance != null)
            {
                FriendManager.Instance.OpenFriendModal();
            }
            else
            {
                Debug.LogWarning("[FriendEntryButton] FriendManager instance not found in scene.");
            }
        }
    }
}
