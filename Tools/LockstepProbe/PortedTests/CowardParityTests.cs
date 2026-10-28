using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class CowardParityTests {
        [PortedTest("game/domain/object/component/mob/statemachine/attacker/non_attacking_coward_mob.rs")]
        public static void start_replaces_the_attack_range_with_the_preferred_range() {
            var context = new SimulationContext(0);
            var owner = new SimObject(context, Master.LeftPlayer, "ZapMouse", new Vector3(1, 0, 1));
            var mob = new NonAttackingCowardMob(owner, 10, 1, 1, 1, 3, 1, 4);
            Assert.Equal((Fix64)0.5m, mob.AttackRange); mob.Start();
            Assert.Equal((Fix64)4, mob.AttackRange); Assert.True(mob.Behavior(owner));
        }
        [PortedTest("tests/physic_system_wall_collision.rs")]
        public static void full_map_wall_keeps_coward_inside_during_actual_panic_state() {
            var context = new SimulationContext(0) { DeltaTime = (Fix64)0.1m };
            context.RegisterPrefab("ZapMouse", _ => { }); TerrainPrefabs.Register(context);
            var owner = context.Spawn(Master.LeftPlayer, "ZapMouse", new Vector3((Fix64)0.4m, 0, 5));
            owner.AddComponent(new RigidBody(owner, 2));
            var mob = new CowardMob(owner, 10, 2, 1, 1, 1, 3, 1); owner.AddComponent(mob);
            owner.AddComponent(new LightningSummonEffectReceiver(owner));
            owner.AddCollider(new CircleCollider(owner, (Fix64)0.5m));
            owner.FlushComponents(); owner.SetStatus(ObjectStatus.Idle); context.FlushObjects();
            var threat = new SimObject(context, Master.RightPlayer, "FireSpirit", new Vector3(2, 0, 5)); mob.SetPanic(threat);
            context.Spawn(Master.None, "Wall", Vector3.Zero); context.FlushObjects();
            var physics = new BattlePhysicsSystem();
            for (int i = 0; i < 5; i++) {
                owner.Update(); Assert.True(owner.HasEffect(VisualEffect.Panic)); physics.Update(context);
                Assert.Equal(false, owner.IsDestroyed); Assert.True(owner.Position.X >= Fix64.Zero);
            }
        }
    }
}
