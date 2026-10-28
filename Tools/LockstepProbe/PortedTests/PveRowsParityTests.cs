using System;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class PveRowsParityTests {
        private const string Source = "game/service/pve/pve_scenario_rows.rs";
        private static PveInstallerRow Installer(string id) => new PveInstallerRow { InstallerId = id, PrefabType = "ZapMouse", Master = "RightPlayer", PositionX = 3, PositionZ = -4, MaxHp = 500 };
        private static PveActionRow Action(string type) => new PveActionRow { ActionType = type, InstallerId = "boss", PrefabType = "ZapMouse", Count = 2, IntervalSeconds = (Fix64)1.5m, PositionX = 1, PositionZ = 2 };
        private static void Error(Action action, string message) { try { action(); throw new Exception("Expected conversion error"); } catch (ArgumentException error) { Assert.Equal(message, error.Message); } }
        [PortedTest(Source)] public static void no_installer_row_means_no_scenario() { Assert.True(new PveScenarioRows().Build() == null); }
        [PortedTest(Source)] public static void survive_scenario_with_no_installers_loads() {
            var scenario = new PveScenarioRows { Rules = new PveRulesRow { WinCondition = "Survive", SurviveSeconds = 90 } }.Build(); Assert.Equal(0, scenario.Installers.Count); Assert.Equal(0, scenario.ObjectiveInstallerIds.Count); Assert.Equal(PveWinCondition.Survive, scenario.Rules.WinCondition);
        }
        [PortedTest(Source)] public static void scenario_with_no_installers_loads_when_an_objective_is_installed_later() {
            var rows = new PveScenarioRows(); rows.Objectives.Add("gate_a"); var scenario = rows.Build(); Assert.Equal(0, scenario.Installers.Count); Assert.Sequence(new[] { "gate_a" }, scenario.ObjectiveInstallerIds); Assert.Equal(PveWinCondition.DestroyObjectives, scenario.Rules.WinCondition);
        }
        [PortedTest(Source)] public static void destroy_objectives_scenario_with_no_installer_and_no_objective_still_fails_to_load() { Assert.True(new PveScenarioRows { Rules = new PveRulesRow { WinCondition = "DestroyObjectives" } }.Build() == null); }
        [PortedTest(Source)] public static void maps_play_bgm_rows_with_and_without_bgm_key() {
            var row = Action("PlayBgm"); row.BgmKey = "boss_battle"; var result = PveScenarioRows.ConvertAction(row); Assert.Equal(PveActionType.SetBgm, result.Type); Assert.Equal("boss_battle", result.BgmKey);
            result = PveScenarioRows.ConvertAction(Action("PlayBgm")); Assert.Equal(PveActionType.SetBgm, result.Type); Assert.True(result.BgmKey == null);
        }
        [PortedTest(Source)] public static void maps_a_pull_player_units_row_to_its_destination_installer() {
            var row = Action("PullPlayerUnits"); row.InstallerId = "corrupted"; var result = PveScenarioRows.ConvertAction(row); Assert.Equal(PveActionType.PullPlayerUnits, result.Type); Assert.Equal("corrupted", result.InstallerId);
        }
        [PortedTest(Source)] public static void rows_become_a_scenario_with_default_rules() {
            var rows = new PveScenarioRows(); rows.Objectives.Add("boss"); rows.Installers.Add(Installer("boss"));
            var e = new PveEventRow { EventId = "e1", TriggerType = "SecondsGte", TriggerValue = 5, SpeakerInstallerId = "boss", MessageKey = "k" }; e.Lines.Add("hi"); e.Actions.Add(Action("SpawnWave")); e.Actions.Add(Action("InstallObject")); var spawner = Action("SetSpawner"); spawner.PrefabType = null; e.Actions.Add(spawner); rows.Events.Add(e);
            rows.Shields.Add(new PveShieldRow { InstallerId = "boss", SourceInstallerId = "pylon" }); var s = rows.Build();
            Assert.Equal(PveWinCondition.DestroyObjectives, s.Rules.WinCondition); Assert.True(s.Rules.SurviveSeconds == null); Assert.Sequence(new[] { "boss" }, s.ObjectiveInstallerIds);
            Assert.Equal(Master.RightPlayer, s.Installers[0].Master); Assert.Equal(new Vector3(3, 0, -4), s.Installers[0].Position); Assert.Equal<int?>(500, s.Installers[0].MaxHp);
            Assert.Equal(PveTriggerType.SecondsGte, s.Events[0].TriggerType); Assert.Equal(3, s.Events[0].Actions.Count); var a = s.Events[0].Actions[0]; Assert.Equal(PveActionType.SpawnWave, a.Type); Assert.Equal("ZapMouse", a.Prefab); Assert.Equal(2, a.Count); Assert.Equal(1, a.X); Assert.Equal(2, a.Z);
            Assert.Equal(PveActionType.SetSpawner, s.Events[0].Actions[2].Type); Assert.True(s.Events[0].Actions[2].Prefab == null); Assert.Equal<Fix64?>((Fix64)1.5m, s.Events[0].Actions[2].Interval); Assert.Equal(1, s.Shields.Count);
        }
        [PortedTest(Source)] public static void survive_rule_row_is_read() { var rows = new PveScenarioRows { Rules = new PveRulesRow { WinCondition = "Survive", SurviveSeconds = 90 } }; rows.Installers.Add(Installer("a")); var s = rows.Build(); Assert.Equal(PveWinCondition.Survive, s.Rules.WinCondition); Assert.Equal<int?>(90, s.Rules.SurviveSeconds); }
        [PortedTest(Source)] public static void unknown_names_fail_like_java_value_of() {
            var rows = new PveScenarioRows(); var bad = Installer("a"); bad.PrefabType = "Nope"; rows.Installers.Add(bad); Error(() => rows.Build(), "No enum constant PrefabType.Nope");
            rows.Installers[0] = Installer("a"); var e = new PveEventRow { EventId = "e", TriggerType = "SecondsGte" }; e.Actions.Add(Action("Explode")); rows.Events.Add(e); Error(() => rows.Build(), "Unknown action_type: Explode");
        }
        [PortedTest(Source)] public static void null_unboxed_column_fails() {
            var rows = new PveScenarioRows(); rows.Installers.Add(Installer("a")); var e = new PveEventRow { EventId = "e", TriggerType = "FrameNumGte" }; var action = Action("SpawnWave"); action.Count = null; e.Actions.Add(action); rows.Events.Add(e); Error(() => rows.Build(), "SpawnWave action is missing column count");
        }
    }
}
