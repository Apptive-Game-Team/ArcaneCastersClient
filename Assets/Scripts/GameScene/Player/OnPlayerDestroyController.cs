using GameScene.ServedObjectComponent;
using UnityEngine;

namespace GameScene.Player
{
    public class OnPlayerDestroyController : MonoBehaviour
    {
        [SerializeField] private ServedObject servedObject;
        [SerializeField] private PlayerFallDownController playerFallDownController;
        
        private void Awake()
        {
            if (servedObject == null) servedObject = GetComponent<ServedObject>();
            if (servedObject != null) servedObject.OnDestroyed += FallPlayer;
        }

        private void OnDestroy()
        {
            // 장면 종료는 전투 중 사망이 아니므로 새 tween을 만들지 않는다.
            if (servedObject != null) servedObject.OnDestroyed -= FallPlayer;
        }

        private void FallPlayer()
        {
            if (playerFallDownController != null) playerFallDownController.FallDown();
        }
    }
}
