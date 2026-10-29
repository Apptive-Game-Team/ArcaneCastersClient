param([string]$AssemblyPath = "$PSScriptRoot/bin/Release/net8.0/LockstepProbe.dll")
$ErrorActionPreference = 'Stop'
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath))
$runtime = [IO.Path]::GetFullPath("$PSScriptRoot/../../LocalPackages/com.arcanecasters.lockstep/Runtime")
$names = @(
 'BEPUutilities.PermutationMapper',
 'BEPUphysics.Constraints.Collision.ContactFrictionConstraint',
 'BEPUphysics.CollisionShapes.CollisionShape', 'BEPUphysics.CollisionShapes.EntityShape',
 'BEPUphysics.CollisionShapes.ConvexShapes.ConvexShape', 'BEPUphysics.CollisionShapes.ConvexShapes.SphereShape',
 'BEPUphysics.CollisionShapes.ConvexShapes.BoxShape', 'BEPUphysics.CollisionShapes.ConvexShapes.CapsuleShape',
 'BEPUphysics.Entities.Entity', 'BEPUphysics.Materials.Material',
 'BEPUphysics.DeactivationManagement.SimulationIslandMember', 'BEPUphysics.DeactivationManagement.SimulationIsland',
 'BEPUphysics.DeactivationManagement.DeactivationManager',
 'BEPUphysics.Constraints.Solver', 'BEPUphysics.Constraints.SolverUpdateable', 'BEPUphysics.Constraints.SolverSettings',
 'BEPUphysics.Constraints.Collision.ContactManifoldConstraint', 'BEPUphysics.Constraints.Collision.ContactPenetrationConstraint',
 'BEPUphysics.Constraints.Collision.SlidingFrictionTwoAxis', 'BEPUphysics.Constraints.Collision.TwistFrictionConstraint',
 'BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler',
 'BEPUphysics.CollisionTests.Manifolds.ContactManifold',
 'BEPUphysics.CollisionTests.Manifolds.SphereContactManifold', 'BEPUphysics.CollisionTests.Manifolds.BoxSphereContactManifold',
 'BEPUphysics.CollisionTests.CollisionAlgorithms.GeneralConvexPairTester',
 'BEPUphysics.BroadPhaseEntries.BroadPhaseEntry', 'BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable'
)
$flags = [Reflection.BindingFlags]'Instance,Public,NonPublic,DeclaredOnly'
function CanonicalType([Type]$type) {
 if ($type.IsArray) { return (CanonicalType $type.GetElementType()) + '[]' }
 if ($type.IsGenericType) { return $type.GetGenericTypeDefinition().FullName + '[' + ((@($type.GetGenericArguments() | ForEach-Object { CanonicalType $_ })) -join ',') + ']' }
 return $type.FullName
}
$structs = [Collections.Generic.HashSet[Type]]::new()
$generated = [Text.StringBuilder]::new()
[void]$generated.AppendLine('// Generate-PhysicsStateAccessors.ps1로 생성한다. 실행 중 reflection을 사용하지 않는다.')
$inventory = [Collections.Generic.List[object]]::new()
$excludedScalars = @('BEPUphysics.Entities.Entity.hashCode','BEPUphysics.Entities.Entity.<InstanceId>k__BackingField','BEPUphysics.Materials.Material.hashCode','BEPUphysics.BroadPhaseEntries.BroadPhaseEntry.hashCode')
function Expression([Type]$t, [string]$v) {
 if ($t.IsEnum) { return "writer.Write((int)$v);" }
 if ($t.IsPrimitive -or $t -eq [decimal] -or $t -eq [string]) { return "writer.Write($v);" }
 if ($t.FullName -eq 'FixMath.NET.Fix64') { return "writer.Write($v.RawValue);" }
 [void]$structs.Add($t)
 return "ArcaneCasters.Simulation.PhysicsStateValue.Write(writer, $v);"
}
foreach ($name in $names) {
 $t = $assembly.GetType($name, $true)
 $source = Get-ChildItem "$runtime/Physics" -Recurse -Filter "$($t.Name).cs" | Where-Object { [IO.File]::ReadAllText($_.FullName) -match "namespace\s+$([regex]::Escape($t.Namespace))\s*\{" } | Select-Object -First 1
 if (!$source) { throw "Source not found: $name" }
 $text = [IO.File]::ReadAllText($source.FullName)
 if ($text -notmatch "partial class $($t.Name)\b") {
  $text = [regex]::Replace($text, "\bclass $($t.Name)\b", "partial class $($t.Name)", 1)
  $originalBytes = [IO.File]::ReadAllBytes($source.FullName)
  $hasBom = $originalBytes.Length -ge 3 -and $originalBytes[0] -eq 239 -and $originalBytes[1] -eq 187 -and $originalBytes[2] -eq 191
  [IO.File]::WriteAllText($source.FullName, $text, [Text.UTF8Encoding]::new($hasBom))
 }
 [void]$generated.AppendLine("namespace $($t.Namespace) { partial class $($t.Name) {")
 [void]$generated.AppendLine("    internal void WriteCheckpointScalars_$($t.Name)(System.IO.BinaryWriter writer) {")
 foreach ($field in $t.GetFields($flags) | Sort-Object Name) {
  $key = "$name.$($field.Name)"
  $scalar = ($field.FieldType.IsValueType -and $field.FieldType.FullName -ne 'BEPUutilities.SpinLock') -or $field.FieldType -eq [string]
  $status = if ($excludedScalars -contains $key) { 'process-identity' } elseif ($name -eq 'BEPUphysics.Constraints.SolverSettings' -and $field.Name -in @('currentIterations','iterationsAtZeroImpulse')) { 'per-step-scratch' } elseif ($name -in @('BEPUphysics.Constraints.Collision.ContactPenetrationConstraint','BEPUphysics.Constraints.Collision.ContactFrictionConstraint','BEPUphysics.Constraints.Collision.SlidingFrictionTwoAxis','BEPUphysics.Constraints.Collision.TwistFrictionConstraint') -and $scalar -and $field.Name -ne 'accumulatedImpulse') { 'per-step-scratch' } elseif ($scalar) { 'typed-scalar' } else { 'explicit-reference-or-excluded' }
  $inventory.Add([pscustomobject]@{Type=$name; Field=$field.Name; FieldType=(CanonicalType $field.FieldType); Policy=$status})
  if ($status -ne 'typed-scalar') { continue }
  $access = if ($field.Name -match '^<(.+)>k__BackingField$') { $Matches[1] } else { $field.Name }
  [void]$generated.AppendLine('        writer.Write("' + $key + '");')
  if ($key -eq 'BEPUphysics.Constraints.SolverUpdateable.solverIndex') {
   [void]$generated.AppendLine('        writer.Write(solver == null ? -1 : solverIndex);')
  } elseif ($key -eq 'BEPUphysics.Constraints.SolverUpdateable.isActiveInSolver') {
   [void]$generated.AppendLine('        writer.Write((solver != null || SolverGroup?.Solver != null) && isActiveInSolver);')
  } else {
   [void]$generated.AppendLine("        " + (Expression $field.FieldType $access))
  }
 }
 [void]$generated.AppendLine('    }')
 [void]$generated.AppendLine('} }')
}
$inventoryOnly = @(
 'BEPUphysics.TimeStepSettings', 'BEPUphysics.CollisionTests.Contact', 'BEPUphysics.CollisionTests.ContactSupplementData',
 'BEPUphysics.Constraints.Collision.ConvexContactManifoldConstraint', 'BEPUphysics.Constraints.Collision.NonConvexContactManifoldConstraint',
 'BEPUphysics.Constraints.SolverGroups.SolverGroup', 'BEPUphysics.NarrowPhaseSystems.NarrowPhase',
 'BEPUphysics.NarrowPhaseSystems.Pairs.NarrowPhasePair', 'BEPUphysics.NarrowPhaseSystems.Pairs.StandardPairHandler',
 'BEPUphysics.NarrowPhaseSystems.Pairs.SpherePairHandler', 'BEPUphysics.NarrowPhaseSystems.Pairs.BoxSpherePairHandler',
 'BEPUphysics.NarrowPhaseSystems.Pairs.BoxPairHandler', 'BEPUphysics.NarrowPhaseSystems.Pairs.GeneralConvexPairHandler',
 'BEPUphysics.BroadPhaseSystems.BroadPhase', 'BEPUphysics.BroadPhaseSystems.Hierarchies.DynamicHierarchy',
 'BEPUphysics.BroadPhaseSystems.Hierarchies.Node', 'BEPUphysics.BroadPhaseSystems.Hierarchies.InternalNode',
 'BEPUphysics.BroadPhaseSystems.Hierarchies.LeafNode', 'BEPUphysics.BroadPhaseEntries.Collidable',
 'BEPUphysics.CollisionTests.Manifolds.GeneralConvexContactManifold', 'BEPUphysics.CollisionTests.Manifolds.BoxContactManifold',
 'BEPUphysics.CollisionTests.CollisionAlgorithms.GJK.CachedSimplex', 'BEPUphysics.CollisionTests.CollisionAlgorithms.GJK.ContributingShapeSimplex',
 'BEPUphysics.CollisionRuleManagement.CollisionRules', 'BEPUphysics.OtherSpaceStages.ForceUpdater',
 'BEPUphysics.PositionUpdating.PositionUpdater', 'BEPUphysics.PositionUpdating.ContinuousPositionUpdater',
 'BEPUphysics.DeactivationManagement.SimulationIslandConnection'
)
foreach ($name in $inventoryOnly) {
 $type = $assembly.GetType($name, $true)
 foreach ($field in $type.GetFields($flags) | Sort-Object Name) {
  $inventory.Add([pscustomobject]@{Type=$name; Field=$field.Name; FieldType=(CanonicalType $field.FieldType); Policy='explicit-reference-or-excluded'})
 }
}
[void]$generated.AppendLine('namespace ArcaneCasters.Simulation { public static class PhysicsStateValue {')
[void]$structs.Add($assembly.GetType("BEPUphysics.CollisionTests.ContactSupplementData", $true))
$done = [Collections.Generic.HashSet[Type]]::new()
while (($structs | Where-Object { !$done.Contains($_) }).Count -gt 0) {
 foreach ($t in @($structs | Where-Object { !$done.Contains($_) })) {
  [void]$done.Add($t)
  [void]$generated.AppendLine("    public static void Write(System.IO.BinaryWriter writer, global::$($t.FullName) value) {")
  foreach ($f in $t.GetFields($flags) | Sort-Object Name) {
   if (!$f.IsPublic -and !$f.IsAssembly -and !$f.IsFamilyOrAssembly) { throw "Private value field needs accessor: $($t.FullName).$($f.Name)" }
   if (!$f.FieldType.IsValueType -and $f.FieldType -ne [string]) { throw "Reference in scalar struct: $($t.FullName).$($f.Name)" }
   [void]$generated.AppendLine('        ' + (Expression $f.FieldType "value.$($f.Name)"))
  }
  [void]$generated.AppendLine('    }')
 }
}
[void]$generated.AppendLine('} }')
[void]$generated.AppendLine('namespace ArcaneCasters.Simulation { public static class PhysicsStateFieldManifest { public static readonly string[] Expected = {')
foreach ($row in $inventory) { [void]$generated.AppendLine('    "' + $row.Type + '|' + $row.Field + '|' + $row.FieldType + '|' + $row.Policy + '",') }
[void]$generated.AppendLine('}; } }')
[IO.File]::WriteAllText("$runtime/Physics/Checkpoint/PhysicsStateEngineAccessors.cs", $generated.ToString(), [Text.UTF8Encoding]::new($false))
$inventory | Export-Csv "$PSScriptRoot/physics-state-fields.csv" -NoTypeInformation -Encoding utf8
Write-Output "Generated typed physics accessors: classes=$($names.Count), structs=$($done.Count), fields=$($inventory.Count)"
