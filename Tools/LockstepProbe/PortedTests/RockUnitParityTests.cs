using System.Collections.Generic;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class RockUnitParityTests {
        private const string Source = "game/domain/object/prefab/implement/rock/mini_rock_prefab_initializer.rs";
        private sealed class Receiver : SimComponent, IDamageable, IEffectReceiver {
            public readonly List<Hit> Hits = new List<Hit>(); public readonly List<Vector3> Directions = new List<Vector3>(); public Fix64 Strength;
            public Receiver(SimObject owner) : base(owner) { }
            public void OnDamaged(Hit hit) { Hits.Add(hit); }
            public void OnDamagedWithDelay(Hit hit, Fix64 delay) { Hits.Add(hit); }
            public void Receive(EffectApplication value) { }
            public void Receive(VisualEffect value) { }
            public void Receive(VisualEffect value, Fix64 duration) { }
            public void ReceiveDirectional(VisualEffect value, Vector3 direction, Fix64 strength) { Assert.Equal(VisualEffect.Knockback, value); Directions.Add(direction); Strength = strength; }
        }
        private static SimulationContext Context(int hp = 17, Fix64? radius = null) {
            var c = new SimulationContext(); c.RegisterPrefab("Object", _ => { }); RockUnitPrefabs.Register(c); RockRemnantPrefabs.Register(c); c.InitializationFailed += (o, e) => throw e;
            foreach (var key in new[] { "mini_rock", "rock_golem" }) {
                foreach (var p in new[] { "mass", "speed", "damage", "attack_interval" }) c.GameParameters[key + "." + p] = 0;
                c.GameParameters[key + ".hp"] = hp; c.GameParameters[key + ".radius"] = radius ?? (Fix64)0.75m;
            }
            return c;
        }
        [PortedTest(Source)] public static void advertises_mini_rock_prefab_type() { Assert.Equal("MiniRock", Context().Spawn(Master.LeftPlayer, "MiniRock", Vector3.Zero).Prefab); }
        [PortedTest(Source)] public static void does_not_leave_a_remnant_because_it_is_small() { Assert.True(!Context(0, 0).Spawn(Master.LeftPlayer, "MiniRock", Vector3.Zero).HasComponent<RockDeathRemnant>()); }
        [PortedTest(Source)] public static void remains_hp_based_slime_with_solid_circle_collider() {
            var o = Context().Spawn(Master.LeftPlayer, "MiniRock", Vector3.Zero); var mob = o.GetComponent<Slime>(); Assert.Equal(17, mob.Hp); Assert.Equal(17, mob.MaxHp); Assert.Equal((Fix64)0.75m, o.FirstCircleRadius(false).Value); Assert.True(!o.HasComponent<TimedSelfDestroyer>());
        }
        [PortedTest("game/domain/object/component/mob/statemachine/attacker/rock_golem_mob.rs")] public static void melee_attack_damages_and_lightly_knocks_target_away() {
            var c = Context(); c.DeltaTime = (Fix64)1.1m; var owner = c.Spawn(Master.LeftPlayer, "Object", new Vector3(1, 0, 1)); owner.Elements.AddNative(Element.Rock);
            var target = c.Spawn(Master.RightPlayer, "Object", new Vector3((Fix64)1.18m, 0, (Fix64)1.24m)); var receiver = new Receiver(target); target.AddComponentImmediately(receiver);
            var mob = new RockGolemMob(owner, 20, 1, 1, 7, 1); mob.Target = target; mob.SetAttack(); mob.Update();
            Assert.Equal(1, receiver.Hits.Count); Assert.Equal(7, receiver.Hits[0].Damage); Assert.Equal(0, receiver.Hits[0].AttackerId); Assert.Sequence(new[] { Element.Rock }, receiver.Hits[0].Elements);
            Assert.Equal(1, receiver.Directions.Count); Assert.Equal((Fix64)0.25m, receiver.Strength); Assert.Near((Fix64)0.6m, receiver.Directions[0].X, (Fix64)0.0001m); Assert.Equal(Fix64.Zero, receiver.Directions[0].Y); Assert.Near((Fix64)0.8m, receiver.Directions[0].Z, (Fix64)0.0001m); Assert.Equal(ObjectStatus.Attack, owner.Status);
        }
        public static void CheckActualDeaths() {
            var c = Context(); var mini = c.Spawn(Master.LeftPlayer, "MiniRock", new Vector3(3, 0, 5)); var golem = c.Spawn(Master.RightPlayer, "RockGolem", new Vector3(8, 0, 5)); c.FlushObjects();
            mini.GetComponent<Slime>().ApplyDamage(new Hit(17, 0, Element.None)); Assert.Equal(0, c.PendingObjects.Length);
            golem.GetComponent<RockGolemMob>().ApplyDamage(new Hit(17, 0, Element.None)); Assert.Equal(1, c.PendingObjects.Length); Assert.Equal("RockRemnant", c.PendingObjects[0].Prefab); Assert.Equal(Master.RightPlayer, c.PendingObjects[0].Master);
        }
    }
}
