using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace Data.Adventures.Local
{
    public enum AdventureDialogueSpeakerSide
    {
        Left,
        Right,
    }

    [Serializable]
    public class AdventureStoryCharacterData
    {
        [SerializeField] private LocalizedString localizedName;

        public LocalizedString LocalizedName => localizedName;
    }

    [Serializable]
    public class AdventureStoryLineData
    {
        [SerializeField] private AdventureDialogueSpeakerSide speakerSide = AdventureDialogueSpeakerSide.Left;
        [SerializeField] private LocalizedString localizedDialogue;

        public AdventureDialogueSpeakerSide SpeakerSide => speakerSide;
        public LocalizedString LocalizedDialogue => localizedDialogue;
        public bool HasDialogue => localizedDialogue != null && !localizedDialogue.IsEmpty;
    }

    [Serializable]
    public class AdventureScenarioStoryData
    {
        [SerializeField] private long scenarioId;
        [SerializeField] private Sprite leftImage;
        [SerializeField] private Sprite rightImage;
        [SerializeField] private AdventureStoryCharacterData leftCharacter;
        [SerializeField] private AdventureStoryCharacterData rightCharacter;
        [SerializeField] private List<AdventureStoryLineData> lines = new List<AdventureStoryLineData>();

        public long ScenarioId => scenarioId;
        public Sprite LeftImage => leftImage;
        public Sprite RightImage => rightImage;
        public AdventureStoryCharacterData LeftCharacter => leftCharacter;
        public AdventureStoryCharacterData RightCharacter => rightCharacter;
        public List<AdventureStoryLineData> Lines => lines;

        public bool HasLines
        {
            get
            {
                if (lines == null)
                {
                    return false;
                }

                foreach (var line in lines)
                {
                    if (line != null && line.HasDialogue)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }

    [CreateAssetMenu(menuName = "Game/Adventure/Stage")]
    public class AdventureStageScriptableObject : ScriptableObject
    {
        [SerializeField] private long stageId;
        [SerializeField] private Sprite backgroundImage;
        [SerializeField] private GameObject stagePanelPrefab;
        [SerializeField] private LocalizedString stageName;
        [SerializeField] private List<AdventureScenarioStoryData> scenarioStories = new List<AdventureScenarioStoryData>();

        public long Id => stageId;
        public GameObject StagePanelPrefab => stagePanelPrefab;
        public Sprite BackgroundImage => backgroundImage;

        /// <summary>
        /// Display name for this stage, or null when no localized name has been
        /// assigned yet. The Adventure map screen leaves the name off rather than
        /// showing a raw or placeholder string when this is null.
        /// </summary>
        public string StageName => stageName != null && !stageName.IsEmpty ? stageName.GetLocalizedString() : null;

        public AdventureScenarioStoryData FindStoryByScenarioId(long scenarioId)
        {
            if (scenarioStories == null)
            {
                return null;
            }

            AdventureScenarioStoryData fallbackStory = null;
            foreach (var story in scenarioStories)
            {
                if (story == null || !story.HasLines)
                {
                    continue;
                }

                if (fallbackStory == null)
                {
                    fallbackStory = story;
                }

                if (story.ScenarioId == scenarioId)
                {
                    return story;
                }
            }

            return fallbackStory;
        }
    }
}
