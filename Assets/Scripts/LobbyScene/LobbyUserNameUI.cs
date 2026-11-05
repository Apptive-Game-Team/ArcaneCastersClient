using Global;
using TMPro;
using UnityEngine;

namespace LobbyScene
{
    public class LobbyUserNameUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI userNameText;

        private void Awake()
        {
            // This component sits on the name text, whose parent is the UserNamePill.
            ProfileEntryButton.Attach(transform.parent);
            FriendBootstrap.Attach(transform.parent);
        }

        public void SetUserName(string userName)
        {
            WDebug.Log($"Name : {userName}");
            userNameText.SetText(userName);
            WDebug.Log("Set UserName");
        }
    }
}
