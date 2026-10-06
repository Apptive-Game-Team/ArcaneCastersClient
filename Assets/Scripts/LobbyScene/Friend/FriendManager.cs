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

        private void HandleStreamDisconnected()
        {
            Debug.Log("[FriendManager] FriendEventStream disconnected.");
        }
    }
}
