namespace GameScene.Dto
{
    /// <summary>
    /// Remembers the highest PVE script event <c>seq</c> this client has shown, so a replayed event
    /// (after a <c>pveSync</c> request or a reconnect) is not shown twice. Pure C# with no Unity
    /// dependency so the EditMode tests can cover it.
    /// </summary>
    public class PveSyncState
    {
        /// <summary>Highest accepted <c>seq</c>; 0 when no numbered event has been shown yet.</summary>
        public int LastEventSeq { get; private set; }

        /// <summary>
        /// True when the event should be shown, and records its <c>seq</c>. An event with <c>seq</c> 0
        /// or missing (an older server) is always shown and never moves <see cref="LastEventSeq"/>.
        /// </summary>
        public bool ShouldShow(int seq)
        {
            if (seq <= 0)
            {
                return true;
            }

            if (seq <= LastEventSeq)
            {
                return false;
            }

            LastEventSeq = seq;
            return true;
        }

        /// <summary>Call when a new match starts so its events are not compared with the last match.</summary>
        public void Reset()
        {
            LastEventSeq = 0;
        }
    }
}
