using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace LobbyScene
{
    public class FriendItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text mmrText;
        [SerializeField] private Button inviteButton;
        [SerializeField] private Button deleteButton;

        private FriendSummary data;
        private Action<FriendSummary> onInviteClicked;
        private Action<FriendSummary> onDeleteClicked;

        private void Awake()
        {
            if (inviteButton != null)
            {
                inviteButton.onClick.AddListener(() =>
                {
                    if (data != null) onInviteClicked?.Invoke(data);
                });
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(() =>
                {
                    if (data != null) onDeleteClicked?.Invoke(data);
                });
            }
        }

        public void Bind(FriendSummary friend, Action<FriendSummary> onInvite, Action<FriendSummary> onDelete)
        {
            data = friend;
            onInviteClicked = onInvite;
            onDeleteClicked = onDelete;

            if (nameText != null)
            {
                nameText.text = friend.name ?? "알 수 없음";
            }

            if (statusText != null)
            {
                statusText.text = FormatStatus(friend.status);
            }

            if (mmrText != null)
            {
                mmrText.text = $"MMR: {friend.mmr}";
            }

            if (inviteButton != null)
            {
                // Can invite only when online/in lobby
                bool canInvite = friend.status != null &&
                                 friend.status.Equals("Online", StringComparison.OrdinalIgnoreCase);
                inviteButton.interactable = canInvite;
            }
        }

        private static string FormatStatus(string status)
        {
            if (string.IsNullOrEmpty(status)) return "오프라인";
            return status.ToUpperInvariant() switch
            {
                "ONLINE" => "온라인",
                "ONMATCHING" => "매칭 중",
                "ONPLAYING" => "게임 중",
                "OFFLINE" => "오프라인",
                _ => status
            };
        }
    }
}
