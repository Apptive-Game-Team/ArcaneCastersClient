using System;

namespace TutorialScene
{
    [Serializable]
    public class TutorialStep
    {
        public string localizationKey;
        public string[] cardNames;
        public bool shouldClearCards;
        public TutorialWaitType waitType;

        /// <summary>waitType이 UsedMagic일 때 기다릴 마법의 serverName.</summary>
        public string magicName;
    }
}
