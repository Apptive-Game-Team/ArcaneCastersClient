using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LobbyScene.SettingPage
{
    public class SettingPage : MonoBehaviour
    {
        [SerializeField] private GameObject closeButton;
        [SerializeField] private GameObject settingPage;
        [SerializeField] private GameObject guestRegisterPage;
        [SerializeField] private GameObject creditsPage;
        [SerializeField] private bool gameplayOnly;
        [SerializeField] private Transform settingsContent;
        [SerializeField] private GameObject soundSettings;
        [SerializeField] private GameObject coachSettings;

        public bool IsOpen => pageStack.Count > 0;
    
        private readonly Stack<GameObject> pageStack = new Stack<GameObject>();
    
        public void OpenPage(string pageName = "Setting")
        {
            if (gameplayOnly)
            {
                if (pageName != "Setting") return;
                // An allowlist keeps newly added account/navigation controls out of a live match.
                foreach (Transform row in settingsContent)
                    row.gameObject.SetActive(row.gameObject == soundSettings);
                coachSettings.SetActive(false);
            }
            if (pageStack.Count > 0 && pageStack.Peek() == settingPage && pageName == "Setting") return;
            switch (pageName)
            {
                case "Setting":
                    pageStack.Push(settingPage);
                    break;
                case "GuestRegister":
                    pageStack.Push(guestRegisterPage);
                    break;
                case "Credits":
                    pageStack.Push(creditsPage);
                    break;
                case "Profile":
                    SceneManager.LoadScene("ProfileScene");
                    return;
                default:
                    pageStack.Push(settingPage);
                    break;
            }
            pageStack.Peek().SetActive(true);
            closeButton.SetActive(true);
        }
    
        public void ClosePage()
        {
            if (pageStack.Count > 0)
            {
                pageStack.Pop().SetActive(false);
            }
            if (pageStack.Count == 0)
            {
                closeButton.SetActive(false);
            }
        }
    }
}
