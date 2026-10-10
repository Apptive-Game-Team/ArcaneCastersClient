using System.Collections.Generic;
using GameScene.Dto.Event;
using GameScene.Dto.Projectile;

namespace GameScene.Dto
{
    [System.Serializable]
    public class SyncFrameInfo : ServerMessage
    {
        // 서버 tick rate. 옛 서버는 보내지 않아 0 으로 들어오며, 이때는 기본값 20 으로 본다.
        public int tickRate;
        public int remainingTime;

        public int updatedMana;
        public int leftPlayerHp;
        public int rightPlayerHp;
        public SnapshotDto snapshotResponseDto;
        public List<ProjectileDto> projectileDtos;
        public List<GameEvent> events;
    }
}
