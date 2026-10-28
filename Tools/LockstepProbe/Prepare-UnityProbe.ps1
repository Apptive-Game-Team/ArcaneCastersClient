$ErrorActionPreference = 'Stop'
$simulation = Join-Path $PSScriptRoot '../../LocalPackages/com.arcanecasters.lockstep/Runtime/Simulation'
$seenGuids = @{}
foreach ($source in Get-ChildItem -LiteralPath $simulation -Filter '*.cs') {
    $metaPath = $source.FullName + '.meta'
    if (!(Test-Path -LiteralPath $metaPath)) { throw "Missing Unity metadata: $metaPath" }
    $guidLine = @(Get-Content -LiteralPath $metaPath | Where-Object { $_ -match '^guid: [a-f0-9]{32}$' })
    if ($guidLine.Count -ne 1) { throw "Invalid Unity GUID: $metaPath" }
    if ($seenGuids.ContainsKey($guidLine[0])) { throw "Duplicate Unity GUID: $metaPath" }
    $seenGuids[$guidLine[0]] = $metaPath
}
$project = Join-Path $PSScriptRoot '.generated/UnityProbe'
if (!(Test-Path "$project/ProjectSettings/ProjectVersion.txt")) { throw 'Unity -createProject로 검증 프로젝트를 먼저 생성하세요.' }
$assets = Join-Path $project 'Assets'
$engine = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../LocalPackages/com.arcanecasters.lockstep/Runtime/Physics'))
foreach ($part in @('BEPUphysics','BEPUutilities','FixedMath.Net/src')) {
    $root = Join-Path $engine $part
    foreach ($source in Get-ChildItem -LiteralPath $root -Recurse -Filter '*.cs') {
        if ($source.FullName -match '[/\\](Properties|obj|bin)[/\\]') { continue }
        $relative = $source.FullName.Substring($engine.Length + 1)
        $target = Join-Path "$assets/Engine" $relative
        New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
        Copy-Item -LiteralPath $source.FullName -Destination $target
    }
}
Copy-Item "$engine/LICENSE.md" "$assets/Engine/LICENSE.md"
Copy-Item "$PSScriptRoot/Program.cs" "$assets/LockstepPhysicsProbe.cs"
Copy-Item "$PSScriptRoot/CoreTests.cs" "$assets/CoreTests.cs"
New-Item -ItemType Directory -Path "$assets/PortedTests" -Force | Out-Null
Copy-Item "$PSScriptRoot/PortedTests/*.cs" "$assets/PortedTests/"
New-Item -ItemType Directory -Path "$assets/Resources/LockstepFixtures" -Force | Out-Null
foreach ($fixture in Get-ChildItem "$PSScriptRoot/Fixtures/*.sql") {
    Copy-Item $fixture.FullName (Join-Path "$assets/Resources/LockstepFixtures" ($fixture.BaseName + '.txt'))
}
Set-Content "$assets/PortedTests/link.xml" '<linker><assembly fullname="Assembly-CSharp"><type fullname="ArcaneCasters.Parity.*" preserve="all" /></assembly></linker>'
New-Item -ItemType Directory -Path "$assets/Simulation" -Force | Out-Null
Copy-Item "$PSScriptRoot/../../LocalPackages/com.arcanecasters.lockstep/Runtime/Simulation/*.cs" "$assets/Simulation/"
Copy-Item "$PSScriptRoot/UnityProbeRunner.cs.txt" "$assets/UnityProbeRunner.cs"
# WINDOWS는 이 구형 라이브러리에서 BCL HashSet 선택용이며 실제 빌드 대상과 무관하다.
Set-Content "$assets/csc.rsp" '-define:WINDOWS,CHECKMATH'
