param([switch]$CopyRecordings, [string]$RecordingsRoot = '../game/build/previews/scenarios')
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
Set-Location $root
$fixtureRoot = 'Assets/Tests/Editor/MagicPreviews'
$recordings = Get-ChildItem -LiteralPath $RecordingsRoot -Filter '*.json' | Sort-Object Name
foreach ($file in $recordings) {
    $name = $file.BaseName
    $clip = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
    if ($clip.version -ne 2 -or $clip.magic -ne $name) { throw "Invalid recording: $name" }
    foreach ($scenario in $clip.scenarios) {
        if ($null -eq $scenario.fixtureTargetIds -or $null -eq $scenario.parameters) {
            throw "Regenerate $name with explicit fixture IDs and presentation parameters."
        }
    }
    if ($CopyRecordings) { Copy-Item -LiteralPath $file.FullName -Destination "$fixtureRoot/$name.json" }
    if (!(Test-Path -LiteralPath "$fixtureRoot/$name.json.meta")) { Write-Output "Missing Editor fixture meta: $name" }
}
# Regression fixtures only. Players download server recordings; never register
# these assets in Resources or the runtime MagicPreview prefab.
