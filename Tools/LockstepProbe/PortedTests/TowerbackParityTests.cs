using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class TowerbackParityTests {
        private const string Source = "game/domain/object/component/mob/statemachine/attacker/towerback_mob.rs";
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext { DeltaTime = (Fix64)0.05m };
            public readonly TowerbackMob Mob;
            public Fixture() {
                Context.RegisterPrefab("Object", _ => { }); var tower = Context.Spawn(Master.LeftPlayer, "Object", Vector3.Zero); tower.AddCollider(new CircleCollider(tower, (Fix64)0.5m)); tower.AddComponentImmediately(new RigidBody(tower, 1));
                Mob = new TowerbackMob(tower, 100, 1, 10, (Fix64)0.6m, 20, 5, 1); tower.AddComponentImmediately(Mob); Context.FlushObjects();
            }
            public SimObject Enemy(Fix64 x, bool air = false) {
                var enemy = Context.Spawn(Master.RightPlayer, "Object", new Vector3(x, air ? (Fix64)3 : Fix64.Zero, 0)); enemy.AddCollider(new CircleCollider(enemy, (Fix64)0.3m)); enemy.AddComponentImmediately(new DummyMob(enemy, 100)); Context.FlushObjects(); return enemy;
            }
            public void Run(int frames) { for (int i = 0; i < frames; i++) Mob.Update(); }
        }
        [PortedTest(Source)] public static void picks_an_air_enemy_over_a_closer_ground_enemy() {
            var f = new Fixture(); f.Enemy(1); var air = f.Enemy(4, true); f.Mob.Start(); f.Run(1); Assert.Equal(air, f.Mob.Target);
        }
        [PortedTest(Source)] public static void stands_and_fires_at_an_air_enemy_inside_the_air_range() {
            var f = new Fixture(); var air = f.Enemy(4, true); f.Mob.Start(); f.Run(3); Assert.Equal(air, f.Mob.Target); Assert.True(f.Mob.CurrentState is BehaviorMob.AttackState);
        }
        [PortedTest(Source)] public static void follows_an_air_enemy_just_outside_the_air_range() {
            var f = new Fixture(); var air = f.Enemy((Fix64)6.5m, true); f.Mob.Start(); f.Run(3); Assert.Equal(air, f.Mob.Target); Assert.True(f.Mob.CurrentState is BehaviorMob.MoveState);
        }
        [PortedTest(Source)] public static void ignores_an_air_enemy_beyond_the_chase_range() {
            var f = new Fixture(); var ground = f.Enemy(12); f.Enemy((Fix64)9.5m, true); f.Mob.Start(); f.Run(1); Assert.Equal(ground, f.Mob.Target);
        }
        [PortedTest(Source)] public static void gives_up_on_an_air_enemy_that_flies_out_of_the_chase_range() {
            var f = new Fixture(); var air = f.Enemy((Fix64)6.5m, true); f.Mob.Start(); f.Run(3); Assert.Equal(air, f.Mob.Target); air.SetPosition(new Vector3((Fix64)10.5m, 3, 0)); f.Run(1); Assert.True(f.Mob.CurrentState is BehaviorMob.IdleState);
        }
        [PortedTest(Source)] public static void leaves_a_ground_fight_when_an_air_enemy_arrives() {
            var f = new Fixture(); f.Enemy(1); f.Mob.Start(); f.Run(3); Assert.True(f.Mob.CurrentState is BehaviorMob.AttackState); var air = f.Enemy(4, true); f.Run(24); Assert.Equal(air, f.Mob.Target);
        }
        public static void CheckAttacksAndPrefab() {
            var f = new Fixture(); var ground = f.Enemy(1); var air = f.Enemy(4, true); var splash = f.Enemy((Fix64)4.5m, true); var far = f.Enemy(10, true);
            Assert.True(f.Mob.Behavior(ground)); Assert.Equal(90, ground.GetComponent<DummyMob>().Hp);
            Assert.True(f.Mob.Behavior(air)); Assert.Equal(100, air.GetComponent<DummyMob>().Hp); f.Context.DeltaTime = (Fix64)0.2m;
            air.GetComponent<DummyMob>().Update(); splash.GetComponent<DummyMob>().Update(); far.GetComponent<DummyMob>().Update(); Assert.Equal(80, air.GetComponent<DummyMob>().Hp); Assert.Equal(80, splash.GetComponent<DummyMob>().Hp); Assert.Equal(100, far.GetComponent<DummyMob>().Hp);
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; TowerbackMob.RegisterPrefab(c);
            c.GameParameters["towerback.mass"] = 10; c.GameParameters["towerback.radius"] = (Fix64)0.5m; c.GameParameters["towerback.hp"] = 100;
            c.GameParameters["towerback.sub_speed"] = 1; c.GameParameters["towerback.sub_damage"] = 10; c.GameParameters["towerback.sub_attack_range"] = (Fix64)0.6m;
            c.GameParameters["towerback.damage"] = 20; c.GameParameters["towerback.attack_range"] = 5; c.GameParameters["towerback.attack_interval"] = 1;
            var registry = new MagicRegistry(); BuildMagics.Register(registry); registry.GetBean("towerback").Bind(1, "towerback", Element.Rock).Run(c, Master.LeftPlayer, null, new Vector3(5, 0, 5));
            var tower = c.PendingObjects[0]; Assert.True(tower.HasComponent<TowerbackMob>() && tower.HasComponent<RigidBody>() && tower.HasComponent<ZPhysics>() && tower.HasComponent<CommonEffectReceiver>()); Assert.True(tower.Elements.Has(Element.Rock));
            Assert.Equal(Fix64.One, tower.GetComponent<TowerbackMob>().Speed.Total); Assert.True(!tower.HasComponent<TimedSelfDestroyer>());
        }
    }
}
