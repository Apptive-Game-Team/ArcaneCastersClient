using Global;
using LobbyScene.SettingPage;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GameScene
{
    // Keep this component on the active canvas, not the initially hidden dropdown.
    [DefaultExecutionOrder(-100)]
    public class GameMenu : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private GameObject dropdown;
        [SerializeField] private SettingPage settings;
        [SerializeField] private ConfirmationDialogController confirmation;
        [SerializeField] private Button surrenderButton;
        private static GameMenu activeMenu;
        private bool captured;
        private bool pending;
        private float requestedAt;
        public static bool BlocksGameplayInput => activeMenu != null && activeMenu.IsOpen;
        private bool IsOpen => dropdown.activeSelf || settings.IsOpen || confirmation.IsOpen;

        private void OnEnable() { activeMenu = this; }

        private void Update()
        {
            if (StompConnector.Instance != null && StompConnector.Instance.MatchEnded)
            {
                CloseAll();
                return;
            }
            if (pending && Time.unscaledTime - requestedAt >= 8f)
            {
                pending = false;
                surrenderButton.interactable = true;
                ShowMessage("SurrenderRetry");
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (confirmation.IsOpen) confirmation.Cancel();
                else if (settings.IsOpen) settings.ClosePage();
                else Toggle();
            }
            UpdateCapture();
        }

        public void Toggle()
        {
            if (StompConnector.Instance != null && StompConnector.Instance.MatchEnded) return;
            dropdown.SetActive(!dropdown.activeSelf);
            UpdateCapture();
        }

        public void Close()
        {
            dropdown.SetActive(false);
            UpdateCapture();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.pointerCurrentRaycast.gameObject == dropdown) Close();
        }

        public void OpenSettings()
        {
            dropdown.SetActive(false);
            settings.OpenPage();
            UpdateCapture();
        }

        public void RequestSurrender()
        {
            if (pending) return;
            confirmation.Show(new LocalizedString("LobbyUI", "SurrenderConfirm"), ConfirmSurrender);
            UpdateCapture();
        }

        private void ConfirmSurrender()
        {
            if (pending) return;
            if (StompConnector.Instance == null || !StompConnector.Instance.TrySurrender())
            {
                ShowMessage("SurrenderRetry");
                return;
            }
            pending = true;
            requestedAt = Time.unscaledTime;
            surrenderButton.interactable = false;
            ShowMessage("SurrenderPending");
        }

        private static void ShowMessage(string key)
        {
            if (SystemMessageUI.Instance != null)
                SystemMessageUI.Instance.ShowMessage(new LocalizedString("LobbyUI", key));
        }

        private void UpdateCapture()
        {
            if (IsOpen == captured) return;
            captured = IsOpen;
            if (captured) PointerInputUtility.BeginPointerCapture();
            else PointerInputUtility.EndPointerCapture();
        }

        private void CloseAll()
        {
            dropdown.SetActive(false);
            while (settings.IsOpen) settings.ClosePage();
            if (confirmation.IsOpen) confirmation.Cancel();
            UpdateCapture();
        }

        private void OnDisable()
        {
            if (captured) PointerInputUtility.EndPointerCapture();
            captured = false;
            if (activeMenu == this) activeMenu = null;
        }
    }
}
