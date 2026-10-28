$ErrorActionPreference = 'Stop'
$engine = Join-Path $PSScriptRoot '.generated/engine'
# 고정한 upstream의 컴파일 수정과 선택적 전투 접촉 규칙을 적용한다.
$mathPath = Join-Path $engine 'FixedMath.Net/src/Fix64.cs'
$math = [IO.File]::ReadAllText($mathPath)
if ([regex]::Matches($math, 'm_rawValue').Count -ne 6) { throw 'Fix64 패치 대상이 예상과 다릅니다.' }
[IO.File]::WriteAllText($mathPath, $math.Replace('m_rawValue', 'RawValue'))
$boxPath = Join-Path $engine 'BEPUphysics/CollisionTests/CollisionAlgorithms/BoxBoxCollider.cs'
$box = [IO.File]::ReadAllText($boxPath)
$box = $box.Replace('clipCenterX *= .5f;', 'clipCenterX *= (Fix64)0.5m;')
$box = $box.Replace('clipCenterY *= .5f;', 'clipCenterY *= (Fix64)0.5m;')
$box = $box.Replace('faceCenterX *= .5f;', 'faceCenterX *= (Fix64)0.5m;')
$box = $box.Replace('faceCenterY *= .5f;', 'faceCenterY *= (Fix64)0.5m;')
$box = $box.Replace('= .5f + .01f *', '= (Fix64)0.5m + (Fix64)0.01m *')
[IO.File]::WriteAllText($boxPath, $box)

function Replace-Once([string]$source, [string]$before, [string]$after) {
    if ([regex]::Matches($source, [regex]::Escape($before)).Count -ne 1) { throw '접촉 규칙 패치 대상이 예상과 다릅니다.' }
    return $source.Replace($before, $after)
}
$materialPath = Join-Path $engine 'BEPUphysics/Materials/Material.cs'
$material = [IO.File]::ReadAllText($materialPath).Replace("`r`n", "`n")
$materialFields = @'
    public class Material
    {
        // 전투의 매 틱 속도 요청에는 이전 틱 충격량과 겹침 보정을 누적하지 않는다.
        // 양쪽 재질이 켠 접촉에만 적용하며 기본 엔진 동작은 유지한다.
        public bool VelocityOnlyContactResponse { get; set; }
        public Fix64 MaximumContactImpulse { get; set; } = Fix64.MaxValue;
'@
$material = Replace-Once $material "    public class Material`n    {" $materialFields
[IO.File]::WriteAllText($materialPath, $material)
$contactPath = Join-Path $engine 'BEPUphysics/Constraints/Collision/ContactPenetrationConstraint.cs'
$contact = [IO.File]::ReadAllText($contactPath).Replace("`r`n", "`n")
$contact = Replace-Once $contact '        private Fix64 bias;' "        private Fix64 bias;`n        private Fix64 maximumImpulse = Fix64.MaxValue;"
$jacobian = @'
            //Compute inverse effective mass matrix
            bool velocityOnly = entityA != null && entityB != null
                && entityA.Material.VelocityOnlyContactResponse && entityB.Material.VelocityOnlyContactResponse;
            if (velocityOnly)
            {
                // 전투의 원형 몸체는 회전하지 않는다. 같은 높이의 접촉은 수평면을 유지한다.
                angularAX = angularAY = angularAZ = angularBX = angularBY = angularBZ = F64.C0;
                if (entityA.Position.Y == entityB.Position.Y)
                {
                    linearAY = F64.C0;
                    Fix64 length = Fix64.Sqrt(linearAX * linearAX + linearAZ * linearAZ);
                    if (length > F64.C0) { linearAX /= length; linearAZ /= length; }
                }
            }
'@
$contact = Replace-Once $contact '            //Compute inverse effective mass matrix' $jacobian
$response = @'
            Fix64 effectiveMassInverse = entryA + entryB;
            maximumImpulse = Fix64.MaxValue;
            if (velocityOnly)
            {
                accumulatedImpulse = F64.C0;
                softness = F64.C0;
                velocityToImpulse = effectiveMassInverse == F64.C0 ? F64.C0 : -F64.C1 / effectiveMassInverse;
                bias = MathHelper.Max(F64.C0, -RelativeVelocity) * contactManifoldConstraint.materialInteraction.Bounciness;
                maximumImpulse = contact.PenetrationDepth < F64.C0 ? F64.C0
                    : MathHelper.Max(entityA.Material.MaximumContactImpulse, entityB.Material.MaximumContactImpulse);
                return;
            }
'@
$contact = Replace-Once $contact '            Fix64 effectiveMassInverse = entryA + entryB;' $response
$contact = Replace-Once $contact '            accumulatedImpulse = MathHelper.Max(F64.C0, accumulatedImpulse + lambda);' '            accumulatedImpulse = MathHelper.Clamp(accumulatedImpulse + lambda, F64.C0, maximumImpulse);'
[IO.File]::WriteAllText($contactPath, $contact)
