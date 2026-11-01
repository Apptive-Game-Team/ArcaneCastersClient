using System;
using System.Collections.Generic;
using GameScene.Dto.Event;

namespace GameScene.Dto
{
    [Serializable, UnityEngine.Scripting.Preserve]
    public sealed class TutorialStateDto
    {
        [UnityEngine.Scripting.Preserve] public TutorialStateDto() { }
        public string battleId;
        public long ownerId;
        public string faction;
        public int revision;
        public string stage;
        public string status;
        public int wave;
        public int mana;
        public long[] cards;
        public string recommended;
        public float targetX;
        public float targetZ;
        public int remainingSeconds;
        public TutorialFrameDto[] frames;
        public SnapshotObjectDto[] snapshot;
        public int lastFrame;
    }

    [Serializable, UnityEngine.Scripting.Preserve]
    public sealed class TutorialFrameDto
    {
        [UnityEngine.Scripting.Preserve] public TutorialFrameDto() { }
        public int frame;
        public ObjectsInfo objects;
        public List<GameEvent> events;
    }
}
