namespace GameScene.Dto
{
    /// <summary>
    /// Asks the game server to resend the PVE objective and the script events newer than
    /// <see cref="lastEventSeq"/>: <c>{"type":"pveSync","lastEventSeq":0}</c>. Sent to the same
    /// destination as the other inputs.
    /// </summary>
    [System.Serializable]
    public class PveSyncInput
    {
        public string type = "pveSync";
        public int lastEventSeq;

        public PveSyncInput(int lastEventSeq)
        {
            this.lastEventSeq = lastEventSeq;
        }
    }
}
