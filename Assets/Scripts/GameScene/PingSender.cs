using System;
using Global;
using UnityEngine;
using Global.Serialization;

namespace GameScene
{
    public class PingSender : MonoBehaviour
    {
        private const float PING_INTERVAL = 3f;
        
        private string destination;
        private string pingMessage;
        private float _pingTimer;

        [Serializable]
        class PingDto
        {
            public string type = "ping";
        }
    
        private void Awake()
        {
            // lockstep 연결은 매 틱 입력과 확정 프레임으로 생존 여부를 검사한다.
            if (SceneContext.MatchInfo != null && SceneContext.MatchInfo.IsLockstep)
            {
                enabled = false;
                return;
            }
            pingMessage = JsonCodec.Serialize(new PingDto());
            destination = $"/app/game/input/{SceneContext.MatchInfo.sessionId}/{SceneContext.UserID}";
        }

        private void OnEnable()
        {
            _pingTimer = 0f;
        }
    
        private void OnDisable()
        {
            _pingTimer = 0f;
        }

        private void Update()
        {
            _pingTimer += Time.deltaTime;
            if (_pingTimer < PING_INTERVAL)
            {
                return;
            }

            _pingTimer -= PING_INTERVAL;
            StompConnector.Instance.SendMessageToServer(destination, pingMessage);
        }
    }
}
