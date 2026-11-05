namespace LobbyScene
{
    public class PracticeDifficultiesDto
    {
        public PracticeTierDto[] tiers;
        public string recommendedId;
    }

    public class PracticeTierDto
    {
        public string id;
        public int wins;
        public int losses;
        public int draws;
        public bool available;
    }
}
