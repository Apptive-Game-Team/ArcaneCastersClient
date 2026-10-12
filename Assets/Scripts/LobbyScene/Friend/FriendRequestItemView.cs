using System;
using TMPro;
using UnityEngine;

namespace LobbyScene
{
    /// <summary>
    /// One row of the requests tab (<c>Assets/Prefabs/UI/Lobby/Friend/FriendRequestRow.prefab</c>).
    /// A received request shows accept and reject; a sent request shows cancel.
    /// </summary>
    public class FriendRequestItemView : MonoBehaviour
    {
        private static readonly Color InkColor = new Color32(0x1C, 0x1A, 0x2B, 0xFF);
        private static readonly Color SlateTextColor = new Color32(0x5B, 0x62, 0x75, 0xFF);

        [SerializeField] private TMP_Text nameText;
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

            SetName($"{request.senderName} 님의 친구 요청", InkColor);
            SetButtons(received: true);
        }

        public void BindSent(
            FriendRequestItem request,
            Action<FriendRequestItem> onCancel)
        {
            data = request;
            onAcceptClicked = null;
            onRejectClicked = null;
            onCancelClicked = onCancel;

            SetName($"{request.receiverName} 님에게 보낸 요청 (대기중)", SlateTextColor);
            SetButtons(received: false);
        }

        private void SetName(string text, Color color)
        {
            if (nameText == null) return;
            nameText.text = text;
            nameText.color = color;
        }

        private void SetButtons(bool received)
        {
            if (acceptButton != null) acceptButton.gameObject.SetActive(received);
            if (rejectButton != null) rejectButton.gameObject.SetActive(received);
            if (cancelButton != null) cancelButton.gameObject.SetActive(!received);
        }
    }
}
