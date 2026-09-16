namespace GameScene.Dto
{
    [System.Serializable]
    public class CardInfo
    {
        public long[] added; // 마법 id 목록. GameSceneUIController.AddCard 가 id 로 마법을 찾는다.
    }
}
