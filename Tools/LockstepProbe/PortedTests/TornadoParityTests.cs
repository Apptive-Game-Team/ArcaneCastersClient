using System.Collections.Generic;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class TornadoParityTests {
        private const string Source = "game/domain/object/component/magic/tornado.rs";
        private sealed class Receiver : SimComponent, IDamageable {
            public readonly List<Hit> Hits = new List<Hit>(); public Receiver(SimObject owner) : base(owner) { }
            public void OnDamaged(Hit hit) { Hits.Add(hit); } public void OnDamagedWithDelay(Hit hit, Fix64 delay) { Hits.Add(hit); }
        }
        private static SimulationContext Context() { var c = new SimulationContext { DeltaTime = (Fix64)0.25m }; c.InitializationFailed += (o, e) => throw e; c.RegisterPrefab("Object", _ => { }); return c; }
        [PortedTest(Source)] public static void heads_for_the_middle_of_the_arena_by_default() {
            var c = Context(); var o = c.Spawn(Master.LeftPlayer, "Object", new Vector3(1, 0, 5)); Assert.Equal(Vector3.Right, new Tornado(o, 2, 3, 2, 1).Direction.Value);
        }
        [PortedTest(Source)] public static void lifts_and_spins_a_caught_enemy_and_damages_it_with_a_bonus_per_victim() {
            var c = Context(); var o = c.Spawn(Master.LeftPlayer, "Object", new Vector3(5, 0, 5)); var tornado = new Tornado(o, 0, 3, 2, (Fix64)0.25m);
            var enemy = c.Spawn(Master.RightPlayer, "Object", new Vector3(6, 0, 5)); var receiver = new Receiver(enemy); enemy.AddComponentImmediately(receiver);
            tornado.OnCollisionWithEnemy(enemy); tornado.OnCollisionWithEnemy(enemy); tornado.Update();
            Assert.Near(5, enemy.Position.X, (Fix64)0.001m); Assert.Near(6, enemy.Position.Z, (Fix64)0.001m); Assert.Equal((Fix64)2, enemy.Position.Y); Assert.Equal(1, receiver.Hits.Count); Assert.Equal(5, receiver.Hits[0].Damage); Assert.Sequence(new[] { Element.Wind }, receiver.Hits[0].Elements);
        }
        public static void CheckActualPrefabs() {
            var c = Context(); SpecialSpawnPrefabs.Register(c); RockUnitPrefabs.Register(c); RockRemnantPrefabs.Register(c);
            foreach (var key in new[] { "mini_rock", "rock_golem" }) {
                c.GameParameters[key + ".mass"] = 1; c.GameParameters[key + ".radius"] = (Fix64)0.3m; c.GameParameters[key + ".hp"] = 20; c.GameParameters[key + ".damage"] = 3; c.GameParameters[key + ".speed"] = 1; c.GameParameters[key + ".attack_interval"] = 1;
            }
            c.GameParameters["medium_rock_remnant.radius"] = (Fix64)0.6m; c.GameParameters["earth_call.radius"] = 3;
            var small = c.Spawn(Master.RightPlayer, "RockRemnant", new Vector3(4, 0, 5)); var medium = c.Spawn(Master.None, "MediumRockRemnant", new Vector3(6, 0, 5)); var far = c.Spawn(Master.RightPlayer, "RockRemnant", new Vector3(15, 0, 5)); c.FlushObjects();
            var registry = new MagicRegistry(); SpawnMagics.Register(registry); registry.GetBean("earth_call").Bind(1, "earth_call", Element.Rock).Run(c, Master.LeftPlayer, null, new Vector3(5, 0, 5));
            Assert.Equal(3, c.PendingObjects.Length); Assert.True(c.PendingObjects[1].HasComponent<Slime>()); Assert.True(c.PendingObjects[2].HasComponent<RockGolemMob>()); Assert.True(small.IsDestroyed && medium.IsDestroyed && !far.IsDestroyed);
            var display = c.PendingObjects[0]; c.DeltaTime = 1; display.Update(); Assert.True(display.IsDestroyed); c.FlushObjects();
            c.GameParameters["tornado_strike.radius"] = 2; c.GameParameters["tornado_strike.speed"] = 0; c.GameParameters["tornado_strike.damage"] = 3; c.GameParameters["tornado_strike.attack_interval"] = (Fix64)0.25m; c.GameParameters["tornado_strike.duration"] = 2;
            registry.GetBean("tornado_strike").Bind(2, "tornado_strike", Element.Wind).Run(c, Master.LeftPlayer, null, new Vector3(5, 0, 5)); var owner = c.PendingObjects[0]; var tornado = owner.GetComponent<Tornado>(); Assert.True(owner.Elements.Has(Element.Nature) && owner.Elements.Has(Element.Wind)); Assert.True(owner.Colliders[0].IsTrigger);
            var a = c.Spawn(Master.RightPlayer, "Object", new Vector3(6, 0, 5)); var b = c.Spawn(Master.RightPlayer, "Object", new Vector3(5, 6, 6)); var r1 = new Receiver(a); var r2 = new Receiver(b); a.AddComponentImmediately(r1); b.AddComponentImmediately(r2);
            tornado.OnCollisionWithEnemy(a); tornado.OnCollisionWithEnemy(b); c.DeltaTime = (Fix64)0.25m; tornado.Update(); Assert.Equal(7, r1.Hits[0].Damage); Assert.Equal(7, r2.Hits[0].Damage); Assert.Equal((Fix64)6, b.Position.Y);
            tornado.Update(); tornado.Update(); Assert.Equal((Fix64)5, a.Position.Y); c.DeltaTime = 2; owner.GetComponent<TimedSelfDestroyer>().Update(); Assert.True(owner.IsDestroyed);
        }
    }
}
