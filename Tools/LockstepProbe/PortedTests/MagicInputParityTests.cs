using System.Collections.Generic;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class MagicInputParityTests {
        private const string Source = "game/service/magic_input_handler.rs";
        private sealed class Lookup : IMagicLookup {
            public BoundMagic Custom;
            public int Lookups; public readonly List<(Vector3? origin, Vector3 position)> Runs = new List<(Vector3?, Vector3)>();
            public BoundMagic Magic => Custom ?? new BoundMagic(34, "leafair", (c, m, o, p) => Runs.Add((o, p)));
            public BoundMagic ParseMagic(long user, long id) => GetMagic(id);
            public BoundMagic GetMagic(long id) { Lookups++; return id == 34 ? Magic : null; }
        }
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext(1); public readonly Lookup Lookup = new Lookup(); public readonly MagicInputHandler Handler; public readonly PlayerData Player;
            public readonly List<(long user, long magic)> Saved = new List<(long, long)>(); public readonly SimObject Caster;
            public Fixture() {
                Handler = new MagicInputHandler(Lookup); Context.InputHandler = Handler;
                Context.UserIds.Add(Master.LeftPlayer, 1); Context.UserIds.Add(Master.RightPlayer, 2);
                Player = new PlayerData(new ManaCharger(100)) { Mana = 50 }; Player.AddCard(34); Context.Players.Add(Master.LeftPlayer, Player); Context.Players.Add(Master.RightPlayer, new PlayerData(new ManaCharger(100)));
                Context.CardDecks.Add(Master.LeftPlayer, new CardDeck(new long[0], 0)); Context.GameParameters["leafair.mana_cost"] = 20; Context.GameParameters["leafair.range"] = 18; Context.GameParameters["player.hp"] = 250;
                Context.MagicUsed = (side, user, magic) => Saved.Add((user, magic)); PlayerHealthComponent.Register(Context); Caster = Context.Spawn(Master.LeftPlayer, "Player", new Vector3(1, 0, 1)); Context.FlushObjects();
            }
            public InputResponse Use(long id, Vector3? position) => Handler.HandleInput(Context, 1, id, position, 7);
        }
        private static void Rejected(InputResponse response, int id) { Assert.True(!response.Valid); Assert.Equal(InputResultCode.FailGameEnded, response.ResultCode); Assert.Equal("Game has ended.", response.Message); Assert.Equal(50, response.UpdatedMana); Assert.Equal(id, response.Id); }
        public static void CheckActualCastAndStatusUnsubscribe() {
            var f = new Fixture(); DropPrefabs.Register(f.Context); f.Context.GameParameters["leaf_drop.radius"] = 1; f.Context.GameParameters["leaf_drop.damage"] = 4;
            f.Lookup.Custom = new BoundMagic(34, "leafair", (context, side, origin, position) => context.Spawn(side, "Leafair", new Vector3(position.X, 10, position.Z)));
            var result = f.Use(34, new Vector3(9, 0, 5)); Assert.True(result.Valid); Assert.Equal(ObjectStatus.Attack, f.Caster.Status); Assert.Equal(1, f.Context.PendingObjects.Length);
            var leaf = f.Context.PendingObjects[0]; Assert.True(!leaf.IsDestroyed && leaf.HasComponent<Leafair>()); Assert.Equal(Master.LeftPlayer, leaf.Master); Assert.Equal(new Vector3(9, 10, 5), leaf.Position);
            f.Caster.GetComponent<PlayerStatusSetter>().OnDestroy(); f.Caster.SetStatus(ObjectStatus.Idle); f.Use(0, Vector3.Zero); Assert.Equal(ObjectStatus.Idle, f.Caster.Status);
            f.Context.Result.SetLoser(Master.LeftPlayer); int mana = f.Player.Mana; var refused = f.Handler.HandleBotMagicInput(f.Context, Master.LeftPlayer, f.Lookup.Custom, Vector3.Zero, null); Assert.Equal(InputResultCode.FailGameEnded, refused.ResultCode); Assert.Equal(mana, f.Player.Mana); Assert.Equal(1, f.Context.PendingObjects.Length);
        }
        public static void CheckCardSelectionThroughInput() {
            var f = new Fixture(); f.Context.CardSelectVisualizer = new CardSelectVisualizer(f.Lookup);
            f.Context.SelectCard(1, 34); Assert.True(f.Caster.HasEffect(VisualEffect.CardSelected));
            Assert.True(f.Use(34, new Vector3(2, 0, 2)).Valid); Assert.True(!f.Caster.HasEffect(VisualEffect.CardSelected));
            f.Context.SelectCard(1, 77); f.Player.AddCard(77);
            Assert.True(f.Caster.HasEffect(VisualEffect.CardSelected));
            Assert.Equal(InputResultCode.FailInvalidMagic, f.Use(77, Vector3.Zero).ResultCode);
            Assert.True(!f.Caster.HasEffect(VisualEffect.CardSelected));
            f.Context.SelectCard(1, 34); f.Context.UnselectCard(1, 34); Assert.True(!f.Caster.HasEffect(VisualEffect.CardSelected));
            f.Caster.Destroy(); bool rejected = false;
            try { f.Context.SelectCard(1, 34); } catch (System.InvalidOperationException) { rejected = true; }
            Assert.True(rejected);
            f.Context.CardSelectVisualizer = null; f.Context.SelectCard(1, 34); f.Context.UnselectCard(1, 34);
        }
        private sealed class NeutralCounter : IBotCounterScorer {
            public Fix64 Evaluate(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => Fix64.Zero;
            public Fix64 EvaluateVulnerability(BotMagic magic, IReadOnlyList<BotVisibleObject> enemies) => Fix64.Zero;
        }
        public static void CheckBrainQueuedCast() {
            var f = new Fixture(); f.Context.InsertService(new BotInputHandlerService(new BotMagicInputAdapter(f.Handler)));
            var catalog = new TestBotCatalog(new BotMagic(34, "leafair", summoning: new SummonedBodies("Leafair")));
            var persona = new BotPersona { Tier = BotTier.Intro, CounterAggression = 0, ThinkingTimeMs = 0, ReactionIntervalFrames = 1 };
            var control = new SessionBotControl(f.Context, catalog, new NeutralCounter(), _ => persona, new long[] { 34 }, new long[0], 41, _ => true);
            var lifecycle = new BotLifecycle(f.Context, control, _ => (Fix64)0.5m); lifecycle.ActivateForUser(1);
            var system = new BotAgentSystem(control, _ => null, new FrameMillisClock(f.Context));
            var loop = new BattleFrameLoop(f.Context, new BattleFrameSystems { Bots = system }, () => false);
            f.Context.FrameNumber = 1; loop.Update(); Assert.Equal(50, f.Player.Mana); Assert.Equal(0, f.Lookup.Runs.Count); Assert.Sequence(new long[] { 34 }, f.Player.Cards);
            f.Context.FrameNumber = 2; loop.Update(); Assert.Equal(30, f.Player.Mana); Assert.Equal(1, f.Lookup.Runs.Count); Assert.Equal(0, f.Player.Cards.Count); Assert.Sequence(new long[] { 34 }, f.Context.CardDecks[Master.LeftPlayer].RemainingCards);
            f.Player.AddCard(34); var decision = control.Left.DecisionSource.Think(BotEye.Observe(f.Context, Master.LeftPlayer, _ => null), f.Context.GameParameters, Master.LeftPlayer, false);
            new BotAction().UseCard(f.Context.ActionSender(), new BotInputRequest(3, decision.MagicId, decision.Target), Master.LeftPlayer);
            f.Context.Result.SetEnd(); f.Context.FrameNumber = 3; loop.Update(); Assert.Equal(30, f.Player.Mana); Assert.Equal(1, f.Lookup.Runs.Count); Assert.Sequence(new long[] { 34 }, f.Player.Cards);
            lifecycle.DeactivateForUser(1); Assert.True(!control.HasBotAgent(Master.LeftPlayer));
        }
        [PortedTest(Source)] public static void rejects_player_cast_after_end_without_using_card() { var f = new Fixture(); f.Context.Result.SetEnd(); Rejected(f.Use(34, new Vector3(1, 0, 1)), 7); Assert.Sequence(new long[] { 34 }, f.Player.Cards); Assert.Equal(0, f.Lookup.Lookups); }
        [PortedTest(Source)] public static void rejects_bot_player_cast_after_end_without_using_card() { var f = new Fixture(); f.Context.Result.SetEnd(); Rejected(f.Handler.HandleBotPlayerInput(f.Context, Master.LeftPlayer, 34, new Vector3(1, 0, 1), 8), 8); Assert.Sequence(new long[] { 34 }, f.Player.Cards); Assert.Equal(0, f.Lookup.Lookups); }
        [PortedTest(Source)] public static void rejects_bot_magic_cast_after_end_without_spending_mana() { var f = new Fixture(); f.Context.Result.SetEnd(); Rejected(f.Handler.HandleBotMagicInput(f.Context, Master.LeftPlayer, f.Lookup.Magic, new Vector3(1, 0, 1), Vector3.Zero), -1); Assert.Equal(50, f.Player.Mana); Assert.Equal(0, f.Lookup.Runs.Count); }
        [PortedTest(Source)] public static void charges_the_mana_cost_and_clamps_to_the_range_stored_under_the_magic_name() {
            var f = new Fixture(); var result = f.Use(34, new Vector3(9, 0, 5)); Assert.Equal(InputResultCode.Success, result.ResultCode); Assert.True(result.Valid); Assert.Equal(30, result.UpdatedMana); Assert.Equal(30, f.Player.Mana); Assert.Equal(0, f.Player.Cards.Count);
            Assert.Equal(1, f.Lookup.Runs.Count); Assert.Equal((Vector3?)new Vector3(1, 0, 1), f.Lookup.Runs[0].origin); Assert.Equal(new Vector3(9, 0, 5), f.Lookup.Runs[0].position);
            Assert.Sequence(new long[] { 34 }, f.Context.CardDecks[Master.LeftPlayer].RemainingCards); Assert.Sequence(new[] { (1L, 34L) }, f.Saved);
        }
        [PortedTest(Source)] public static void clamps_a_far_aim_to_the_range_and_the_map() { var f = new Fixture(); f.Context.GameParameters["leafair.mana_cost"] = 1; f.Context.GameParameters["leafair.range"] = 2; f.Player.Mana = 5; var result = f.Use(34, new Vector3(1, 0, -40)); Assert.True(result.Valid); Assert.Equal(new Vector3(1, 0, 0), f.Lookup.Runs[0].position); }
        [PortedTest(Source)] public static void missing_position_is_rejected_without_touching_cards() { var f = new Fixture(); var r = f.Use(34, null); Assert.Equal(InputResultCode.FailInvalidMagic, r.ResultCode); Assert.True(!r.Valid); Assert.Sequence(new long[] { 34 }, f.Player.Cards); }
        [PortedTest(Source)] public static void missing_magic_id_is_rejected_without_touching_cards() { var f = new Fixture(); var r = f.Use(0, new Vector3(1, 0, 1)); Assert.Equal(InputResultCode.FailInvalidMagic, r.ResultCode); Assert.True(!r.Valid); Assert.Sequence(new long[] { 34 }, f.Player.Cards); }
        [PortedTest(Source)] public static void magic_not_in_hand_is_rejected() { var f = new Fixture(); var r = f.Use(99, new Vector3(1, 0, 1)); Assert.Equal(InputResultCode.FailLackOfCard, r.ResultCode); Assert.Sequence(new long[] { 34 }, f.Player.Cards); Assert.Equal(0, f.Lookup.Lookups); }
        [PortedTest(Source)] public static void an_unknown_card_is_discarded_free_of_charge() { var f = new Fixture(); f.Player.AddCard(77); var r = f.Use(77, new Vector3(1, 0, 1)); Assert.Equal(InputResultCode.FailInvalidMagic, r.ResultCode); Assert.True(r.Valid); Assert.Sequence(new long[] { 34 }, f.Player.Cards); Assert.Equal(50, f.Player.Mana); Assert.Equal(0, f.Saved.Count); }
        [PortedTest(Source)] public static void insufficient_mana_keeps_the_card() { var f = new Fixture(); f.Player.Mana = 10; var r = f.Use(34, new Vector3(2, 0, 2)); Assert.Equal(InputResultCode.FailInsufficientMana, r.ResultCode); Assert.Sequence(new long[] { 34 }, f.Player.Cards); }
        [PortedTest(Source)] public static void subscribers_hear_every_outcome() { var f = new Fixture(); int count = 0; f.Handler.Subscribe(_ => count++); f.Use(99, Vector3.Zero); f.Use(34, new Vector3(2, 0, 2)); Assert.Equal(2, count); }
        [PortedTest("game/domain/object/component/player_status_setter.rs")] public static void a_rejected_cast_hinders_the_caster() { var f = new Fixture(); f.Handler.HandleInput(f.Context, 1, 0, Vector3.Zero, 1); Assert.Equal(ObjectStatus.Hindered, f.Caster.Status); }
        [PortedTest("game/domain/object/prefab/implement/misc/player_prefab_initializer.rs")] public static void initializes_player_health_from_database_parameters() { var f = new Fixture(); var health = f.Caster.GetComponent<PlayerHealthComponent>(); Assert.True(health != null); Assert.Equal(250, health.Hp); Assert.Equal(250, health.MaxHp); Assert.Equal(250, f.Player.Hp); }
    }
}
