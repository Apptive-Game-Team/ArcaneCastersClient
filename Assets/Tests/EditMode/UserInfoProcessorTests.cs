using Data;
using LobbyScene;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class UserInfoProcessorTests
    {
        [Test]
        public void TryProcessAccountResponse_WhenSuccessAndValidJson_ReturnsTrueAndAccountUser()
        {
            string json = "{\"name\":\"John Doe\",\"username\":\"johndoe\",\"email\":\"john@example.com\"}";

            bool result = UserInfoProcessor.TryProcessAccountResponse(
                isSuccess: true,
                responseText: json,
                out AccountUser accountUser,
                out string errorMessage
            );

            Assert.IsTrue(result);
            Assert.IsNull(errorMessage);
            Assert.IsNotNull(accountUser);
            Assert.AreEqual("John Doe", accountUser.name);
            Assert.AreEqual("johndoe", accountUser.username);
            Assert.AreEqual("john@example.com", accountUser.email);
            Assert.AreEqual("johndoe", accountUser.DisplayName);
        }

        [Test]
        public void TryProcessAccountResponse_WhenHttpFailure_ReturnsFalseAndErrorMessage()
        {
            bool result = UserInfoProcessor.TryProcessAccountResponse(
                isSuccess: false,
                responseText: "401 Unauthorized",
                out AccountUser accountUser,
                out string errorMessage
            );

            Assert.IsFalse(result);
            Assert.IsNull(accountUser);
            Assert.IsNotNull(errorMessage);
            Assert.AreEqual("Account API request failed.", errorMessage);
        }

        [Test]
        public void TryProcessAccountResponse_WhenInvalidJson_ReturnsFalseAndParseError()
        {
            string invalidJson = "{ invalid_json: ";

            bool result = UserInfoProcessor.TryProcessAccountResponse(
                isSuccess: true,
                responseText: invalidJson,
                out AccountUser accountUser,
                out string errorMessage
            );

            Assert.IsFalse(result);
            Assert.IsNull(accountUser);
            Assert.IsNotNull(errorMessage);
            Assert.That(errorMessage, Does.StartWith("GetUserInfo account parse error:"));
        }

        [Test]
        public void TryProcessGameResponse_WhenSuccessAndValidJson_ReturnsTrueAndGameUser()
        {
            string json = "{\"id\":42,\"selectedDeckId\":101,\"mmr\":1500,\"appearance\":\"storm\"}";

            bool result = UserInfoProcessor.TryProcessGameResponse(
                isSuccess: true,
                responseText: json,
                out GameUser gameUser,
                out string errorMessage
            );

            Assert.IsTrue(result);
            Assert.IsNull(errorMessage);
            Assert.IsNotNull(gameUser);
            Assert.AreEqual(42, gameUser.id);
            Assert.AreEqual(101, gameUser.selectedDeckId);
            Assert.AreEqual(1500, gameUser.mmr);
            Assert.AreEqual("storm", gameUser.appearance);
        }

        [Test]
        public void TryProcessGameResponse_WhenHttpFailure_ReturnsFalseAndErrorMessage()
        {
            bool result = UserInfoProcessor.TryProcessGameResponse(
                isSuccess: false,
                responseText: "500 Internal Server Error",
                out GameUser gameUser,
                out string errorMessage
            );

            Assert.IsFalse(result);
            Assert.IsNull(gameUser);
            Assert.IsNotNull(errorMessage);
            Assert.AreEqual("Game API request failed.", errorMessage);
        }

        [Test]
        public void TryProcessGameResponse_WhenInvalidJson_ReturnsFalseAndParseError()
        {
            string invalidJson = "not a json";

            bool result = UserInfoProcessor.TryProcessGameResponse(
                isSuccess: true,
                responseText: invalidJson,
                out GameUser gameUser,
                out string errorMessage
            );

            Assert.IsFalse(result);
            Assert.IsNull(gameUser);
            Assert.IsNotNull(errorMessage);
            Assert.That(errorMessage, Does.StartWith("GetUserInfo game parse error:"));
        }

        [Test]
        public void CreateUser_WhenBothAccountAndGameUserValid_ReturnsUserInstanceWithPropertiesSet()
        {
            var accountUser = new AccountUser { name = "Jane", username = "janedoe", email = "jane@example.com" };
            var gameUser = new GameUser { id = 12, selectedDeckId = 5, mmr = 1200, appearance = "fire" };

            User user = UserInfoProcessor.CreateUser(accountUser, gameUser);

            Assert.IsNotNull(user);
            Assert.AreEqual(12, user.id);
            Assert.AreEqual("janedoe", user.name);
            Assert.AreEqual(1200, user.mmr);
            Assert.AreEqual(5, user.selectedDeckId);
            Assert.AreEqual("fire", user.appearance);
        }

        [Test]
        public void CreateUser_WhenUsernameIsMissing_DisplayNameFallsBackToName()
        {
            var accountUser = new AccountUser { name = "Fallback Name", username = null, email = "user@example.com" };
            var gameUser = new GameUser { id = 1, selectedDeckId = 2, mmr = 1000 };

            User user = UserInfoProcessor.CreateUser(accountUser, gameUser);

            Assert.IsNotNull(user);
            Assert.AreEqual("Fallback Name", user.name);
            Assert.IsNull(user.appearance);
        }

        [Test]
        public void CreateUser_WhenAccountUserNull_ReturnsNull()
        {
            var gameUser = new GameUser { id = 1 };

            User user = UserInfoProcessor.CreateUser(null, gameUser);

            Assert.IsNull(user);
        }

        [Test]
        public void CreateUser_WhenGameUserNull_ReturnsNull()
        {
            var accountUser = new AccountUser { name = "Test" };

            User user = UserInfoProcessor.CreateUser(accountUser, null);

            Assert.IsNull(user);
        }
    }
}
