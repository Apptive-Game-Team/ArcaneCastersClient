using System;
using UnityEngine;

namespace GameScene.Dto
{
    [Serializable]
    public class BotThoughtInfo : ServerMessage
    {
        public string botSide;
        public string ruleId;
        public string reason;
        // 현재 서버는 magic ID를, 이전 서버는 카드 이름을 보낸다. 둘 다 표시용으로 보존한다.
        public object[] cards;
        public Vector3 target;
    }
}
