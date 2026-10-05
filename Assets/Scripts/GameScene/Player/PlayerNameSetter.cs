using GameScene.ServedObjectComponent;
using Global;
using TMPro;
using UnityEngine;

namespace GameScene.Player
{
    public class PlayerNameSetter : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI playerName;
        [SerializeField] private ServedObject servedObject;

        public void SuppressPreviewName()
        {
            enabled = false;
            if (playerName != null) playerName.gameObject.SetActive(false);
        }

        private void Start()
        {
            playerName.text = SceneContext.MatchInfo.FindUserByMaster(servedObject.GetMaster())?.name ?? "";
        }
    }
}
