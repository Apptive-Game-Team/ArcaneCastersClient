using System;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class ShotMagicParityTests {
        private const string Source = "game/domain/magic/implement/shoot/abstract_shot_magic.rs";
        private const string OverchargeSource = "game/domain/object/component/effect/ally_overcharge_provider.rs";
        private static SimObject Summon(SimulationContext c, Master master, bool lightning = true) {
            var o = c.Spawn(master, "Player", Vector3.Zero); o.Elements.AddNative(lightning ? Element.Lightning : Element.Rock);
            if (lightning) o.AddComponentImmediately(new LightningSummonEffectReceiver(o)); else o.AddComponentImmediately(new CommonEffectReceiver(o)); return o;
        }
        [PortedTest(OverchargeSource)] public static void own_lightning_summon_is_overcharged_on_contact() {
            var c = Context(); var shot = c.Spawn(Master.LeftPlayer, "ElectricShot", Vector3.Zero); var summon = Summon(c, Master.LeftPlayer); shot.GetComponent<AllyOverchargeProvider>().OnCollision(summon);
            Assert.Equal(StatusEffectKey.OverchargeReceive, summon.GetComponent<LightningSummonEffectReceiver>().GetEffectByKey(StatusEffectKey.OverchargeReceive).Key); Assert.Equal(1, summon.GetPendingComponents<OverchargeStatusEffect>().Length);
        }
        [PortedTest(OverchargeSource)] public static void enemy_lightning_summon_is_not_overcharged() {
            var c = Context(); var shot = c.Spawn(Master.LeftPlayer, "ElectricShot", Vector3.Zero); var summon = Summon(c, Master.RightPlayer); shot.GetComponent<AllyOverchargeProvider>().OnCollision(summon); Assert.True(summon.GetComponent<LightningSummonEffectReceiver>().GetEffectByKey(StatusEffectKey.OverchargeReceive) == null);
        }
        [PortedTest(OverchargeSource)] public static void own_non_lightning_summon_takes_nothing() {
            var c = Context(); var shot = c.Spawn(Master.LeftPlayer, "ElectricShot", Vector3.Zero); var summon = Summon(c, Master.LeftPlayer, false); shot.GetComponent<AllyOverchargeProvider>().OnCollision(summon); Assert.Equal(0, summon.GetComponents<BaseStatusEffect>().Length); Assert.Equal(0, summon.PendingComponents.Length);
        }
        [PortedTest(OverchargeSource)] public static void enemy_collision_callback_does_nothing() {
            var c = Context(); var shot = c.Spawn(Master.LeftPlayer, "ElectricShot", Vector3.Zero); var summon = Summon(c, Master.LeftPlayer); shot.GetComponent<AllyOverchargeProvider>().OnCollisionWithEnemy(summon); Assert.True(summon.GetComponent<LightningSummonEffectReceiver>().GetEffectByKey(StatusEffectKey.OverchargeReceive) == null);
        }
        private static SimulationContext Context() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; c.RegisterPrefab("Player", _ => { }); BasicShotPrefabs.Register(c);
            foreach (var key in new[] { "fire_shot", "water_shot", "electric_shot" }) { c.GameParameters[key + ".radius"] = (Fix64)0.3m; c.GameParameters[key + ".damage"] = 5; c.GameParameters[key + ".speed"] = 4; } return c;
        }
        private static BoundMagic Magic(string name) { var registry = new MagicRegistry(); ShotMagics.Register(registry); return registry.GetBean(name).Bind(1, name, Element.None); }
        [PortedTest(Source)] public static void launches_the_shot_from_the_caster_toward_the_aim() {
            var c = Context(); c.Spawn(Master.LeftPlayer, "Player", new Vector3(2, 0, 3)); c.FlushObjects(); Magic("fire_shot").Run(c, Master.LeftPlayer, new Vector3(12, 0, 3));
            var shot = c.PendingObjects.Single(); Assert.Equal("FireShot", shot.Prefab); Assert.Equal(new Vector3(2, 0, 3), shot.Position); Assert.Equal(new Vector3(1, 0, 0), shot.GetComponent<Shot>().Direction.Value);
        }
        [PortedTest(Source)] public static void does_nothing_without_a_caster() { var c = Context(); Magic("fire_shot").Run(c, Master.LeftPlayer, new Vector3(12, 0, 3)); Assert.Equal(0, c.PendingObjects.Length); }
        public static void CheckShotOriginsAndPrefabs() {
            var c = Context(); c.RegisterPrefab("Object", _ => { }); var inactive = c.Spawn(Master.RightPlayer, "Player", new Vector3(3, 0, 5)); var caster = c.Spawn(Master.RightPlayer, "Player", new Vector3(16, 0, 5)); c.FlushObjects(); inactive.Destroy();
            var names = new[] { "fire_shot", "water_shot", "lightning_shot" }; var prefabs = new[] { "FireShot", "WaterShot", "ElectricShot" }; var effects = new[] { VisualEffect.Burn, VisualEffect.Wet, VisualEffect.Shock }; var elements = new[] { Element.Fire, Element.Water, Element.Lightning };
            for (int i = 0; i < names.Length; i++) {
                var magic = Magic(names[i]); magic.Run(c, Master.RightPlayer, new Vector3(4, 0, 5)); var shot = c.PendingObjects.Single(); Assert.Equal(prefabs[i], shot.Prefab); Assert.Equal(caster.Position, shot.Position); Assert.Equal(Master.RightPlayer, shot.Master); Assert.Equal(new Vector3(-1, 0, 0), shot.GetComponent<Shot>().Direction.Value);
                Assert.Equal(effects[i], shot.GetComponent<EffectProvider>().Effect); Assert.True(shot.Elements.NativeHas(elements[i])); Assert.True(shot.Colliders.Single().IsTrigger); Assert.Equal(i == 2, shot.HasComponent<AllyOverchargeProvider>());
                c.DeltaTime = (Fix64)0.5m; shot.Update(); Assert.Equal(new Vector3(14, 0, 5), shot.Position); var target = c.Spawn(Master.LeftPlayer, "Object", shot.Position); target.AddCollider(new CircleCollider(target, (Fix64)0.25m)); var mob = new DummyMob(target, 100); target.AddComponentImmediately(mob); c.FlushObjects(); shot.GetComponent<Shot>().OnCollisionWithEnemy(target); Assert.Equal(95, mob.Hp); Assert.True(shot.IsDestroyed); target.Destroy(); c.FlushObjects();
                magic.Run(c, Master.RightPlayer, null, new Vector3(4, 0, 5)); Assert.Equal(0, c.PendingObjects.Length);
                var origin = new Vector3(10, 3, 2); magic.Run(c, Master.LeftPlayer, origin, new Vector3(10, 3, 8)); shot = c.PendingObjects.Single(); Assert.Equal(origin, shot.Position); Assert.Equal(new Vector3(0, 0, 1), shot.GetComponent<Shot>().Direction.Value); shot.Destroy(); c.FlushObjects();
            }
            CloudDragonMob.RegisterPrefabs(c); c.GameParameters["chain_lightning.radius"] = (Fix64)0.3m; c.GameParameters["chain_lightning.speed"] = 4; c.GameParameters["chain_lightning.damage"] = 10; c.GameParameters["chain_lightning.attack_range"] = 3; c.GameParameters["chain_lightning.min_damage"] = 2;
            Magic("chain_lightning").Run(c, Master.RightPlayer, new Vector3(4, 0, 5)); var chain = c.PendingObjects.Single(); Assert.Equal("ChainLightning", chain.Prefab); chain.Update(); Assert.Equal(new Vector3(14, 0, 5), chain.Position); chain.Destroy(); c.FlushObjects();
            var neutral = ShotMagics.Create("FireShot", _ => Master.None).Bind(2, "test", Element.None); neutral.Run(c, Master.RightPlayer, Vector3.Zero, new Vector3(1, 0, 0)); Assert.Equal(Master.None, c.PendingObjects.Single().Master); c.FlushObjects();
            bool threw = false; try { ShotMagics.Create("Object").Bind(3, "missing", Element.None).Run(c, Master.LeftPlayer, Vector3.Zero, Vector3.Zero); } catch (InvalidOperationException) { threw = true; } Assert.True(threw);
            var registry = new MagicRegistry(); ShotMagics.Register(registry); Assert.Equal(8, registry.BeanNames().Length); Assert.True(registry.ContainsBean("boulder_strike") && registry.ContainsBean("rock_rolling") && registry.ContainsBean("tide_call") && registry.ContainsBean("wind_blade"));
        }
    }
}
