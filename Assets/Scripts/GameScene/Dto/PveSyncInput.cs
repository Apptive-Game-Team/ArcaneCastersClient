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

        // Json.NET이 매개변수 생성자에 null을 Int32로 바인딩하지 않고 필드 기본값을 유지하게 한다.
        public PveSyncInput() { }

        public PveSyncInput(int lastEventSeq)
        {
            this.lastEventSeq = lastEventSeq;
        }
    }
}
