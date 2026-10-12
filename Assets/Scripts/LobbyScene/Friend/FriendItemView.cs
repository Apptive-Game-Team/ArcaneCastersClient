using System;
using TMPro;
using UnityEngine;

namespace LobbyScene
{
    /// <summary>
    /// One row of the friend list (<c>Assets/Prefabs/UI/Lobby/Friend/FriendRow.prefab</c>).
    /// The invite button shows only while the friend is online.
    /// </summary>
    public class FriendItemView : MonoBehaviour
    {
        private static readonly Color TealColor = new Color32(0x2F, 0xB8, 0xA8, 0xFF);
        private static readonly Color PrimaryOrange = new Color32(0xFF, 0x9A, 0x1F, 0xFF);
        private static readonly Color MutedTextColor = new Color32(0x8E, 0x95, 0xA5, 0xFF);

        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private UnityEngine.UI.Button inviteButton;
        [SerializeField] private UnityEngine.UI.Button deleteButton;

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

            bool isOnline = IsStatus(friend.status, "Online");
            bool isBusy = IsStatus(friend.status, "OnMatching") || IsStatus(friend.status, "OnPlaying");

            if (nameText != null)
            {
                nameText.text = $"{friend.name}  MMR {friend.mmr}";
            }

            if (statusText != null)
            {
                statusText.text = $"[{FormatStatus(friend.status)}]";
                statusText.color = isOnline ? TealColor : (isBusy ? PrimaryOrange : MutedTextColor);
            }

            if (inviteButton != null)
            {
                inviteButton.gameObject.SetActive(isOnline);
            }
        }

        private static bool IsStatus(string status, string expected)
        {
            return status != null && status.Equals(expected, StringComparison.OrdinalIgnoreCase);
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
