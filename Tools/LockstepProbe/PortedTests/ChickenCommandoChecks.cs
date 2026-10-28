using System;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class ChickenCommandoChecks {
        private sealed class CountingState : IMobState {
            public int Updates;
            public void OnEnter() { }
            public void OnExit() { }
            public void OnUpdate() { Updates++; }
        }
        public static void Run() {
            var c = new SimulationContext { DeltaTime = (Fix64)0.125m }; c.InitializationFailed += (o, e) => throw e;
            ChickenCommandoMob.RegisterPrefab(c); c.RegisterPrefab("Target", _ => { });
            c.GameParameters["chicken_commando.mass"] = 1; c.GameParameters["chicken_commando.fall_gravity"] = 8;
            c.GameParameters["chicken_commando.radius"] = (Fix64)0.3m; c.GameParameters["chicken_commando.hp"] = 20;
            c.GameParameters["chicken_commando.speed"] = 2; c.GameParameters["chicken_commando.damage"] = 3;
            c.GameParameters["chicken_commando.attack_interval"] = (Fix64)0.25m; c.GameParameters["chicken_commando.spawn_height"] = 4;
            var registry = new MagicRegistry(); DropMagics.Register(registry);
            registry.GetBean("chicken_commando").Bind(1, "chicken_commando", Element.Wind).Run(c, Master.LeftPlayer, null, new Vector3(5, 99, 5));
            var chicken = c.PendingObjects[0]; Assert.Equal(new Vector3(5, 4, 5), chicken.Position);
            var mob = chicken.GetComponent<ChickenCommandoMob>(); var state = new CountingState(); mob.SetState(state);
            chicken.SetPosition(new Vector3(5, (Fix64)0.05m + Fix64.FromRaw(1), 5)); mob.Update(); Assert.Equal(0, state.Updates);
            chicken.SetPosition(new Vector3(5, (Fix64)0.05m, 5)); mob.Update(); Assert.Equal(1, state.Updates);
            chicken.SetPosition(new Vector3(5, 4, 5)); mob.SetIdle();
            var enemy = c.Spawn(Master.RightPlayer, "Target", new Vector3((Fix64)5.9m, 0, 5)); enemy.AddCollider(new CircleCollider(enemy, (Fix64)0.3m)); enemy.AddComponentImmediately(new DummyMob(enemy, 100));
            c.FlushObjects(); var updates = new ComponentUpdateSystem(); var physics = new BattlePhysicsSystem(); int ticks = 0;
            while (chicken.Position.Y > (Fix64)0.05m && ticks++ < 40) {
                updates.Update(c); Assert.True(mob.Target == null); Assert.Equal(100, enemy.GetComponent<DummyMob>().Hp); physics.Update(c); c.FlushObjects();
            }
            Assert.True(ticks < 40); Assert.Equal(20, mob.Hp); Assert.Equal(Fix64.Zero, chicken.Position.Y);
            for (int i = 0; i < 20 && enemy.GetComponent<DummyMob>().Hp == 100; i++) { updates.Update(c); physics.Update(c); c.FlushObjects(); }
            Assert.True(enemy.GetComponent<DummyMob>().Hp < 100); Assert.Equal(20, mob.Hp);
            Assert.Sequence(new[] { Element.Wind }, chicken.Elements.Total);
        }
    }
}
