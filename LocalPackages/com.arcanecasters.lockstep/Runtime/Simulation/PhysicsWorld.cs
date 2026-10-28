using System;
using System.Collections.Generic;
using BEPUphysics;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.CollisionRuleManagement;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public enum ContactPhase { Enter, Stay, Exit }
    public readonly struct BodyContact {
        public readonly int First, Second;
        public readonly ContactPhase Phase;
        internal BodyContact(long pair, ContactPhase phase) { First = (int)(pair >> 32); Second = (int)pair; Phase = phase; }
    }

    // 물리엔진 이벤트 안에서 전투 상태를 수정하지 않는다. Step 결과를 ID 순서대로 적용한다.
    public sealed class PhysicsWorld {
        private readonly Space space = new Space();
        private readonly SortedDictionary<int, Entity> bodies = new SortedDictionary<int, Entity>();
        private SortedSet<long> contacts = new SortedSet<long>();
        private int lastId;
        public static readonly Fix64 StepDuration = Fix64.One / 20;
        public PhysicsWorld(Vector3 gravity) {
            if (space.ParallelLooper != null) throw new InvalidOperationException("Physics must run on one thread");
            space.TimeStepSettings.TimeStepDuration = StepDuration;
            space.ForceUpdater.Gravity = gravity;
        }

        public void AddSphere(int id, Vector3 position, Fix64 radius, Fix64 mass, bool trigger = false) {
            if (radius <= Fix64.Zero || mass < Fix64.Zero) throw new ArgumentOutOfRangeException();
            Add(id, mass == Fix64.Zero ? new Sphere(position, radius) : new Sphere(position, radius, mass), trigger);
        }

        public void AddBox(int id, Vector3 position, Vector3 size, Fix64 mass, bool trigger = false) {
            if (size.X <= Fix64.Zero || size.Y <= Fix64.Zero || size.Z <= Fix64.Zero || mass < Fix64.Zero) throw new ArgumentOutOfRangeException();
            Add(id, mass == Fix64.Zero ? new Box(position, size.X, size.Y, size.Z) : new Box(position, size.X, size.Y, size.Z, mass), trigger);
        }

        private void Add(int id, Entity body, bool trigger) {
            // ID 재사용과 임의 삽입 순서를 금지한다. 물리 broad phase의 삽입 순서도 입력 재생에 포함된다.
            if (id <= lastId) throw new ArgumentOutOfRangeException(nameof(id));
            body.Tag = id; body.CollisionInformation.Tag = id;
            if (trigger) body.CollisionInformation.CollisionRules.Personal = CollisionRule.NoSolver;
            bodies.Add(id, body); space.Add(body); lastId = id;
        }

        public void Remove(int id) {
            if (!bodies.TryGetValue(id, out var body)) return;
            foreach (var other in bodies.Values) other.CollisionInformation.CollisionRules.Specific.Remove(body.CollisionInformation.CollisionRules);
            body.CollisionInformation.CollisionRules.Specific.Clear(); space.Remove(body); bodies.Remove(id);
        }
        public void AddEdge(int id, Vector3 start, Vector3 end) { Add(id, new Capsule(start, end, Fix64.Zero), false); }
        public void ConfigureBattleBody(int id, bool boundary = false) {
            var body = bodies[id]; body.LinearDamping = Fix64.Zero; body.AngularDamping = Fix64.Zero;
            body.Material.StaticFriction = Fix64.Zero; body.Material.KineticFriction = Fix64.Zero; body.Material.Bounciness = Fix64.One;
            body.Material.VelocityOnlyContactResponse = true;
            body.Material.MaximumContactImpulse = boundary ? Fix64.MaxValue : Fix64.One;
        }
        public void SetPairEnabled(int first, int second, bool enabled) {
            var rules = bodies[first].CollisionInformation.CollisionRules.Specific;
            var other = bodies[second].CollisionInformation.CollisionRules;
            if (enabled) rules.Remove(other); else if (!rules.WrappedDictionary.ContainsKey(other)) rules.Add(other, CollisionRule.NoBroadPhase);
        }
        public Vector3 Position(int id) => bodies[id].Position;
        public Vector3 Velocity(int id) => bodies[id].LinearVelocity;
        public Quaternion Orientation(int id) => bodies[id].Orientation;
        public void SetVelocity(int id, Vector3 velocity) { bodies[id].LinearVelocity = velocity; }
        public void Move(int id, Vector3 position) { bodies[id].Position = position; }

        public BodyContact[] Step() => Step(StepDuration);
        public BodyContact[] Step(Fix64 duration, Action beforePositionUpdate = null) {
            if (duration <= Fix64.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
            space.TimeStepSettings.TimeStepDuration = duration;
            if (beforePositionUpdate != null) space.PositionUpdater.Starting += beforePositionUpdate;
            try { space.Update(); }
            finally { if (beforePositionUpdate != null) space.PositionUpdater.Starting -= beforePositionUpdate; }
            var current = new SortedSet<long>();
            foreach (var item in bodies) {
                foreach (var pair in item.Value.CollisionInformation.Pairs) {
                    if (pair.Contacts.Count == 0 || !(pair.EntityA?.Tag is int a) || !(pair.EntityB?.Tag is int b)) continue;
                    if (a > b) { int swap = a; a = b; b = swap; }
                    current.Add(((long)a << 32) | (uint)b);
                }
            }
            var all = new SortedSet<long>(contacts); all.UnionWith(current);
            var events = new List<BodyContact>(all.Count);
            foreach (long pair in all) events.Add(new BodyContact(pair,
                !current.Contains(pair) ? ContactPhase.Exit : contacts.Contains(pair) ? ContactPhase.Stay : ContactPhase.Enter));
            contacts = current;
            return events.ToArray();
        }

        public bool RayCast(Vector3 origin, Vector3 direction, Fix64 distance, out int id, out Vector3 point) {
            id = 0; point = default;
            if (distance < Fix64.Zero || direction.LengthSquared() == Fix64.Zero) return false;
            direction.Normalize();
            var hits = new List<RayCastResult>();
            space.RayCast(new Ray(origin, direction), distance, hits);
            // 같은 거리의 충돌은 ID로 고른다. broad phase 순회 순서에 맡기지 않는다.
            hits.Sort((a, b) => {
                int order = a.HitData.T.CompareTo(b.HitData.T);
                return order != 0 ? order : ((int)a.HitObject.Tag).CompareTo((int)b.HitObject.Tag);
            });
            if (hits.Count == 0) return false;
            id = (int)hits[0].HitObject.Tag; point = hits[0].HitData.Location; return true;
        }
    }
}
