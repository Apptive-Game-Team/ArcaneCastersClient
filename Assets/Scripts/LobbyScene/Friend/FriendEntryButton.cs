using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace LobbyScene
{
    [RequireComponent(typeof(Button))]
    public class FriendEntryButton : MonoBehaviour
    {
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
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
