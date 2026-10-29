using System;
using System.Collections.Generic;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public interface ICollidable {
        void OnCollision(SimObject other);
        void OnCollisionWithEnemy(SimObject other);
    }
    public sealed class WaterBarrier : SimComponent, ICollidable {
        public WaterBarrier(SimObject owner) : base(owner) { }
        public void OnCollision(SimObject other) { }
        public void OnCollisionWithEnemy(SimObject other) { }
    }
    // BEPU에 실제 이동과 충돌 응답을 맡기는 전투 어댑터. 원본 물리 수치 동일성 검증은 진행 중이다.
    public sealed class BattlePhysicsSystem : IGameSystem {
        private readonly PhysicsWorld world = new PhysicsWorld(Vector3.Zero);
        private readonly SortedDictionary<int, Binding> bindings = new SortedDictionary<int, Binding>();
        private readonly HashSet<int> failedCallbacks = new HashSet<int>();
        private int nextBody = 1;
        public event Action<SimObject, Exception> CollisionFailed;
        private sealed class Binding {
            public SimObject Owner;
            public int Sphere;
            public Fix64 Radius;
            public int Mass;
            public readonly List<KeyValuePair<EdgeCollider, int>> Edges = new List<KeyValuePair<EdgeCollider, int>>();
            public IEnumerable<int> BodyIds => (Sphere == 0 ? Enumerable.Empty<int>() : new[] { Sphere }).Concat(Edges.Select(edge => edge.Value));
        }
        private static bool GroundBody(SimObject owner) => !owner.IsDestroyed && !owner.IsDying && owner.Position.Y < (Fix64)2 && owner.HasComponent<IDamageable>();
        private static bool Eligible(SimObject owner) => owner.IsActive && !owner.IsDying && owner.HasComponent<ICollidable>();
        private void Remove(int ownerId) { var binding = bindings[ownerId]; foreach (int body in binding.BodyIds) world.Remove(body); bindings.Remove(ownerId); }
        private void Synchronize(SimObject[] objects) {
            var eligible = objects.Where(Eligible).ToArray(); var live = new HashSet<int>(eligible.Select(owner => owner.Id));
            foreach (int id in bindings.Keys.ToArray()) if (!live.Contains(id)) Remove(id);
            foreach (var owner in eligible) {
                var solids = owner.Colliders.OfType<SimCollider>().Where(c => !c.IsTrigger).ToArray();
                var circles = solids.OfType<CircleCollider>().ToArray(); var edges = solids.OfType<EdgeCollider>().ToArray();
                Fix64 radius = circles.Length == 0 ? Fix64.Zero : circles.Max(circle => circle.Radius); int mass = owner.GetComponent<RigidBody>()?.Mass ?? -1;
                if (bindings.TryGetValue(owner.Id, out var existing) && (existing.Radius != radius || existing.Mass != mass || !existing.Edges.Select(edge => edge.Key).SequenceEqual(edges))) Remove(owner.Id);
                if (!bindings.TryGetValue(owner.Id, out var binding)) {
                    binding = new Binding { Owner = owner, Radius = radius, Mass = mass }; bindings.Add(owner.Id, binding);
                    if (radius > Fix64.Zero) { binding.Sphere = nextBody++; world.AddSphere(binding.Sphere, owner.Position, radius, mass > 0 ? (Fix64)mass : Fix64.Zero); world.ConfigureBattleBody(binding.Sphere); }
                    foreach (var edge in edges) { int id = nextBody++; world.AddEdge(id, edge.Point1, edge.Point2); world.ConfigureBattleBody(id, true); binding.Edges.Add(new KeyValuePair<EdgeCollider, int>(edge, id)); }
                }
                if (binding.Sphere != 0) world.Move(binding.Sphere, owner.Position);
                foreach (var edge in binding.Edges) world.Move(edge.Value, (edge.Key.Point1 + edge.Key.Point2) / 2);
            }
            var all = bindings.Values.ToArray();
            for (int i = 0; i < all.Length; i++) for (int j = i; j < all.Length; j++) {
                var a = all[i]; var b = all[j]; bool enabled = i != j;
                if (a.Owner.HasComponent<WaterBarrier>()) enabled &= GroundBody(b.Owner);
                if (b.Owner.HasComponent<WaterBarrier>()) enabled &= GroundBody(a.Owner);
                foreach (int first in a.BodyIds) foreach (int second in b.BodyIds) if (i != j || first < second) world.SetPairEnabled(Math.Min(first, second), Math.Max(first, second), enabled);
            }
        }
        private void Dispatch(SimObject[] objects) {
            var candidates = objects.Where(Eligible).Where(owner => !owner.HasComponent<WaterBarrier>()).ToArray();
            var pairs = new List<KeyValuePair<SimObject, SimObject>>();
            for (int i = 0; i < candidates.Length; i++) for (int j = i + 1; j < candidates.Length; j++) if (CollisionChecker.IsColliding(candidates[i], candidates[j])) pairs.Add(new KeyValuePair<SimObject, SimObject>(candidates[i], candidates[j]));
            foreach (var pair in pairs) {
                var a = pair.Key; var b = pair.Value; if (a.IsDestroyed || b.IsDestroyed) continue;
                try {
                    foreach (var callback in a.GetComponents<ICollidable>()) callback.OnCollision(b);
                    foreach (var callback in b.GetComponents<ICollidable>()) callback.OnCollision(a);
                    if (a.Master == b.Master && b.Master != Master.None || a.Prefab == "Wall" || b.Prefab == "Wall" || a.IsDestroyed || b.IsDestroyed) continue;
                    foreach (var callback in a.GetComponents<ICollidable>()) callback.OnCollisionWithEnemy(b);
                    foreach (var callback in b.GetComponents<ICollidable>()) callback.OnCollisionWithEnemy(a);
                } catch (Exception exception) { if (failedCallbacks.Add(a.Id)) CollisionFailed?.Invoke(a, exception); }
            }
        }
        public void WriteCheckpoint(System.IO.BinaryWriter writer) {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            writer.Write(nextBody); writer.Write(bindings.Count);
            foreach (var entry in bindings) {
                var binding = entry.Value;
                writer.Write(entry.Key); writer.Write(binding.Owner.Id); writer.Write(binding.Sphere);
                writer.Write(binding.Radius.RawValue); writer.Write(binding.Mass); writer.Write(binding.Edges.Count);
                foreach (var edge in binding.Edges) {
                    writer.Write(edge.Value);
                    PhysicsStateValue.Write(writer, edge.Key.Point1); PhysicsStateValue.Write(writer, edge.Key.Point2);
                }
            }
            var failed = failedCallbacks.OrderBy(id => id).ToArray(); writer.Write(failed.Length);
            foreach (int id in failed) writer.Write(id);
            world.WriteCheckpoint(writer);
        }

        public void Update(SimulationContext context) {
            var active = context.Objects.Where(owner => owner.IsActive).ToArray(); Synchronize(active);
            foreach (var owner in active) if (bindings.TryGetValue(owner.Id, out var binding) && binding.Sphere != 0) world.SetVelocity(binding.Sphere, owner.GetComponent<RigidBody>()?.ConsumeVelocity() ?? Vector3.Zero);
            world.Step(context.DeltaTime, () => {
                // 충격량 계산 뒤 콜백이 최종 속도를 읽고 바꾼 다음, 엔진이 위치를 적분한다.
                foreach (var owner in active) if (bindings.TryGetValue(owner.Id, out var binding) && binding.Sphere != 0) owner.GetComponent<RigidBody>()?.AddVelocity(world.Velocity(binding.Sphere));
                Dispatch(active);
                var remaining = new HashSet<int>(context.Objects.Where(owner => owner.IsActive).Select(owner => owner.Id));
                foreach (var binding in bindings.Values) if (binding.Sphere != 0) {
                    if (!remaining.Contains(binding.Owner.Id)) { world.SetVelocity(binding.Sphere, Vector3.Zero); continue; }
                    world.Move(binding.Sphere, binding.Owner.Position);
                    world.SetVelocity(binding.Sphere, binding.Owner.GetComponent<RigidBody>()?.ConsumeVelocity() ?? Vector3.Zero);
                }
            });
            foreach (var owner in context.Objects.Where(owner => owner.IsActive).ToArray()) {
                if (bindings.TryGetValue(owner.Id, out var binding) && binding.Sphere != 0) owner.SetPosition(world.Position(binding.Sphere));
                else owner.GetComponent<RigidBody>()?.ApplyVelocity();
                var vertical = owner.GetComponent<ZPhysics>(); if (vertical == null) continue;
                if (vertical.CanHover) vertical.ApplyHover(); else vertical.ApplyZForce();
            }
        }
    }
}
