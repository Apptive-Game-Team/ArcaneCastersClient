using Global.Button;
using UnityEngine.SceneManagement;

namespace ResultScene
{
    public class GotoLobbyButton : ButtonBase
    {
        protected override void OnClickButton()
        {
            SceneManager.LoadScene("LobbyScene");
        }
    }
}
