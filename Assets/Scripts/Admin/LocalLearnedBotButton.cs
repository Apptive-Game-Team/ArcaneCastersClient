using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Admin {
    public sealed class LocalLearnedBotButton : MonoBehaviour {
        private GameObject page;
        private void Awake() {
            GetComponentInChildren<TMP_Text>().text = "커스텀 봇 전투";
            GetComponent<Button>().onClick.AddListener(Open);
        }
        private void Open() {
            if (page != null) return;
            var prefab = Resources.Load<GameObject>("CustomBotBattlePage");
            if (prefab != null) page = Instantiate(prefab);
        }
    }
}
