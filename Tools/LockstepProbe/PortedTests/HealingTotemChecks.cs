using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class HealingTotemChecks {
        public static void Run() {
            var c = new SimulationContext { DeltaTime = 1 }; c.InitializationFailed += (o, e) => throw e;
            HealingTotemPrefab.Register(c); c.RegisterPrefab("Object", _ => { });
            c.GameParameters["healing_totem.mass"] = 1; c.GameParameters["healing_totem.radius"] = (Fix64)0.5m;
            c.GameParameters["healing_totem.hp"] = 20; c.GameParameters["healing_totem.damage"] = -3;
            c.GameParameters["healing_totem.attack_interval"] = 1; c.GameParameters["healing_totem.effect_radius"] = 3; c.GameParameters["healing_totem.duration"] = (Fix64)4.9m;
            SimObject Target(Master master, Fix64 x) {
                var o = c.Spawn(master, "Object", new Vector3(x, 0, 5)); o.AddCollider(new CircleCollider(o, (Fix64)0.5m));
                var mob = new DummyMob(o, 30); o.AddComponentImmediately(mob); mob.ApplyDamage(new Hit(10, 0, Element.None)); return o;
            }
            var enemy = Target(Master.RightPlayer, 5); var ally = Target(Master.LeftPlayer, (Fix64)5.25m); var far = Target(Master.LeftPlayer, 15);
            var registry = new MagicRegistry(); BuildMagics.Register(registry);
            registry.GetBean("healing_totem").Bind(1, "healing_totem", Element.Nature).Run(c, Master.LeftPlayer, null, new Vector3(5, 0, 5));
            var totem = c.PendingObjects[3]; c.FlushObjects(); var item = totem.GetComponent<Item>(); var heal = totem.GetComponent<Totem>();
            Assert.True(item != null); Assert.True(totem.Elements.Has(Element.Nature) && totem.Elements.Has(Element.Water));
            Assert.Equal((Fix64)4, totem.GetComponent<TimedSelfDestroyer>().GetGauge().MaxValue);
            item.Update(); Assert.True(ReferenceEquals(ally, item.Parent)); Assert.Equal(new Vector3(5, 0, 5), totem.Position);
            heal.Update(); Assert.Equal(23, ally.GetComponent<DummyMob>().Hp); Assert.Equal(20, enemy.GetComponent<DummyMob>().Hp); Assert.Equal(20, far.GetComponent<DummyMob>().Hp);
            ally.SetPosition(new Vector3(9, 2, 6)); item.Update(); Assert.Equal(ally.Position, totem.Position);
            ally.Destroy(); ally.SetPosition(new Vector3(10, 2, 6)); item.Update(); Assert.Equal(ally.Position, totem.Position);
            var neutral = Target(Master.None, 12); var loose = Target(Master.LeftPlayer, 12); var pickup = new Item(loose); loose.AddComponentImmediately(pickup); c.FlushObjects(); pickup.Update(); Assert.True(ReferenceEquals(neutral, pickup.Parent));
            var noCollider = c.Spawn(Master.LeftPlayer, "Object", Vector3.Zero); var empty = new Item(noCollider); empty.Update(); Assert.True(empty.Parent == null);
            var lifetime = totem.GetComponent<TimedSelfDestroyer>(); for (int i = 0; i < 4; i++) lifetime.Update(); Assert.True(totem.IsDestroyed);
        }
    }
}
