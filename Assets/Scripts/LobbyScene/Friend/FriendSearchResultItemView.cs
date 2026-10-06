using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace LobbyScene
{
    public class FriendSearchResultItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text emailText;
        [SerializeField] private TMP_Text statusBadgeText;
        [SerializeField] private Button addFriendButton;

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
                nameText.text = result.name ?? "알 수 없음";
            }

            if (emailText != null)
            {
                emailText.text = MaskEmail(result.email);
            }

            if (result.isFriend)
            {
                if (statusBadgeText != null)
                {
                    statusBadgeText.gameObject.SetActive(true);
                    statusBadgeText.text = "이미 친구입니다";
                }
                if (addFriendButton != null) addFriendButton.gameObject.SetActive(false);
            }
            else if (result.hasPendingRequest)
            {
                if (statusBadgeText != null)
                {
                    statusBadgeText.gameObject.SetActive(true);
                    statusBadgeText.text = "요청 대기 중";
                }
                if (addFriendButton != null) addFriendButton.gameObject.SetActive(false);
            }
            else
            {
                if (statusBadgeText != null) statusBadgeText.gameObject.SetActive(false);
                if (addFriendButton != null)
                {
                    addFriendButton.gameObject.SetActive(true);
                    addFriendButton.interactable = true;
                }
            }
        }

        public void SetRequestSent()
        {
            if (statusBadgeText != null)
            {
                statusBadgeText.gameObject.SetActive(true);
                statusBadgeText.text = "요청 전송됨";
            }
            if (addFriendButton != null) addFriendButton.gameObject.SetActive(false);
        }

        private static string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@")) return email;
            int atIndex = email.IndexOf('@');
            string local = email.Substring(0, atIndex);
            string domain = email.Substring(atIndex);
            if (local.Length <= 2) return $"{local}***{domain}";
            return $"{local.Substring(0, 2)}***{domain}";
        }
    }
}
