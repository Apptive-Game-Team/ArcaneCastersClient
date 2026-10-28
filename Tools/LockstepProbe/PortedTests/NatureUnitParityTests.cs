using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class NatureUnitParityTests {
        private const string SlimeSource = "game/domain/object/component/mob/statemachine/attacker/slime.rs";
        private static SimObject SlimeObject(Element element, out SimulationContext context) {
            context = new SimulationContext(0); foreach (string name in new[] { "ElectricSlime", "ElectricField", "FireField" }) context.RegisterPrefab(name, _ => { });
            var owner = context.Spawn(Master.LeftPlayer, "ElectricSlime", Vector3.Zero); owner.Elements.AddNative(element); context.FlushObjects(); return owner;
        }
        [PortedTest(SlimeSource)] public static void lightning_death_creates_one_electric_field() {
            var owner = SlimeObject(Element.Lightning, out var context); new Slime(owner, 1, 1, 0, 0, 1).ApplyDamage(new Hit(1, 0, Element.None));
            Assert.Equal(1, context.PendingObjects.Length); Assert.Equal("ElectricField", context.PendingObjects[0].Prefab); Assert.True(owner.IsDestroyed);
        }
        [PortedTest(SlimeSource)] public static void elemental_death_field_belongs_to_the_slime_owner() {
            var owner = SlimeObject(Element.Fire, out var context); new Slime(owner, 1, 1, 0, 0, 1).ApplyDamage(new Hit(1, 0, Element.None));
            Assert.Equal(1, context.PendingObjects.Length); Assert.Equal("FireField", context.PendingObjects[0].Prefab); Assert.Equal(Master.LeftPlayer, context.PendingObjects[0].Master); Assert.True(owner.IsDestroyed);
        }
        [PortedTest(SlimeSource)] public static void direct_on_death_leaves_no_field() {
            var owner = SlimeObject(Element.Fire, out var context); new Slime(owner, 1, 1, 0, 0, 1).OnDeath(); Assert.Equal(0, context.PendingObjects.Length);
        }
        [PortedTest("game/domain/object/prefab/implement/field/abstract_field_prefab_initializer.rs")] public static void grounds_the_field_and_adds_effect_provider_then_lifetime() {
            var context = new SimulationContext(0); context.GameParameters["fire_field.radius"] = 2; context.GameParameters["fire_field.duration"] = 4;
            var field = new SimObject(context, Master.LeftPlayer, "FireField", new Vector3(1, 5, 2)); FieldPrefabs.Initialize(field, "fire_field", Element.Fire, VisualEffect.Burn);
            Assert.Equal(new Vector3(1, 0, 2), field.Position); Assert.Equal((Fix64)2, field.Colliders[0].CircleRadius.Value); Assert.True(field.Colliders[0].IsTrigger);
            Assert.True(field.Elements.Has(Element.Fire)); Assert.True(field.HasComponent<EffectProvider>()); Assert.Equal((Fix64)4, field.GetComponent<TimedSelfDestroyer>().GetGauge().MaxValue);
        }
        public static void CheckEvolvedUnitCombat() {
            var context = new SimulationContext(0) { DeltaTime = (Fix64)0.1m, SuppressDeathFields = true };
            var failures = new System.Collections.Generic.List<System.Exception>();
            context.InitializationFailed += (owner, error) => failures.Add(error);
            foreach (string key in new[] { "leaf_slime", "seed_spirit", "vine_spirit" }) {
                context.GameParameters[key + ".mass"] = 1; context.GameParameters[key + ".radius"] = (Fix64)0.4m; context.GameParameters[key + ".hp"] = 20;
                context.GameParameters[key + ".speed"] = 2; context.GameParameters[key + ".damage"] = 3; context.GameParameters[key + ".attack_interval"] = (Fix64)0.2m; context.GameParameters[key + ".attack_range"] = 3;
            }
            foreach (string key in new[] { "leaf_field", "fire_field", "vine" }) { context.GameParameters[key + ".radius"] = 1; context.GameParameters[key + ".duration"] = (Fix64)0.5m; }
            context.GameParameters["vine.damage"] = 4; context.GameParameters["game.snare_duration"] = 1; context.GameParameters["game.snare_fire_damage"] = 2; context.GameParameters["game.snare_slow_percent"] = (Fix64)0.5m;
            context.GameParameters["game.leaf_field_heal_duration"] = 1; context.GameParameters["game.leaf_field_heal_amount"] = 0;
            context.GameParameters["game.burn_duration"] = 1; context.GameParameters["game.burn_total_damage"] = 1;
            context.GameParameters["seed_nest.radius"] = 1; context.GameParameters["seed_nest.hp"] = 20;
            context.GameParameters["seed_nest.attack_interval"] = (Fix64)0.1m; context.GameParameters["seed_nest.duration"] = 5;
            NatureUnitPrefabs.Register(context); FieldPrefabs.RegisterNatureFields(context); VinePrefabs.Register(context);
            var nest = context.Spawn(Master.LeftPlayer, "SeedNest", new Vector3(2, 0, 5)); context.FlushObjects();
            nest.GetComponent<Spawner>().Update(); var seed = context.PendingObjects.Single(); Assert.Equal("SeedSpirit", seed.Prefab); Assert.Equal(nest.Position, seed.Position); Assert.Equal(nest.Master, seed.Master);
            nest.Destroy(); context.FlushObjects(); Assert.True(seed.HasComponent<Slime>());
            seed.SetPosition(new Vector3((Fix64)3.1m, 0, 5)); seed.GetComponent<PathSpawner>().Update(); var field = context.PendingObjects.Single(); Assert.Equal("LeafField", field.Prefab); Assert.Equal(Master.None, field.Master);
            field.GetComponent<LeafFieldEffectReceiver>().Receive(VisualEffect.Burn); Assert.True(field.IsDestroyed); Assert.True(context.PendingObjects.Any(owner => owner.Prefab == "FireField" && owner.Master == Master.None));
            context.FlushObjects(); context.Spawn(Master.LeftPlayer, "Vine", seed.Position); context.FlushObjects(); Assert.True(seed.IsDestroyed);
            var evolved = context.Objects.Single(owner => owner.Prefab == "VineSpirit"); Assert.True(evolved.HasComponent<EffectProvideProjectileRangeAttackMob>()); Assert.True(!evolved.IsDestroyed);
            context.RegisterPrefab("Enemy", owner => { owner.AddComponentImmediately(new DummyMob(owner, 100)); owner.AddComponentImmediately(new CommonEffectReceiver(owner)); owner.AddCollider(new CircleCollider(owner, (Fix64)0.5m)); });
            var enemy = context.Spawn(Master.RightPlayer, "Enemy", new Vector3(11, 0, 5)); context.FlushObjects();
            var updates = new ComponentUpdateSystem(); var physics = new BattlePhysicsSystem(); bool hit = false;
            updates.Failed += (owner, error) => failures.Add(error); physics.CollisionFailed += (owner, error) => failures.Add(error);
            for (int i = 0; i < 100; i++) { context.FrameNumber++; updates.Update(context); physics.Update(context); context.FlushObjects(); if (enemy.GetComponent<DummyMob>().Hp < 100) { hit = true; break; } }
            Assert.True(hit); Assert.True(evolved.Position.X > (Fix64)3.1m); Assert.True(context.DrainEvents().Any(item => item.Type == "NatureShot"));
            Assert.True(enemy.GetComponent<CommonEffectReceiver>().GetEffectByKey(StatusEffectKey.SnaredReceive) != null);
            if (failures.Count != 0) throw new System.AggregateException(failures);
        }
    }
}
