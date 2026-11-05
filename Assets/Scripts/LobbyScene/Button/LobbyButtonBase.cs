using Global.Button;

namespace LobbyScene.Button
{
    public abstract class LobbyButtonBase : DisableableButtonBase
    {
        private void Start()
        {
            LobbySceneViewModel.Instance.CurrentState.OnStateChange += OnStatusChanged;
        }
        
        private void OnDestroy()
        {
            LobbySceneViewModel.Instance.CurrentState.OnStateChange -= OnStatusChanged;
        }
        
        private void OnStatusChanged(LobbySceneViewModel.LobbyState state)
        {
            switch (state)
            {
                case LobbySceneViewModel.LobbyState.Idle:
                    ResetButton();
                    break;
                case LobbySceneViewModel.LobbyState.Matching:
                case LobbySceneViewModel.LobbyState.PreparingPractice:
                    SetButton();
                    break;
            }
        }
    }
}