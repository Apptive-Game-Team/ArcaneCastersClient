namespace GameScene.Dto.Event
{
    [System.Serializable]
    public class ShockEvent : GameEvent
    {
        public int actorId;
        public int targetId;
    }
}
