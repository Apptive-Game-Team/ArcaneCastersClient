using UnityEngine;

namespace LobbyScene
{
    // Sits on the scene-authored friend button. FriendBootstrap is created at
    // runtime by LobbyUserNameUI.Awake, so the scene cannot hold a persistent
    // onClick to it; the listener is added here instead.
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class FriendButton : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<UnityEngine.UI.Button>().onClick.AddListener(FriendBootstrap.ToggleFriendModal);
        }
    }
}
