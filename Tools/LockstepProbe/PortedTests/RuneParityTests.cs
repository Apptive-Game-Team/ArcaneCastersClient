using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class RuneParityTests {
        [PortedTest("game/domain/object/component/magic/rune.rs")] public static void damages_every_mob_in_range_once_and_disappears() {
            var c = new SimulationContext(); c.RegisterPrefab("Object", _ => { }); c.GameParameters["rune.attack_range"] = 2; c.GameParameters["rune.damage"] = 7;
            var owner = c.Spawn(Master.LeftPlayer, "Object", Vector3.Zero); var rune = new Rune(owner); var victim = c.Spawn(Master.RightPlayer, "Object", Vector3.Right); var mob = new DummyMob(victim, 20); victim.AddComponentImmediately(mob); victim.AddCollider(new CircleCollider(victim, (Fix64)0.5m)); var wall = c.Spawn(Master.None, "Object", Vector3.Right); wall.AddCollider(new CircleCollider(wall, (Fix64)0.5m)); c.FlushObjects();
            rune.OnCollisionWithEnemy(victim); Assert.Equal(13, mob.Hp); Assert.True(owner.IsDestroyed);
        }
        [PortedTest("game/domain/object/prefab/implement/rune/mod.rs")] public static void every_prefab_resolves_to_its_own_initializer_by_bean_name() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; RunePrefabs.Register(c); c.GameParameters["rune.radius"] = (Fix64)0.5m;
            foreach (var element in new[] { Element.Fire, Element.Lightning, Element.Nature, Element.Rock, Element.Water, Element.Wind }) {
                var name = element + "Rune"; var rune = c.Spawn(Master.LeftPlayer, name, new Vector3(5, 0, 5)); Assert.Equal(name, rune.Prefab); Assert.True(rune.HasComponent<Rune>() && rune.Elements.NativeHas(element)); Assert.True(rune.Colliders[0].IsTrigger); Assert.Equal((Fix64)0.5m, rune.Colliders[0].CircleRadius.Value); Assert.Equal(element.ToString().ToLowerInvariant() + "_rune_prefab", PrefabCatalog.BeanName(name));
            }
        }
    }
}
