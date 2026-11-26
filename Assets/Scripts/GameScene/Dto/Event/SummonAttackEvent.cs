namespace GameScene.Dto.Event
{
    [System.Serializable]
    public sealed class SummonAttackEvent : GameEvent
    {
        public int actorId;
        public int targetId;
    }
}
