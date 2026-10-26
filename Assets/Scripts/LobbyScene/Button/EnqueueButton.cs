using Global;

namespace LobbyScene.Button
{
    public class EnqueueButton : LobbyButtonBase
    {
        private void Start()
        {
            LobbySceneViewModel.Instance.CurrentState.OnStateChange += OnStatusChanged;
        }
    
        private void OnDestroy()
        {
            LobbySceneViewModel.Instance.CurrentState.OnStateChange -= OnStatusChanged;
        }
    
        protected override void OnClickButton()
        {
            switch (LobbySceneViewModel.Instance.CurrentState.Data)
            {
                case LobbySceneViewModel.LobbyState.Idle:
                    WDebug.Log("Enqueue button clicked: Enqueuing player.");
                    LobbySceneViewModel.Instance.Enqueue();
                    break;
                case LobbySceneViewModel.LobbyState.Matching:
                    LobbySceneViewModel.Instance.RemoveFromQueue();
                    break;
            }
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
