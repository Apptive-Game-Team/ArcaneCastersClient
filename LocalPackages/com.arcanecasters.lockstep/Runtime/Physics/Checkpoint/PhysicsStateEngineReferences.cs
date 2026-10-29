using System;
using System.IO;
using ArcaneCasters.Simulation;
namespace BEPUphysics.DeactivationManagement {
    partial class SimulationIslandMember {
        internal void WriteCheckpointReferences(PhysicsStateEngineWriter capture) {
            capture.Island(simulationIsland); capture.Writer.Write(connections.Count);
            foreach (var connection in connections) {
                capture.Writer.Write(connection.SlatedForRemoval); capture.Writer.Write(connection.entries.Count);
                foreach (var entry in connection.entries) { capture.Writer.Write(capture.Id(entry.Member.Owner)); capture.Writer.Write(entry.Index); }
            }
        }
    }
    partial class DeactivationManager {
        internal void WriteCheckpointReferences(PhysicsStateEngineWriter capture) {
            if (member1Friends.Count != 0 || member2Friends.Count != 0 || searchedMembers1.Count != 0 || searchedMembers2.Count != 0)
                throw new InvalidOperationException("Physics checkpoint was captured during island search.");
            capture.Writer.Write(simulationIslandMembers.Count);
            foreach (var member in simulationIslandMembers) capture.Writer.Write(capture.Id(member.Owner));
            capture.Writer.Write(simulationIslands.Count); foreach (var island in simulationIslands) capture.Island(island);
            var pending = new SimulationIslandConnection[splitAttempts.Count]; for (int index = 0; index < pending.Length; index++) pending[index] = splitAttempts.array[(splitAttempts.firstIndex + index) % splitAttempts.array.Length]; capture.Writer.Write(pending.Length);
            foreach (var connection in pending) {
                capture.Writer.Write(connection.SlatedForRemoval); capture.Writer.Write(connection.entries.Count);
                foreach (var entry in connection.entries) { capture.Writer.Write(capture.Id(entry.Member.Owner)); capture.Writer.Write(entry.Index); }
            }
        }
    }
}
namespace BEPUphysics.CollisionTests.Manifolds {
    partial class ContactManifold {
        internal void WriteCheckpointPendingRemovals(BinaryWriter writer) {
            writer.Write(contactIndicesToRemove?.Count ?? 0);
            if (contactIndicesToRemove != null) foreach (int index in contactIndicesToRemove) writer.Write(index);
        }
    }
    partial class GeneralConvexContactManifold {
        internal void WriteCheckpointReferences(PhysicsStateEngineWriter capture) {
            capture.Writer.Write(supplementData.Count);
            for (int index = 0; index < supplementData.Count; index++) PhysicsStateValue.Write(capture.Writer, supplementData.Elements[index]);
            pairTester.WriteCheckpointScalars_GeneralConvexPairTester(capture.Writer);
        }
    }
}
namespace BEPUphysics.Constraints.Collision {
    partial class TwistFrictionConstraint {
        internal void WriteCheckpointLeverArms(BinaryWriter writer) { writer.Write(leverArms.Length); foreach (var arm in leverArms) writer.Write(arm.RawValue); }
    }
    partial class ConvexContactManifoldConstraint {
        internal void WriteCheckpointPoolOrder(BinaryWriter writer) {
                        writer.Write(penetrationConstraintPool.Count);
            foreach (var constraint in penetrationConstraintPool) constraint.SolverSettings.WriteCheckpointScalars_SolverSettings(writer);
        }
    }
}
namespace BEPUphysics.Constraints.Collision {
    partial class NonConvexContactManifoldConstraint {
        internal void WriteCheckpointPoolOrder(System.IO.BinaryWriter writer) {
            writer.Write(penetrationConstraintPool.Count); foreach (var constraint in penetrationConstraintPool) constraint.SolverSettings.WriteCheckpointScalars_SolverSettings(writer);
            writer.Write(frictionConstraintPool.Count); foreach (var constraint in frictionConstraintPool) constraint.SolverSettings.WriteCheckpointScalars_SolverSettings(writer);
        }
    }
}
