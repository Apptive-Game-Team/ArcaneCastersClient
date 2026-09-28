using System;
using System.Collections.Generic;
using Data.Adventures.Dto;
using Data.Adventures.Local;
using Data.BattleThemes;
using UnityEngine;

namespace Data.Adventures.Domain
{
    public class Scenario
    {
        public long Id { get; }
        public State State { get; }
        public AdventureScenarioStoryData Story { get; }

        public Scenario(long id, State state)
        {
            Id = id;
            State = state;
            Story = null;
        }

        public Scenario(ScenarioDto scenarioDto, AdventureScenarioStoryData story)
        {
            Id = scenarioDto.id;
            State = (State)Enum.Parse(typeof(State), scenarioDto.state.ToUpper());
            Story = story;
        }
    }

    public class Stage
    {
        public long Id { get; }
        public State State { get; }
        public GameObject StagePanelPrefab { get; }
        public List<Scenario> Scenarios { get; }

        /// <summary>Display name from the local scenario data, or null when none is set yet.</summary>
        public string Name { get; }

        /// <summary>Chapter map artwork shared by every stage of the same adventure, or null.</summary>
        public Sprite BackgroundImage { get; }

        public Stage(long id, State state, List<Scenario> scenarios)
        {
            Id = id;
            State = state;
            scenarios.Sort((a, b) => a.Id.CompareTo(b.Id));
            Scenarios = scenarios;
            Name = null;
            BackgroundImage = null;
        }

        public Stage(StageDto stageDto, AdventureStageScriptableObject stageScriptableObject)
        {
            Id = stageDto.id;
            State = (State)Enum.Parse(typeof(State), stageDto.state.ToUpper());
            StagePanelPrefab = stageScriptableObject.StagePanelPrefab;
            Name = stageScriptableObject.StageName;
            BackgroundImage = stageScriptableObject.BackgroundImage;
            Scenarios = new List<Scenario>();
            foreach (var scenarioDto in stageDto.scenarios)
            {
                Scenarios.Add(new Scenario(
                    scenarioDto,
                    stageScriptableObject.FindStoryByScenarioId(scenarioDto.id)));
            }
            Scenarios.Sort((a, b) => a.Id.CompareTo(b.Id));
        }
    }

    public class Adventure
    {
        public long Id { get; }
        public State State { get; }
        public string Name { get; }
        public Sprite IconImage { get; }
        public List<Stage> Stages { get; }
        public BattleThemeScriptableObject BattleTheme { get; }

        public Adventure(long id, State state, string name, Sprite iconImage, List<Stage> stages,
            BattleThemeScriptableObject battleTheme = null)
        {
            Id = id;
            State = state;
            Name = name;
            IconImage = iconImage;
            Stages = stages;
            BattleTheme = battleTheme;
        }

        public Adventure(AdventureDto adventureDto, AdventureScriptableObject adventureScriptableObject)
        {
            Id = adventureDto.id;
            State = (State)Enum.Parse(typeof(State), adventureDto.state.ToUpper());
            Name = adventureScriptableObject.AdventureName;
            IconImage = adventureScriptableObject.IconImage;
            BattleTheme = adventureScriptableObject.BattleTheme;
            Stages = new List<Stage>();
            foreach (var stageDto in adventureDto.stages)
            {
                var stageScriptableObject = adventureScriptableObject.FindStageById(stageDto.id);
                Stages.Add(new Stage(stageDto, stageScriptableObject));
            }
        }
    }
}
