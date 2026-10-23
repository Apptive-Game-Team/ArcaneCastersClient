using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Admin {
    public sealed class AdminSceneLink : MonoBehaviour {
        [SerializeField] private string sceneName;
        [SerializeField] private string label;

        private void Awake() {
            GetComponentInChildren<TMP_Text>().text = label;
            GetComponent<Button>().onClick.AddListener(Open);
        }

        private void Open() {
            SceneManager.LoadScene(sceneName);
        }
    }
}
