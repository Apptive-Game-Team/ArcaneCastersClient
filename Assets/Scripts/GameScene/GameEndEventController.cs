using Global;
using UnityEngine.SceneManagement;

namespace GameScene
{
    public class GameEndEventController : LocalSingletonObject<GameEndEventController>
    {
        public bool HasEnded { get; private set; }

        public void TriggerGameEnd()
        {
            HasEnded = true;
            if (SystemMessageUI.Instance != null)
                SystemMessageUI.Instance.ClearTransientMessage();
            Invoke(nameof(GameEnd), 2f);
        }
        
        private void GameEnd()
        {
            SceneManager.LoadScene("ResultScene");
        }
    }
}
