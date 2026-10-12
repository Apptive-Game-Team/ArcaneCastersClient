using System;
using System.Collections.Generic;
using Data;

namespace LobbyScene
{
    [Serializable]
    public class FriendSummary
    {
        public long userId;
        public string name;
        public string email;
        public long mmr;
        public string status; // Online, OnMatching, OnPlaying, Offline
    }

    [Serializable]
    public class FriendRequestItem
    {
        public long id;
        public long senderId;
        public string senderName;
        public long receiverId;
        public string receiverName;
        public string status; // PENDING, ACCEPTED, REJECTED, CANCELED
        public string createdAt;
    }

    [Serializable]
    public class FriendInviteItem
    {
        public string inviteId;
        public long inviterId;
        public string inviterName;
        public long inviteeId;
        public string inviteeName;
        public string status; // PENDING, ACCEPTED, REJECTED, CANCELED, EXPIRED
        public string createdAt;
        public string expiresAt;
        public MatchedInfoDto matchInfo;
    }

    [Serializable]
    public class FriendSearchResult
    {
        public long userId;
        public string name;
        public string email;
        public bool isFriend;
        public bool hasPendingRequest;
    }

    [Serializable]
    public class SendFriendRequestPayload
    {
        public string targetQuery;

        public SendFriendRequestPayload() { }

        public SendFriendRequestPayload(string targetQuery)
        {
            this.targetQuery = targetQuery;
        }
    }

    [Serializable]
    public class FriendEventPayload
    {
        public string type;
        public FriendRequestItem request;
        public FriendRequestItem friendRequest;
        public FriendInviteItem invite;
        public FriendSummary friend;
        public long? unfriendMemberId;
        public MatchedInfoDto matchInfo;
        public long? targetUserId;
        public string message;

        public FriendRequestItem ResolveRequest() => request ?? friendRequest;
        public MatchedInfoDto ResolveMatchInfo() => matchInfo ?? invite?.matchInfo;
    }
}
