// Generate-PhysicsStateAccessors.ps1로 생성한다. 실행 중 reflection을 사용하지 않는다.
namespace BEPUutilities { partial class PermutationMapper {
    internal void WriteCheckpointScalars_PermutationMapper(System.IO.BinaryWriter writer) {
        writer.Write("BEPUutilities.PermutationMapper.currentOffset");
        writer.Write(currentOffset);
        writer.Write("BEPUutilities.PermutationMapper.currentPrime");
        writer.Write(currentPrime);
        writer.Write("BEPUutilities.PermutationMapper.permutationIndex");
        writer.Write(permutationIndex);
    }
} }
namespace BEPUphysics.Constraints.Collision { partial class ContactFrictionConstraint {
    internal void WriteCheckpointScalars_ContactFrictionConstraint(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.Constraints.Collision.ContactFrictionConstraint.accumulatedImpulse");
        writer.Write(accumulatedImpulse.RawValue);
    }
} }
namespace BEPUphysics.CollisionShapes { partial class CollisionShape {
    internal void WriteCheckpointScalars_CollisionShape(System.IO.BinaryWriter writer) {
    }
} }
namespace BEPUphysics.CollisionShapes { partial class EntityShape {
    internal void WriteCheckpointScalars_EntityShape(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.CollisionShapes.EntityShape.<Volume>k__BackingField");
        writer.Write(Volume.RawValue);
        writer.Write("BEPUphysics.CollisionShapes.EntityShape.volumeDistribution");
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, volumeDistribution);
    }
} }
namespace BEPUphysics.CollisionShapes.ConvexShapes { partial class ConvexShape {
    internal void WriteCheckpointScalars_ConvexShape(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.CollisionShapes.ConvexShapes.ConvexShape.<MaximumRadius>k__BackingField");
        writer.Write(MaximumRadius.RawValue);
        writer.Write("BEPUphysics.CollisionShapes.ConvexShapes.ConvexShape.<MinimumRadius>k__BackingField");
        writer.Write(MinimumRadius.RawValue);
        writer.Write("BEPUphysics.CollisionShapes.ConvexShapes.ConvexShape.collisionMargin");
        writer.Write(collisionMargin.RawValue);
    }
} }
namespace BEPUphysics.CollisionShapes.ConvexShapes { partial class SphereShape {
    internal void WriteCheckpointScalars_SphereShape(System.IO.BinaryWriter writer) {
    }
} }
namespace BEPUphysics.CollisionShapes.ConvexShapes { partial class BoxShape {
    internal void WriteCheckpointScalars_BoxShape(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.CollisionShapes.ConvexShapes.BoxShape.halfHeight");
        writer.Write(halfHeight.RawValue);
        writer.Write("BEPUphysics.CollisionShapes.ConvexShapes.BoxShape.halfLength");
        writer.Write(halfLength.RawValue);
        writer.Write("BEPUphysics.CollisionShapes.ConvexShapes.BoxShape.halfWidth");
        writer.Write(halfWidth.RawValue);
    }
} }
namespace BEPUphysics.CollisionShapes.ConvexShapes { partial class CapsuleShape {
    internal void WriteCheckpointScalars_CapsuleShape(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.CollisionShapes.ConvexShapes.CapsuleShape.halfLength");
        writer.Write(halfLength.RawValue);
    }
} }
namespace BEPUphysics.Entities { partial class Entity {
    internal void WriteCheckpointScalars_Entity(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.Entities.Entity.<IgnoreShapeChanges>k__BackingField");
        writer.Write(IgnoreShapeChanges);
        writer.Write("BEPUphysics.Entities.Entity.angularDamping");
        writer.Write(angularDamping.RawValue);
        writer.Write("BEPUphysics.Entities.Entity.angularDampingBoost");
        writer.Write(angularDampingBoost.RawValue);
        writer.Write("BEPUphysics.Entities.Entity.angularVelocity");
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, angularVelocity);
        writer.Write("BEPUphysics.Entities.Entity.hasPersonalGravity");
        writer.Write(hasPersonalGravity);
        writer.Write("BEPUphysics.Entities.Entity.inertiaTensor");
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, inertiaTensor);
        writer.Write("BEPUphysics.Entities.Entity.inertiaTensorInverse");
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, inertiaTensorInverse);
        writer.Write("BEPUphysics.Entities.Entity.inverseMass");
        writer.Write(inverseMass.RawValue);
        writer.Write("BEPUphysics.Entities.Entity.isDynamic");
        writer.Write(isDynamic);
        writer.Write("BEPUphysics.Entities.Entity.linearDamping");
        writer.Write(linearDamping.RawValue);
        writer.Write("BEPUphysics.Entities.Entity.linearDampingBoost");
        writer.Write(linearDampingBoost.RawValue);
        writer.Write("BEPUphysics.Entities.Entity.linearVelocity");
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, linearVelocity);
        writer.Write("BEPUphysics.Entities.Entity.localInertiaTensor");
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, localInertiaTensor);
        writer.Write("BEPUphysics.Entities.Entity.localInertiaTensorInverse");
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, localInertiaTensorInverse);
        writer.Write("BEPUphysics.Entities.Entity.mass");
        writer.Write(mass.RawValue);
        writer.Write("BEPUphysics.Entities.Entity.orientation");
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, orientation);
        writer.Write("BEPUphysics.Entities.Entity.orientationMatrix");
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, orientationMatrix);
        writer.Write("BEPUphysics.Entities.Entity.personalGravity");
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, personalGravity);
        writer.Write("BEPUphysics.Entities.Entity.position");
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, position);
        writer.Write("BEPUphysics.Entities.Entity.positionUpdateMode");
        writer.Write((int)positionUpdateMode);
    }
} }
namespace BEPUphysics.Materials { partial class Material {
    internal void WriteCheckpointScalars_Material(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.Materials.Material.<MaximumContactImpulse>k__BackingField");
        writer.Write(MaximumContactImpulse.RawValue);
        writer.Write("BEPUphysics.Materials.Material.<VelocityOnlyContactResponse>k__BackingField");
        writer.Write(VelocityOnlyContactResponse);
        writer.Write("BEPUphysics.Materials.Material.bounciness");
        writer.Write(bounciness.RawValue);
        writer.Write("BEPUphysics.Materials.Material.kineticFriction");
        writer.Write(kineticFriction.RawValue);
        writer.Write("BEPUphysics.Materials.Material.staticFriction");
        writer.Write(staticFriction.RawValue);
    }
} }
namespace BEPUphysics.DeactivationManagement { partial class SimulationIslandMember {
    internal void WriteCheckpointScalars_SimulationIslandMember(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.DeactivationManagement.SimulationIslandMember.allowStabilization");
        writer.Write(allowStabilization);
        writer.Write("BEPUphysics.DeactivationManagement.SimulationIslandMember.isAlwaysActive");
        writer.Write(isAlwaysActive);
        writer.Write("BEPUphysics.DeactivationManagement.SimulationIslandMember.isDeactivationCandidate");
        writer.Write(isDeactivationCandidate);
        writer.Write("BEPUphysics.DeactivationManagement.SimulationIslandMember.isSlowing");
        writer.Write(isSlowing);
        writer.Write("BEPUphysics.DeactivationManagement.SimulationIslandMember.previouslyActive");
        writer.Write(previouslyActive);
        writer.Write("BEPUphysics.DeactivationManagement.SimulationIslandMember.previousVelocity");
        writer.Write(previousVelocity.RawValue);
        writer.Write("BEPUphysics.DeactivationManagement.SimulationIslandMember.searchState");
        writer.Write((int)searchState);
        writer.Write("BEPUphysics.DeactivationManagement.SimulationIslandMember.velocityTimeBelowLimit");
        writer.Write(velocityTimeBelowLimit.RawValue);
    }
} }
namespace BEPUphysics.DeactivationManagement { partial class SimulationIsland {
    internal void WriteCheckpointScalars_SimulationIsland(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.DeactivationManagement.SimulationIsland.allowDeactivation");
        writer.Write(allowDeactivation);
        writer.Write("BEPUphysics.DeactivationManagement.SimulationIsland.deactivationCandidateCount");
        writer.Write(deactivationCandidateCount);
        writer.Write("BEPUphysics.DeactivationManagement.SimulationIsland.isActive");
        writer.Write(isActive);
        writer.Write("BEPUphysics.DeactivationManagement.SimulationIsland.memberCount");
        writer.Write(memberCount);
    }
} }
namespace BEPUphysics.DeactivationManagement { partial class DeactivationManager {
    internal void WriteCheckpointScalars_DeactivationManager(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.DeactivationManagement.DeactivationManager.deactivationIslandIndex");
        writer.Write(deactivationIslandIndex);
        writer.Write("BEPUphysics.DeactivationManagement.DeactivationManager.lowVelocityTimeMinimum");
        writer.Write(lowVelocityTimeMinimum.RawValue);
        writer.Write("BEPUphysics.DeactivationManagement.DeactivationManager.maximumDeactivationAttemptsPerFrame");
        writer.Write(maximumDeactivationAttemptsPerFrame);
        writer.Write("BEPUphysics.DeactivationManagement.DeactivationManager.useStabilization");
        writer.Write(useStabilization);
        writer.Write("BEPUphysics.DeactivationManagement.DeactivationManager.velocityLowerLimit");
        writer.Write(velocityLowerLimit.RawValue);
        writer.Write("BEPUphysics.DeactivationManagement.DeactivationManager.velocityLowerLimitSquared");
        writer.Write(velocityLowerLimitSquared.RawValue);
    }
} }
namespace BEPUphysics.Constraints { partial class Solver {
    internal void WriteCheckpointScalars_Solver(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.Constraints.Solver.iterationLimit");
        writer.Write(iterationLimit);
    }
} }
namespace BEPUphysics.Constraints { partial class SolverUpdateable {
    internal void WriteCheckpointScalars_SolverUpdateable(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.Constraints.SolverUpdateable.isActive");
        writer.Write(isActive);
        writer.Write("BEPUphysics.Constraints.SolverUpdateable.isActiveInSolver");
        writer.Write((solver != null || SolverGroup?.Solver != null) && isActiveInSolver);
        writer.Write("BEPUphysics.Constraints.SolverUpdateable.numberOfInvolvedEntities");
        writer.Write(numberOfInvolvedEntities);
        writer.Write("BEPUphysics.Constraints.SolverUpdateable.solverIndex");
        writer.Write(solver == null ? -1 : solverIndex);
    }
} }
namespace BEPUphysics.Constraints { partial class SolverSettings {
    internal void WriteCheckpointScalars_SolverSettings(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.Constraints.SolverSettings.maximumIterationCount");
        writer.Write(maximumIterationCount);
        writer.Write("BEPUphysics.Constraints.SolverSettings.minimumImpulse");
        writer.Write(minimumImpulse.RawValue);
        writer.Write("BEPUphysics.Constraints.SolverSettings.minimumIterationCount");
        writer.Write(minimumIterationCount);
    }
} }
namespace BEPUphysics.Constraints.Collision { partial class ContactManifoldConstraint {
    internal void WriteCheckpointScalars_ContactManifoldConstraint(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.Constraints.Collision.ContactManifoldConstraint.materialInteraction");
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, materialInteraction);
    }
} }
namespace BEPUphysics.Constraints.Collision { partial class ContactPenetrationConstraint {
    internal void WriteCheckpointScalars_ContactPenetrationConstraint(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.Constraints.Collision.ContactPenetrationConstraint.accumulatedImpulse");
        writer.Write(accumulatedImpulse.RawValue);
    }
} }
namespace BEPUphysics.Constraints.Collision { partial class SlidingFrictionTwoAxis {
    internal void WriteCheckpointScalars_SlidingFrictionTwoAxis(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.Constraints.Collision.SlidingFrictionTwoAxis.accumulatedImpulse");
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, accumulatedImpulse);
    }
} }
namespace BEPUphysics.Constraints.Collision { partial class TwistFrictionConstraint {
    internal void WriteCheckpointScalars_TwistFrictionConstraint(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.Constraints.Collision.TwistFrictionConstraint.accumulatedImpulse");
        writer.Write(accumulatedImpulse.RawValue);
    }
} }
namespace BEPUphysics.NarrowPhaseSystems.Pairs { partial class CollidablePairHandler {
    internal void WriteCheckpointScalars_CollidablePairHandler(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler.listIndexA");
        writer.Write(listIndexA);
        writer.Write("BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler.listIndexB");
        writer.Write(listIndexB);
        writer.Write("BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler.previousContactCount");
        writer.Write(previousContactCount);
        writer.Write("BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler.suppressEvents");
        writer.Write(suppressEvents);
        writer.Write("BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler.timeOfImpact");
        writer.Write(timeOfImpact.RawValue);
    }
} }
namespace BEPUphysics.CollisionTests.Manifolds { partial class ContactManifold {
    internal void WriteCheckpointScalars_ContactManifold(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.CollisionTests.Manifolds.ContactManifold.<IsQuery>k__BackingField");
        writer.Write(IsQuery);
    }
} }
namespace BEPUphysics.CollisionTests.Manifolds { partial class SphereContactManifold {
    internal void WriteCheckpointScalars_SphereContactManifold(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.CollisionTests.Manifolds.SphereContactManifold.previouslyColliding");
        writer.Write(previouslyColliding);
    }
} }
namespace BEPUphysics.CollisionTests.Manifolds { partial class BoxSphereContactManifold {
    internal void WriteCheckpointScalars_BoxSphereContactManifold(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.CollisionTests.Manifolds.BoxSphereContactManifold.previouslyColliding");
        writer.Write(previouslyColliding);
    }
} }
namespace BEPUphysics.CollisionTests.CollisionAlgorithms { partial class GeneralConvexPairTester {
    internal void WriteCheckpointScalars_GeneralConvexPairTester(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.CollisionTests.CollisionAlgorithms.GeneralConvexPairTester.cachedSimplex");
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, cachedSimplex);
        writer.Write("BEPUphysics.CollisionTests.CollisionAlgorithms.GeneralConvexPairTester.localDirection");
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, localDirection);
        writer.Write("BEPUphysics.CollisionTests.CollisionAlgorithms.GeneralConvexPairTester.localSeparatingAxis");
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, localSeparatingAxis);
        writer.Write("BEPUphysics.CollisionTests.CollisionAlgorithms.GeneralConvexPairTester.previousState");
        writer.Write((int)previousState);
        writer.Write("BEPUphysics.CollisionTests.CollisionAlgorithms.GeneralConvexPairTester.state");
        writer.Write((int)state);
    }
} }
namespace BEPUphysics.BroadPhaseEntries { partial class BroadPhaseEntry {
    internal void WriteCheckpointScalars_BroadPhaseEntry(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.BroadPhaseEntries.BroadPhaseEntry.boundingBox");
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, boundingBox);
    }
} }
namespace BEPUphysics.BroadPhaseEntries.MobileCollidables { partial class EntityCollidable {
    internal void WriteCheckpointScalars_EntityCollidable(System.IO.BinaryWriter writer) {
        writer.Write("BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable.localPosition");
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, localPosition);
        writer.Write("BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable.worldTransform");
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, worldTransform);
    }
} }
namespace ArcaneCasters.Simulation { public static class PhysicsStateValue {
    public static void Write(System.IO.BinaryWriter writer, global::BEPUutilities.Matrix3x3 value) {
        writer.Write(value.M11.RawValue);
        writer.Write(value.M12.RawValue);
        writer.Write(value.M13.RawValue);
        writer.Write(value.M21.RawValue);
        writer.Write(value.M22.RawValue);
        writer.Write(value.M23.RawValue);
        writer.Write(value.M31.RawValue);
        writer.Write(value.M32.RawValue);
        writer.Write(value.M33.RawValue);
    }
    public static void Write(System.IO.BinaryWriter writer, global::BEPUutilities.Vector3 value) {
        writer.Write(value.X.RawValue);
        writer.Write(value.Y.RawValue);
        writer.Write(value.Z.RawValue);
    }
    public static void Write(System.IO.BinaryWriter writer, global::BEPUutilities.Quaternion value) {
        writer.Write(value.W.RawValue);
        writer.Write(value.X.RawValue);
        writer.Write(value.Y.RawValue);
        writer.Write(value.Z.RawValue);
    }
    public static void Write(System.IO.BinaryWriter writer, global::BEPUphysics.Materials.InteractionProperties value) {
        writer.Write(value.Bounciness.RawValue);
        writer.Write(value.KineticFriction.RawValue);
        writer.Write(value.StaticFriction.RawValue);
    }
    public static void Write(System.IO.BinaryWriter writer, global::BEPUutilities.Vector2 value) {
        writer.Write(value.X.RawValue);
        writer.Write(value.Y.RawValue);
    }
    public static void Write(System.IO.BinaryWriter writer, global::BEPUphysics.CollisionTests.CollisionAlgorithms.GJK.CachedSimplex value) {
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, value.LocalSimplexA);
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, value.LocalSimplexB);
        writer.Write((int)value.State);
    }
    public static void Write(System.IO.BinaryWriter writer, global::BEPUutilities.BoundingBox value) {
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, value.Max);
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, value.Min);
    }
    public static void Write(System.IO.BinaryWriter writer, global::BEPUutilities.RigidTransform value) {
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, value.Orientation);
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, value.Position);
    }
    public static void Write(System.IO.BinaryWriter writer, global::BEPUphysics.CollisionTests.ContactSupplementData value) {
        writer.Write(value.BasePenetrationDepth.RawValue);
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, value.LocalOffsetA);
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, value.LocalOffsetB);
    }
    public static void Write(System.IO.BinaryWriter writer, global::BEPUphysics.CollisionTests.CollisionAlgorithms.GJK.ContributingShapeSimplex value) {
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, value.A);
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, value.B);
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, value.C);
        ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, value.D);
    }
} }
namespace ArcaneCasters.Simulation { public static class PhysicsStateFieldManifest { public static readonly string[] Expected = {
    "BEPUutilities.PermutationMapper|currentOffset|System.Int64|typed-scalar",
    "BEPUutilities.PermutationMapper|currentPrime|System.Int64|typed-scalar",
    "BEPUutilities.PermutationMapper|permutationIndex|System.Int64|typed-scalar",
    "BEPUphysics.Constraints.Collision.ContactFrictionConstraint|accumulatedImpulse|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.Constraints.Collision.ContactFrictionConstraint|angularAX|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactFrictionConstraint|angularAY|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactFrictionConstraint|angularAZ|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactFrictionConstraint|angularBX|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactFrictionConstraint|angularBY|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactFrictionConstraint|angularBZ|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactFrictionConstraint|contactManifoldConstraint|BEPUphysics.Constraints.Collision.ContactManifoldConstraint|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.ContactFrictionConstraint|entityA|BEPUphysics.Entities.Entity|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.ContactFrictionConstraint|entityAIsDynamic|System.Boolean|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactFrictionConstraint|entityB|BEPUphysics.Entities.Entity|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.ContactFrictionConstraint|entityBIsDynamic|System.Boolean|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactFrictionConstraint|friction|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactFrictionConstraint|linearAX|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactFrictionConstraint|linearAY|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactFrictionConstraint|linearAZ|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactFrictionConstraint|penetrationConstraint|BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.ContactFrictionConstraint|velocityToImpulse|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.CollisionShapes.CollisionShape|ShapeChanged|System.Action`1[BEPUphysics.CollisionShapes.CollisionShape]|explicit-reference-or-excluded",
    "BEPUphysics.CollisionShapes.EntityShape|<Volume>k__BackingField|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.CollisionShapes.EntityShape|volumeDistribution|BEPUutilities.Matrix3x3|typed-scalar",
    "BEPUphysics.CollisionShapes.ConvexShapes.ConvexShape|<MaximumRadius>k__BackingField|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.CollisionShapes.ConvexShapes.ConvexShape|<MinimumRadius>k__BackingField|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.CollisionShapes.ConvexShapes.ConvexShape|collisionMargin|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.CollisionShapes.ConvexShapes.BoxShape|halfHeight|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.CollisionShapes.ConvexShapes.BoxShape|halfLength|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.CollisionShapes.ConvexShapes.BoxShape|halfWidth|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.CollisionShapes.ConvexShapes.CapsuleShape|halfLength|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.Entities.Entity|<BEPUphysics.PositionUpdating.IPositionUpdateable.PositionUpdater>k__BackingField|BEPUphysics.PositionUpdating.PositionUpdater|explicit-reference-or-excluded",
    "BEPUphysics.Entities.Entity|<BufferedStates>k__BackingField|BEPUphysics.EntityStateManagement.EntityBufferedStates|explicit-reference-or-excluded",
    "BEPUphysics.Entities.Entity|<IgnoreShapeChanges>k__BackingField|System.Boolean|typed-scalar",
    "BEPUphysics.Entities.Entity|<InstanceId>k__BackingField|System.Int64|process-identity",
    "BEPUphysics.Entities.Entity|<Tag>k__BackingField|System.Object|explicit-reference-or-excluded",
    "BEPUphysics.Entities.Entity|activityInformation|BEPUphysics.DeactivationManagement.SimulationIslandMember|explicit-reference-or-excluded",
    "BEPUphysics.Entities.Entity|angularDamping|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.Entities.Entity|angularDampingBoost|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.Entities.Entity|angularVelocity|BEPUutilities.Vector3|typed-scalar",
    "BEPUphysics.Entities.Entity|collisionInformation|BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable|explicit-reference-or-excluded",
    "BEPUphysics.Entities.Entity|forceUpdater|BEPUphysics.OtherSpaceStages.ForceUpdater|explicit-reference-or-excluded",
    "BEPUphysics.Entities.Entity|hashCode|System.Int32|process-identity",
    "BEPUphysics.Entities.Entity|hasPersonalGravity|System.Boolean|typed-scalar",
    "BEPUphysics.Entities.Entity|inertiaTensor|BEPUutilities.Matrix3x3|typed-scalar",
    "BEPUphysics.Entities.Entity|inertiaTensorInverse|BEPUutilities.Matrix3x3|typed-scalar",
    "BEPUphysics.Entities.Entity|inverseMass|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.Entities.Entity|isDynamic|System.Boolean|typed-scalar",
    "BEPUphysics.Entities.Entity|linearDamping|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.Entities.Entity|linearDampingBoost|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.Entities.Entity|linearVelocity|BEPUutilities.Vector3|typed-scalar",
    "BEPUphysics.Entities.Entity|localInertiaTensor|BEPUutilities.Matrix3x3|typed-scalar",
    "BEPUphysics.Entities.Entity|localInertiaTensorInverse|BEPUutilities.Matrix3x3|typed-scalar",
    "BEPUphysics.Entities.Entity|locker|BEPUutilities.SpinLock|explicit-reference-or-excluded",
    "BEPUphysics.Entities.Entity|mass|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.Entities.Entity|material|BEPUphysics.Materials.Material|explicit-reference-or-excluded",
    "BEPUphysics.Entities.Entity|materialChangedDelegate|System.Action`1[BEPUphysics.Materials.Material]|explicit-reference-or-excluded",
    "BEPUphysics.Entities.Entity|orientation|BEPUutilities.Quaternion|typed-scalar",
    "BEPUphysics.Entities.Entity|orientationMatrix|BEPUutilities.Matrix3x3|typed-scalar",
    "BEPUphysics.Entities.Entity|personalGravity|BEPUutilities.Vector3|typed-scalar",
    "BEPUphysics.Entities.Entity|position|BEPUutilities.Vector3|typed-scalar",
    "BEPUphysics.Entities.Entity|PositionUpdated|System.Action`1[BEPUphysics.Entities.Entity]|explicit-reference-or-excluded",
    "BEPUphysics.Entities.Entity|positionUpdateMode|BEPUphysics.PositionUpdating.PositionUpdateMode|typed-scalar",
    "BEPUphysics.Entities.Entity|shapeChangedDelegate|System.Action`1[BEPUphysics.CollisionShapes.CollisionShape]|explicit-reference-or-excluded",
    "BEPUphysics.Entities.Entity|space|BEPUphysics.Space|explicit-reference-or-excluded",
    "BEPUphysics.Materials.Material|<MaximumContactImpulse>k__BackingField|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.Materials.Material|<Tag>k__BackingField|System.Object|explicit-reference-or-excluded",
    "BEPUphysics.Materials.Material|<VelocityOnlyContactResponse>k__BackingField|System.Boolean|typed-scalar",
    "BEPUphysics.Materials.Material|bounciness|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.Materials.Material|hashCode|System.Int32|process-identity",
    "BEPUphysics.Materials.Material|kineticFriction|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.Materials.Material|MaterialChanged|System.Action`1[BEPUphysics.Materials.Material]|explicit-reference-or-excluded",
    "BEPUphysics.Materials.Material|staticFriction|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.DeactivationManagement.SimulationIslandMember|<DeactivationManager>k__BackingField|BEPUphysics.DeactivationManagement.DeactivationManager|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.SimulationIslandMember|Activated|System.Action`1[BEPUphysics.DeactivationManagement.SimulationIslandMember]|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.SimulationIslandMember|allowStabilization|System.Boolean|typed-scalar",
    "BEPUphysics.DeactivationManagement.SimulationIslandMember|BecameDeactivationCandidate|System.Action`1[BEPUphysics.DeactivationManagement.SimulationIslandMember]|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.SimulationIslandMember|BecameNonDeactivationCandidate|System.Action`1[BEPUphysics.DeactivationManagement.SimulationIslandMember]|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.SimulationIslandMember|connections|BEPUutilities.DataStructures.RawList`1[BEPUphysics.DeactivationManagement.SimulationIslandConnection]|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.SimulationIslandMember|Deactivated|System.Action`1[BEPUphysics.DeactivationManagement.SimulationIslandMember]|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.SimulationIslandMember|isAlwaysActive|System.Boolean|typed-scalar",
    "BEPUphysics.DeactivationManagement.SimulationIslandMember|isDeactivationCandidate|System.Boolean|typed-scalar",
    "BEPUphysics.DeactivationManagement.SimulationIslandMember|isSlowing|System.Boolean|typed-scalar",
    "BEPUphysics.DeactivationManagement.SimulationIslandMember|owner|BEPUphysics.Entities.Entity|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.SimulationIslandMember|previouslyActive|System.Boolean|typed-scalar",
    "BEPUphysics.DeactivationManagement.SimulationIslandMember|previousVelocity|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.DeactivationManagement.SimulationIslandMember|searchState|BEPUphysics.DeactivationManagement.SimulationIslandSearchState|typed-scalar",
    "BEPUphysics.DeactivationManagement.SimulationIslandMember|simulationIsland|BEPUphysics.DeactivationManagement.SimulationIsland|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.SimulationIslandMember|simulationIslandChangeLocker|BEPUutilities.SpinLock|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.SimulationIslandMember|velocityTimeBelowLimit|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.DeactivationManagement.SimulationIsland|allowDeactivation|System.Boolean|typed-scalar",
    "BEPUphysics.DeactivationManagement.SimulationIsland|becameDeactivationCandidateDelegate|System.Action`1[BEPUphysics.DeactivationManagement.SimulationIslandMember]|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.SimulationIsland|becameNonDeactivationCandidateDelegate|System.Action`1[BEPUphysics.DeactivationManagement.SimulationIslandMember]|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.SimulationIsland|deactivationCandidateCount|System.Int32|typed-scalar",
    "BEPUphysics.DeactivationManagement.SimulationIsland|immediateParent|BEPUphysics.DeactivationManagement.SimulationIsland|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.SimulationIsland|isActive|System.Boolean|typed-scalar",
    "BEPUphysics.DeactivationManagement.SimulationIsland|memberActivatedDelegate|System.Action`1[BEPUphysics.DeactivationManagement.SimulationIslandMember]|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.SimulationIsland|memberCount|System.Int32|typed-scalar",
    "BEPUphysics.DeactivationManagement.DeactivationManager|addLocker|BEPUutilities.SpinLock|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.DeactivationManager|deactivationIslandIndex|System.Int32|typed-scalar",
    "BEPUphysics.DeactivationManagement.DeactivationManager|islandPool|BEPUutilities.ResourceManagement.UnsafeResourcePool`1[BEPUphysics.DeactivationManagement.SimulationIsland]|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.DeactivationManager|lowVelocityTimeMinimum|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.DeactivationManagement.DeactivationManager|maximumDeactivationAttemptsPerFrame|System.Int32|typed-scalar",
    "BEPUphysics.DeactivationManagement.DeactivationManager|member1Friends|System.Collections.Generic.Queue`1[BEPUphysics.DeactivationManagement.SimulationIslandMember]|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.DeactivationManager|member2Friends|System.Collections.Generic.Queue`1[BEPUphysics.DeactivationManagement.SimulationIslandMember]|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.DeactivationManager|multithreadedCandidacyLoopDelegate|System.Action`1[System.Int32]|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.DeactivationManager|searchedMembers1|System.Collections.Generic.List`1[BEPUphysics.DeactivationManagement.SimulationIslandMember]|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.DeactivationManager|searchedMembers2|System.Collections.Generic.List`1[BEPUphysics.DeactivationManagement.SimulationIslandMember]|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.DeactivationManager|simulationIslandMembers|BEPUutilities.DataStructures.RawList`1[BEPUphysics.DeactivationManagement.SimulationIslandMember]|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.DeactivationManager|simulationIslands|BEPUutilities.DataStructures.RawList`1[BEPUphysics.DeactivationManagement.SimulationIsland]|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.DeactivationManager|splitAttempts|BEPUutilities.DataStructures.ConcurrentDeque`1[BEPUphysics.DeactivationManagement.SimulationIslandConnection]|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.DeactivationManager|timeStepSettings|BEPUphysics.TimeStepSettings|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.DeactivationManager|useStabilization|System.Boolean|typed-scalar",
    "BEPUphysics.DeactivationManagement.DeactivationManager|velocityLowerLimit|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.DeactivationManagement.DeactivationManager|velocityLowerLimitSquared|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.Constraints.Solver|<DeactivationManager>k__BackingField|BEPUphysics.DeactivationManagement.DeactivationManager|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Solver|<PermutationMapper>k__BackingField|BEPUutilities.PermutationMapper|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Solver|addRemoveLocker|BEPUutilities.SpinLock|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Solver|iterationLimit|System.Int32|typed-scalar",
    "BEPUphysics.Constraints.Solver|multithreadedExclusiveUpdateDelegate|System.Action`1[System.Int32]|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Solver|multithreadedIterationDelegate|System.Action`1[System.Int32]|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Solver|multithreadedPrestepDelegate|System.Action`1[System.Int32]|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Solver|solverUpdateables|BEPUutilities.DataStructures.RawList`1[BEPUphysics.Constraints.SolverUpdateable]|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Solver|timeStepSettings|BEPUphysics.TimeStepSettings|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.SolverUpdateable|<SolverGroup>k__BackingField|BEPUphysics.Constraints.SolverGroups.SolverGroup|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.SolverUpdateable|<Tag>k__BackingField|System.Object|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.SolverUpdateable|involvedEntities|BEPUutilities.DataStructures.RawList`1[BEPUphysics.Entities.Entity]|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.SolverUpdateable|isActive|System.Boolean|typed-scalar",
    "BEPUphysics.Constraints.SolverUpdateable|isActiveInSolver|System.Boolean|typed-scalar",
    "BEPUphysics.Constraints.SolverUpdateable|numberOfInvolvedEntities|System.Int32|typed-scalar",
    "BEPUphysics.Constraints.SolverUpdateable|simulationIslandConnection|BEPUphysics.DeactivationManagement.SimulationIslandConnection|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.SolverUpdateable|solver|BEPUphysics.Constraints.Solver|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.SolverUpdateable|solverIndex|System.Int32|typed-scalar",
    "BEPUphysics.Constraints.SolverUpdateable|solverSettings|BEPUphysics.Constraints.SolverSettings|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.SolverUpdateable|space|BEPUphysics.Space|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.SolverSettings|currentIterations|System.Int32|per-step-scratch",
    "BEPUphysics.Constraints.SolverSettings|iterationsAtZeroImpulse|System.Int32|per-step-scratch",
    "BEPUphysics.Constraints.SolverSettings|maximumIterationCount|System.Int32|typed-scalar",
    "BEPUphysics.Constraints.SolverSettings|minimumImpulse|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.Constraints.SolverSettings|minimumIterationCount|System.Int32|typed-scalar",
    "BEPUphysics.Constraints.Collision.ContactManifoldConstraint|entityA|BEPUphysics.Entities.Entity|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.ContactManifoldConstraint|entityB|BEPUphysics.Entities.Entity|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.ContactManifoldConstraint|materialInteraction|BEPUphysics.Materials.InteractionProperties|typed-scalar",
    "BEPUphysics.Constraints.Collision.ContactManifoldConstraint|pair|BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|accumulatedImpulse|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|angularAX|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|angularAY|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|angularAZ|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|angularBX|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|angularBY|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|angularBZ|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|bias|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|contact|BEPUphysics.CollisionTests.Contact|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|contactManifoldConstraint|BEPUphysics.Constraints.Collision.ContactManifoldConstraint|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|entityA|BEPUphysics.Entities.Entity|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|entityADynamic|System.Boolean|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|entityB|BEPUphysics.Entities.Entity|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|entityBDynamic|System.Boolean|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|linearAX|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|linearAY|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|linearAZ|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|maximumImpulse|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|ra|BEPUutilities.Vector3|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|rb|BEPUutilities.Vector3|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|softness|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.ContactPenetrationConstraint|velocityToImpulse|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.SlidingFrictionTwoAxis|accumulatedImpulse|BEPUutilities.Vector2|typed-scalar",
    "BEPUphysics.Constraints.Collision.SlidingFrictionTwoAxis|angularA|BEPUutilities.Matrix2x3|per-step-scratch",
    "BEPUphysics.Constraints.Collision.SlidingFrictionTwoAxis|angularB|BEPUutilities.Matrix2x3|per-step-scratch",
    "BEPUphysics.Constraints.Collision.SlidingFrictionTwoAxis|contactCount|System.Int32|per-step-scratch",
    "BEPUphysics.Constraints.Collision.SlidingFrictionTwoAxis|contactManifoldConstraint|BEPUphysics.Constraints.Collision.ConvexContactManifoldConstraint|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.SlidingFrictionTwoAxis|entityA|BEPUphysics.Entities.Entity|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.SlidingFrictionTwoAxis|entityADynamic|System.Boolean|per-step-scratch",
    "BEPUphysics.Constraints.Collision.SlidingFrictionTwoAxis|entityB|BEPUphysics.Entities.Entity|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.SlidingFrictionTwoAxis|entityBDynamic|System.Boolean|per-step-scratch",
    "BEPUphysics.Constraints.Collision.SlidingFrictionTwoAxis|friction|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.SlidingFrictionTwoAxis|linearA|BEPUutilities.Matrix2x3|per-step-scratch",
    "BEPUphysics.Constraints.Collision.SlidingFrictionTwoAxis|manifoldCenter|BEPUutilities.Vector3|per-step-scratch",
    "BEPUphysics.Constraints.Collision.SlidingFrictionTwoAxis|ra|BEPUutilities.Vector3|per-step-scratch",
    "BEPUphysics.Constraints.Collision.SlidingFrictionTwoAxis|rb|BEPUutilities.Vector3|per-step-scratch",
    "BEPUphysics.Constraints.Collision.SlidingFrictionTwoAxis|relativeVelocity|BEPUutilities.Vector3|per-step-scratch",
    "BEPUphysics.Constraints.Collision.SlidingFrictionTwoAxis|velocityToImpulse|BEPUutilities.Matrix2x2|per-step-scratch",
    "BEPUphysics.Constraints.Collision.TwistFrictionConstraint|accumulatedImpulse|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.Constraints.Collision.TwistFrictionConstraint|angularX|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.TwistFrictionConstraint|angularY|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.TwistFrictionConstraint|angularZ|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.TwistFrictionConstraint|contactCount|System.Int32|per-step-scratch",
    "BEPUphysics.Constraints.Collision.TwistFrictionConstraint|contactManifoldConstraint|BEPUphysics.Constraints.Collision.ConvexContactManifoldConstraint|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.TwistFrictionConstraint|entityA|BEPUphysics.Entities.Entity|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.TwistFrictionConstraint|entityADynamic|System.Boolean|per-step-scratch",
    "BEPUphysics.Constraints.Collision.TwistFrictionConstraint|entityB|BEPUphysics.Entities.Entity|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.TwistFrictionConstraint|entityBDynamic|System.Boolean|per-step-scratch",
    "BEPUphysics.Constraints.Collision.TwistFrictionConstraint|friction|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.Constraints.Collision.TwistFrictionConstraint|leverArms|FixMath.NET.Fix64[]|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.TwistFrictionConstraint|velocityToImpulse|FixMath.NET.Fix64|per-step-scratch",
    "BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler|<Contacts>k__BackingField|BEPUphysics.NarrowPhaseSystems.Pairs.ContactCollection|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler|<Parent>k__BackingField|BEPUphysics.NarrowPhaseSystems.Pairs.IPairHandlerParent|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler|listIndexA|System.Int32|typed-scalar",
    "BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler|listIndexB|System.Int32|typed-scalar",
    "BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler|previousContactCount|System.Int32|typed-scalar",
    "BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler|suppressEvents|System.Boolean|typed-scalar",
    "BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler|timeOfImpact|FixMath.NET.Fix64|typed-scalar",
    "BEPUphysics.CollisionTests.Manifolds.ContactManifold|<IsQuery>k__BackingField|System.Boolean|typed-scalar",
    "BEPUphysics.CollisionTests.Manifolds.ContactManifold|ContactAdded|System.Action`1[BEPUphysics.CollisionTests.Contact]|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.Manifolds.ContactManifold|contactIndicesToRemove|BEPUutilities.DataStructures.RawList`1[System.Int32]|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.Manifolds.ContactManifold|ContactRemoved|System.Action`1[BEPUphysics.CollisionTests.Contact]|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.Manifolds.ContactManifold|contacts|BEPUutilities.DataStructures.RawList`1[BEPUphysics.CollisionTests.Contact]|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.Manifolds.ContactManifold|unusedContacts|BEPUutilities.ResourceManagement.UnsafeResourcePool`1[BEPUphysics.CollisionTests.Contact]|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.Manifolds.SphereContactManifold|contact|BEPUphysics.CollisionTests.Contact|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.Manifolds.SphereContactManifold|previouslyColliding|System.Boolean|typed-scalar",
    "BEPUphysics.CollisionTests.Manifolds.SphereContactManifold|sphereA|BEPUphysics.BroadPhaseEntries.MobileCollidables.ConvexCollidable`1[BEPUphysics.CollisionShapes.ConvexShapes.SphereShape]|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.Manifolds.SphereContactManifold|sphereB|BEPUphysics.BroadPhaseEntries.MobileCollidables.ConvexCollidable`1[BEPUphysics.CollisionShapes.ConvexShapes.SphereShape]|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.Manifolds.BoxSphereContactManifold|box|BEPUphysics.BroadPhaseEntries.MobileCollidables.ConvexCollidable`1[BEPUphysics.CollisionShapes.ConvexShapes.BoxShape]|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.Manifolds.BoxSphereContactManifold|contact|BEPUphysics.CollisionTests.Contact|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.Manifolds.BoxSphereContactManifold|previouslyColliding|System.Boolean|typed-scalar",
    "BEPUphysics.CollisionTests.Manifolds.BoxSphereContactManifold|sphere|BEPUphysics.BroadPhaseEntries.MobileCollidables.ConvexCollidable`1[BEPUphysics.CollisionShapes.ConvexShapes.SphereShape]|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.CollisionAlgorithms.GeneralConvexPairTester|cachedSimplex|BEPUphysics.CollisionTests.CollisionAlgorithms.GJK.CachedSimplex|typed-scalar",
    "BEPUphysics.CollisionTests.CollisionAlgorithms.GeneralConvexPairTester|collidableA|BEPUphysics.BroadPhaseEntries.MobileCollidables.ConvexCollidable|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.CollisionAlgorithms.GeneralConvexPairTester|collidableB|BEPUphysics.BroadPhaseEntries.MobileCollidables.ConvexCollidable|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.CollisionAlgorithms.GeneralConvexPairTester|localDirection|BEPUutilities.Vector3|typed-scalar",
    "BEPUphysics.CollisionTests.CollisionAlgorithms.GeneralConvexPairTester|localSeparatingAxis|BEPUutilities.Vector3|typed-scalar",
    "BEPUphysics.CollisionTests.CollisionAlgorithms.GeneralConvexPairTester|previousState|BEPUphysics.CollisionTests.CollisionAlgorithms.GeneralConvexPairTester+CollisionState|typed-scalar",
    "BEPUphysics.CollisionTests.CollisionAlgorithms.GeneralConvexPairTester|state|BEPUphysics.CollisionTests.CollisionAlgorithms.GeneralConvexPairTester+CollisionState|typed-scalar",
    "BEPUphysics.BroadPhaseEntries.BroadPhaseEntry|<BroadPhase>k__BackingField|BEPUphysics.BroadPhaseSystems.BroadPhase|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseEntries.BroadPhaseEntry|<Tag>k__BackingField|System.Object|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseEntries.BroadPhaseEntry|boundingBox|BEPUutilities.BoundingBox|typed-scalar",
    "BEPUphysics.BroadPhaseEntries.BroadPhaseEntry|collisionRules|BEPUphysics.CollisionRuleManagement.CollisionRules|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseEntries.BroadPhaseEntry|collisionRulesUpdatedDelegate|System.Action|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseEntries.BroadPhaseEntry|hashCode|System.Int32|process-identity",
    "BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable|entity|BEPUphysics.Entities.Entity|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable|events|BEPUphysics.BroadPhaseEntries.Events.ContactEventManager`1[BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable]|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable|localPosition|BEPUutilities.Vector3|typed-scalar",
    "BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable|worldTransform|BEPUutilities.RigidTransform|typed-scalar",
    "BEPUphysics.TimeStepSettings|AccumulatedTime|FixMath.NET.Fix64|explicit-reference-or-excluded",
    "BEPUphysics.TimeStepSettings|MaximumTimeStepsPerFrame|System.Int32|explicit-reference-or-excluded",
    "BEPUphysics.TimeStepSettings|TimeStepDuration|FixMath.NET.Fix64|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.Contact|Id|System.Int32|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.Contact|Normal|BEPUutilities.Vector3|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.Contact|PenetrationDepth|FixMath.NET.Fix64|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.Contact|Position|BEPUutilities.Vector3|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.ContactSupplementData|BasePenetrationDepth|FixMath.NET.Fix64|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.ContactSupplementData|LocalOffsetA|BEPUutilities.Vector3|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.ContactSupplementData|LocalOffsetB|BEPUutilities.Vector3|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.ConvexContactManifoldConstraint|penetrationConstraintPool|System.Collections.Generic.Stack`1[BEPUphysics.Constraints.Collision.ContactPenetrationConstraint]|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.ConvexContactManifoldConstraint|penetrationConstraints|BEPUutilities.DataStructures.RawList`1[BEPUphysics.Constraints.Collision.ContactPenetrationConstraint]|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.ConvexContactManifoldConstraint|slidingFriction|BEPUphysics.Constraints.Collision.SlidingFrictionTwoAxis|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.ConvexContactManifoldConstraint|twistFriction|BEPUphysics.Constraints.Collision.TwistFrictionConstraint|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.NonConvexContactManifoldConstraint|frictionConstraintPool|System.Collections.Generic.Stack`1[BEPUphysics.Constraints.Collision.ContactFrictionConstraint]|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.NonConvexContactManifoldConstraint|frictionConstraints|BEPUutilities.DataStructures.RawList`1[BEPUphysics.Constraints.Collision.ContactFrictionConstraint]|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.NonConvexContactManifoldConstraint|penetrationConstraintPool|System.Collections.Generic.Stack`1[BEPUphysics.Constraints.Collision.ContactPenetrationConstraint]|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.Collision.NonConvexContactManifoldConstraint|penetrationConstraints|BEPUutilities.DataStructures.RawList`1[BEPUphysics.Constraints.Collision.ContactPenetrationConstraint]|explicit-reference-or-excluded",
    "BEPUphysics.Constraints.SolverGroups.SolverGroup|solverUpdateables|BEPUutilities.DataStructures.RawList`1[BEPUphysics.Constraints.SolverUpdateable]|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.NarrowPhase|<Solver>k__BackingField|BEPUphysics.Constraints.Solver|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.NarrowPhase|<TimeStepSettings>k__BackingField|BEPUphysics.TimeStepSettings|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.NarrowPhase|broadPhaseOverlaps|BEPUutilities.DataStructures.RawList`1[BEPUphysics.BroadPhaseSystems.BroadPhaseOverlap]|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.NarrowPhase|CreatingPair|System.Action`1[BEPUphysics.NarrowPhaseSystems.Pairs.NarrowPhasePair]|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.NarrowPhase|narrowPhasePairs|BEPUutilities.DataStructures.RawList`1[BEPUphysics.NarrowPhaseSystems.Pairs.NarrowPhasePair]|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.NarrowPhase|newNarrowPhasePairs|BEPUutilities.DataStructures.ConcurrentDeque`1[BEPUphysics.NarrowPhaseSystems.Pairs.NarrowPhasePair]|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.NarrowPhase|overlapMapping|System.Collections.Generic.Dictionary`2[BEPUphysics.BroadPhaseSystems.BroadPhaseOverlap,BEPUphysics.NarrowPhaseSystems.Pairs.NarrowPhasePair]|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.NarrowPhase|RemovingPair|System.Action`1[BEPUphysics.NarrowPhaseSystems.Pairs.NarrowPhasePair]|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.NarrowPhase|updateBroadPhaseOverlapDelegate|System.Action`1[System.Int32]|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.Pairs.NarrowPhasePair|<Factory>k__BackingField|BEPUphysics.NarrowPhaseSystems.NarrowPhasePairFactory|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.Pairs.NarrowPhasePair|<NarrowPhase>k__BackingField|BEPUphysics.NarrowPhaseSystems.NarrowPhase|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.Pairs.NarrowPhasePair|<NeedsUpdate>k__BackingField|System.Boolean|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.Pairs.NarrowPhasePair|broadPhaseOverlap|BEPUphysics.BroadPhaseSystems.BroadPhaseOverlap|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.Pairs.SpherePairHandler|contactConstraint|BEPUphysics.Constraints.Collision.NonConvexContactManifoldConstraint|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.Pairs.SpherePairHandler|contactManifold|BEPUphysics.CollisionTests.Manifolds.SphereContactManifold|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.Pairs.SpherePairHandler|sphereA|BEPUphysics.BroadPhaseEntries.MobileCollidables.ConvexCollidable`1[BEPUphysics.CollisionShapes.ConvexShapes.SphereShape]|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.Pairs.SpherePairHandler|sphereB|BEPUphysics.BroadPhaseEntries.MobileCollidables.ConvexCollidable`1[BEPUphysics.CollisionShapes.ConvexShapes.SphereShape]|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.Pairs.BoxSpherePairHandler|box|BEPUphysics.BroadPhaseEntries.MobileCollidables.ConvexCollidable`1[BEPUphysics.CollisionShapes.ConvexShapes.BoxShape]|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.Pairs.BoxSpherePairHandler|contactConstraint|BEPUphysics.Constraints.Collision.NonConvexContactManifoldConstraint|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.Pairs.BoxSpherePairHandler|contactManifold|BEPUphysics.CollisionTests.Manifolds.BoxSphereContactManifold|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.Pairs.BoxSpherePairHandler|sphere|BEPUphysics.BroadPhaseEntries.MobileCollidables.ConvexCollidable`1[BEPUphysics.CollisionShapes.ConvexShapes.SphereShape]|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.Pairs.BoxPairHandler|boxA|BEPUphysics.BroadPhaseEntries.MobileCollidables.ConvexCollidable`1[BEPUphysics.CollisionShapes.ConvexShapes.BoxShape]|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.Pairs.BoxPairHandler|boxB|BEPUphysics.BroadPhaseEntries.MobileCollidables.ConvexCollidable`1[BEPUphysics.CollisionShapes.ConvexShapes.BoxShape]|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.Pairs.BoxPairHandler|contactManifold|BEPUphysics.CollisionTests.Manifolds.BoxContactManifold|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.Pairs.GeneralConvexPairHandler|contactManifold|BEPUphysics.CollisionTests.Manifolds.GeneralConvexContactManifold|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.Pairs.GeneralConvexPairHandler|convexA|BEPUphysics.BroadPhaseEntries.MobileCollidables.ConvexCollidable|explicit-reference-or-excluded",
    "BEPUphysics.NarrowPhaseSystems.Pairs.GeneralConvexPairHandler|convexB|BEPUphysics.BroadPhaseEntries.MobileCollidables.ConvexCollidable|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseSystems.BroadPhase|<Locker>k__BackingField|System.Object|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseSystems.BroadPhase|<QueryAccelerator>k__BackingField|BEPUphysics.BroadPhaseSystems.IQueryAccelerator|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseSystems.BroadPhase|overlapAddLock|BEPUutilities.SpinLock|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseSystems.BroadPhase|overlaps|BEPUutilities.DataStructures.RawList`1[BEPUphysics.BroadPhaseSystems.BroadPhaseOverlap]|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseSystems.Hierarchies.DynamicHierarchy|leafNodes|BEPUutilities.ResourceManagement.UnsafeResourcePool`1[BEPUphysics.BroadPhaseSystems.Hierarchies.LeafNode]|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseSystems.Hierarchies.DynamicHierarchy|multithreadedOverlap|System.Action`1[System.Int32]|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseSystems.Hierarchies.DynamicHierarchy|multithreadedRefit|System.Action`1[System.Int32]|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseSystems.Hierarchies.DynamicHierarchy|multithreadingSourceNodes|BEPUutilities.DataStructures.RawList`1[BEPUphysics.BroadPhaseSystems.Hierarchies.Node]|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseSystems.Hierarchies.DynamicHierarchy|multithreadingSourceOverlaps|BEPUutilities.DataStructures.RawList`1[BEPUphysics.BroadPhaseSystems.Hierarchies.DynamicHierarchy+NodePair]|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseSystems.Hierarchies.DynamicHierarchy|root|BEPUphysics.BroadPhaseSystems.Hierarchies.Node|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseSystems.Hierarchies.DynamicHierarchy|threadSplitOffsets|System.Int32[]|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseSystems.Hierarchies.Node|BoundingBox|BEPUutilities.BoundingBox|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseSystems.Hierarchies.InternalNode|childA|BEPUphysics.BroadPhaseSystems.Hierarchies.Node|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseSystems.Hierarchies.InternalNode|childB|BEPUphysics.BroadPhaseSystems.Hierarchies.Node|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseSystems.Hierarchies.InternalNode|currentVolume|FixMath.NET.Fix64|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseSystems.Hierarchies.InternalNode|maximumVolume|FixMath.NET.Fix64|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseSystems.Hierarchies.LeafNode|element|BEPUphysics.BroadPhaseEntries.BroadPhaseEntry|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseEntries.Collidable|<IgnoreShapeChanges>k__BackingField|System.Boolean|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseEntries.Collidable|pairs|BEPUutilities.DataStructures.RawList`1[BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler]|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseEntries.Collidable|shape|BEPUphysics.CollisionShapes.CollisionShape|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseEntries.Collidable|shapeChangedDelegate|System.Action`1[BEPUphysics.CollisionShapes.CollisionShape]|explicit-reference-or-excluded",
    "BEPUphysics.BroadPhaseEntries.Collidable|shapeChangedHooked|System.Boolean|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.Manifolds.GeneralConvexContactManifold|collidableA|BEPUphysics.BroadPhaseEntries.MobileCollidables.ConvexCollidable|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.Manifolds.GeneralConvexContactManifold|collidableB|BEPUphysics.BroadPhaseEntries.MobileCollidables.ConvexCollidable|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.Manifolds.GeneralConvexContactManifold|pairTester|BEPUphysics.CollisionTests.CollisionAlgorithms.GeneralConvexPairTester|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.Manifolds.GeneralConvexContactManifold|supplementData|BEPUutilities.DataStructures.RawValueList`1[BEPUphysics.CollisionTests.ContactSupplementData]|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.Manifolds.BoxContactManifold|boxA|BEPUphysics.BroadPhaseEntries.MobileCollidables.ConvexCollidable`1[BEPUphysics.CollisionShapes.ConvexShapes.BoxShape]|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.Manifolds.BoxContactManifold|boxB|BEPUphysics.BroadPhaseEntries.MobileCollidables.ConvexCollidable`1[BEPUphysics.CollisionShapes.ConvexShapes.BoxShape]|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.CollisionAlgorithms.GJK.CachedSimplex|LocalSimplexA|BEPUphysics.CollisionTests.CollisionAlgorithms.GJK.ContributingShapeSimplex|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.CollisionAlgorithms.GJK.CachedSimplex|LocalSimplexB|BEPUphysics.CollisionTests.CollisionAlgorithms.GJK.ContributingShapeSimplex|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.CollisionAlgorithms.GJK.CachedSimplex|State|BEPUphysics.CollisionTests.CollisionAlgorithms.GJK.SimplexState|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.CollisionAlgorithms.GJK.ContributingShapeSimplex|A|BEPUutilities.Vector3|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.CollisionAlgorithms.GJK.ContributingShapeSimplex|B|BEPUutilities.Vector3|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.CollisionAlgorithms.GJK.ContributingShapeSimplex|C|BEPUutilities.Vector3|explicit-reference-or-excluded",
    "BEPUphysics.CollisionTests.CollisionAlgorithms.GJK.ContributingShapeSimplex|D|BEPUutilities.Vector3|explicit-reference-or-excluded",
    "BEPUphysics.CollisionRuleManagement.CollisionRules|CollisionRulesChanged|System.Action|explicit-reference-or-excluded",
    "BEPUphysics.CollisionRuleManagement.CollisionRules|group|BEPUphysics.CollisionRuleManagement.CollisionGroup|explicit-reference-or-excluded",
    "BEPUphysics.CollisionRuleManagement.CollisionRules|hashCode|System.Int32|explicit-reference-or-excluded",
    "BEPUphysics.CollisionRuleManagement.CollisionRules|OnChangedDelegate|System.Action|explicit-reference-or-excluded",
    "BEPUphysics.CollisionRuleManagement.CollisionRules|personal|BEPUphysics.CollisionRuleManagement.CollisionRule|explicit-reference-or-excluded",
    "BEPUphysics.CollisionRuleManagement.CollisionRules|specific|BEPUutilities.DataStructures.ObservableDictionary`2[BEPUphysics.CollisionRuleManagement.CollisionRules,BEPUphysics.CollisionRuleManagement.CollisionRule]|explicit-reference-or-excluded",
    "BEPUphysics.OtherSpaceStages.ForceUpdater|dynamicObjects|BEPUutilities.DataStructures.RawList`1[BEPUphysics.OtherSpaceStages.IForceUpdateable]|explicit-reference-or-excluded",
    "BEPUphysics.OtherSpaceStages.ForceUpdater|gravity|BEPUutilities.Vector3|explicit-reference-or-excluded",
    "BEPUphysics.OtherSpaceStages.ForceUpdater|gravityDt|BEPUutilities.Vector3|explicit-reference-or-excluded",
    "BEPUphysics.OtherSpaceStages.ForceUpdater|multithreadedLoopBodyDelegate|System.Action`1[System.Int32]|explicit-reference-or-excluded",
    "BEPUphysics.OtherSpaceStages.ForceUpdater|timeStepSettings|BEPUphysics.TimeStepSettings|explicit-reference-or-excluded",
    "BEPUphysics.PositionUpdating.PositionUpdater|timeStepSettings|BEPUphysics.TimeStepSettings|explicit-reference-or-excluded",
    "BEPUphysics.PositionUpdating.ContinuousPositionUpdater|continuousUpdateables|BEPUutilities.DataStructures.RawList`1[BEPUphysics.PositionUpdating.ICCDPositionUpdateable]|explicit-reference-or-excluded",
    "BEPUphysics.PositionUpdating.ContinuousPositionUpdater|discreteUpdateables|BEPUutilities.DataStructures.RawList`1[BEPUphysics.PositionUpdating.IPositionUpdateable]|explicit-reference-or-excluded",
    "BEPUphysics.PositionUpdating.ContinuousPositionUpdater|passiveUpdateables|BEPUutilities.DataStructures.RawList`1[BEPUphysics.PositionUpdating.ICCDPositionUpdateable]|explicit-reference-or-excluded",
    "BEPUphysics.PositionUpdating.ContinuousPositionUpdater|preUpdate|System.Action`1[System.Int32]|explicit-reference-or-excluded",
    "BEPUphysics.PositionUpdating.ContinuousPositionUpdater|updateContinuous|System.Action`1[System.Int32]|explicit-reference-or-excluded",
    "BEPUphysics.PositionUpdating.ContinuousPositionUpdater|updateTimeOfImpact|System.Action`1[System.Int32]|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.SimulationIslandConnection|<DeactivationManager>k__BackingField|BEPUphysics.DeactivationManagement.DeactivationManager|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.SimulationIslandConnection|<Owner>k__BackingField|BEPUphysics.DeactivationManagement.ISimulationIslandConnectionOwner|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.SimulationIslandConnection|<SlatedForRemoval>k__BackingField|System.Boolean|explicit-reference-or-excluded",
    "BEPUphysics.DeactivationManagement.SimulationIslandConnection|entries|BEPUutilities.DataStructures.RawList`1[BEPUphysics.DeactivationManagement.SimulationIslandConnection+Entry]|explicit-reference-or-excluded",
}; } }
