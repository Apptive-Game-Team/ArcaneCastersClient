using System.Collections.Generic;
using GameScene.Dto.Event;

namespace GameScene.Dto
{
    [System.Serializable]
    public class FrameInfoDto : ServerMessage
    {
        public int remainingTime;
        public bool practicePaused;

        public int updatedMana;
        public int leftPlayerHp;
        public int rightPlayerHp;
        public CardInfo cards;
        public ObjectsInfo objects;
        public List<GameEvent> events;
    }
}
