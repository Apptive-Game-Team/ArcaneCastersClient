namespace GameScene.Dto
{
    /// <summary>
    /// The adventure match's win condition and its progress. The server sends it once at the start of a
    /// PVE match and again whenever <see cref="remainingSeconds"/> or <see cref="objectivesRemaining"/>
    /// changes; a PVP or tutorial match never sends it.
    /// </summary>
    [System.Serializable]
    public class PveObjectiveInfo : ServerMessage
    {
        public const string DestroyObjectives = "DestroyObjectives";
        public const string Survive = "Survive";

        // A string, not an enum: JsonCodec throws on an enum name it does not know (json-payloads.md).
        public string winCondition;
        public int surviveSeconds;
        public int remainingSeconds;
        public int objectivesTotal;
        public int objectivesRemaining;
    }
}
