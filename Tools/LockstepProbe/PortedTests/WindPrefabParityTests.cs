using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class WindPrefabParityTests {
        private const string Source = "game/domain/object/prefab/implement/wind/mod.rs";
        private static SimulationContext Context(string name, params (string key, decimal value)[] parameters) {
            var c = new SimulationContext(); c.InitializationFailed += (_, error) => throw error;
            BombSpritePrefabs.Register(c); SpecialShotPrefabs.Register(c); SelfDestructMob.RegisterWindSpirit(c); ElementalNests.Register(c);
            foreach (var p in parameters) c.GameParameters[name + "." + p.key] = (Fix64)p.value;
            return c;
        }
        [PortedTest(Source)] public static void wind_spirit_targets_only_aerial_units() {
            var c = Context("wind_spirit", ("hp",12), ("mass",1), ("damage",3), ("radius",0.5m), ("speed",2), ("attack_interval",1), ("attack_range",1.5m));
            var o = c.Spawn(Master.LeftPlayer, "WindSpirit", Vector3.Zero); var mob = o.GetComponent<SelfDestructMob>();
            Assert.Equal(2, mob.TargetMask); Assert.True((mob.TargetMask & 2) != 0); Assert.Equal(0, mob.TargetMask & 1); Assert.True(mob.VerticalRangeIgnored); Assert.Equal(12, mob.MaxHp);
            Assert.Equal(1, o.GetComponents<ZPhysics>().Length); Assert.Equal(1, o.GetComponents<CommonEffectReceiver>().Length); Assert.True(o.Elements.Has(Element.Wind));
        }
        [PortedTest(Source)] public static void bomb_sprite_is_an_aerial_mob_with_a_bomb_sprite_mob() {
            var c = Context("bomb_sprite", ("mass",1), ("radius",0.5m), ("hp",8), ("speed",2), ("attack_interval",2), ("attack_range",4));
            var o = c.Spawn(Master.LeftPlayer, "BombSprite", Vector3.Zero);
            Assert.Equal(8, o.GetComponent<BombSpriteMob>().MaxHp); Assert.Equal(1, o.GetComponents<ZPhysics>().Length); Assert.Equal(1, o.GetComponents<RigidBody>().Length); Assert.True(o.Elements.Has(Element.Wind));
        }
        [PortedTest(Source)] public static void bomb_sprite_bomb_and_explosion_are_small_prefabs() {
            var c = Context("bomb_sprite_bomb", ("damage",5), ("speed",8), ("radius",1.5m));
            var bomb = c.Spawn(Master.LeftPlayer, "BombSpriteBomb", Vector3.Zero); var explosion = c.Spawn(Master.LeftPlayer, "BombSpriteExplosion", Vector3.Zero);
            Assert.Equal(1, bomb.GetComponents<BombSpriteBomb>().Length); Assert.True(bomb.Elements.Has(Element.Wind)); Assert.Equal(1, explosion.GetComponents<TimedSelfDestroyer>().Length);
        }
        [PortedTest(Source)] public static void wind_blade_is_a_piercing_trigger_shot() {
            var c = Context("wind_blade", ("radius",0.6m), ("damage",4), ("speed",7)); var o = c.Spawn(Master.LeftPlayer, "WindBlade", Vector3.Zero);
            Assert.True(o.HasComponent<WindBladeShot>()); Assert.True(o.Colliders[0].IsTrigger); Assert.Equal((Fix64)0.6m, o.Colliders[0].CircleRadius.Value);
        }
        [PortedTest(Source)] public static void wind_slime_is_a_ground_slime() {
            var c = Context("wind_slime", ("mass",2), ("radius",0.5m), ("hp",9), ("speed",1.5m), ("damage",2), ("attack_interval",1)); var o = c.Spawn(Master.LeftPlayer, "WindSlime", Vector3.Zero);
            var mob = o.GetComponent<Slime>(); Assert.Equal(1, mob.TargetMask); Assert.Equal(9, mob.MaxHp); Assert.True(o.HasComponent<CommonEffectReceiver>()); Assert.True(o.Elements.Has(Element.Wind));
        }
        [PortedTest(Source)] public static void wind_summon_spawns_wind_slimes() {
            var c = Context("wind_summon", ("radius",1), ("hp",20)); var o = c.Spawn(Master.LeftPlayer, "WindSummon", Vector3.Zero);
            Assert.Equal(20, o.GetComponent<Spawner>().MaxHp); Assert.True(o.Colliders[0].IsTrigger);
        }
        [PortedTest(Source)] public static void registers_the_seven_java_beans() {
            var c = Context(""); foreach (var name in new[] { "BombSpriteBomb", "BombSpriteExplosion", "BombSprite", "WindBlade", "WindSlime", "WindSpirit", "WindSummon" }) Assert.True(c.HasPrefab(name));
        }
    }
}
