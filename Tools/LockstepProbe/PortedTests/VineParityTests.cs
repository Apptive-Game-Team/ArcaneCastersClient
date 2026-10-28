using System.Collections.Generic;
using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class VineParityTests {
        public static void CheckRegisteredToss() {
            var context = Context(); VineToss.RegisterPrefab(context);
            var owner = context.Spawn(Master.LeftPlayer, "VineToss", new Vector3(1, 0, 5));
            Assert.True(owner.Elements.Has(Element.Nature));
            Assert.Equal(0, owner.Colliders.Length);
            owner.GetComponent<IShotTarget>().SetTarget(new Vector3(7, 0, 5));
            Assert.True(owner.IsDestroyed);
            var vine = context.PendingObjects.Single(item => item.Prefab == "Vine");
            Assert.Equal(new Vector3(3, 0, 5), vine.Position);
            Assert.Equal(Master.LeftPlayer, vine.Master);
        }
        public static void CheckSharedHitTracker() {
            var context = new SimulationContext(0); context.RegisterPrefab("Target", _ => { });
            var target = context.Spawn(Master.RightPlayer, "Target", new Vector3(5, 0, 5)); target.AddComponentImmediately(new DummyMob(target, 20)); context.FlushObjects();
            var tracker = new VineHitTracker();
            for (int i = 0; i < 2; i++) {
                var vine = new SimObject(context, Master.LeftPlayer, "Vine", new Vector3(5 + i, 0, 5));
                new OnStartAttacker(vine, 2, 4, tracker).Start();
            }
            Assert.Equal(16, target.GetComponent<DummyMob>().Hp); Assert.Equal(1, tracker.HitCount);
            try { VineSpawnContext.RunWithTracker<int>(context, tracker, () => throw new System.InvalidOperationException("fixture")); } catch (System.InvalidOperationException) { }
            Assert.True(VineSpawnContext.CurrentTracker(context) == null);
        }
        private const string Mind = "game/domain/object/component/magic/mind_control_shot.rs";
        private const string Toss = "game/domain/object/component/magic/vine_toss.rs";
        private const string Line = "game/domain/object/component/magic/sequential_line_spawner.rs";
        private sealed class Immune : SimComponent, IMindControlImmune { public Immune(SimObject owner) : base(owner) { } }
        private static SimObject Enemy(SimulationContext context) { var target = new SimObject(context, Master.RightPlayer, "FireSlime", Vector3.Zero); target.AddComponentImmediately(new AttackMob(target, 10, 1, 1, 1, 1, 1)); return target; }
        [PortedTest(Mind)] public static void flips_an_enemy_summon_to_the_caster_s_opponent_side_and_vanishes() {
            var context = new SimulationContext(0); var owner = new SimObject(context, Master.LeftPlayer, "WillOWisp", Vector3.Zero); var shot = new MindControlShot(owner, 3); var target = Enemy(context); target.SetStatus(ObjectStatus.Attack);
            shot.OnCollisionWithEnemy(target); Assert.Equal(Master.LeftPlayer, target.Master); Assert.Equal(ObjectStatus.Idle, target.Status); Assert.True(owner.IsDestroyed);
        }
        [PortedTest(Mind)] public static void leaves_immune_classes_neutral_objects_and_non_mobs_alone() {
            var context = new SimulationContext(0); var owner = new SimObject(context, Master.LeftPlayer, "WillOWisp", Vector3.Zero); var shot = new MindControlShot(owner, 3);
            var immune = Enemy(context); immune.AddComponentImmediately(new Immune(immune)); var neutral = Enemy(context); neutral.SetMaster(Master.None); var notMob = new SimObject(context, Master.RightPlayer, "Wall", Vector3.Zero);
            foreach (var target in new[] { immune, neutral, notMob }) shot.OnCollisionWithEnemy(target);
            Assert.Equal(Master.RightPlayer, immune.Master); Assert.Equal(Master.None, neutral.Master); Assert.Equal(Master.RightPlayer, notMob.Master); Assert.Equal(false, owner.IsDestroyed);
        }
        private static SimulationContext Context() {
            var context = new SimulationContext(0); context.RegisterPrefab("Vine", _ => { }); context.GameParameters["vine_toss.vine_count"] = 3; context.GameParameters["vine_toss.vine_spacing"] = 2; context.GameParameters["vine_toss.vine_spawn_interval"] = (Fix64)0.5m; return context;
        }
        [PortedTest(Toss)] public static void plants_the_first_vine_one_spacing_ahead_and_destroys_the_toss() {
            var context = Context(); var owner = new SimObject(context, Master.LeftPlayer, "VineToss", new Vector3(1, 0, 5)); var toss = new VineToss(owner); owner.AddComponentImmediately(toss); toss.SetTarget(new Vector3(9, 0, 5));
            Assert.True(owner.IsDestroyed); var created = context.PendingObjects; Assert.Equal(1, created.Length); Assert.Equal("Vine", created[0].Prefab); Assert.Equal(new Vector3(3, 0, 5), created[0].Position); Assert.Equal(1, created[0].GetPendingComponents<SequentialLineSpawner>().Length);
        }
        [PortedTest(Toss)] public static void a_target_on_top_of_the_toss_only_destroys_it() {
            var context = Context(); var owner = new SimObject(context, Master.LeftPlayer, "VineToss", new Vector3(1, 0, 5)); new VineToss(owner).SetTarget(owner.Position); Assert.Equal(0, context.PendingObjects.Length);
        }
        [PortedTest("game/domain/object/component/magic/vine_hit_tracker.rs")] public static void counts_each_damaged_object_only_once() {
            var tracker = new VineHitTracker(); Assert.True(tracker.MarkIfFirstHit(10)); Assert.Equal(false, tracker.MarkIfFirstHit(10)); Assert.True(tracker.MarkIfFirstHit(20)); Assert.Equal(2, tracker.HitCount);
        }
        [PortedTest("game/domain/object/component/magic/vine_spawn_context.rs")] public static void restores_the_previous_tracker_after_a_nested_run() {
            var context = Context(); var outer = new VineHitTracker(); var inner = new VineHitTracker(); Assert.True(VineSpawnContext.CurrentTracker(context) == null);
            VineSpawnContext.RunWithTracker(context, outer, () => {
                Assert.True(ReferenceEquals(outer, VineSpawnContext.CurrentTracker(context)));
                VineSpawnContext.RunWithTracker(context, inner, () => { Assert.True(ReferenceEquals(inner, VineSpawnContext.CurrentTracker(context))); return 0; });
                Assert.True(ReferenceEquals(outer, VineSpawnContext.CurrentTracker(context))); return 0;
            }); Assert.True(VineSpawnContext.CurrentTracker(context) == null);
        }
        private static List<KeyValuePair<SimObject, int>> RunLine() {
            var context = Context(); context.DeltaTime = Fix64.One / 20;
            var first = context.Spawn(Master.LeftPlayer, "Vine", Vector3.Zero); first.AddComponent(new SequentialLineSpawner(first, Master.LeftPlayer, "Vine", Vector3.Zero, Vector3.Right, 1, (Fix64)0.12m, 9, new VineHitTracker()));
            var created = new List<KeyValuePair<SimObject, int>> { new KeyValuePair<SimObject, int>(first, 0) };
            for (int frame = 1; frame <= 40; frame++) {
                context.FrameNumber = frame; var alive = created.Where(item => !item.Key.IsDestroyed).ToArray();
                foreach (var item in alive) foreach (var component in item.Key.Components) component.Update();
                foreach (var item in alive) item.Key.FlushComponents();
                foreach (var item in alive) if (frame - item.Value >= 10) item.Key.Destroy();
                foreach (var owner in context.PendingObjects) if (!created.Any(item => ReferenceEquals(item.Key, owner))) created.Add(new KeyValuePair<SimObject, int>(owner, frame));
            }
            return created;
        }
        [PortedTest(Line)] public static void finishes_the_line_after_the_first_vine_expires() { var created = RunLine(); Assert.Equal(9, created.Count); Assert.Equal((Fix64)8, created[8].Key.Position.X); }
        [PortedTest(Line)] public static void spawns_on_the_cast_schedule_even_though_the_spawner_moves_between_vines() {
            var created = RunLine(); for (int i = 1; i < 9; i++) { Fix64 spawn = created[i].Value * (Fix64.One / 20), earliest = (Fix64)0.12m * i; Assert.True(spawn >= earliest && spawn <= earliest + (Fix64.One / 20) * 2); }
        }
    }
}
