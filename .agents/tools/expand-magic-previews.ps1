param([switch]$CopyRecordings)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
Set-Location $root
$prefabPath = 'Assets/Prefabs/UI/MagicPreview.prefab'
$prefab = Get-Content $prefabPath -Raw
$recordings = Get-ChildItem '../game/build/previews/scenarios/*.json' | Sort-Object Name
$patch = [Collections.Generic.List[string]]::new()
$patch.Add('*** Begin Patch')
$clipRows = [Collections.Generic.List[string]]::new()
foreach ($file in $recordings) {
    $name = $file.BaseName
    $clip = Get-Content $file.FullName -Raw | ConvertFrom-Json
    if ($clip.version -ne 2 -or $clip.magic -ne $name) { throw "Invalid recording: $name" }
    foreach ($scenario in $clip.scenarios) {
        if ($null -eq $scenario.fixtureTargetIds -or $null -eq $scenario.parameters) {
            throw "Regenerate $name with explicit fixture IDs and presentation parameters."
        }
    }
    if ($CopyRecordings) { Copy-Item -LiteralPath $file.FullName -Destination "Assets/Resources/MagicPreviews/$name.json" }
    if ($prefab -match "(?m)^  - magicId: $([regex]::Escape($name))\r?$") { continue }
    $metaPath = "Assets/Resources/MagicPreviews/$name.json.meta"
    if (Test-Path $metaPath) {
        $guid = [regex]::Match((Get-Content $metaPath -Raw), '(?m)^guid: ([a-f0-9]{32})').Groups[1].Value
        if (!$guid) { throw "Invalid meta: $metaPath" }
    } else {
        $hash = [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes("arcane-preview-v2:$name"))
        $guid = [Convert]::ToHexString($hash).Substring(0,32).ToLowerInvariant()
        $patch.Add("*** Add File: $root/$metaPath")
        foreach ($line in @('fileFormatVersion: 2', "guid: $guid", 'TextScriptImporter:', '  externalObjects: {}', '  userData:', '  assetBundleName:', '  assetBundleVariant:')) { $patch.Add('+' + $line) }
    }
    foreach ($line in @("  - magicId: $name", "    recordingAsset: {fileID: 4900000, guid: $guid, type: 3}")) { $clipRows.Add('+' + $line) }
}
if ($clipRows.Count) {
    $patch.Add("*** Update File: $root/$prefabPath")
    $patch.Add('@@')
    foreach ($line in $clipRows) { $patch.Add($line) }
    $patch.Add('   scenarioCaption: {fileID: 5424425187506454539}')
}
if ($patch.Count -gt 1) {
    $patch.Add('*** End Patch')
    $patch -join "`n"
}
# No sprite/style maps: run MagicPreviewTests to validate real runtime dependencies.
