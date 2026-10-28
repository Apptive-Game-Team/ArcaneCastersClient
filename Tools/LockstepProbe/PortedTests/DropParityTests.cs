using System.Collections.Generic;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class DropParityTests {
        private const string Source = "game/domain/object/component/magic/drop.rs";
        private const string LeafSource = "game/domain/object/component/magic/leafair.rs";
        private sealed class Recorder : SimComponent, IDamageable {
            public readonly List<Hit> Hits = new List<Hit>(); public Recorder(SimObject owner) : base(owner) { }
            public void OnDamaged(Hit hit) { Hits.Add(hit); } public void OnDamagedWithDelay(Hit hit, Fix64 delay) { Hits.Add(hit); }
        }
        private static SimulationContext Context() { var c = new SimulationContext(1) { DeltaTime = 0 }; c.RegisterPrefab("Object", _ => { }); return c; }
        private static SimObject Object(SimulationContext c, Master master) { var o = c.Spawn(master, "Object", new Vector3(3, 3, 3)); o.AddCollider(new CircleCollider(o, (Fix64)0.5m)); c.FlushObjects(); return o; }
        [PortedTest(Source)] public static void falls_at_five_units_per_second_and_is_destroyed_below_the_ground() {
            var c = Context(); c.DeltaTime = (Fix64)0.5m; var owner = Object(c, Master.LeftPlayer); var drop = new Drop(owner, 4);
            drop.Update(); Assert.Equal(new Vector3(3, (Fix64)0.5m, 3), owner.Position); Assert.True(!owner.IsDestroyed); drop.Update(); Assert.True(owner.IsDestroyed);
        }
        [PortedTest(Source)] public static void hits_the_first_enemy_it_touches_with_its_element_and_vanishes() {
            var c = Context(); var owner = Object(c, Master.LeftPlayer); owner.Elements.AddNative(Element.Rock); var drop = new Drop(owner, 4); var enemy = Object(c, Master.RightPlayer); var recorder = new Recorder(enemy); enemy.AddComponentImmediately(recorder);
            drop.OnCollisionWithEnemy(enemy); Assert.Equal(1, recorder.Hits.Count); Assert.Equal(4, recorder.Hits[0].Damage); Assert.Sequence(new[] { Element.Rock }, recorder.Hits[0].Elements); Assert.True(owner.IsDestroyed); Assert.Equal(0, drop.Direction);
        }
        [PortedTest(Source)] public static void one_point_of_damage_destroys_it() { var c = Context(); var owner = Object(c, Master.LeftPlayer); var drop = new Drop(owner, 4); owner.AddComponentImmediately(drop); drop.OnDamaged(new Hit(1, 0, Element.None)); Assert.Equal(0, drop.Hp); Assert.True(owner.IsDestroyed); }
        [PortedTest(LeafSource)] public static void heals_an_allied_mob_in_range_and_is_spent() {
            var c = Context(); var owner = Object(c, Master.LeftPlayer); var leaf = new Leafair(owner, 6, 4, 2); owner.AddComponentImmediately(leaf); var ally = Object(c, Master.LeftPlayer); ally.AddComponentImmediately(new DummyMob(ally, 10)); leaf.Update(); Assert.True(owner.IsDestroyed); Assert.Equal(ObjectStatus.Destroyed, owner.Status);
        }
        [PortedTest(LeafSource)] public static void damages_and_snares_an_enemy_in_range() {
            var c = Context(); var owner = Object(c, Master.LeftPlayer); var leaf = new Leafair(owner, 6, 4, 2); owner.AddComponentImmediately(leaf); var enemy = Object(c, Master.RightPlayer); var recorder = new Recorder(enemy); enemy.AddComponentImmediately(recorder); leaf.Update(); Assert.Equal(1, recorder.Hits.Count); Assert.Equal(6, recorder.Hits[0].Damage); Assert.True(owner.IsDestroyed);
        }
        [PortedTest(LeafSource)] public static void ignores_collisions_because_it_works_by_area() {
            var c = Context(); var owner = Object(c, Master.LeftPlayer); var leaf = new Leafair(owner, 6, 4, 2); var enemy = Object(c, Master.RightPlayer); var recorder = new Recorder(enemy); enemy.AddComponentImmediately(recorder); leaf.OnCollisionWithEnemy(enemy); Assert.Equal(0, recorder.Hits.Count); Assert.True(!owner.IsDestroyed);
        }
        public static void CheckLeafairRecoveryAndDropDeath() {
            var c = Context(); DropPrefabs.Register(c); c.GameParameters["leaf_drop.radius"] = 2; c.GameParameters["leaf_drop.damage"] = 4;
            var ally = Object(c, Master.LeftPlayer); var mob = new DummyMob(ally, 20); ally.AddComponentImmediately(mob); mob.ApplyDamage(new Hit(10, 0, Element.None));
            var lifetime = new TimedSelfDestroyer(ally, 10); ally.AddComponentImmediately(lifetime); c.DeltaTime = 1; lifetime.Update(); c.DeltaTime = 0;
            var leaf = c.Spawn(Master.LeftPlayer, "Leafair", ally.Position); leaf.GetComponent<Leafair>().Update(); Assert.Equal(14, mob.Hp); Assert.Equal((Fix64)10, lifetime.GetGauge().Value); Assert.True(leaf.IsDestroyed);
            c.GameParameters["meteor_drop.radius"] = 1; c.GameParameters["meteor_drop.damage"] = 4; var meteor = c.Spawn(Master.RightPlayer, "MeteorDrop", new Vector3(5, 5, 5)); c.FlushObjects();
            meteor.GetComponent<Drop>().OnDamaged(new Hit(1, 0, Element.None)); Assert.True(meteor.IsDestroyed); Assert.Equal(0, c.PendingObjects.Length);
            c.RemoveObject(ally); c.GameParameters["game.snare_duration"] = 1; c.GameParameters["game.snare_fire_damage"] = 2; c.GameParameters["game.snare_slow_percent"] = (Fix64)0.5m;
            var enemy = Object(c, Master.RightPlayer); var enemyMob = new DummyMob(enemy, 20); enemy.AddComponentImmediately(enemyMob); enemy.AddComponentImmediately(new CommonEffectReceiver(enemy));
            var attackLeaf = c.Spawn(Master.LeftPlayer, "Leafair", enemy.Position); attackLeaf.GetComponent<Leafair>().Update(); Assert.Equal(16, enemyMob.Hp); Assert.True(enemy.GetComponent<CommonEffectReceiver>().GetEffectByKey(StatusEffectKey.SnaredReceive) != null);
        }
    }
}
