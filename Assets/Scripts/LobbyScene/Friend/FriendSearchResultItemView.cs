using System;
using TMPro;
using UnityEngine;

namespace LobbyScene
{
    /// <summary>
    /// One row of the search tab (<c>Assets/Prefabs/UI/Lobby/Friend/FriendSearchResultRow.prefab</c>).
    /// Shows a badge when the user is already a friend or has a pending request, and the add button otherwise.
    /// </summary>
    public class FriendSearchResultItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject alreadyFriendBadge;
        [SerializeField] private GameObject pendingBadge;
        [SerializeField] private UnityEngine.UI.Button addFriendButton;

        private FriendSearchResult data;
        private Action<FriendSearchResult> onSendRequestClicked;

        private void Awake()
        {
            if (addFriendButton != null)
            {
                addFriendButton.onClick.AddListener(() =>
                {
                    if (data != null) onSendRequestClicked?.Invoke(data);
                });
            }
        }

        public void Bind(FriendSearchResult result, Action<FriendSearchResult> onSendRequest)
        {
            data = result;
            onSendRequestClicked = onSendRequest;

            if (nameText != null)
            {
                nameText.text = $"{result.name}  ({result.email})";
            }

            if (alreadyFriendBadge != null) alreadyFriendBadge.SetActive(result.isFriend);
            if (pendingBadge != null) pendingBadge.SetActive(!result.isFriend && result.hasPendingRequest);
            if (addFriendButton != null) addFriendButton.gameObject.SetActive(!result.isFriend && !result.hasPendingRequest);
        }
    }
}
