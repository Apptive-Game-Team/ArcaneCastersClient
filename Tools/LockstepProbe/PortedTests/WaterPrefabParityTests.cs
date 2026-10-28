using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class WaterPrefabParityTests {
        private const string Source = "game/domain/object/prefab/implement/water/mod.rs";
        private static SimulationContext Context(string name, params (string key, decimal value)[] parameters) {
            var c = new SimulationContext(); c.InitializationFailed += (_, error) => throw error; StandardPrefabs.Register(c);
            foreach (var p in parameters) c.GameParameters[name + "." + p.key] = (Fix64)p.value;
            return c;
        }
        [PortedTest(Source)] public static void sea_serpent_attacks_any_target_with_a_beam_and_leaves_a_water_field_trail() {
            var c = Context("sea_serpent", ("mass",10), ("radius",1.2m), ("hp",160), ("speed",0.55m), ("damage",16), ("attack_interval",3.5m), ("attack_range",7), ("beam_width",1));
            var o = c.Spawn(Master.LeftPlayer, "SeaSerpent", Vector3.Zero); var mob = o.GetComponent<SeaSerpentMob>();
            Assert.Equal(3, mob.TargetMask); Assert.Equal((Fix64)7, mob.AttackRange); Assert.Equal(160, mob.MaxHp);
            Assert.Equal(1, o.GetComponents<PathSpawner>().Length); Assert.Equal(1, o.GetComponents<RigidBody>().Length);
            Assert.Equal((Fix64)1.2m, o.Colliders[0].CircleRadius.Value); Assert.True(!o.Colliders[0].IsTrigger); Assert.True(o.Elements.Has(Element.Water));
        }
        [PortedTest(Source)] public static void water_slime_throws_projectiles_with_the_aqua_archer_range() {
            var c = Context("water_slime", ("mass",3), ("radius",0.75m), ("hp",11), ("speed",1.25m), ("damage",4), ("attack_interval",1.5m)); c.GameParameters["aqua_archer.attack_range"] = 6;
            var o = c.Spawn(Master.LeftPlayer, "WaterSlime", Vector3.Zero); Assert.True(!o.HasComponent<Slime>()); var mob = o.GetComponent<WaterSlimeRangeAttackMob>();
            Assert.Equal(11, mob.MaxHp); Assert.Equal((Fix64)1.5m, mob.AttackInterval.Total); Assert.Equal((Fix64)6, mob.AttackRange); Assert.Equal(1, mob.TargetMask);
            Assert.Equal(3, o.GetComponent<RigidBody>().Mass); Assert.Equal((Fix64)0.75m, o.Colliders[0].CircleRadius.Value); Assert.True(!o.Colliders[0].IsTrigger); Assert.True(o.HasComponent<PathSpawner>());
        }
        [PortedTest(Source)] public static void tidal_warheads_share_one_body_under_two_prefab_types() {
            var c = Context("tidal_warhead", ("damage",9), ("speed",5), ("radius",2));
            foreach (var name in new[] { "TidalWarhead", "GroundTidalWarhead" }) {
                var o = c.Spawn(Master.LeftPlayer, name, Vector3.Zero); Assert.Equal(name, o.Prefab); Assert.Equal(1, o.GetComponents<TidalWarheadProjectile>().Length);
                Assert.Equal((Fix64)0.25m, o.Colliders[0].CircleRadius.Value); Assert.True(o.Colliders[0].IsTrigger); Assert.True(o.Elements.Has(Element.Water));
            }
        }
        [PortedTest(Source)] public static void warhead_explosion_is_a_timed_presentation() {
            var c = Context(""); var o = c.Spawn(Master.LeftPlayer, "TidalWarheadExplosion", Vector3.Zero); Assert.Equal(1, o.GetComponents<TimedSelfDestroyer>().Length);
        }
        [PortedTest(Source)] public static void water_field_is_grounded_wets_and_expires() {
            var c = Context("water_field", ("radius",2.5m), ("duration",8)); var o = c.Spawn(Master.LeftPlayer, "WaterField", new Vector3(1,3,2));
            Assert.Equal(new Vector3(1,0,2), o.Position); Assert.Equal((Fix64)2.5m, o.Colliders[0].CircleRadius.Value); Assert.True(o.Colliders[0].IsTrigger);
            Assert.True(o.HasComponent<EffectProvider>()); Assert.True(o.HasComponent<WaterFieldEffectReceiver>()); Assert.True(o.HasComponent<TimedSelfDestroyer>()); Assert.True(o.Elements.Has(Element.Water));
        }
        [PortedTest(Source)] public static void water_shot_wets_what_it_hits() {
            var c = Context("water_shot", ("radius",0.4m), ("damage",3), ("speed",6)); var o = c.Spawn(Master.LeftPlayer, "WaterShot", Vector3.Zero);
            Assert.True(o.HasComponent<EffectProvider>()); Assert.True(o.HasComponent<Shot>()); Assert.True(o.Colliders[0].IsTrigger); Assert.True(o.Elements.Has(Element.Water));
        }
        [PortedTest(Source)] public static void registers_the_seven_java_beans() {
            var c = Context(""); foreach (var name in new[] { "GroundTidalWarhead", "SeaSerpent", "TidalWarheadExplosion", "TidalWarhead", "WaterField", "WaterShot", "WaterSlime" }) Assert.True(c.HasPrefab(name));
        }
    }
}
