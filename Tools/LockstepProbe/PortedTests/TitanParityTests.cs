using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class TitanParityTests {
        private static SimulationContext Context() {
            var c = new SimulationContext { DeltaTime = 1 }; c.InitializationFailed += (o, e) => throw e; c.RegisterPrefab("Object", _ => { }); TitanPrefabs.Register(c); RockRemnantPrefabs.Register(c);
            c.GameParameters["titan_remnant.mass"] = 8; c.GameParameters["titan_remnant.radius"] = (Fix64)1.2m; c.GameParameters["titan_remnant.hp"] = 40;
            c.GameParameters["titan_remnant.attack_interval"] = 2; c.GameParameters["titan_remnant.attack_range"] = 5; c.GameParameters["titan_remnant.duration"] = 15;
            c.GameParameters["titan_fist.radius"] = 2; c.GameParameters["titan_fist.damage"] = 30; c.GameParameters["titan_fist.duration"] = 1; c.GameParameters["medium_rock_remnant.radius"] = 1; return c;
        }
        [PortedTest("game/domain/object/component/mob/simple/titan_remnant_mob.rs")] public static void creates_fist_at_server_selected_enemy_position() {
            var c = Context(); var owner = c.Spawn(Master.LeftPlayer, "Object", Vector3.Zero); owner.AddCollider(new CircleCollider(owner, (Fix64)0.5m));
            var enemy = c.Spawn(Master.RightPlayer, "Object", new Vector3(2, 0, 1)); enemy.AddCollider(new CircleCollider(enemy, (Fix64)0.5m)); enemy.AddComponentImmediately(new DummyMob(enemy, 20)); c.FlushObjects();
            var mob = new TitanRemnantMob(owner, 40, 1, 4); mob.Start(); mob.Update();
            Assert.Equal(1, c.PendingObjects.Length); var fist = c.PendingObjects[0]; Assert.Equal("TitanFist", fist.Prefab); Assert.Equal(Master.LeftPlayer, fist.Master); Assert.Equal(new Vector3(2, 0, 1), fist.Position); Assert.Equal(ObjectStatus.Attack, owner.Status);
        }
        [PortedTest("game/domain/object/prefab/implement/build/tests.rs")] public static void configures_durable_timed_rock_building() {
            var o = Context().Spawn(Master.LeftPlayer, "TitanRemnant", Vector3.Zero);
            Assert.True(o.Elements.Has(Element.Rock)); Assert.Equal((Fix64)1.2m, o.FirstCircleRadius(false).Value);
            Assert.True(o.HasComponent<RigidBody>() && o.HasComponent<TitanRemnantMob>() && o.HasComponent<BuildingEffectReceiver>() && o.HasComponent<RockDeathRemnant>());
            var gauge = o.GetComponent<TimedSelfDestroyer>().GetGauge(); Assert.Equal(GaugeCategory.Ttl, gauge.Category); Assert.Equal((Fix64)15, gauge.MaxValue);
        }
        [PortedTest("game/domain/object/prefab/implement/subprefab/titan_fist_prefab_initializer.rs")] public static void configures_rock_area_attack_and_presentation_lifetime() {
            var o = Context().Spawn(Master.LeftPlayer, "TitanFist", Vector3.Zero); Assert.True(o.Elements.Has(Element.Rock)); Assert.Equal(1, o.GetComponents<OnStartAttacker>().Length); Assert.Equal((Fix64)2, o.FirstCircleRadius(true).Value);
            var gauge = o.GetComponent<TimedSelfDestroyer>().GetGauge(); Assert.Equal(Fix64.One, gauge.MaxValue); Assert.Equal(GaugeCategory.Ttl, gauge.Category);
        }
        public static void CheckActualAttackAndDeath() {
            var c = Context(); var registry = new MagicRegistry(); BuildMagics.Register(registry); registry.GetBean("titan_remnant").Bind(1, "titan_remnant", Element.Rock).Run(c, Master.LeftPlayer, null, new Vector3(5, 0, 5)); var owner = c.PendingObjects[0];
            SimObject Target(Master master, Fix64 x) { var o = c.Spawn(master, "Object", new Vector3(x, 1, 5)); o.AddCollider(new CircleCollider(o, (Fix64)0.5m)); o.AddComponentImmediately(new DummyMob(o, 100)); return o; }
            var enemy = Target(Master.RightPlayer, 7); var ally = Target(Master.LeftPlayer, 7); var far = Target(Master.RightPlayer, 15); c.FlushObjects();
            var mob = owner.GetComponent<TitanRemnantMob>(); mob.Update(); Assert.Equal(0, c.PendingObjects.Length); mob.Update(); var fist = c.PendingObjects[0];
            Assert.Equal(new Vector3(7, 0, 5), fist.Position); Assert.Equal(70, enemy.GetComponent<DummyMob>().Hp); Assert.Equal(100, ally.GetComponent<DummyMob>().Hp); Assert.Equal(100, far.GetComponent<DummyMob>().Hp);
            fist.GetComponent<TimedSelfDestroyer>().Update(); Assert.True(fist.IsDestroyed); Assert.Equal(70, enemy.GetComponent<DummyMob>().Hp); c.FlushObjects();
            mob.ApplyDamage(new Hit(40, 0, Element.None)); Assert.Equal(1, c.PendingObjects.Length); Assert.Equal("MediumRockRemnant", c.PendingObjects[0].Prefab); Assert.True(c.PendingObjects[0].HasComponent<StaticObstacle>());
        }
    }
}
