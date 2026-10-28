using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class FireLordSpiritChecks {
        public static void Run() {
            foreach (int rate in new[] { 20, 60 }) {
                var context = new SimulationContext(0); context.SetTickRate(rate); StandardPrefabs.Register(context);
                foreach (var key in new[] { "fire_lord_spirit", "fire_child_spirit" }) {
                    context.GameParameters[key + ".mass"] = 2; context.GameParameters[key + ".radius"] = (Fix64)0.5m;
                    context.GameParameters[key + ".hp"] = 100; context.GameParameters[key + ".speed"] = 2;
                }
                context.GameParameters["fire_child_spirit.damage"] = 7;
                context.GameParameters["fire_child_spirit.attack_interval"] = 1;
                context.GameParameters["fire_child_spirit.attack_range"] = 4;
                context.RegisterPrefab("TestTarget", owner => { owner.AddCollider(new CircleCollider(owner, (Fix64)0.5m)); owner.AddComponentImmediately(new DummyMob(owner, 100)); });
                var enemy = context.Spawn(Master.RightPlayer, "TestTarget", new Vector3(10, 0, 5)); context.FlushObjects();
                var lord = context.Spawn(Master.LeftPlayer, "FireLordSpirit", new Vector3(1, 3, 5));
                Assert.Equal(false, lord.IsDestroyed); Assert.True(lord.Elements.Has(Element.Fire) && lord.Elements.Has(Element.Wind));
                var mob = lord.GetComponent<KeepDistanceMob>(); var body = lord.GetComponent<RigidBody>();
                mob.Update(); Assert.Equal(new Vector3(2, 0, 0), body.ConsumeVelocity());
                enemy.SetPosition(new Vector3((Fix64)6.25m, 0, 5)); mob.Update(); Assert.Equal(Vector3.Zero, body.ConsumeVelocity());
                enemy.SetPosition(new Vector3(2, 0, 5)); mob.Update(); Assert.Equal(Vector3.Zero, body.ConsumeVelocity());
                enemy.SetMaster(Master.LeftPlayer); enemy.SetPosition(new Vector3(10, 0, 5)); mob.Update(); Assert.Equal(Vector3.Zero, body.ConsumeVelocity());
                enemy.SetMaster(Master.RightPlayer);
                var spawner = lord.GetComponent<LimitedSequenceSpawner>();
                for (int tick = 1; tick <= rate * 30; tick++) {
                    spawner.Update();
                    Assert.Equal(System.Math.Min(5, tick / (rate * 5)), context.PendingObjects.Count(owner => owner.Prefab == "FireChildSpirit"));
                }
                var children = context.PendingObjects.Where(owner => owner.Prefab == "FireChildSpirit").ToArray();
                Assert.True(children.All(owner => !owner.IsDestroyed && owner.Master == Master.LeftPlayer && owner.Elements.Has(Element.Fire) && !owner.Elements.Has(Element.Wind)));
                var child = children[0]; Assert.Near((Fix64)0.65m, child.Position.X, (Fix64)0.000000001m); Assert.Near((Fix64)2.825m, child.Position.Y, (Fix64)0.000000001m); Assert.Equal((Fix64)5, child.Position.Z);
                Assert.True(child.GetComponent<ProjectileRangeAttackMob>().Behavior(enemy)); Assert.Equal(93, enemy.GetComponent<DummyMob>().Hp);
                var shot = context.DrainEvents().Single(item => item.Type == "FireShot"); Assert.Equal((Fix64)0.4m, shot.Duration); Assert.Equal(child.Id, shot.SourceId); Assert.Equal(enemy.Id, shot.TargetId);
                mob.OnDeath(); Assert.True(lord.IsDestroyed);
            }
        }
    }
}
