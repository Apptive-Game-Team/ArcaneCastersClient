using System;
using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class BrickGolemParityTests {
        private const string Source = "game/domain/object/component/mob/statemachine/attacker/pve_brick_golem_mob.rs";
        [PortedTest("game/domain/object/prefab/implement/pve/tests.rs")] public static void builds_a_stationary_rock_boss_from_its_own_parameter_object() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; PveBrickGolemMob.RegisterPrefabs(c);
            string[] keys = { "hp", "mass", "radius", "damage", "attack_interval", "attack_range", "effect_radius", "sub_damage", "sub_attack_interval", "sub_attack_range" }; int[] values = { 4000, -1, 2, 30, 4, 12, 2, 12, 3, 4 };
            for (int i = 0; i < keys.Length; i++) c.GameParameters["pve_brick_golem." + keys[i]] = values[i]; var boss = c.Spawn(Master.RightPlayer, "PveBrickGolem", new Vector3(14, 0, 5));
            Assert.True(boss.HasComponent<PveBrickGolemMob>() && boss.HasComponent<IPveHealthMob>()); Assert.Equal(4000, boss.GetComponent<MobComponent>().MaxHp); Assert.Equal(Fix64.Zero, boss.GetComponent<MobComponent>().Speed.Total);
            Assert.True(boss.HasComponent<ICollidable>() && boss.Elements.NativeHas(Element.Rock) && !boss.HasComponent<Spawner>() && boss.HasComponent<IPveObjectiveTarget>());
            Assert.Equal("pve_brick_golem_prefab", PrefabCatalog.BeanName("PveBrickGolem")); Assert.Equal("pve_brick_golem", PrefabCatalog.ParameterKey("PveBrickGolem"));
        }
        private sealed class Receiver : SimComponent, IEffectReceiver {
            public VisualEffect? Effect; public Vector3 Direction; public Fix64 Strength;
            public Receiver(SimObject owner) : base(owner) { }
            public void Receive(EffectApplication a) { } public void Receive(VisualEffect e) { } public void Receive(VisualEffect e, Fix64 duration) { }
            public void ReceiveDirectional(VisualEffect effect, Vector3 direction, Fix64 strength) { Effect = effect; Direction = direction; Strength = strength; }
        }
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext { DeltaTime = (Fix64)0.1m };
            public readonly SimObject Owner; public readonly PveBrickGolemMob Mob; public readonly List<BattleEvent> Events = new List<BattleEvent>();
            public Fixture(Fix64? interval = null) { Context.RegisterPrefab("Object", _ => { }); Owner = Context.Spawn(Master.RightPlayer, "Object", new Vector3(10, 0, 5)); Owner.AddCollider(new CircleCollider(Owner, 2, true)); Mob = new PveBrickGolemMob(Owner, 3000, 30, 4, 12, 2, 12, interval ?? (Fix64)100, 4); Owner.AddComponentImmediately(Mob); Context.FlushObjects(); }
            public SimObject Victim(int x, Fix64 z, int y = 0, Master master = Master.LeftPlayer) { var o = Context.Spawn(master, "Object", new Vector3(x, y, z)); o.AddCollider(new CircleCollider(o, (Fix64)0.5m)); o.AddComponentImmediately(new BehaviorMob(o, 1000, 0, 3, 0, 0, null)); Context.FlushObjects(); return o; }
            public void Tick(int n) { for (int i = 0; i < n; i++) { foreach (var o in Context.Objects) o.Update(); Events.AddRange(Context.DrainEvents()); } }
            public void Until(Func<bool> reached) { for (int i = 0; i < 400 && !reached(); i++) Tick(1); Assert.True(reached()); }
            public bool Sent(string type) => Events.Any(e => e.Type == type);
        }
        private static int Hp(SimObject o) => o.GetComponent<MobComponent>().Hp;
        public static void CheckActualBoss() {
            var c = new SimulationContext { DeltaTime = 1 }; c.InitializationFailed += (o, e) => throw e; PveBrickGolemMob.RegisterPrefabs(c); c.RegisterPrefab("Target", _ => { });
            string[] keys = { "mass", "radius", "hp", "damage", "attack_interval", "attack_range", "effect_radius", "sub_damage", "sub_attack_interval", "sub_attack_range" }; decimal[] values = { 10, 2, 3000, 30, 4, 12, 2, 12, 100, 4 };
            for (int i = 0; i < keys.Length; i++) c.GameParameters["pve_brick_golem." + keys[i]] = (Fix64)values[i];
            var boss = c.Spawn(Master.RightPlayer, "PveBrickGolem", new Vector3(10, 0, 5)); var mob = boss.GetComponent<PveBrickGolemMob>();
            Assert.True(boss.HasComponent<ICollidable>() && boss.HasComponent<ZPhysics>() && boss.HasComponent<RigidBody>()); Assert.True(boss.Colliders.Single().IsTrigger && boss.Elements.NativeHas(Element.Rock)); Assert.True(!boss.HasComponent<Spawner>() && mob.CurrentState == null); Assert.Equal(3000, mob.GetGauge().Value);
            var target = c.Spawn(Master.LeftPlayer, "Target", new Vector3(4, 0, 5)); target.AddCollider(new CircleCollider(target, (Fix64)0.5m)); var health = new DummyMob(target, 100); target.AddComponentImmediately(health); c.FlushObjects();
            for (int i = 0; i < 4; i++) mob.Update(); Assert.Equal(100, health.Hp); mob.Update(); Assert.Equal(70, health.Hp);
            mob.OverrideMaxHp(400); Assert.Equal(400, mob.Hp); Assert.Equal(400, mob.MaxHp); mob.ApplyDamage(new Hit(1000, 0, Element.None)); Assert.True(mob.IsTerminal && boss.IsDestroyed); Assert.Equal(0, c.PendingObjects.Length);
        }
        [PortedTest(Source)] public static void slam_warns_at_once_pins_the_spot_and_damages_only_inside_the_radius_after_one_second() {
            var f = new Fixture(); var target = f.Victim(4, 5); var neighbour = f.Victim(4, (Fix64)6.5m); var far = f.Victim(4, 9); f.Until(() => f.Sent("BrickGolemSlamWarning"));
            var warning = f.Events.First(e => e.Type == "BrickGolemSlamWarning"); Assert.Equal(Fix64.One, warning.Duration); Assert.Equal(new Vector3(4, 0, 5), warning.StartPosition.Value); Assert.Equal(warning.StartPosition, warning.EndPosition); Assert.Equal(ObjectStatus.Attack, f.Owner.Status); Assert.Equal(1000, Hp(target));
            target.SetPosition(new Vector3(8, 0, 5)); f.Tick(5); Assert.Equal(1000, Hp(neighbour)); f.Tick(6); Assert.Equal(1000, Hp(target)); Assert.Equal(970, Hp(neighbour)); Assert.Equal(1000, Hp(far)); var slam = f.Events.First(e => e.Type == "BrickGolemSlam"); Assert.Equal((Fix64)0.3m, slam.Duration); Assert.Equal((Fix64)10, slam.StartPosition.Value.X); Assert.Equal((Fix64)4, slam.EndPosition.Value.X);
        }
        [PortedTest(Source)] public static void slam_does_not_hurt_its_own_side() { var f = new Fixture(); f.Victim(4, 5); var ally = f.Victim(4, (Fix64)5.5m, master: Master.RightPlayer); f.Tick(60); Assert.Equal(1000, Hp(ally)); }
        [PortedTest(Source)] public static void sweep_hits_a_near_enemy_and_knocks_it_away_from_the_golem() {
            var f = new Fixture((Fix64)0.1m); var near = f.Victim(7, 5); var receiver = new Receiver(near); near.AddComponentImmediately(receiver); f.Until(() => f.Sent("BrickGolemSweep")); var sweep = f.Events.First(e => e.Type == "BrickGolemSweep");
            Assert.Equal((Fix64)0.4m, sweep.Duration); Assert.Equal((Fix64)6, sweep.EndPosition.Value.X); Assert.Equal(988, Hp(near)); Assert.Equal(VisualEffect.Knockback, receiver.Effect.Value); Assert.True(receiver.Direction.X < 0); Assert.Equal(Fix64.Zero, receiver.Direction.Z); Assert.Equal((Fix64)0.75m, receiver.Strength); Assert.Equal(ObjectStatus.Attack, f.Owner.Status);
        }
        [PortedTest(Source)] public static void sweep_misses_an_enemy_behind_the_golem() { var f = new Fixture((Fix64)0.1m); var near = f.Victim(7, 5); var behind = f.Victim(13, 5); var receiver = new Receiver(behind); behind.AddComponentImmediately(receiver); f.Until(() => Hp(near) < 1000); Assert.Equal(988, Hp(near)); Assert.Equal(1000, Hp(behind)); Assert.True(!receiver.Effect.HasValue); }
        [PortedTest(Source)] public static void sweep_waits_while_no_enemy_is_within_its_range() { var f = new Fixture((Fix64)0.1m); var far = f.Victim(4, 5); f.Tick(10); Assert.True(!f.Sent("BrickGolemSweep")); Assert.Equal(1000, Hp(far)); }
        [PortedTest(Source)] public static void does_not_move_and_ignores_stun_and_knockback_state() { var f = new Fixture((Fix64)0.1m); var enemy = f.Victim(7, 5); f.Owner.AddComponentImmediately(new RigidBody(f.Owner, 10)); ((IStunnableBehavior)f.Mob).SetStun(5); f.Tick(100); Assert.Equal(new Vector3(10, 0, 5), f.Owner.Position); Assert.Equal(Fix64.Zero, f.Mob.Speed.Total); Assert.True(Hp(enemy) < 1000); }
        [PortedTest(Source)] public static void air_units_are_never_targeted() { var f = new Fixture((Fix64)0.1m); var flyer = f.Victim(7, 5, 3); Assert.Equal(2, CombatRange.TargetMask(flyer.Position)); f.Tick(100); Assert.Equal(0, f.Events.Count); Assert.Equal(1000, Hp(flyer)); }
        [PortedTest(Source)] public static void is_an_objective_that_ends_when_destroyed_and_leaves_no_death_field() { var f = new Fixture(); Assert.True(!PveObjectiveTarget.IsTerminal(f.Owner)); f.Mob.ApplyDamage(new Hit(5000, 0, Element.None)); Assert.True(f.Owner.IsDestroyed && PveObjectiveTarget.IsTerminal(f.Owner)); Assert.True(!f.Context.PendingObjects.Any(o => o.Prefab == "FireField" || o.Prefab == "LeafField")); }
    }
}
