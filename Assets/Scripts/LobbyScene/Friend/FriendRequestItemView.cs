using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LobbyScene
{
    public class FriendRequestItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private UnityEngine.UI.Button acceptButton;
        [SerializeField] private UnityEngine.UI.Button rejectButton;
        [SerializeField] private UnityEngine.UI.Button cancelButton;

        private FriendRequestItem data;
        private Action<FriendRequestItem> onAcceptClicked;
        private Action<FriendRequestItem> onRejectClicked;
        private Action<FriendRequestItem> onCancelClicked;

        private void Awake()
        {
            if (acceptButton != null)
            {
                acceptButton.onClick.AddListener(() =>
                {
                    if (data != null) onAcceptClicked?.Invoke(data);
                });
            }

            if (rejectButton != null)
            {
                rejectButton.onClick.AddListener(() =>
                {
                    if (data != null) onRejectClicked?.Invoke(data);
                });
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(() =>
                {
                    if (data != null) onCancelClicked?.Invoke(data);
                });
            }
        }

        public void BindReceived(
            FriendRequestItem request,
            Action<FriendRequestItem> onAccept,
            Action<FriendRequestItem> onReject)
        {
            data = request;
            onAcceptClicked = onAccept;
            onRejectClicked = onReject;
            onCancelClicked = null;

            if (nameText != null)
            {
                nameText.text = request.senderName ?? "알 수 없음";
            }

            if (timeText != null)
            {
                timeText.text = FormatTime(request.createdAt);
            }

            if (acceptButton != null) acceptButton.gameObject.SetActive(true);
            if (rejectButton != null) rejectButton.gameObject.SetActive(true);
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
        }

        public void BindSent(
            FriendRequestItem request,
            Action<FriendRequestItem> onCancel)
        {
            data = request;
            onAcceptClicked = null;
            onRejectClicked = null;
            onCancelClicked = onCancel;

            if (nameText != null)
            {
                nameText.text = request.receiverName ?? "알 수 없음";
            }

            if (timeText != null)
            {
                timeText.text = FormatTime(request.createdAt);
            }

            if (acceptButton != null) acceptButton.gameObject.SetActive(false);
            if (rejectButton != null) rejectButton.gameObject.SetActive(false);
            if (cancelButton != null) cancelButton.gameObject.SetActive(true);
        }

        private static string FormatTime(string isoTime)
        {
            if (string.IsNullOrEmpty(isoTime)) return string.Empty;
            if (DateTime.TryParse(isoTime, out DateTime parsed))
            {
                return parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            }
            return isoTime;
        }
    }
}
