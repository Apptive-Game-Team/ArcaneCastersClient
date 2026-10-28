using System.Collections.Generic;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class PhysicsParityTests {
        private const string Source = "game/service/system/physic_system.rs";
        private const string Wall = "tests/physic_system_wall_collision.rs";
        private const string River = "tests/physic_system_river_collider.rs";
        private const string Crossing = "tests/river_crossing.rs";
        private sealed class CrossingWorld {
            public readonly SimulationContext Context = new SimulationContext();
            public readonly List<SimObject> Water = new List<SimObject>(), Walkers = new List<SimObject>();
            private readonly BattlePhysicsSystem physics = new BattlePhysicsSystem();
            public CrossingWorld() {
                Context.DeltaTime = (Fix64)0.05m; TerrainPrefabs.Register(Context);
                Context.RegisterPrefab("FireSlime", _ => { });
                Context.Spawn(Master.None,"Wall",Vector3.Zero);
                Context.Terrain = Terrain.River;
                foreach (var cell in Terrain.River.WaterCells) Water.Add(Context.Spawn(Master.None,"RiverWater",cell.Center));
                Context.FlushObjects();
            }
            public SimObject Body(Vector3 position, bool walker) {
                var owner = Context.Spawn(walker ? Master.LeftPlayer : Master.RightPlayer,"FireSlime",position);
                owner.AddComponent(new RigidBody(owner,2));
                owner.AddCollider(new CircleCollider(owner,(Fix64)0.5m,false));
                owner.AddComponent(new Recorder(owner));
                if (walker) { owner.AddComponent(new BehaviorMob(owner,100,2,1,1,1,_ => true)); Walkers.Add(owner); }
                else owner.AddComponent(new DummyMob(owner,100));
                owner.FlushComponents(); Context.FlushObjects(); return owner;
            }
            public void Tick() {
                Context.FrameNumber++;
                foreach (var owner in Walkers) owner.GetComponent<BehaviorMob>().Update();
                physics.Update(Context);
            }
        }
        [PortedTest(Crossing)] public static void a_walker_crosses_on_the_middle_of_a_bridge_and_never_touches_the_water() {
            var world = new CrossingWorld(); world.Body(new Vector3(16,0,5),false);
            var walker = world.Body(new Vector3(2,0,5),true); var last = walker.Position; var crossings = new List<Fix64>();
            for (int frame = 0; frame < 400; frame++) {
                world.Tick(); var position = walker.Position;
                foreach (var water in world.Water)
                    if (CollisionChecker.IsColliding(water,walker)) throw new System.Exception($"Water collision at frame {frame}: {position}");
                if ((last.X - 9) * (position.X - 9) < 0) crossings.Add(position.Z);
                last = position;
            }
            Assert.True(walker.Position.X > 13); Assert.Equal(1,crossings.Count);
            var z = crossings[0]; Assert.True(z >= 2 && z <= 3 || z >= 7 && z <= 8);
        }
        [PortedTest(Crossing)] public static void a_crowd_of_walkers_all_reach_the_other_bank_over_the_bridges_and_never_stand_in_the_water() {
            var world = new CrossingWorld(); world.Body(new Vector3(16,0,5),false);
            for (int i = 0; i < 12; i++) world.Body(new Vector3(6 + (i % 3) * (Fix64)0.4m,0,(Fix64)3.5m + (i / 3) * (Fix64)0.6m),true);
            for (int frame = 0; frame < 500; frame++) {
                world.Tick();
                foreach (var walker in world.Walkers)
                    if (Terrain.River.IsWaterAt(walker.Position)) throw new System.Exception($"Walker {walker.Id} in water at frame {frame}: {walker.Position}");
            }
            foreach (var walker in world.Walkers)
                if (walker.Position.X <= 10) throw new System.Exception($"Walker {walker.Id} did not cross: {walker.Position}");
        }
        private static SimulationContext RiverContext() {
            var context = Context(); context.DeltaTime = (Fix64)0.05m; context.Terrain = Terrain.River; TerrainPrefabs.Register(context);
            context.Spawn(Master.None, "Wall", Vector3.Zero);
            foreach (var cell in context.Terrain.WaterCells) context.Spawn(Master.None, "RiverWater", cell.Center);
            context.FlushObjects(); return context;
        }
        private static Fix64 WalkBank(bool left) {
            var context = RiverContext(); var physics = new BattlePhysicsSystem(); Fix64 result = Fix64.Zero;
            foreach (decimal z in new[] { 0.5m, 4.5m, 5.5m, 9.5m }) {
                var body = Object(context, Master.LeftPlayer, new Vector3(left ? 5 : 13, 0, (Fix64)z), false, 2); body.AddComponentImmediately(new DummyMob(body, 10)); Fix64 furthest = body.Position.X;
                for (int i = 0; i < 120; i++) {
                    body.GetComponent<RigidBody>().AddVelocity(new Vector3(left ? 3 : -3, 0, 0)); context.FrameNumber++; physics.Update(context);
                    furthest = left ? MathHelper.Max(furthest, body.Position.X) : MathHelper.Min(furthest, body.Position.X);
                }
                Assert.True(left ? furthest < (Fix64)7.65m : furthest > (Fix64)10.35m); Assert.Equal(false, Terrain.River.IsWaterAt(body.Position)); result = furthest;
            }
            return result;
        }
        [PortedTest(River)] public static void a_body_walking_straight_at_the_water_from_the_left_bank_stops_at_the_bank() { WalkBank(true); }
        [PortedTest(River)] public static void a_body_walking_straight_at_the_water_from_the_right_bank_stops_at_the_bank() { WalkBank(false); }
        [PortedTest(River)] public static void a_body_walking_along_the_bridge_edge_into_the_side_of_the_water_stops_on_the_bridge() {
            var context = RiverContext(); var physics = new BattlePhysicsSystem(); var body = Object(context, Master.LeftPlayer, new Vector3((Fix64)8.5m, 0, (Fix64)2.5m), false, 2); body.AddComponentImmediately(new DummyMob(body, 10));
            for (int i = 0; i < 80; i++) { body.GetComponent<RigidBody>().AddVelocity(new Vector3(0, 0, 3)); context.FrameNumber++; physics.Update(context); }
            Assert.True(body.Position.Z < (Fix64)3.65m); Assert.Near((Fix64)8.5m, body.Position.X, (Fix64)0.0001m);
        }
        private sealed class Recorder : SimComponent, ICollidable {
            public readonly List<int> Every = new List<int>(), Enemy = new List<int>();
            public Recorder(SimObject owner) : base(owner) { }
            public void OnCollision(SimObject other) { Every.Add(other.Id); }
            public void OnCollisionWithEnemy(SimObject other) { Enemy.Add(other.Id); }
        }
        private static SimulationContext Context() { var context = new SimulationContext(0); context.RegisterPrefab("ElectricSlime", _ => { }); return context; }
        private static SimObject Object(SimulationContext context, Master master, Vector3 position, bool trigger, int mass = 1, Fix64? radius = null) {
            var owner = context.Spawn(master, "ElectricSlime", position); owner.AddComponent(new Recorder(owner));
            if (!trigger) owner.AddComponent(new RigidBody(owner, mass));
            owner.AddCollider(new CircleCollider(owner, radius ?? (trigger ? Fix64.One : (Fix64)0.5m), trigger)); owner.FlushComponents(); context.FlushObjects(); return owner;
        }
        [PortedTest(Source)] public static void friendly_pair_only_receives_the_all_collision_callback() {
            var context = Context(); var a = Object(context, Master.LeftPlayer, Vector3.Zero, true); var b = Object(context, Master.LeftPlayer, Vector3.Zero, true); new BattlePhysicsSystem().Update(context);
            Assert.Sequence(new[] { b.Id }, a.GetComponent<Recorder>().Every); Assert.Sequence(new[] { a.Id }, b.GetComponent<Recorder>().Every); Assert.Equal(0, a.GetComponent<Recorder>().Enemy.Count); Assert.Equal(0, b.GetComponent<Recorder>().Enemy.Count);
        }
        [PortedTest(Source)] public static void enemy_pair_receives_both_callbacks() {
            var context = Context(); var a = Object(context, Master.LeftPlayer, Vector3.Zero, true); var b = Object(context, Master.RightPlayer, Vector3.Zero, true); new BattlePhysicsSystem().Update(context);
            Assert.Sequence(new[] { b.Id }, a.GetComponent<Recorder>().Every); Assert.Sequence(new[] { b.Id }, a.GetComponent<Recorder>().Enemy); Assert.Sequence(new[] { a.Id }, b.GetComponent<Recorder>().Every); Assert.Sequence(new[] { a.Id }, b.GetComponent<Recorder>().Enemy);
        }
        [PortedTest(Wall)] public static void an_object_pushed_out_of_the_arena_without_a_wall_is_destroyed() {
            var context = Context(); context.DeltaTime = (Fix64)0.1m; var unit = Object(context, Master.LeftPlayer, new Vector3((Fix64)0.4m, 0, 5), false, 2); var system = new BattlePhysicsSystem();
            for (int i = 0; i < 5; i++) { unit.GetComponent<RigidBody>().AddVelocity(new Vector3(-6, 0, 0)); system.Update(context); } Assert.True(unit.IsDestroyed);
        }
        [PortedTest(Wall)] public static void two_overlapping_bodies_are_pushed_apart_with_the_capped_impulse() {
            var context = Context(); context.DeltaTime = (Fix64)0.05m; var left = Object(context, Master.LeftPlayer, new Vector3(5, 0, 5), false); var right = Object(context, Master.LeftPlayer, new Vector3((Fix64)5.5m, 0, 5), false);
            new BattlePhysicsSystem().Update(context); Assert.Equal(new Vector3(5, 0, 5), left.Position); Assert.Equal(new Vector3((Fix64)5.5m, 0, 5), right.Position);
            left.GetComponent<RigidBody>().AddVelocity(new Vector3(1, 0, 0)); new BattlePhysicsSystem().Update(context);
            Assert.Equal(new Vector3(5, 0, 5), left.Position); Assert.Equal(new Vector3((Fix64)5.55m, 0, 5), right.Position);
        }
        private static SimObject Body(SimulationContext context, Vector3 position, int mass = 2, Master master = Master.LeftPlayer) {
            var body = Object(context, master, position, false, mass); body.AddComponentImmediately(new DummyMob(body, 10)); return body;
        }
        private static void Push(SimObject body, Fix64 x, Fix64 z) { body.GetComponent<RigidBody>().AddVelocity(new Vector3(x, 0, z)); }
        private static void Tick(SimulationContext context, BattlePhysicsSystem physics) { context.FrameNumber++; physics.Update(context); }
        [PortedTest(River)] public static void a_knockback_of_boulder_strength_does_not_carry_a_body_across_the_river() {
            var context = RiverContext(); var physics = new BattlePhysicsSystem();
            foreach (int mass in new[] { 1, 2 }) foreach (decimal z in new[] { 5m, 4.2m, 5.8m }) {
                var left = Body(context, new Vector3((Fix64)6.9m, 0, (Fix64)z), mass); var right = Body(context, new Vector3((Fix64)11.1m, 0, (Fix64)z), mass, Master.RightPlayer); Fix64 speed = (Fix64)8 / Fix64.Sqrt(mass);
                for (int i = 0; i < 13; i++) { Push(left, speed, 0); Push(right, -speed, 0); Tick(context, physics); Assert.True(left.Position.X < (Fix64)8); Assert.True(right.Position.X > (Fix64)10); }
                left.Destroy(); right.Destroy(); context.RemoveObject(left); context.RemoveObject(right);
            }
        }
        [PortedTest(River)] public static void a_body_is_stopped_at_speeds_up_to_twelve_units_per_second_from_the_start_used() {
            var context = RiverContext(); var physics = new BattlePhysicsSystem();
            foreach (int speed in new[] { 1, 3, 4, 6, 8, 10, 12 }) {
                var body = Body(context, new Vector3((Fix64)5.2m, 0, 5)); Fix64 furthest = body.Position.X;
                for (int i = 0; i < 60; i++) { Push(body, speed, 0); Tick(context, physics); furthest = MathHelper.Max(furthest, body.Position.X); }
                Assert.True(furthest < (Fix64)8); context.RemoveObject(body);
            }
        }
        [PortedTest(River)] public static void a_body_headed_for_a_convex_corner_of_a_pocket_does_not_enter_the_water() {
            var context = RiverContext(); var physics = new BattlePhysicsSystem();
            foreach (var velocity in new[] { new Vector3(3, 0, 3), new Vector3(4, 0, 2), new Vector3(2, 0, 4), new Vector3(3, 0, (Fix64)3.2m) }) {
                var body = Body(context, new Vector3((Fix64)6.8m, 0, (Fix64)2.8m));
                for (int i = 0; i < 100; i++) { Push(body, velocity.X, velocity.Z); Tick(context, physics); Assert.Equal(false, Terrain.River.IsWaterAt(body.Position)); } context.RemoveObject(body);
            }
        }
        [PortedTest(River)] public static void a_body_pressed_along_the_bank_slides_along_it() {
            var context = RiverContext(); var physics = new BattlePhysicsSystem(); var body = Body(context, new Vector3((Fix64)7.4m, 0, (Fix64)4.2m));
            for (int i = 0; i < 15; i++) { Push(body, 3, 2); Tick(context, physics); }
            Fix64 slide = body.Position.Z - (Fix64)4.2m; Assert.True(body.Position.X < (Fix64)7.65m); Assert.True(slide >= (Fix64)1.3m && slide <= (Fix64)1.7m); Assert.Equal(false, Terrain.River.IsWaterAt(body.Position));
        }
        [PortedTest(River)] public static void a_crowd_leaning_on_the_bank_does_not_push_a_body_over() {
            var context = RiverContext(); var physics = new BattlePhysicsSystem(); var crowd = new List<SimObject>();
            for (int i = 0; i < 12; i++) crowd.Add(Body(context, new Vector3((Fix64)6.6m + (i % 3) * (Fix64)0.35m, 0, (Fix64)4.1m + (i / 3) * (Fix64)0.55m)));
            for (int frame = 0; frame < 300; frame++) { for (int i = 0; i < crowd.Count; i += 2) Push(crowd[i], 3, 0); Tick(context, physics); foreach (var body in crowd) if (Terrain.River.IsWaterAt(body.Position)) throw new System.Exception($"Water entry: frame={frame} id={body.Id} position={body.Position}"); }
        }
        [PortedTest(River)] public static void a_flyer_crosses_the_river_untouched() {
            var context = RiverContext(); var physics = new BattlePhysicsSystem(); var flyer = Body(context, new Vector3(5, 3, 5));
            for (int i = 0; i < 80; i++) { Push(flyer, 3, 0); Tick(context, physics); } Assert.Near(17, flyer.Position.X, (Fix64)0.001m); Assert.Equal((Fix64)5, flyer.Position.Z);
        }
        [PortedTest(River)] public static void a_projectile_crosses_the_river_untouched_at_ground_height_too() {
            var context = RiverContext(); var physics = new BattlePhysicsSystem();
            foreach (decimal height in new[] { 0m, 0.5m }) {
                var projectile = Object(context, Master.LeftPlayer, new Vector3(5, (Fix64)height, 5), false, 1, (Fix64)0.25m);
                for (int i = 0; i < 15; i++) { Push(projectile, 14, 0); Tick(context, physics); } Assert.Near((Fix64)15.5m, projectile.Position.X, (Fix64)0.001m); context.RemoveObject(projectile);
            }
        }
        [PortedTest(River)] public static void a_dying_body_falling_over_the_water_is_not_held_by_it() {
            var context = RiverContext(); var physics = new BattlePhysicsSystem(); var body = Body(context, new Vector3((Fix64)7.7m, 0, 5)); body.SetStatus(ObjectStatus.Dying);
            for (int i = 0; i < 20; i++) { Push(body, 3, 0); Tick(context, physics); } Assert.Near((Fix64)10.7m, body.Position.X, (Fix64)0.001m);
        }
        [PortedTest(River)] public static void a_ground_body_crosses_a_bridge_from_end_to_end_without_touching_any_edge() {
            var context = RiverContext(); var physics = new BattlePhysicsSystem();
            foreach (decimal z in new[] { 2.5m, 7.5m }) {
                var body = Body(context, new Vector3(5, 0, (Fix64)z));
                for (int i = 0; i < 80; i++) { Push(body, 3, 0); Tick(context, physics); foreach (var water in context.Objects) if (water.HasComponent<WaterBarrier>()) Assert.Equal(false, CollisionChecker.IsColliding(body, water)); }
                Assert.Near(17, body.Position.X, (Fix64)0.001m); Assert.Equal((Fix64)z, body.Position.Z); context.RemoveObject(body);
            }
        }
        [PortedTest(River)] public static void water_only_pushes_and_never_reports_a_collision_to_either_side() {
            var context = RiverContext(); var physics = new BattlePhysicsSystem(); var body = Body(context, new Vector3((Fix64)7.3m, 0, 5));
            for (int i = 0; i < 40; i++) { Push(body, 3, 0); Tick(context, physics); } Assert.Equal(0, body.GetComponent<Recorder>().Every.Count + body.GetComponent<Recorder>().Enemy.Count);
        }
        [PortedTest(River)] public static void a_body_that_ends_up_inside_a_pocket_cannot_leave_it_but_is_not_thrown_across() {
            var context = RiverContext(); var physics = new BattlePhysicsSystem();
            foreach (var direction in new[] { new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1), new Vector3(1, 0, 1), new Vector3(-1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, -1) }) {
                var body = Body(context, new Vector3(5, 0, 5)); body.SetPosition(new Vector3((Fix64)8.3m, 0, (Fix64)5.1m));
                for (int i = 0; i < 100; i++) { Push(body, direction.X * 3, direction.Z * 3); Tick(context, physics); }
                Assert.True(body.Position.X >= (Fix64)8 && body.Position.X <= (Fix64)10); Assert.True(body.Position.Z >= (Fix64)4 && body.Position.Z <= (Fix64)6); context.RemoveObject(body);
            }
        }
    }
}
