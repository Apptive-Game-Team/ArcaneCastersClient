using System;
using Data;
using Global;
using UnityEngine;

namespace LobbyScene
{
    public class FriendManager : MonoBehaviour
    {
        public static FriendManager Instance { get; private set; }

        [SerializeField] private FriendApiClient apiClient;
        [SerializeField] private FriendEventStream eventStream;
        [SerializeField] private FriendModalUIController friendModal;
        [SerializeField] private FriendMatchInviteDialog inviteDialog;

        public FriendApiClient ApiClient => apiClient;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (apiClient == null)
            {
                apiClient = gameObject.GetComponent<FriendApiClient>() ?? gameObject.AddComponent<FriendApiClient>();
            }

            if (eventStream == null)
            {
                eventStream = gameObject.GetComponent<FriendEventStream>() ?? gameObject.AddComponent<FriendEventStream>();
            }

            if (friendModal != null)
            {
                friendModal.Initialize(apiClient);
            }
        }

        private void Start()
        {
            ConnectEventStream();
        }

        private void OnDestroy()
        {
            isStopping = true;
            if (reconnectCoroutine != null)
            {
                StopCoroutine(reconnectCoroutine);
                reconnectCoroutine = null;
            }

            if (Instance == this)
            {
                Instance = null;
            }

            if (eventStream != null)
            {
                eventStream.EventReceived -= HandleFriendEvent;
                eventStream.Disconnected -= HandleStreamDisconnected;
            }
        }

        public void ConnectEventStream()
        {
            if (eventStream == null) return;

            eventStream.EventReceived -= HandleFriendEvent;
            eventStream.Disconnected -= HandleStreamDisconnected;
            eventStream.EventReceived += HandleFriendEvent;
            eventStream.Disconnected += HandleStreamDisconnected;

            eventStream.Connect(ServerList.MatchingServer.Api, SceneContext.JwtToken);
        }

        // FriendBootstrap adds this component at runtime, so the scene's modal cannot be a serialized
        // reference here; the bootstrap hands it over once it has found the LobbyScene instance.
        public void BindFriendModal(FriendModalUIController modal)
        {
            friendModal = modal;
            if (friendModal != null)
            {
                friendModal.Initialize(apiClient);
            }
        }

        // Same hand-over as the modal: the invite dialog is a LobbyScene prefab instance, and this
        // component only exists at runtime. Binding last matters because the pending invites are
        // fetched here: a RecoverSnapshot that ran earlier (application focus on scene start) found no
        // dialog and dropped the invite, so a player returning to the lobby never saw it.
        public void BindInviteDialog(FriendMatchInviteDialog dialog)
        {
            if (inviteDialog == dialog) return;

            inviteDialog = dialog;
            if (inviteDialog != null)
            {
                RecoverSnapshot();
            }
        }

        public void OpenFriendModal()
        {
            if (friendModal != null)
            {
                friendModal.Open();
            }
        }

        public void CloseFriendModal()
        {
            if (friendModal != null)
            {
                friendModal.Close();
            }
        }

        private void HandleFriendEvent(FriendEventPayload payload)
        {
            if (payload == null || string.IsNullOrEmpty(payload.type)) return;

            Debug.Log($"[FriendManager] Received SSE Event: {payload.type}");

            switch (payload.type)
            {
                case "FRIEND_INVITE_RECEIVED":
                    HandleInviteReceived(payload.invite);
                    break;

                case "FRIEND_INVITE_ACCEPTED":
                    HandleInviteAccepted(payload);
                    break;

                case "FRIEND_INVITE_CANCELED":
                case "FRIEND_INVITE_EXPIRED":
                    if (inviteDialog != null && payload.invite != null)
                    {
                        inviteDialog.DismissIfMatching(payload.invite.inviteId);
                    }
                    break;

                case "FRIEND_REQUEST_RECEIVED":
                case "FRIEND_REQUEST_ACCEPTED":
                case "FRIEND_REQUEST_REJECTED":
                case "FRIEND_REQUEST_CANCELED":
                case "FRIEND_REMOVED":
                    if (friendModal != null && friendModal.IsOpen)
                    {
                        friendModal.RefreshCurrentTab();
                    }
                    break;
            }
        }

        private void HandleInviteReceived(FriendInviteItem invite)
        {
            if (invite == null || inviteDialog == null) return;

            inviteDialog.ShowInvite(
                invite,
                apiClient,
                onAccepted: matchInfo =>
                {
                    Debug.Log($"[FriendManager] Friendly match accepted: {matchInfo.sessionId}");
                    if (LobbySceneViewModel.Instance != null)
                    {
                        LobbySceneViewModel.Instance.EnterFriendMatch(matchInfo);
                    }
                }
            );
        }

        private void HandleInviteAccepted(FriendEventPayload payload)
        {
            MatchedInfoDto matchInfo = payload.ResolveMatchInfo();
            if (matchInfo == null)
            {
                Debug.LogWarning("[FriendManager] FRIEND_INVITE_ACCEPTED event received but no matchInfo found.");
                return;
            }

            Debug.Log($"[FriendManager] Opponent accepted match invite! Starting session {matchInfo.sessionId}");
            if (inviteDialog != null)
            {
                inviteDialog.Close();
            }

            if (LobbySceneViewModel.Instance != null)
            {
                LobbySceneViewModel.Instance.EnterFriendMatch(matchInfo);
            }
        }

        private Coroutine reconnectCoroutine;
        private bool isStopping;

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                RecoverSnapshot();
            }
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (!isPaused)
            {
                RecoverSnapshot();
            }
        }

        public void RecoverSnapshot()
        {
            if (apiClient == null) return;

            // 1. 대기 중인 친선전 초대 복구
            StartCoroutine(apiClient.GetPendingInvites((success, invites) =>
            {
                if (!success || invites == null || invites.Count == 0) return;

                // 유효한 첫 번째 대기 초대를 다이얼로그로 복구
                FriendInviteItem firstPending = invites[0];
                if (inviteDialog != null && !inviteDialog.IsShowing)
                {
                    Debug.Log($"[FriendManager] Recovered pending invite: {firstPending.inviteId} from {firstPending.inviterName}");
                    HandleInviteReceived(firstPending);
                }
            }));

            // 2. 친구 모달이 열려 있는 상태라면 현재 탭 갱신
            if (friendModal != null && friendModal.IsOpen)
            {
                friendModal.RefreshCurrentTab();
            }
        }

        private void HandleStreamDisconnected()
        {
            Debug.Log("[FriendManager] FriendEventStream disconnected. Scheduling reconnect...");
            if (isStopping) return;

            if (reconnectCoroutine == null)
            {
                reconnectCoroutine = StartCoroutine(ReconnectRoutine());
            }
        }

        private System.Collections.IEnumerator ReconnectRoutine()
        {
            yield return new WaitForSecondsRealtime(3f);
            reconnectCoroutine = null;
            if (isStopping) yield break;

            Debug.Log("[FriendManager] Reconnecting FriendEventStream...");
            ConnectEventStream();
            RecoverSnapshot();
        }
    }
}
