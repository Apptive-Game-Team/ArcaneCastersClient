using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class ElementalNestChecks {
        public static void Run() {
            CheckEmberSpirit();
            foreach (var element in new[] { Element.Fire, Element.Rock, Element.Wind }) {
                var name = element.ToString(); var key = name.ToLowerInvariant(); var c = new SimulationContext { DeltaTime = 1 }; c.InitializationFailed += (o, e) => throw e;
                ElementalNests.Register(c); FieldPrefabs.RegisterNatureFields(c); c.RegisterPrefab("Object", _ => { });
                c.GameParameters[key + "_summon.radius"] = 1; c.GameParameters[key + "_summon.hp"] = 20; c.GameParameters[key + "_summon.attack_interval"] = 100;
                c.GameParameters[key + "_slime.mass"] = 1; c.GameParameters[key + "_slime.radius"] = (Fix64)0.4m; c.GameParameters[key + "_slime.hp"] = 30;
                c.GameParameters[key + "_slime.speed"] = 2; c.GameParameters[key + "_slime.damage"] = 4; c.GameParameters[key + "_slime.attack_interval"] = 1;
                c.GameParameters["fire_field.radius"] = 1; c.GameParameters["fire_field.duration"] = 5;
                var registry = new MagicRegistry(); BuildMagics.Register(registry); registry.GetBean(key + "_slime_nest").Bind(1, key, element).Run(c, Master.RightPlayer, null, new Vector3(8, 0, 5));
                var nest = c.PendingObjects[0]; c.FlushObjects(); Assert.Equal(name + "Summon", nest.Prefab); Assert.True(nest.Colliders[0].IsTrigger); Assert.True(nest.Elements.Has(element)); Assert.Equal(1, nest.Components.Length);
                nest.Update(); Assert.Equal(0, c.PendingObjects.Length); nest.Update(); Assert.Equal(1, c.PendingObjects.Length);
                var slime = c.PendingObjects[0]; c.FlushObjects(); Assert.Equal(name + "Slime", slime.Prefab); Assert.Equal(nest.Master, slime.Master); Assert.Equal(nest.Position, slime.Position);
                Assert.True(slime.HasComponent<RigidBody>() && slime.HasComponent<ZPhysics>() && slime.HasComponent<CommonEffectReceiver>()); Assert.True(!slime.Colliders[0].IsTrigger); Assert.True(slime.Elements.Has(element));
                var enemy = c.Spawn(Master.LeftPlayer, "Object", slime.Position); var victim = new DummyMob(enemy, 100); enemy.AddComponentImmediately(victim); c.FlushObjects();
                Assert.True(slime.GetComponent<Slime>().Behavior(enemy)); Assert.Equal(96, victim.Hp);
                if (element == Element.Fire) {
                    slime.SetPosition(new Vector3(9, 0, 5)); slime.GetComponent<PathSpawner>().Update(); Assert.Equal(1, c.PendingObjects.Length); Assert.Equal("FireField", c.PendingObjects[0].Prefab); Assert.Equal(Master.None, c.PendingObjects[0].Master); c.FlushObjects();
                } else Assert.True(!slime.HasComponent<PathSpawner>());
                nest.GetComponent<Spawner>().OnDamaged(new Hit(20, 0, Element.None)); Assert.True(nest.IsDestroyed); Assert.Equal(0, c.PendingObjects.Length);
            }
        }
        private static void CheckEmberSpirit() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; ElementalNests.Register(c); FieldPrefabs.RegisterNatureFields(c); c.RegisterPrefab("Target", _ => { });
            string[] keys = { "mass", "radius", "hp", "speed", "damage", "attack_interval" }; decimal[] values = { 2, 0.4m, 30, 2, 4, 1 };
            for (int i = 0; i < keys.Length; i++) { c.GameParameters["fire_slime." + keys[i]] = (Fix64)values[i]; c.GameParameters["ember_spirit." + keys[i]] = 99; }
            c.GameParameters["fire_field.radius"] = 1; c.GameParameters["fire_field.duration"] = 5;
            var ember = c.Spawn(Master.LeftPlayer, "EmberSpirit", new Vector3(5, 0, 5)); var mob = ember.GetComponent<Slime>(); Assert.Equal("EmberSpirit", ember.Prefab); Assert.Equal(30, mob.MaxHp); Assert.Equal((Fix64)2, mob.Speed.Total); Assert.Equal((Fix64)0.4m, ember.Colliders[0].CircleRadius.Value);
            Assert.True(ember.HasComponent<PathSpawner>() && ember.HasComponent<CommonEffectReceiver>() && ember.Elements.NativeHas(Element.Fire));
            var target = c.Spawn(Master.RightPlayer, "Target", new Vector3(6, 0, 5)); var health = new DummyMob(target, 100); target.AddComponentImmediately(health); c.FlushObjects(); mob.Behavior(target); Assert.Equal(96, health.Hp);
            ember.SetPosition(new Vector3(6, 0, 5)); ember.GetComponent<PathSpawner>().Update(); Assert.Equal(1, c.PendingObjects.Length); Assert.Equal("FireField", c.PendingObjects[0].Prefab); Assert.Equal(Master.None, c.PendingObjects[0].Master);
        }
    }
}
