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

        private void Start()
        {
            playerName.text = SceneContext.MatchInfo.FindUserByMaster(servedObject.GetMaster())?.name ?? "";
        }
    }
}
