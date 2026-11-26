using System;

namespace GameScene.Dto.Event
{
    [Serializable]
    public sealed class VineWitchCastEvent : GameEvent
    {
        internal const string CastType = "vineWitchCast";
        public const string FanType = "vineWitchFan";

        public int actorId;
        public int targetId;
    }
}
