using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Data;

namespace LobbyScene
{
    public class FriendMatchInviteDialog : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button rejectButton;

        private FriendInviteItem currentInvite;
        private FriendApiClient apiClient;
        private Action<MatchedInfoDto> onMatchAccepted;
        private Coroutine timerCoroutine;

        public bool IsShowing => root != null && root.activeSelf;
        public string CurrentInviteId => currentInvite?.inviteId;

        private void Awake()
        {
            EnsureRoot();
            if (root != null)
            {
                root.SetActive(false);
            }

            if (acceptButton != null)
            {
                acceptButton.onClick.AddListener(OnAcceptClicked);
            }

            if (rejectButton != null)
            {
                rejectButton.onClick.AddListener(OnRejectClicked);
            }
        }

        private void OnDestroy()
        {
            StopTimer();
        }

        public void ShowInvite(FriendInviteItem invite, FriendApiClient client, Action<MatchedInfoDto> onAccepted)
        {
            EnsureRoot();
            StopTimer();

            currentInvite = invite;
            apiClient = client;
            onMatchAccepted = onAccepted;

            string inviter = string.IsNullOrEmpty(invite.inviterName) ? "친구" : invite.inviterName;
            if (messageText != null)
            {
                messageText.text = $"{inviter}님이 친선전 대전을 요청했습니다.";
            }

            if (root != null)
            {
                root.SetActive(true);
            }

            timerCoroutine = StartCoroutine(CountdownRoutine(30f));
        }

        public void DismissIfMatching(string inviteId)
        {
            if (currentInvite != null && currentInvite.inviteId == inviteId)
            {
                Close();
            }
        }

        public void Close()
        {
            StopTimer();
            currentInvite = null;
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void StopTimer()
        {
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }
        }

        private IEnumerator CountdownRoutine(float durationSeconds)
        {
            float remaining = durationSeconds;
            while (remaining > 0f)
            {
                if (timerText != null)
                {
                    timerText.text = $"{Mathf.CeilToInt(remaining)}초 남음";
                }
                yield return new WaitForSecondsRealtime(1f);
                remaining -= 1f;
            }

            if (timerText != null)
            {
                timerText.text = "초대 만료됨";
            }

            Close();
        }

        private void OnAcceptClicked()
        {
            if (currentInvite == null || apiClient == null)
            {
                Close();
                return;
            }

            string inviteId = currentInvite.inviteId;
            SetButtonsInteractable(false);

            StartCoroutine(apiClient.AcceptInvite(inviteId, (success, matchInfo) =>
            {
                SetButtonsInteractable(true);
                if (success && matchInfo != null)
                {
                    Action<MatchedInfoDto> callback = onMatchAccepted;
                    Close();
                    callback?.Invoke(matchInfo);
                }
                else
                {
                    Debug.LogWarning($"[FriendMatchInviteDialog] Failed to accept invite {inviteId}");
                    Close();
                }
            }));
        }

        private void OnRejectClicked()
        {
            if (currentInvite == null || apiClient == null)
            {
                Close();
                return;
            }

            string inviteId = currentInvite.inviteId;
            SetButtonsInteractable(false);

            StartCoroutine(apiClient.RejectInvite(inviteId, (success, _) =>
            {
                SetButtonsInteractable(true);
                Close();
            }));
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (acceptButton != null) acceptButton.interactable = interactable;
            if (rejectButton != null) rejectButton.interactable = interactable;
        }

        private void EnsureRoot()
        {
            if (root == null)
            {
                root = gameObject;
            }
        }
    }
}
