using System.Threading;
using ArcaneCasters.Simulation;
using BEPUutilities;

namespace ArcaneCasters.Parity {
    public static class BotEyeParityTests {
        private const string Source = "game/domain/bot/bot_eye.rs";
        private static SimulationContext Context() {
            var c = new SimulationContext(); c.RegisterPrefab("Player", _ => { });
            c.Players[Master.LeftPlayer] = new PlayerData(new ManaCharger(10)); c.Players[Master.RightPlayer] = new PlayerData(new ManaCharger(10)); return c;
        }
        private static BotEye Observe(SimulationContext c, Master side = Master.LeftPlayer) => BotEye.Observe(c, side, _ => null);
        public static void CheckVisibleHealthAndStatus() {
            var c = Context(); var owner = c.Spawn(Master.RightPlayer, "Player", Vector3.Zero); c.FlushObjects();
            Assert.Equal(10, BotVisibleObject.Of(owner, _ => 99).Hp);
            owner.AddComponentImmediately(new DummyMob(owner, 20));
            var visible = BotVisibleObject.Of(owner, o => o.GetComponent<MobComponent>().Hp);
            Assert.Equal(20, visible.Hp); Assert.True(visible.Mob && visible.Targetable);
            Assert.Equal(10, BotVisibleObject.Of(owner, _ => null).Hp); Assert.Equal(1, BotVisibleObject.Of(owner, _ => 0).Hp);
            owner.SetStatus(ObjectStatus.Dying); Assert.True(!BotVisibleObject.Of(owner, _ => 20).Targetable);
            Assert.True(visible.Targetable); owner.Destroy(); Assert.True(!BotVisibleObject.Of(owner, _ => 20).Targetable);
        }
        [RequiresThreads]
        [PortedTest(Source)] public static void the_snapshot_can_cross_to_the_executor_thread() {
            var c = Context(); c.Players[Master.LeftPlayer].Mana = 7; var eye = Observe(c); int mana = -1;
            var thread = new Thread(() => mana = eye.Mana); thread.Start(); thread.Join(); Assert.Equal(7, mana);
        }
        [PortedTest(Source)] public static void copies_what_the_side_can_see() {
            var c = Context(); c.Players[Master.LeftPlayer].Mana = 7; c.Players[Master.LeftPlayer].AddCard(34);
            c.Spawn(Master.RightPlayer, "Player", new Vector3(1, 0, 2)); c.FlushObjects(); var eye = Observe(c);
            Assert.Equal(7, eye.Mana); Assert.Sequence(new long[] { 34 }, eye.Cards); Assert.Equal(1, eye.Objects.Count);
            var item = eye.Objects[0]; Assert.Equal(Master.RightPlayer, item.Master); Assert.Equal("Player", item.Prefab); Assert.Equal(new Vector3(1, 0, 2), item.Position); Assert.True(!item.Mob);
        }
        [PortedTest(Source)] public static void keeps_the_position_the_object_had_when_observed() {
            var c = Context(); var live = c.Spawn(Master.RightPlayer, "Player", new Vector3(1, 0, 2)); c.FlushObjects();
            var eye = Observe(c); live.SetPosition(new Vector3(9, 0, 2)); Assert.Equal(new Vector3(1, 0, 2), eye.Objects[0].Position);
        }
        [PortedTest(Source)] public static void is_not_affected_by_later_changes_to_the_world_or_the_hand() {
            var c = Context(); var player = c.Players[Master.LeftPlayer]; player.Mana = 7; player.AddCard(34);
            var live = c.Spawn(Master.RightPlayer, "Player", new Vector3(1, 0, 2)); c.FlushObjects(); var eye = Observe(c);
            player.Mana = 0; player.Cards.Clear(); c.RemoveObject(live);
            Assert.Equal(7, eye.Mana); Assert.Sequence(new long[] { 34 }, eye.Cards); Assert.Equal(1, eye.Objects.Count);
        }
        [PortedTest(Source)] public static void reads_the_mana_and_hand_of_the_side_it_is_asked_about() {
            var c = Context(); c.Players[Master.LeftPlayer].Mana = 1; c.Players[Master.RightPlayer].Mana = 9; c.Players[Master.RightPlayer].AddCard(12);
            var eye = Observe(c, Master.RightPlayer); Assert.Equal(9, eye.Mana); Assert.Sequence(new long[] { 12 }, eye.Cards);
        }
        [PortedTest(Source)] public static void takes_the_enemy_player_hp_from_the_other_side() {
            var c = Context(); c.Players[Master.LeftPlayer].Hp = 30; c.Players[Master.RightPlayer].Hp = 80; Assert.Equal(80, Observe(c).EnemyPlayerHp);
        }
    }
}
