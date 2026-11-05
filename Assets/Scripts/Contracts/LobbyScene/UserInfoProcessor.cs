using Data;
using Global.Serialization;

namespace LobbyScene
{
    public static class UserInfoProcessor
    {
        public static bool TryProcessAccountResponse(bool isSuccess, string responseText, out AccountUser accountUser, out string errorMessage)
        {
            accountUser = null;

            if (!isSuccess)
            {
                errorMessage = "Account API request failed.";
                return false;
            }

            if (!JsonCodec.TryDeserialize(responseText, out accountUser, out string error))
            {
                errorMessage = $"GetUserInfo account parse error: {error} / {JsonCodec.Excerpt(responseText)}";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public static bool TryProcessGameResponse(bool isSuccess, string responseText, out GameUser gameUser, out string errorMessage)
        {
            gameUser = null;

            if (!isSuccess)
            {
                errorMessage = "Game API request failed.";
                return false;
            }

            if (!JsonCodec.TryDeserialize(responseText, out gameUser, out string error))
            {
                errorMessage = $"GetUserInfo game parse error: {error} / {JsonCodec.Excerpt(responseText)}";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public static User CreateUser(AccountUser accountUser, GameUser gameUser)
        {
            if (accountUser == null || gameUser == null)
            {
                return null;
            }

            return new User(accountUser, gameUser);
        }
    }
}
