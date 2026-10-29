using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseSystems.Hierarchies;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.CollisionTests;
using BEPUphysics.CollisionTests.Manifolds;
using BEPUphysics.Constraints;
using BEPUphysics.Constraints.Collision;
using BEPUphysics.Constraints.SolverGroups;
using BEPUphysics.DeactivationManagement;
using BEPUphysics.Entities;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUutilities;

namespace ArcaneCasters.Simulation {
    // 참조 주소 대신 world 안의 명시적 body ID와 순회 순번을 쓴다. 복원 형식이 아니다.
    public sealed class PhysicsStateEngineWriter {
        internal readonly BinaryWriter Writer;
        private readonly SortedDictionary<int, Entity> bodies;
        private readonly Dictionary<SimulationIsland, int> islands = new Dictionary<SimulationIsland, int>();
        public PhysicsStateEngineWriter(BinaryWriter writer, SortedDictionary<int, Entity> bodies) { Writer = writer; this.bodies = bodies; }
        internal int Id(Entity entity) {
            if (entity == null) return 0;
            if (!(entity.Tag is int id) || !bodies.TryGetValue(id, out var registered) || !ReferenceEquals(registered, entity))
                throw new InvalidOperationException("Physics checkpoint encountered an unregistered body.");
            return id;
        }
        internal int Id(BroadPhaseEntry entry) {
            if (entry == null) return 0;
            if (!(entry.Tag is int id) || !bodies.TryGetValue(id, out var body) || !ReferenceEquals(body.CollisionInformation, entry))
                throw new InvalidOperationException("Physics checkpoint encountered an unregistered collidable.");
            return id;
        }
        internal void Vector(Vector3 value) => PhysicsStateValue.Write(Writer, value);
        internal void Contact(Contact contact) {
            Writer.Write(contact != null);
            if (contact == null) return;
            Writer.Write(contact.Id); Vector(contact.Position); Vector(contact.Normal); Writer.Write(contact.PenetrationDepth.RawValue);
        }
        internal void Island(SimulationIsland island) {
            if (island == null) { Writer.Write(0); return; }
            if (islands.TryGetValue(island, out int known)) { Writer.Write(known); Writer.Write(false); return; }
            int next = islands.Count + 1; islands.Add(island, next); Writer.Write(next); Writer.Write(true);
            island.WriteCheckpointScalars_SimulationIsland(Writer);
            Island(ReferenceEquals(island.immediateParent, island) ? null : island.immediateParent);
        }
        internal void Body(Entity body) {
            Writer.Write(Id(body)); body.WriteCheckpointScalars_Entity(Writer);
            body.Material.WriteCheckpointScalars_Material(Writer);
            body.CollisionInformation.WriteCheckpointScalars_BroadPhaseEntry(Writer);
            body.CollisionInformation.WriteCheckpointScalars_EntityCollidable(Writer);
            var shape = body.CollisionInformation.Shape;
            Writer.Write(shape.GetType().FullName);
            shape.WriteCheckpointScalars_CollisionShape(Writer); ((BEPUphysics.CollisionShapes.EntityShape)shape).WriteCheckpointScalars_EntityShape(Writer);
            ((ConvexShape)shape).WriteCheckpointScalars_ConvexShape(Writer);
            Writer.Write(((ConvexShape)shape).CollisionMargin.RawValue);
            if (shape is SphereShape sphere) { sphere.WriteCheckpointScalars_SphereShape(Writer); Writer.Write(sphere.Radius.RawValue); }
            else if (shape is BoxShape box) { box.WriteCheckpointScalars_BoxShape(Writer); Writer.Write(box.Width.RawValue); Writer.Write(box.Height.RawValue); Writer.Write(box.Length.RawValue); }
            else if (shape is CapsuleShape capsule) { capsule.WriteCheckpointScalars_CapsuleShape(Writer); Writer.Write(capsule.Length.RawValue); Writer.Write(capsule.Radius.RawValue); }
            else throw new InvalidOperationException("Physics checkpoint does not support shape " + shape.GetType().FullName);
            var rules = body.CollisionInformation.CollisionRules;
            Writer.Write((int)rules.Personal);
            // 공개된 PhysicsWorld는 사용자 정의 collision group을 설치하지 않는다.
            if (!ReferenceEquals(rules.Group, body.IsDynamic ? CollisionRules.DefaultDynamicCollisionGroup : CollisionRules.DefaultKinematicCollisionGroup))
                throw new InvalidOperationException("Physics checkpoint does not support custom collision groups.");
            Writer.Write(body.IsDynamic);
            var specific = rules.Specific.WrappedDictionary.Select(pair => new KeyValuePair<int, CollisionRule>(RuleOwner(pair.Key), pair.Value)).OrderBy(pair => pair.Key).ToArray();
            Writer.Write(specific.Length);
            foreach (var rule in specific) { Writer.Write(rule.Key); Writer.Write((int)rule.Value); }
            body.ActivityInformation.WriteCheckpointScalars_SimulationIslandMember(Writer);
            body.ActivityInformation.WriteCheckpointReferences(this);
        }
        private int RuleOwner(CollisionRules rule) {
            foreach (var pair in bodies) if (ReferenceEquals(pair.Value.CollisionInformation.CollisionRules, rule)) return pair.Key;
            throw new InvalidOperationException("Physics checkpoint encountered an external collision rule.");
        }
        internal void Tree(Node node) {
            Writer.Write(node != null); if (node == null) return;
            Writer.Write("broadphase-node"); Writer.Write(node.IsLeaf); PhysicsStateValue.Write(Writer, node.BoundingBox);
            if (node.IsLeaf) { Writer.Write(Id(node.Element)); return; }
            var branch = (InternalNode)node;
            Writer.Write("currentVolume"); Writer.Write(branch.currentVolume.RawValue); Writer.Write("maximumVolume"); Writer.Write(branch.maximumVolume.RawValue);
            Tree(node.ChildA); Tree(node.ChildB);
        }
        internal void Pair(CollidablePairHandler pair) {
            Writer.Write(pair.GetType().FullName); Writer.Write(Id(pair.EntityA)); Writer.Write(Id(pair.EntityB));
            Writer.Write(pair.NeedsUpdate); Writer.Write((int)pair.CollisionRule);
            pair.WriteCheckpointScalars_CollidablePairHandler(Writer);
            if (!(pair is StandardPairHandler standard)) throw new InvalidOperationException("Unsupported physics checkpoint pair.");
            var manifold = standard.ContactManifold;
            Writer.Write(manifold.GetType().FullName); manifold.WriteCheckpointScalars_ContactManifold(Writer);
            Writer.Write(manifold.Contacts.Count);
            foreach (var contact in manifold.Contacts) Contact(contact);
            manifold.WriteCheckpointPendingRemovals(Writer);
            if (manifold is GeneralConvexContactManifold general) general.WriteCheckpointReferences(this);
            else if (manifold is SphereContactManifold sphere) sphere.WriteCheckpointScalars_SphereContactManifold(Writer);
            else if (manifold is BoxSphereContactManifold boxSphere) boxSphere.WriteCheckpointScalars_BoxSphereContactManifold(Writer);
            else if (!(manifold is BoxContactManifold)) throw new InvalidOperationException("Unsupported physics checkpoint manifold.");
            Constraint(standard.ContactConstraint);
        }
        internal void Constraint(SolverUpdateable constraint) {
            Writer.Write(constraint.GetType().FullName); Writer.Write(constraint.isActive);
            constraint.SolverSettings.WriteCheckpointScalars_SolverSettings(Writer);
            if (!constraint.isActive) return;
            constraint.WriteCheckpointScalars_SolverUpdateable(Writer);
            constraint.SolverSettings.WriteCheckpointScalars_SolverSettings(Writer);
            Writer.Write(constraint.involvedEntities.Count);
            foreach (var entity in constraint.involvedEntities) Writer.Write(Id(entity));
            if (constraint is ContactManifoldConstraint manifold) {
                manifold.WriteCheckpointScalars_ContactManifoldConstraint(Writer);
                Writer.Write(Id(manifold.EntityA)); Writer.Write(Id(manifold.EntityB));
            }
            // 미사용 pool의 계산 scratch는 Setup/Update에서 덮어쓴다. slot 순서는 아래에 별도로 보존한다.
            if (constraint.isActive) {
                if (constraint is ContactPenetrationConstraint penetration) { Contact(penetration.contact); penetration.WriteCheckpointScalars_ContactPenetrationConstraint(Writer); }
                else if (constraint is SlidingFrictionTwoAxis sliding) sliding.WriteCheckpointScalars_SlidingFrictionTwoAxis(Writer);
                else if (constraint is TwistFrictionConstraint twist) { twist.WriteCheckpointScalars_TwistFrictionConstraint(Writer); }
                else if (constraint is ContactFrictionConstraint friction) { Contact(friction.PenetrationConstraint.contact); friction.WriteCheckpointScalars_ContactFrictionConstraint(Writer); }
                else if (!(constraint is ConvexContactManifoldConstraint) && !(constraint is NonConvexContactManifoldConstraint)) throw new InvalidOperationException("Unsupported physics checkpoint constraint: " + constraint.GetType().FullName);
            }
            // 원본 solver는 allocation slot 대신 contact 추가순으로 Update/Solve를 수행한다.
            if (constraint is ConvexContactManifoldConstraint convex) {
                Writer.Write(convex.ContactPenetrationConstraints.Count);
                foreach (var child in convex.ContactPenetrationConstraints) Constraint(child);
                Constraint(convex.SlidingFriction); Constraint(convex.TwistFriction);
                convex.WriteCheckpointPoolOrder(Writer);
            } else if (constraint is NonConvexContactManifoldConstraint nonConvex) {
                Writer.Write(nonConvex.ContactPenetrationConstraints.Count);
                foreach (var child in nonConvex.ContactPenetrationConstraints) Constraint(child);
                Writer.Write(nonConvex.ContactFrictionConstraints.Count);
                foreach (var child in nonConvex.ContactFrictionConstraints) Constraint(child);
                nonConvex.WriteCheckpointPoolOrder(Writer);
            } else if (constraint is SolverGroup) throw new InvalidOperationException("Unsupported checkpoint solver group.");
        }
        public void World(Space space) {
            if (space.ParallelLooper != null) throw new InvalidOperationException("Checkpoint requires single-thread physics.");
            Writer.Write(space.TimeStepSettings.TimeStepDuration.RawValue); Writer.Write(space.TimeStepSettings.AccumulatedTime.RawValue); Writer.Write(space.TimeStepSettings.MaximumTimeStepsPerFrame);
            Vector(space.ForceUpdater.Gravity);
            space.DeactivationManager.WriteCheckpointScalars_DeactivationManager(Writer);
            space.DeactivationManager.WriteCheckpointReferences(this);
            Writer.Write(bodies.Count); foreach (var pair in bodies) Body(pair.Value);
            if (!(space.BroadPhase is DynamicHierarchy hierarchy)) throw new InvalidOperationException("Unsupported physics checkpoint broad phase.");
            Tree(hierarchy.root);
            Writer.Write(hierarchy.Overlaps.Count);
            foreach (var overlap in hierarchy.Overlaps) { Writer.Write(Id(overlap.EntryA)); Writer.Write(Id(overlap.EntryB)); Writer.Write((int)overlap.CollisionRule); }
            Writer.Write(space.NarrowPhase.Pairs.Count);
            foreach (var pair in space.NarrowPhase.Pairs) {
                if (!(pair is CollidablePairHandler collidable)) throw new InvalidOperationException("Unsupported narrow phase pair.");
                Pair(collidable);
            }
            space.Solver.WriteCheckpointScalars_Solver(Writer); space.Solver.PermutationMapper.WriteCheckpointScalars_PermutationMapper(Writer);
            Writer.Write(space.Solver.SolverUpdateables.Count);
            foreach (var constraint in space.Solver.SolverUpdateables) {
                if (!(constraint is ContactManifoldConstraint manifold)) throw new InvalidOperationException("External constraints are not supported.");
                Writer.Write(Id(manifold.EntityA)); Writer.Write(Id(manifold.EntityB));
            }
        }
    }
}
