using System;

namespace GameScene.Dto
{
    [Serializable]
    public class SnapshotDto
    {
        public int frame;
        public SnapshotObjectDto[] objects;
        // 서버 SnapshotResponseDto.myCards 는 List<Long>, 즉 마법 id 목록이다.
        public long[] myCards;
    }
}