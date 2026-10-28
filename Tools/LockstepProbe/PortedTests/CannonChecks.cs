using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class CannonChecks {
        public static void Run() {
            var c = new SimulationContext { DeltaTime = 1 }; c.InitializationFailed += (o, e) => throw e;
            c.RegisterPrefab("Object", _ => { }); Cannon.RegisterPrefab(c); RockRemnantPrefabs.Register(c);
            c.GameParameters["ground_cannon.mass"] = 100; c.GameParameters["ground_cannon.radius"] = (Fix64)0.5m;
            c.GameParameters["ground_cannon.hp"] = 40; c.GameParameters["ground_cannon.damage"] = 4;
            c.GameParameters["ground_cannon.attack_interval"] = 1; c.GameParameters["ground_cannon.attack_range"] = 3;
            c.GameParameters["ground_cannon.duration"] = 2; c.GameParameters["mini_rock.radius"] = (Fix64)0.25m;
            var registry = new MagicRegistry(); BuildMagics.Register(registry);
            registry.GetBean("cannon").Bind(1, "cannon", Element.Rock).Run(c, Master.LeftPlayer, null, new Vector3(5, 7, 5));
            var owner = c.PendingObjects[0]; var cannon = owner.GetComponent<Cannon>(); c.FlushObjects();
            Assert.Equal(Fix64.Zero, owner.Position.Y); Assert.Equal((Fix64)60, owner.GetComponent<TimedSelfDestroyer>().GetGauge().MaxValue);
            Assert.True(owner.HasComponent<IFrenzyImmune>() && owner.HasComponent<IMindControlImmune>());
            cannon.Update(); Assert.Equal(0, c.DrainEvents().Length);
            DummyMob Target(Master master, Fix64 x, Fix64 y = default) {
                var o = c.Spawn(master, "Object", new Vector3(x, y, 5)); o.AddCollider(new CircleCollider(o, (Fix64)0.25m));
                var mob = new DummyMob(o, 100); o.AddComponentImmediately(mob); return mob;
            }
            var air = Target(Master.RightPlayer, 5, 2); var ally = Target(Master.LeftPlayer, 6);
            var near = Target(Master.RightPlayer, 7); var other = Target(Master.RightPlayer, (Fix64)7.5m); c.FlushObjects();
            c.DeltaTime = (Fix64)0.125m; cannon.Update(); var events = c.DrainEvents();
            Assert.Equal(1, events.Length); Assert.Equal("RockShot", events[0].Type); Assert.Equal(near.Owner.Id, events[0].TargetId); Assert.Equal((Fix64)0.5m, events[0].Duration);
            Assert.Equal(100, near.Hp); c.DeltaTime = (Fix64)0.25m; near.Update(); Assert.Equal(100, near.Hp); near.Update(); Assert.Equal(96, near.Hp);
            air.Update(); ally.Update(); other.Update(); Assert.Equal(100, air.Hp); Assert.Equal(100, ally.Hp); Assert.Equal(100, other.Hp);
            cannon.ApplyDamage(new Hit(1000, 0, Element.None)); Assert.True(owner.IsDestroyed); Assert.Equal(1, c.PendingObjects.Length);
            Assert.Equal("RockRemnant", c.PendingObjects[0].Prefab); Assert.Equal(Master.LeftPlayer, c.PendingObjects[0].Master);
        }
    }
}
