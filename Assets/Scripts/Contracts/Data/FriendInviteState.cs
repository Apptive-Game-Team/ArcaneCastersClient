using System;
using System.Globalization;
using LobbyScene;

namespace Data
{
    public static class FriendInviteState
    {
        public static double RemainingSeconds(FriendInviteItem invite, DateTimeOffset now)
        {
            return invite != null && DateTimeOffset.TryParse(invite.expiresAt, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var expiry) ? Math.Max(0, (expiry - now).TotalSeconds) : 0;
        }

        public static bool IsPending(FriendInviteItem invite) => invite != null && invite.status == "PENDING";

        public static bool IsNewer(FriendInviteItem current, FriendInviteItem incoming)
        {
            if (incoming == null) return false;
            if (current == null) return true;
            if (current.inviteId == incoming.inviteId) return incoming.revision > current.revision;
            return DateTimeOffset.TryParse(incoming.createdAt, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var incomingTime) &&
                DateTimeOffset.TryParse(current.createdAt, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var currentTime) && incomingTime > currentTime;
        }

        public static string Message(FriendInviteItem invite)
        {
            if (invite == null) return null;
            switch (invite.status)
            {
                case "PENDING":
                    if (RemainingSeconds(invite, DateTimeOffset.UtcNow) <= 0) return "초대 만료";
                    return string.IsNullOrEmpty(invite.deliveredAt) ? "전달 대기" : "전달 완료";
                case "ACCEPTED": return "대전 준비 중";
                case "REJECTED": return "초대 거절";
                case "CANCELED": return "초대 취소됨";
                case "EXPIRED": return "초대 만료";
                default: return "초대 상태 확인 중";
            }
        }
    }
}
