namespace Data
{
    [System.Serializable]
    public class User
    {
        public long id;
        public long selectedDeckId;
        public int mmr;
        public string name;
        public string email;
        /// <summary>Look key for a bot player body; null for human users. See <see cref="PlayerAppearanceResolver"/>.</summary>
        public string appearance;

        /// <summary>
        /// Json.NET needs a parameterless constructor. Declaring any constructor removes the implicit one,
        /// and the two below are ambiguous to the serializer.
        /// </summary>
        public User() { }

        public User(AccountUser accountUser, GameUser gameUser)
        {
            this.id = gameUser.id;
            this.selectedDeckId = gameUser.selectedDeckId;
            this.mmr = gameUser.mmr;
            this.name = accountUser?.DisplayName;
            this.email = accountUser?.email;
        }
        
        public User(long id, string name, string email, long selectedDeckId, int mmr = 0)
        {
            this.id = id;
            this.name = name;
            this.email = email;
            this.selectedDeckId = selectedDeckId;
            this.mmr = mmr;
        }
    }
}
