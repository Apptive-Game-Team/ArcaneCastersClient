using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class GeneralToadChecks {
        public static void Run() {
            foreach (int rate in new[] { 20, 60 }) {
                var c = new SimulationContext(0); c.SetTickRate(rate); StandardPrefabs.Register(c); c.InitializationFailed += (owner, error) => throw error;
                foreach (var key in new[] { "dimension_toad", "fire_tadpole", "lightning_tadpole", "arcane_infantry" }) {
                    c.GameParameters[key + ".mass"] = 2; c.GameParameters[key + ".radius"] = (Fix64)0.3m;
                    c.GameParameters[key + ".hp"] = 100; c.GameParameters[key + ".speed"] = 2;
                    c.GameParameters[key + ".damage"] = 9; c.GameParameters[key + ".attack_interval"] = 1;
                }
                c.GameParameters["dimension_toad.spawn_interval"] = 2; c.GameParameters["dimension_toad.detection_range"] = 3; c.GameParameters["dimension_toad.panic_duration"] = 1;
                c.GameParameters["fire_tadpole.duration"] = 3; c.GameParameters["lightning_tadpole.duration"] = 3;
                c.GameParameters["arcane_infantry.attack_range"] = (Fix64)1.75m;
                var toad = c.Spawn(Master.LeftPlayer, "DimensionToad", new Vector3(5, 0, 5)); c.FlushObjects();
                Assert.True(toad.GetComponent<MobComponent>() is NonAttackingCowardMob); Assert.Equal(false, toad.Colliders.Single().IsTrigger);
                Assert.Equal((Fix64)4, toad.GetComponent<BehaviorMob>().AttackRange); Assert.Equal((Fix64)2, toad.GetComponent<MobComponent>().Speed.Total);
                Assert.True(toad.Elements.NativeHas(Element.Fire) && toad.Elements.NativeHas(Element.Lightning));
                var sequence = toad.GetComponent<LimitedSequenceSpawner>(); sequence.Update(); Assert.Equal("FireTadpole", c.PendingObjects.Single().Prefab);
                for (int i = 0; i < rate * 12; i++) sequence.Update();
                var children = c.PendingObjects.ToArray(); Assert.Equal(7, children.Length);
                for (int i = 0; i < children.Length; i++) {
                    var child = children[i]; bool lightning = i % 2 == 1;
                    Assert.Equal(lightning ? "LightningTadpole" : "FireTadpole", child.Prefab); Assert.Equal(Master.LeftPlayer, child.Master);
                    Assert.Equal(1, child.GetComponent<MeleeAttackMob>().TargetMask); Assert.Equal(false, child.Colliders.Single().IsTrigger);
                    Assert.Equal(lightning, child.HasComponent<ElectricDeathEnergy>()); Assert.Equal(lightning, child.HasComponent<LightningSummonEffectReceiver>());
                }
                var victim = new SimObject(c, Master.RightPlayer, "Target", new Vector3(6, 0, 5)); victim.AddCollider(new CircleCollider(victim, (Fix64)0.3m)); victim.AddComponentImmediately(new DummyMob(victim, 100));
                toad.GetComponent<BehaviorMob>().Behavior(victim); Assert.Equal(100, victim.GetComponent<DummyMob>().Hp);
                children[0].GetComponent<MeleeAttackMob>().Behavior(victim); children[1].GetComponent<MeleeAttackMob>().Behavior(victim); Assert.Equal(82, victim.GetComponent<DummyMob>().Hp);
                var infantry = c.Spawn(Master.LeftPlayer, "ArcaneInfantry", new Vector3(4, 0, 5));
                var attack = infantry.GetComponent<AttackMob>(); Assert.Equal((Fix64)1.75m, attack.AttackRange); Assert.Equal(1, attack.TargetMask); Assert.True(infantry.HasComponent<CommonEffectReceiver>());
                attack.Behavior(victim); Assert.Equal(73, victim.GetComponent<DummyMob>().Hp);
                foreach (var child in children.Take(2)) {
                    var lifetime = child.GetComponent<TimedSelfDestroyer>();
                    for (int i = 0; i < rate * 3 - 1; i++) lifetime.Update(); Assert.Equal(false, child.IsDestroyed);
                    lifetime.Update(); lifetime.Update(); Assert.True(child.IsDestroyed);
                }
            }
        }
    }
}
