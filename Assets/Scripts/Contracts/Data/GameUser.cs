namespace Data
{
    [System.Serializable]
    public class GameUser
    {
        public long id;
        public long selectedDeckId;
        public int mmr;
        /// <summary>Look key from <c>GET /api/users/mine</c>; null when the user has not chosen one. See <see cref="PlayerAppearanceResolver"/>.</summary>
        public string appearance;
    }
}
