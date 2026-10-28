using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class CraterParityTests {
        private const string EmberSource = "game/domain/object/component/magic/crater_ember.rs";
        private static SimulationContext Context() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; c.RegisterPrefab("Object", _ => { }); CraterPrefabs.Register(c); FieldPrefabs.RegisterNatureFields(c);
            c.GameParameters["crater.mass"] = 1; c.GameParameters["crater.radius"] = 1; c.GameParameters["crater.hp"] = 10; c.GameParameters["crater.attack_interval"] = 1; c.GameParameters["crater.duration"] = 5;
            c.GameParameters["crater_ember.radius"] = (Fix64)0.25m; c.GameParameters["crater_ember.damage"] = 3; c.GameParameters["crater_ember.attack_range"] = 4;
            c.GameParameters["fire_field.radius"] = 1; c.GameParameters["fire_field.duration"] = 5; return c;
        }
        [PortedTest(EmberSource)] public static void flies_an_arc_then_lands_on_the_ground_and_leaves_a_neutral_fire_field() {
            var c = Context(); var o = c.Spawn(Master.LeftPlayer, "CraterEmber", new Vector3(5, 0, 5)); var ember = o.GetComponent<CraterEmber>(); c.FlushObjects();
            Assert.True(ember.TravelDuration > 0); c.DeltaTime = ember.TravelDuration / 2; ember.Update(); Assert.Near((Fix64)3, o.Position.Y, (Fix64)0.001m);
            // 홀수 raw duration도 마지막 Q32.32 단위까지 진행한다.
            c.DeltaTime = ember.TravelDuration - c.DeltaTime; ember.Update(); Assert.True(o.IsDestroyed); Assert.Equal(GroundVector.Of(ember.LandingPosition), o.Position);
            Assert.Equal(1, c.PendingObjects.Length); Assert.Equal("FireField", c.PendingObjects[0].Prefab); Assert.Equal(Master.None, c.PendingObjects[0].Master);
        }
        [PortedTest(EmberSource)] public static void the_landing_point_is_between_the_minimum_and_maximum_travel_distance() {
            for (var i = 0; i < 20; i++) {
                var c = Context(); c.Random = new JavaRandom(i); var o = c.Spawn(Master.LeftPlayer, "CraterEmber", Vector3.Zero);
                var distance = o.GetComponent<CraterEmber>().LandingPosition.Length(); Assert.True(distance >= (Fix64)0.9999m && distance <= (Fix64)4.0001m);
            }
        }
        [PortedTest(EmberSource)] public static void damages_the_first_enemy_mob_it_hits_and_does_not_land() {
            var c = Context(); var o = c.Spawn(Master.LeftPlayer, "CraterEmber", Vector3.Zero); var enemy = c.Spawn(Master.RightPlayer, "Object", Vector3.Zero);
            var mob = new DummyMob(enemy, 10); enemy.AddComponentImmediately(mob); c.FlushObjects(); var ember = o.GetComponent<CraterEmber>();
            ember.OnCollisionWithEnemy(enemy); ember.Update(); Assert.True(o.IsDestroyed); Assert.Equal(ObjectStatus.Damaged, enemy.Status); Assert.Equal(7, mob.Hp); Assert.Equal(0, c.PendingObjects.Length);
        }
        [PortedTest("game/domain/object/prefab/implement/build/tests.rs")] public static void crater_carries_the_five_components_the_java_initializer_adds() {
            var o = Context().Spawn(Master.LeftPlayer, "Crater", Vector3.Zero); Assert.Equal(5, o.Components.Length); Assert.True(o.Elements.Has(Element.Fire));
        }
        public static void CheckMagicAndRandom() {
            var rng = new JavaRandom(1); rng.NextFraction(); Assert.Equal(1761283584L, rng.NextFloatFraction().RawValue); Assert.Equal(115427488297881L, rng.State);
            var c = Context(); var registry = new MagicRegistry(); BuildMagics.Register(registry); registry.GetBean("crater").Bind(1, "crater", Element.Fire).Run(c, Master.LeftPlayer, null, new Vector3(5, 0, 5));
            var crater = c.PendingObjects[0]; c.FlushObjects(); c.DeltaTime = 1; crater.Update(); Assert.Equal(1, c.PendingObjects.Length);
            var emberObject = c.PendingObjects[0]; var ember = emberObject.GetComponent<CraterEmber>(); Assert.Equal("CraterEmber", emberObject.Prefab); Assert.Equal(Master.LeftPlayer, emberObject.Master); c.FlushObjects();
            c.DeltaTime = ember.TravelDuration; ember.Update(); var field = c.PendingObjects[0]; Assert.True(field.HasComponent<EffectProvider>()); Assert.True(field.Elements.Has(Element.Fire));
            ember.Update(); Assert.Equal(1, c.PendingObjects.Length);
            // 아군/비전투 객체를 무시하고 첫 Mob만 한 번 타격한다.
            var shot = c.Spawn(Master.LeftPlayer, "CraterEmber", new Vector3(5, 0, 5)); var attack = shot.GetComponent<CraterEmber>();
            var ally = c.Spawn(Master.LeftPlayer, "Object", Vector3.Zero); var friendly = new DummyMob(ally, 10); ally.AddComponentImmediately(friendly);
            var target = c.Spawn(Master.RightPlayer, "Object", Vector3.Zero); attack.OnCollisionWithEnemy(target); attack.OnCollisionWithEnemy(ally); Assert.True(!shot.IsDestroyed); Assert.Equal(10, friendly.Hp);
            var first = new DummyMob(target, 10); var second = new DummyMob(target, 10); target.AddComponentImmediately(first); target.AddComponentImmediately(second);
            attack.OnCollisionWithEnemy(target); attack.OnCollisionWithEnemy(target); Assert.Equal(7, first.Hp); Assert.Equal(10, second.Hp);
        }
    }
}
