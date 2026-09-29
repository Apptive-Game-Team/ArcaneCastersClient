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

        /// <summary>
        /// Stage progress recomputed from <see cref="Scenarios"/> instead of trusting
        /// <see cref="State"/>. The lobby's per-stage aggregate groups its SQL by
        /// scenario id, so the value it sends is really the *first* scenario's own
        /// state relabeled as the stage's — a stage with 4 scenarios reports FINISHED
        /// as soon as only the first one clears. Each `Scenario.State` is read from its
        /// own `user_scenarios` row directly and is not affected by that bug, so
        /// re-deriving the stage's status from the scenario list here is reliable:
        /// FINISHED only when every scenario is finished, ACTIVE when any scenario has
        /// been started, INACTIVE otherwise.
        /// </summary>
        public State EffectiveState
        {
            get
            {
                if (Scenarios.Count == 0)
                {
                    return State.INACTIVE;
                }

                bool allFinished = true;
                bool anyStarted = false;
                foreach (Scenario scenario in Scenarios)
                {
                    if (scenario.State != State.FINISHED)
                    {
                        allFinished = false;
                    }
                    if (scenario.State != State.INACTIVE)
                    {
                        anyStarted = true;
                    }
                }

                if (allFinished) return State.FINISHED;
                if (anyStarted) return State.ACTIVE;
                return State.INACTIVE;
            }
        }

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
