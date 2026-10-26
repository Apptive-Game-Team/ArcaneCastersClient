using System;
using System.Collections;
using System.Collections.Generic;
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
        private readonly Dictionary<long, FriendInviteItem> sentInvites = new();
        private readonly Dictionary<long, string> sendMessages = new();
        private readonly HashSet<long> sending = new();
        private readonly HashSet<string> acknowledging = new();
        private bool snapshotInFlight;
        private int receivedVersion;
        private long ownerUserId = -1;
        public event Action InviteStateChanged;

        public string InviteMessage(long friendId) => sendMessages.TryGetValue(friendId, out var message)
            ? message : sentInvites.TryGetValue(friendId, out var invite) ? FriendInviteState.Message(invite) : null;

        public bool IsSending(long friendId) => sending.Contains(friendId);

        public bool CanCancelInvite(long friendId) => sentInvites.TryGetValue(friendId, out var invite) &&
            FriendInviteState.IsPending(invite) && FriendInviteState.RemainingSeconds(invite, DateTimeOffset.UtcNow) > 0;

        public void SendOrCancelInvite(FriendSummary friend)
        {
            if (SceneContext.User == null) return;
            EnsureAccount();
            if (apiClient == null || !sending.Add(friend.userId)) return;
            bool cancel = CanCancelInvite(friend.userId);
            sendMessages[friend.userId] = cancel ? "취소 중" : "전송 중";
            InviteStateChanged?.Invoke();
            long userId = SceneContext.UserID;
            Action<bool, FriendInviteItem> completed = (success, invite) =>
            {
                if (SceneContext.User == null || SceneContext.UserID != userId) return;
                sending.Remove(friend.userId);
                if (success && invite != null)
                {
                    sendMessages.Remove(friend.userId);
                    RememberSentInvite(invite);
                }
                else
                {
                    sendMessages[friend.userId] = cancel ? "취소 확인 실패" : "전송 확인 실패";
                }
                InviteStateChanged?.Invoke();
                RecoverSnapshot();
            };
            StartCoroutine(cancel ? apiClient.CancelInvite(sentInvites[friend.userId].inviteId, completed)
                : apiClient.InviteFriend(friend.userId, completed));
        }

        private void RememberSentInvite(FriendInviteItem invite)
        {
            if (invite == null || SceneContext.User == null || invite.inviterId != SceneContext.UserID) return;
            sentInvites.TryGetValue(invite.inviteeId, out var current);
            if (!FriendInviteState.IsNewer(current, invite)) return;
            sentInvites[invite.inviteeId] = invite;
            if (!sending.Contains(invite.inviteeId)) sendMessages.Remove(invite.inviteeId);
            InviteStateChanged?.Invoke();
        }

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
            StartCoroutine(PollInvites());
        }

        private IEnumerator PollInvites()
        {
            while (true)
            {
                RecoverSnapshot();
                yield return new WaitForSecondsRealtime(5f);
            }
        }

        private void EnsureAccount()
        {
            long userId = SceneContext.User != null ? SceneContext.UserID : -1;
            if (ownerUserId == userId) return;
            ownerUserId = userId;
            sentInvites.Clear();
            sendMessages.Clear();
            sending.Clear();
            acknowledging.Clear();
            receivedVersion++;
            if (inviteDialog != null) inviteDialog.Close();
            InviteStateChanged?.Invoke();
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
            receivedVersion++;
            RememberSentInvite(payload.invite);

            Debug.Log($"[FriendManager] Received SSE Event: {payload.type}");

            switch (payload.type)
            {
                case "FRIEND_INVITE_RECEIVED":
                    HandleInviteReceived(payload.invite);
                    break;

                case "FRIEND_MATCH_READY":
                case "FRIEND_INVITE_ACCEPTED":
                    HandleInviteAccepted(payload);
                    break;

                case "FRIEND_INVITE_CANCELED":
                case "FRIEND_INVITE_REJECTED":
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
            if (SceneContext.User == null || invite.inviteeId != SceneContext.UserID) return;
            if (FriendInviteState.RemainingSeconds(invite, DateTimeOffset.UtcNow) <= 0) return;
            if (inviteDialog.IsShowing)
            {
                if (inviteDialog.CurrentInviteId == invite.inviteId) AcknowledgeInvite(invite);
                return;
            }

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
            AcknowledgeInvite(invite);
        }

        private void AcknowledgeInvite(FriendInviteItem invite)
        {
            if (!string.IsNullOrEmpty(invite.deliveredAt) || !acknowledging.Add(invite.inviteId)) return;
            StartCoroutine(apiClient.AcknowledgeInvite(invite.inviteId, (success, updated) =>
            {
                acknowledging.Remove(invite.inviteId);
            }));
        }

        private void HandleInviteAccepted(FriendEventPayload payload)
        {
            MatchedInfoDto matchInfo = payload.ResolveMatchInfo();
            if (matchInfo == null)
            {
                Debug.LogWarning($"[FriendManager] {payload.type} 이벤트에 matchInfo가 없습니다.");
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
            EnsureAccount();
            if (apiClient == null || SceneContext.User == null || snapshotInFlight) return;
            StartCoroutine(RecoverInvites());
        }

        private IEnumerator RecoverInvites()
        {
            snapshotInFlight = true;
            long userId = SceneContext.UserID;
            int version = receivedVersion;

            // 1. 대기 중인 친선전 초대 복구
            yield return apiClient.GetPendingInvites((success, invites) =>
            {
                if (SceneContext.User == null || SceneContext.UserID != userId || version != receivedVersion) return;
                if (!success || invites == null) return;
                if (inviteDialog != null && inviteDialog.IsShowing &&
                    !invites.Exists(item => item.inviteId == inviteDialog.CurrentInviteId)) inviteDialog.Close();
                if (invites.Count == 0) return;

                // 유효한 첫 번째 대기 초대를 다이얼로그로 복구
                FriendInviteItem firstPending = invites[0];
                if (inviteDialog != null)
                {
                    Debug.Log($"[FriendManager] Recovered pending invite: {firstPending.inviteId} from {firstPending.inviterName}");
                    HandleInviteReceived(firstPending);
                }
            });

            yield return apiClient.GetSentInvites((success, invites) =>
            {
                if (SceneContext.User == null || SceneContext.UserID != userId || !success || invites == null) return;
                foreach (var invite in invites)
                {
                    bool wasPending = sentInvites.TryGetValue(invite.inviteeId, out var previous) &&
                        previous.inviteId == invite.inviteId && FriendInviteState.IsPending(previous);
                    RememberSentInvite(invite);
                    if (wasPending && invite.status == "ACCEPTED" && invite.matchInfo != null)
                        HandleInviteAccepted(new FriendEventPayload { matchInfo = invite.matchInfo });
                }
            });
            snapshotInFlight = false;

            // 초대 만료 시각도 서버 응답 여부와 별개로 화면에 반영한다.
            InviteStateChanged?.Invoke();
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
