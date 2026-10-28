$ErrorActionPreference = 'Stop'
$revision = '9237daa68c3014fd7c2e93c6a99326ba5248d60b'
$generated = Join-Path $PSScriptRoot '.generated'
$engine = Join-Path $generated 'engine'
if (Test-Path -LiteralPath $engine) {
    throw '기존 엔진 폴더가 있습니다. 소스 변경 여부를 확인하고 별도 폴더로 보관한 뒤 다시 실행하세요.'
}
New-Item -ItemType Directory -Path $generated -Force | Out-Null
$archive = Join-Path $generated 'engine.zip'
Invoke-WebRequest "https://api.github.com/repos/sam-vdp/bepuphysics1int/zipball/$revision" -OutFile $archive
$unpacked = Join-Path $generated 'unpacked'
Expand-Archive -LiteralPath $archive -DestinationPath $unpacked
$source = @(Get-ChildItem -LiteralPath $unpacked -Directory)
if ($source.Count -ne 1) { throw '예상하지 않은 아카이브 구조입니다.' }
New-Item -ItemType Directory -Path $engine | Out-Null
foreach ($name in @('BEPUphysics', 'BEPUutilities', 'FixedMath.Net', 'LICENSE.md', 'README.md')) {
    Copy-Item -LiteralPath (Join-Path $source[0].FullName $name) -Destination $engine -Recurse
}
Set-Content -LiteralPath (Join-Path $engine 'UPSTREAM_COMMIT') -Value $revision
& (Join-Path $PSScriptRoot 'Patch-Engine.ps1')
Write-Output "엔진 준비 완료: $revision"
