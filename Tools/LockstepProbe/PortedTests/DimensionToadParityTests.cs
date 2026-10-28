using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class DimensionToadParityTests {
        private const string Source = "game/domain/object/prefab/implement/pve/tests.rs";
        private static SimulationContext Context() {
            var c = new SimulationContext { DeltaTime = (Fix64)0.05m }; c.InitializationFailed += (o, e) => throw e; DimensionToadPrefabs.Register(c); c.RegisterPrefab("Target", _ => { });
            c.GameParameters["pve_dimension_toad.hp"] = 800; c.GameParameters["pve_dimension_toad.mass"] = 1000; c.GameParameters["pve_dimension_toad.radius"] = (Fix64)0.65m; c.GameParameters["pve_dimension_toad.spawn_interval"] = 10;
            foreach (var key in new[] { "pve_fire_tadpole", "pve_lightning_tadpole" }) {
                c.GameParameters[key + ".hp"] = 30; c.GameParameters[key + ".damage"] = key == "pve_fire_tadpole" ? 12 : 10; c.GameParameters[key + ".speed"] = key == "pve_fire_tadpole" ? (Fix64)0.9m : Fix64.One;
                c.GameParameters[key + ".mass"] = 1; c.GameParameters[key + ".radius"] = (Fix64)0.3m; c.GameParameters[key + ".attack_interval"] = 1; c.GameParameters[key + ".duration"] = 20;
            }
            return c;
        }
        private static SimObject Toad(SimulationContext c) => c.Spawn(Master.RightPlayer, "PveDimensionToad", new Vector3(14, 0, 5));
        private static void Tick(SimObject owner, int frames) { for (int i = 0; i < frames; i++) owner.Update(); }
        [PortedTest(Source)] public static void stays_put_and_never_attacks_even_with_a_threat_next_to_it() {
            var c = Context(); var toad = Toad(c); var target = c.Spawn(Master.LeftPlayer, "Target", new Vector3(13, 0, 5)); target.AddComponentImmediately(new DummyMob(target, 100)); c.FlushObjects(); var before = toad.Position; Tick(toad, 600);
            Assert.Equal(before, toad.Position); Assert.True(toad.GetComponent<MobComponent>() is PveGateKeeperMob); Assert.Equal(Fix64.Zero, toad.GetComponent<MobComponent>().Speed.Total); Assert.True(!toad.HasComponent<MeleeAttackMob>() && toad.HasComponent<ICollidable>()); Assert.True(toad.Elements.NativeHas(Element.Fire) && toad.Elements.NativeHas(Element.Lightning)); Assert.Equal(100, target.GetComponent<DummyMob>().Hp);
        }
        [PortedTest(Source)] public static void spawns_fire_and_lightning_tadpoles_in_turn_at_the_parameter_interval() {
            var c = Context(); var toad = Toad(c); c.FlushObjects(); Tick(toad, 1); Assert.Sequence(new[] { "PveFireTadpole" }, c.PendingObjects.Select(o => o.Prefab)); Tick(toad, 198); Assert.Equal(1, c.PendingObjects.Length); Tick(toad, 2);
            Assert.Sequence(new[] { "PveFireTadpole", "PveLightningTadpole" }, c.PendingObjects.Select(o => o.Prefab)); Tick(toad, 200); Assert.Sequence(new[] { "PveFireTadpole", "PveLightningTadpole", "PveFireTadpole" }, c.PendingObjects.Select(o => o.Prefab));
        }
        [PortedTest(Source)] public static void max_hp_override_from_the_installer_reaches_the_gate_keeper_mob() { var c = Context(); var mob = Toad(c).GetComponent<PveGateKeeperMob>(); Assert.Equal(800, mob.MaxHp); mob.OverrideMaxHp(200); Assert.Equal(200, mob.Hp); Assert.Equal(200, mob.MaxHp); }
        [PortedTest(Source)] public static void killed_toad_is_terminal_and_leaves_no_death_field() {
            var c = Context(); var toad = Toad(c); c.FlushObjects(); Assert.True(!PveObjectiveTarget.IsTerminal(toad)); toad.GetComponent<MobComponent>().ApplyDamage(new Hit(5000, 0, Element.None)); Assert.True(toad.IsDestroyed && PveObjectiveTarget.IsTerminal(toad)); Assert.Equal(0, c.PendingObjects.Length);
        }
        [PortedTest(Source)] public static void tadpoles_expire_after_their_own_duration() {
            foreach (var prefab in new[] { "PveFireTadpole", "PveLightningTadpole" }) { var c = Context(); var tadpole = c.Spawn(Master.RightPlayer, prefab, Vector3.Zero); Assert.True(tadpole.HasComponent<MeleeAttackMob>()); var lifetime = tadpole.GetComponent<TimedSelfDestroyer>(); lifetime.Update(); Assert.True(!tadpole.IsDestroyed); for (int i = 0; i < 400; i++) lifetime.Update(); Assert.True(tadpole.IsDestroyed); }
        }
        public static void CheckTadpoleCombat() {
            var c = Context(); var toad = Toad(c); c.FlushObjects(); toad.Update(); var fire = c.PendingObjects.Single(); Assert.Equal(Master.RightPlayer, fire.Master); Assert.Near((Fix64)13.65m, fire.Position.X, Fix64.FromRaw(1)); Assert.Near((Fix64)(-0.175m), fire.Position.Y, Fix64.FromRaw(1));
            var target = c.Spawn(Master.LeftPlayer, "Target", new Vector3(13, 0, 5)); var victim = new DummyMob(target, 100); target.AddComponentImmediately(victim); fire.GetComponent<MeleeAttackMob>().Behavior(target); Assert.Equal(88, victim.Hp); Assert.True(fire.Elements.NativeHas(Element.Fire) && !fire.Colliders.Single().IsTrigger);
            var electric = c.Spawn(Master.RightPlayer, "PveLightningTadpole", new Vector3(12, 0, 5)); electric.GetComponent<MeleeAttackMob>().Behavior(target); Assert.Equal(78, victim.Hp); Assert.True(electric.HasComponent<ElectricDeathEnergy>() && electric.HasComponent<LightningSummonEffectReceiver>()); Assert.Equal(1, electric.GetComponent<MeleeAttackMob>().TargetMask);
            c.FlushObjects(); electric.GetComponent<LightningSummonEffectReceiver>().Receive(VisualEffect.Shock); Assert.True(electric.GetComponent<LightningSummonEffectReceiver>().GetEffectByKey(StatusEffectKey.OverchargeReceive) != null);
        }
    }
}


