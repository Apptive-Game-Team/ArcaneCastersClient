using System;
using System.Threading;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class ContextParityTests {
        private const string Source = "game/service/game_context.rs";
        [PortedTest("game/domain/parameters.rs")] public static void loads_both_insert_shapes_of_the_java_fixture() {
            string sql = "INSERT INTO parameters(name)\nSELECT 'mass'\nWHERE NOT EXISTS (SELECT 1);\n"
                + "INSERT INTO parameter_values(game_object_id, parameter_id, value)\nSELECT go.id, p.id, 0.5\nFROM game_objects go\nJOIN parameters p ON p.name = 'radius'\nWHERE go.name IN ('ember_spirit', 'seed_spirit')\nAND NOT EXISTS (SELECT 1);\n"
                + "INSERT INTO parameter_values(game_object_id, parameter_id, value)\nSELECT go.id, p.id, -3\nFROM game_objects go\nJOIN parameters p ON p.name = 'push_force'\nWHERE go.name = 'wind_totem'\nAND NOT EXISTS (SELECT 1);\n";
            var source = new BattleParameters(); Assert.Equal(3, ParameterSqlFixture.Load(source, sql));
            Assert.Equal((Fix64)0.5m, source["seed_spirit.radius"]); Assert.Equal((Fix64)(-3), source["wind_totem.push_force"]);
            Assert.Equal(0, ParameterSqlFixture.Load(source, sql));
        }
        [PortedTest("tests/parameter_fixture.rs")] public static void loads_every_parameter_value_of_the_java_fixtures() {
            var source = new BattleParameters();
            foreach (string name in new[] { "component_parameters", "component_prefab_parameters" }) {
                string sql = ParameterSqlFixture.Read(name);
                Assert.True(ParameterSqlFixture.Load(source, sql) > 0);
                Assert.Equal(0, ParameterSqlFixture.Load(source, sql));
            }
            Assert.Equal((Fix64)3, source["chicken_commando.fall_gravity"]);
            Assert.Equal((Fix64)3, source["wind_totem.push_force"]);
            Assert.Equal((Fix64)0.5m, source["seed_spirit.radius"]);
            Assert.Equal((Fix64)4.5m, source["tide_call.speed"]);
            Assert.Equal((Fix64)1.15m, source["towerback.sub_attack_range"]);
        }
        [PortedTest("game/domain/parameters.rs")] public static void get_value_reads_keys_and_names_alike() {
            var context = new SimulationContext(); var parameters = (BattleParameters)context.GameParameters;
            parameters["game.duration"] = 180000;
            Assert.Equal((Fix64)180000, context.GameParameters["game.duration"]);
            Assert.Equal((Fix64)180000, parameters.GetValue("GAME", "duration"));
            Assert.Equal(180000L, parameters.GetValue("game", "duration").RawValue / (1L << 32));
            Assert.Equal((Fix64)3, parameters.GetValueOrDefault("game", "missing", 3));
            Assert.True(!parameters.TryGetValue("game.missing", out _));
            try { parameters.GetValue("game", "missing"); throw new Exception("Missing parameter accepted"); }
            catch (System.Collections.Generic.KeyNotFoundException error) { Assert.Equal("Parameter not found: game, missing", error.Message); }
            Assert.True(!parameters.ContainsKey("game.Duration"));
        }
        [PortedTest("game/domain/parameters.rs")] public static void get_value_panics_on_a_missing_row() {
            try { var value = new SimulationContext().GameParameters["fire_slime.hp"]; throw new Exception("Missing parameter accepted"); }
            catch (System.Collections.Generic.KeyNotFoundException error) { Assert.Equal("Parameter not found: fire_slime, hp", error.Message); }
        }
        [PortedTest(Source)] public static void queued_projections_reach_the_frame_once_in_emission_order() {
            var context = new SimulationContext();
            var economy = new BattleEconomySystem(); economy.Update(context);
            BattlePresentationFrame frame = null;
            using (var output = new BattlePresentationOutput(null, value => frame = value)) {
                output.Initialize(context,economy,100000);
                var first = new BattleEvent("first",Vector3.Zero,new Vector3(1,0,0),(Fix64)0.2m);
                var second = new BattleEvent("second",Vector3.Zero,new Vector3(1,0,0),(Fix64)0.2m);
                context.AddEvent(first); context.AddEvent(second);
                output.BeginFrame(context); output.EndFrame(context);
                Assert.Sequence(new[] { first,second },frame.Events);
                output.BeginFrame(context); output.EndFrame(context);
                Assert.Equal(0,frame.Events.Length);
            }
        }
        private sealed class Navigation { public readonly int Value; public Navigation(int value) { Value = value; } }
        [PortedTest(Source)] public static void drains_events_once_so_they_are_not_resent_every_frame() {
            var context = new SimulationContext(); context.AddEvent((1, 2)); context.AddEvent("separate");
            Assert.Sequence(new[] { (1, 2) }, context.DrainEvents<(int, int)>());
            Assert.Equal(0, context.DrainEvents<(int, int)>().Length);
            Assert.Sequence(new[] { "separate" }, context.DrainEvents<string>());
        }
        [PortedTest(Source)] public static void set_tick_rate_sets_one_nominal_frame() {
            var context = new SimulationContext(); context.SetTickRate(60); Assert.Equal(Fix64.One / 60, context.DeltaTime);
            bool rejected = false; try { context.SetTickRate(0); } catch (ArgumentOutOfRangeException error) { rejected = (int)error.ActualValue == 0; }
            Assert.True(rejected); Assert.Equal(60, context.TickRate); Assert.Equal(Fix64.One / 60, context.DeltaTime);
        }
        [PortedTest(Source)] public static void an_object_that_fails_to_start_is_dropped() {
            var context = new SimulationContext(); var owner = context.Spawn(Master.LeftPlayer, "FireSlime", new Vector3(1, 0, 1)); Assert.True(owner.IsDestroyed);
        }
        [PortedTest(Source)] public static void queued_actions_run_on_drain_with_the_context() {
            var context = new SimulationContext(); var sender = context.ActionSender();
#if UNITY_WEBGL && !UNITY_EDITOR
            sender.Submit("frame", c => c.FrameNumber++);
#else
            var producer = new Thread(() => sender.Submit("frame", c => c.FrameNumber++)); producer.Start(); producer.Join();
#endif
            Assert.Equal(0, context.FrameNumber); context.DrainActions(); Assert.Equal(1, context.FrameNumber);
        }
        [PortedTest(Source)] public static void services_are_found_by_type() {
            var context = new SimulationContext(); Assert.True(context.Service<Navigation>() == null);
            var service = new Navigation(3); context.InsertService(service); Assert.Equal(3, context.Service<Navigation>().Value); Assert.True(ReferenceEquals(service, context.Service<Navigation>()));
        }
        [PortedTest(Source)] public static void find_player_game_object_takes_the_active_player_of_that_side() {
            var context = new SimulationContext(); context.RegisterPrefab("Player", _ => { }); context.UserIds[Master.RightPlayer] = 7;
            var player = context.Spawn(Master.RightPlayer, "Player", Vector3.Zero); context.FlushObjects();
            Assert.True(ReferenceEquals(player, context.FindPlayer(Master.RightPlayer))); Assert.True(context.FindPlayer(Master.LeftPlayer) == null);
            Assert.True(ReferenceEquals(player, context.FindPlayerByUser(7))); Assert.True(context.FindPlayerByUser(8) == null);
            player.Destroy(); Assert.True(context.FindPlayer(Master.RightPlayer) == null);
        }
    }
}
