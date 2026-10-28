using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class PveBossParityTests {
        private const string Source = "game/domain/object/component/mob/statemachine/attacker/pve_boss_mob.rs";
        private const string ShootSource = "game/domain/magic/implement/shoot/mod.rs";
        private sealed class Magic : IBossMagic {
            public long Id { get; set; } = 1; public bool IsSpawnMagic { get; set; } public bool Valid = true; public int Calls; public Vector3 Target, Origin;
            public bool Cast(SimulationContext c, Master m, Vector3 target, Vector3 origin) { Calls++; Target = target; Origin = origin; return Valid; }
        }
        private static void Cooldown(int rate) {
            var c = new SimulationContext { DeltaTime = Fix64.One / rate }; var owner = new SimObject(c, Master.RightPlayer, "ZapMouse", Vector3.Zero); var target = new SimObject(c, Master.LeftPlayer, "ZapMouse", new Vector3(3, 0, 0)); var magic = new Magic(); var mob = new PveBossMob(owner, 100, 1, 0, 1, 5, new[] { magic });
            Assert.True(mob.TryCastMagic(magic, target)); int frames = 0; bool cast; do { mob.Update(); frames++; cast = mob.TryCastMagic(magic, target); } while (!cast && frames < rate * 10); Assert.Equal(rate * 2, frames);
        }
        [PortedTest(Source)] public static void a_cast_magic_is_ready_again_after_two_seconds_at_20_ticks_per_second() => Cooldown(20);
        [PortedTest(Source)] public static void a_cast_magic_is_ready_again_after_two_seconds_at_60_ticks_per_second() => Cooldown(60);
        [PortedTest(ShootSource)] public static void registers_every_shoot_magic_under_its_java_bean_name() {
            var registry = new MagicRegistry(); StandardMagics.RegisterShots(registry);
            Assert.Sequence(new[] { "boulder_strike", "chain_lightning", "fire_shot", "lightning_shot", "rock_rolling", "spirit_bomb", "tidal_warhead", "tide_call", "vine_fan", "vine_toss", "water_shot", "will_o_wisp", "wind_blade" }, registry.BeanNames());
        }
        private sealed class Catalog : IMagicCatalog {
            public IEnumerable<MagicInfo> GetAllMagic() => new[] { new MagicInfo(7, "fire_shot", Element.Fire), new MagicInfo(8, "zap_mouse", Element.Lightning) };
            public bool ExistUserMagic(long user, long magic) => true;
        }
        public static void CheckBoundBossCasting() {
            var c = new SimulationContext { DeltaTime = (Fix64)0.05m }; c.InitializationFailed += (o, e) => throw e; var registry = MagicRegistry.Standard(); Assert.Equal(88, registry.BeanNames().Length);
            var parser = new DatabaseMagicParser(new Catalog(), registry); c.InputHandler = new MagicInputHandler(parser); c.Players.Add(Master.RightPlayer, new PlayerData(new ManaCharger(100)) { Mana = 10 });
            c.GameParameters["fire_shot.range"] = 3; c.GameParameters["fire_shot.mana_cost"] = 4; c.GameParameters["fire_shot.radius"] = (Fix64)0.25m; c.GameParameters["fire_shot.damage"] = 6; c.GameParameters["fire_shot.speed"] = 2; BasicShotPrefabs.Register(c);
            c.RegisterPrefab("ZapMouse", _ => { }); c.GameParameters["zap_mouse.range"] = 3; c.GameParameters["zap_mouse.mana_cost"] = 5;
            var owner = new SimObject(c, Master.RightPlayer, "Boss", new Vector3(5, 0, 5)); var target = new SimObject(c, Master.LeftPlayer, "Target", new Vector3(9, 0, 5)); var shot = parser.GetMagic(7); var summon = parser.GetMagic(8); var boss = new PveBossMob(owner, 100, 1, 3, 1, 6, new IBossMagic[] { shot, summon }); owner.AddComponentImmediately(boss);
            Assert.True(boss.Behavior(target)); Assert.Equal(6, c.Players[Master.RightPlayer].Mana); var projectile = c.PendingObjects.Single(); Assert.Equal(owner.Position, projectile.Position); projectile.GetComponent<Shot>().Update(); Assert.Near((Fix64)5.1m, projectile.Position.X, Fix64.FromRaw(2));
            Assert.True(!boss.TryCastMagic(shot, target)); Assert.True(boss.CastMagic(target)); Assert.Equal(1, c.Players[Master.RightPlayer].Mana); var spawned = c.PendingObjects.Single(o => o.Prefab == "ZapMouse"); Assert.Equal(owner.Position, spawned.Position); Assert.Equal(Master.RightPlayer, spawned.Master);
            for (int i = 0; i < 40; i++) boss.Update(); Assert.True(!boss.CastMagic(target)); c.Players[Master.RightPlayer].Mana = 4; Assert.True(boss.TryCastMagic(shot, target)); Assert.Equal(0, c.Players[Master.RightPlayer].Mana);
            Assert.True(owner.HasComponent<IFrenzyImmune>()); Assert.True(!PveObjectiveTarget.IsTerminal(owner)); boss.OnDamaged(new Hit(100, 0, Element.None)); Assert.True(PveObjectiveTarget.IsTerminal(owner));
            var first = new Magic { Valid = false }; var second = new Magic { Id = 2, IsSpawnMagic = true }; var probe = new PveBossMob(new SimObject(c, Master.LeftPlayer, "Boss", new Vector3(7, 2, 6)), 10, 1, 0, 1, 1, new[] { first, second });
            Assert.True(probe.CastMagic(null)); Assert.Equal(0, first.Calls); Assert.Equal(probe.Owner.Position, second.Target); Assert.Equal(probe.Owner.Position, second.Origin); Assert.True(!probe.TryCastMagic(new Magic { Id = 2 }, target));
            Assert.True(!probe.TryCastMagic(first, target)); first.Valid = true; Assert.True(probe.TryCastMagic(first, target)); Assert.Equal(target.Position, first.Target); Assert.True(!probe.TryCastMagic(null, target));
        }
    }
}
