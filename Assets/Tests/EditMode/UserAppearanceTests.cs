using Data;
using Global.Serialization;
using NUnit.Framework;

namespace WordOnline.Tests
{
    /// <summary>
    /// The lobby sends <c>appearance</c> on <c>/api/users/mine</c>; it must reach <see cref="User"/> so the lobby scene can show it.
    /// </summary>
    public class UserAppearanceTests
    {
        [Test]
        public void GameUserParsesAppearance()
        {
            GameUser gameUser = JsonCodec.Deserialize<GameUser>("{\"id\":7,\"selectedDeckId\":3,\"mmr\":10,\"appearance\":\"storm\"}");

            Assert.AreEqual("storm", gameUser.appearance);
        }

        [Test]
        public void GameUserWithoutAppearanceIsNull()
        {
            GameUser gameUser = JsonCodec.Deserialize<GameUser>("{\"id\":7,\"selectedDeckId\":3,\"mmr\":10}");

            Assert.IsNull(gameUser.appearance);
        }

        [Test]
        public void UserCopiesAppearanceFromGameUser()
        {
            var accountUser = new AccountUser { name = "n", username = "u", email = "e@x" };
            var gameUser = new GameUser { id = 7, selectedDeckId = 3, mmr = 10, appearance = "storm" };

            var user = new User(accountUser, gameUser);

            Assert.AreEqual("storm", user.appearance);
            Assert.AreEqual(7, user.id);
        }

        [Test]
        public void UserKeepsNullAppearanceForHumansWithoutOne()
        {
            var user = new User(new AccountUser { name = "n" }, new GameUser { id = 1 });

            Assert.IsNull(user.appearance);
        }
    }
}
