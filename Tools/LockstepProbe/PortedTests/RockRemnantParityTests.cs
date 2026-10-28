using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class RockRemnantParityTests {
        private const string Source = "game/domain/object/component/effect/rock_death_remnant.rs";
        private const string SmallSource = "game/domain/object/prefab/implement/rock/rock_remnant_prefab_initializer.rs";
        private const string MediumSource = "game/domain/object/prefab/implement/rock/medium_rock_remnant_prefab_initializer.rs";
        private static SimulationContext Context() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; c.RegisterPrefab("Object", _ => { });
            c.GameParameters["mini_rock.radius"] = (Fix64)0.75m; c.GameParameters["medium_rock_remnant.radius"] = 1; RockRemnantPrefabs.Register(c); return c;
        }
        private static SimObject SourceObject(SimulationContext c, Master master = Master.LeftPlayer) { var o = c.Spawn(master, "Object", new Vector3(3, 2, 4)); o.Elements.AddNative(Element.Rock); c.FlushObjects(); return o; }
        private static SimObject Remnant(string prefab = "RockRemnant") => Context().Spawn(Master.LeftPlayer, prefab, Vector3.Zero);
        [PortedTest(Source)] public static void lethal_combat_damage_creates_one_owned_rock_remnant_at_the_death_position() {
            var c = Context(); var o = SourceObject(c); o.AddComponentImmediately(new RockDeathRemnant(o)); var mob = new DummyMob(o, 10); o.AddComponentImmediately(mob); mob.ApplyDamage(new Hit(10, 0, Element.None));
            Assert.Equal(1, c.PendingObjects.Length); var remnant = c.PendingObjects[0]; Assert.Equal("RockRemnant", remnant.Prefab); Assert.Equal(Master.LeftPlayer, remnant.Master); Assert.Equal(new Vector3(3, 2, 4), remnant.Position);
        }
        [PortedTest(Source)] public static void repeated_combat_death_callback_creates_only_one_rock_remnant() { var c = Context(); var r = new RockDeathRemnant(SourceObject(c, Master.RightPlayer)); r.OnCombatDeath(); r.OnCombatDeath(); Assert.Equal(1, c.PendingObjects.Length); }
        [PortedTest(Source)] public static void medium_source_creates_medium_rock_remnant() { var c = Context(); new RockDeathRemnant(SourceObject(c), RockRemnantSize.Medium).OnCombatDeath(); Assert.Equal("MediumRockRemnant", c.PendingObjects[0].Prefab); }
        [PortedTest(Source)] public static void direct_destroy_does_not_create_rock_remnant() { var c = Context(); var o = SourceObject(c); o.AddComponentImmediately(new RockDeathRemnant(o)); o.Destroy(); Assert.Equal(0, c.PendingObjects.Length); }
        [PortedTest(Source)] public static void out_of_bounds_destroy_does_not_create_rock_remnant() { var c = Context(); var o = SourceObject(c); o.AddComponentImmediately(new RockDeathRemnant(o)); o.SetPosition(new Vector3(10000, 0, 10000)); Assert.True(o.IsDestroyed); Assert.Equal(0, c.PendingObjects.Length); }
        [PortedTest(Source)] public static void rock_remnant_size_maps_prefabs_both_ways() {
            Assert.Equal((RockRemnantSize?)RockRemnantSize.Medium, RockRemnantSizes.FromPrefab("MediumRockRemnant")); Assert.True(RockRemnantSizes.FromPrefab("RockGolem") == null);
            Assert.Equal("MiniRock", RockRemnantSize.Small.SummonedPrefab()); Assert.True(RockRemnantSize.Small.BlastRemainderPrefab() == null); Assert.Equal("RockRemnant", RockRemnantSize.Medium.BlastRemainderPrefab());
        }
        [PortedTest(SmallSource)] public static void advertises_rock_remnant_prefab_type() { Assert.Equal("RockRemnant", Remnant().Prefab); }
        [PortedTest(SmallSource)] public static void blocks_path_with_immovable_solid_collider() { var o = Remnant(); Assert.Equal((Fix64)0.75m, o.FirstCircleRadius(false).Value); Assert.Equal(Fix64.Zero, ((CircleCollider)o.Colliders[0]).InverseMass); Assert.True(!o.HasComponent<RigidBody>()); Assert.True(o.HasComponent<ICollidable>()); }
        [PortedTest(SmallSource)] public static void has_no_combat_behaviour() { var o = Remnant(); Assert.True(!o.HasComponent<MobComponent>() && !o.HasComponent<IDamageable>() && o.Elements.Has(Element.Rock)); }
        [PortedTest(SmallSource)] public static void expires_after_twenty_seconds() { var gauge = Remnant().GetComponent<TimedSelfDestroyer>().GetGauge(); Assert.Equal((Fix64)20, gauge.MaxValue); Assert.Equal(GaugeCategory.Ttl, gauge.Category); }
        [PortedTest(SmallSource)] public static void does_not_spawn_another_remnant() { Assert.True(!Remnant().HasComponent<RockDeathRemnant>()); }
        [PortedTest(MediumSource)] public static void is_large_solid_rock_rubble_without_combat_behaviour() {
            var o = Remnant("MediumRockRemnant"); Assert.Equal(Fix64.One, o.FirstCircleRadius(false).Value); Assert.Equal(Fix64.Zero, ((CircleCollider)o.Colliders[0]).InverseMass);
            Assert.True(o.HasComponent<ICollidable>() && o.HasComponent<TimedSelfDestroyer>() && !o.HasComponent<MobComponent>() && !o.HasComponent<IDamageable>() && o.Elements.Has(Element.Rock));
        }
        public static void CheckTowerDeathAndCollision() {
            var c = Context(); c.GameParameters["ground_tower.mass"] = 1; c.GameParameters["ground_tower.radius"] = 1;
            c.GameParameters["ground_tower.hp"] = 10; c.GameParameters["ground_tower.damage"] = 4; c.GameParameters["ground_tower.attack_interval"] = 1; c.GameParameters["ground_tower.attack_range"] = 5; c.GameParameters["ground_tower.duration"] = 10;
            var r = new MagicRegistry(); BuildMagics.Register(r); r.GetBean("tower").Bind(1, "tower", Element.Rock).Run(c, Master.LeftPlayer, null, new Vector3(8, 3, 5));
            var tower = c.PendingObjects[0]; Assert.Equal(new Vector3(8, 0, 5), tower.Position); Assert.True(tower.HasComponent<Tower>()); c.FlushObjects();
            tower.GetComponent<DummyMob>().ApplyDamage(new Hit(10, 0, Element.None)); Assert.Equal(1, c.PendingObjects.Length); var remnant = c.PendingObjects[0]; Assert.True(remnant.HasComponent<StaticObstacle>()); Assert.Equal(tower.Position, remnant.Position);
            var expiring = c.Spawn(Master.LeftPlayer, "GroundTower", new Vector3(12, 0, 5)); c.FlushObjects(); c.DeltaTime = 10; expiring.GetComponent<TimedSelfDestroyer>().Update(); Assert.True(expiring.IsDestroyed); Assert.Equal(0, c.PendingObjects.Length);
            var mover = c.Spawn(Master.RightPlayer, "Object", new Vector3(7, 0, 5)); var body = new RigidBody(mover, 1); mover.AddComponentImmediately(body); mover.AddComponentImmediately(new CommonEffectReceiver(mover)); mover.AddCollider(new CircleCollider(mover, (Fix64)0.5m)); c.FlushObjects(); c.DeltaTime = (Fix64)0.5m;
            body.AddVelocity(new Vector3(2, 0, 0)); new BattlePhysicsSystem().Update(c);
            Assert.Equal(new Vector3(8, 0, 5), remnant.Position); Assert.Near((Fix64)7.5m, mover.Position.X, (Fix64)0.00001m);
        }
    }
}
