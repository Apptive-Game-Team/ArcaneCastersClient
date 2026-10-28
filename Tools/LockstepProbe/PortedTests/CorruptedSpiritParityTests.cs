using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class CorruptedSpiritParityTests {
        private const string Source = "game/domain/object/prefab/implement/pve/tests.rs";
        private static SimulationContext Context() {
            var c = new SimulationContext { DeltaTime = (Fix64)0.05m }; c.InitializationFailed += (o, e) => throw e; CorruptedSpiritPrefabs.Register(c); c.RegisterPrefab("Target", _ => { });
            foreach (var key in new[] { "pve_corrupted_vine_spirit", "pve_corrupted_wind_spirit" }) {
                c.GameParameters[key + ".hp"] = 40; c.GameParameters[key + ".damage"] = 8; c.GameParameters[key + ".mass"] = 1; c.GameParameters[key + ".speed"] = 1;
                c.GameParameters[key + ".attack_interval"] = 1; c.GameParameters[key + ".attack_range"] = (Fix64)1.5m; c.GameParameters[key + ".radius"] = (Fix64)0.4m;
            }
            return c;
        }
        [PortedTest(Source)] public static void corrupted_vine_spirit_is_a_ground_melee_mob() {
            var c = Context(); var owner = c.Spawn(Master.RightPlayer, "PveCorruptedVineSpirit", new Vector3(14, 0, 5)); Assert.True(owner.HasComponent<MeleeAttackMob>()); Assert.True(!owner.GetComponent<ZPhysics>().CanHover); Assert.True(owner.Elements.NativeHas(Element.Nature));
        }
        [PortedTest(Source)] public static void corrupted_wind_spirit_is_an_aerial_self_destruct_mob() {
            var c = Context(); var owner = c.Spawn(Master.RightPlayer, "PveCorruptedWindSpirit", new Vector3(14, 0, 5)); Assert.True(owner.HasComponent<SelfDestructMob>()); Assert.True(owner.GetComponent<ZPhysics>().CanHover); Assert.True(owner.Elements.NativeHas(Element.Wind));
        }
        private static DummyMob Target(SimulationContext c, Master side, Vector3 position) {
            var owner = c.Spawn(side, "Target", position); owner.AddCollider(new CircleCollider(owner, (Fix64)0.4m)); var mob = new DummyMob(owner, 100); owner.AddComponentImmediately(mob); return mob;
        }
        public static void CheckCombatVariants() {
            var c = Context(); var vine = c.Spawn(Master.RightPlayer, "PveCorruptedVineSpirit", new Vector3(5, 0, 5)); var enemy = Target(c, Master.LeftPlayer, new Vector3(6, 0, 5)); var melee = vine.GetComponent<MeleeAttackMob>(); Assert.Equal(1, melee.TargetMask); Assert.Equal(40, melee.Hp); melee.Behavior(enemy.Owner); Assert.Equal(92, enemy.Hp);
            var wind = c.Spawn(Master.RightPlayer, "PveCorruptedWindSpirit", new Vector3(6, 3, 5)); var air = Target(c, Master.LeftPlayer, new Vector3(6, 2, 5)); var ally = Target(c, Master.RightPlayer, new Vector3(6, 0, 5)); c.FlushObjects(); var dive = wind.GetComponent<SelfDestructMob>(); Assert.Equal(3, dive.TargetMask); Assert.True(dive.IsValidTarget(enemy.Owner) && dive.IsValidTarget(air.Owner)); Assert.True(dive.Behavior(enemy.Owner));
            wind.SetPosition(new Vector3(6, 1, 5)); dive.Update(); Assert.True(wind.IsDestroyed); Assert.Equal(84, enemy.Hp); Assert.Equal(92, air.Hp); Assert.Equal(100, ally.Hp); dive.OnCollisionWithEnemy(enemy.Owner); Assert.Equal(84, enemy.Hp);
            Assert.True(!vine.HasComponent<TimedSelfDestroyer>() && !wind.HasComponent<TimedSelfDestroyer>()); Assert.True(vine.HasComponent<CommonEffectReceiver>() && wind.HasComponent<CommonEffectReceiver>());
        }
    }
}
