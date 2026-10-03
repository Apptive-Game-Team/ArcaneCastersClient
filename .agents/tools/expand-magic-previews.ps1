param([switch]$CopyRecordings)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
Set-Location $root
$prefabPath = 'Assets/Prefabs/UI/MagicPreview.prefab'
$prefab = Get-Content $prefabPath -Raw
$recordings = Get-ChildItem '../game/build/previews/scenarios/*.json' | Sort-Object Name
$types = [Collections.Generic.SortedSet[string]]::new()
$projectiles = [Collections.Generic.SortedSet[string]]::new()
$effects = [Collections.Generic.SortedSet[string]]::new()
$assetGuids = [Collections.Generic.HashSet[string]]::new()
foreach ($meta in (Get-ChildItem Assets -Filter '*.meta' -Recurse -File)) {
    $match = [regex]::Match((Get-Content $meta.FullName -Raw), '(?m)^guid: ([a-f0-9]{32})')
    if ($match.Success) { [void]$assetGuids.Add($match.Groups[1].Value) }
}
$patch = [Collections.Generic.List[string]]::new()
$patch.Add('*** Begin Patch')
$clipRows = [Collections.Generic.List[string]]::new()
foreach ($file in $recordings) {
    $name = $file.BaseName
    $clip = Get-Content $file.FullName -Raw | ConvertFrom-Json
    foreach ($scenario in $clip.scenarios) { foreach ($frame in $scenario.frames) {
        foreach ($o in $frame.objects.create) { [void]$types.Add($o.type) }
        foreach ($p in $frame.objects.projectile) { [void]$projectiles.Add($p.type) }
        foreach ($u in $frame.objects.update) { foreach ($e in $u.effects) { if ($e -ne 'None') { [void]$effects.Add($e) } } }
    } }
    if ($CopyRecordings -and $clip.source.StartsWith('RemainingMagicPreviewTest;')) { Copy-Item -LiteralPath $file.FullName -Destination "Assets/Resources/MagicPreviews/$name.json" }
    if ($prefab -match "(?m)^  - magicId: $([regex]::Escape($name))`r?$") { continue }
    $hash = [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes("arcane-preview-v2:$name"))
    $guid = [Convert]::ToHexString($hash).Substring(0,32).ToLowerInvariant()
    if (!(Test-Path "Assets/Resources/MagicPreviews/$name.json.meta")) {
        $patch.Add("*** Add File: $root/Assets/Resources/MagicPreviews/$name.json.meta")
        foreach ($line in @('fileFormatVersion: 2', "guid: $guid", 'TextScriptImporter:', '  externalObjects: {}', '  userData:', '  assetBundleName:', '  assetBundleVariant:')) { $patch.Add('+' + $line) }
    }
    foreach ($line in @("  - magicId: $name", "    recordingAsset: {fileID: 4900000, guid: $guid, type: 3}", '    impactSprite: {fileID: 0}', '    visuals: []')) { $clipRows.Add('+' + $line) }
}
$spriteAliases = @{GroundCannon='Cannon';GroundTower='Tower';PveNatureSlimeNest='LeafSlime';PveWaterSlimeNest='WaterSlime';EmberSpirit='EmberSpiritSwarm';SeedSpirit='SeedSpiritSwarm';MeteorDrop='RockDrop';SpiritBombBeam='SpiritBomb';Inspired='RallyingTotem';ShockOverloadSecondary='ShockOverload';ElectricSummon='SeedNest';FireSummon='SeedNest';RockSummon='SeedNest';WindSummon='SeedNest'}
function SpriteReference([string]$type, [string]$category) {
    if ($type -match '^StormStagCharge[234]$') { return SpriteReference 'Overcharge' 'effectVisuals' }
    $path = if ($category -eq 'effectVisuals') { "Assets/Resources/Prefabs/Effects/$type.prefab" } elseif ($category -eq 'projectileVisuals') { "Assets/Resources/Projectiles/$type.prefab" } else { "Assets/Resources/Prefabs/$type.prefab" }
    if (Test-Path $path) {
        $yaml = Get-Content $path -Raw
        $match = [regex]::Match($yaml, 'propertyPath: m_Sprite\s+value:[^\r\n]*\s+objectReference: (\{fileID: 21300000, guid: [a-f0-9]{32}, type: 3\})')
        if (!$match.Success) { $match = [regex]::Match($yaml, 'm_Sprite: (\{fileID: 21300000, guid: [a-f0-9]{32}, type: 3\})') }
        if ($match.Success) {
            $spriteGuid = [regex]::Match($match.Groups[1].Value, 'guid: ([a-f0-9]{32})').Groups[1].Value
            if ($assetGuids.Contains($spriteGuid)) { return $match.Groups[1].Value }
        }
    }
    $spriteName = if ($spriteAliases.ContainsKey($type)) { $spriteAliases[$type] } else { $type }
    $spritePath = "Assets/Resources/Game/sprites/$spriteName.png.meta"
    if (!(Test-Path $spritePath)) { throw "Missing $category visual: $type" }
    $guid = [regex]::Match((Get-Content $spritePath -Raw), '(?m)^guid: ([a-f0-9]{32})').Groups[1].Value
    return "{fileID: 21300000, guid: $guid, type: 3}"
}
function StyleRows($names, [string]$category) {
    $section = [regex]::Match($prefab, "(?s)  ${category}:\s*(.*?)(?=\r?\n  \w+:|\r?\n---|\z)").Groups[1].Value
    foreach ($name in $names) {
        if ($section -match "(?m)^  - prefabType: $([regex]::Escape($name))`r?$") { continue }
        $sprite = SpriteReference $name $category
        $height = if ($category -eq 'effectVisuals') { 0.55 } elseif ($category -eq 'projectileVisuals') { 0.65 } elseif ($name -match 'Field|Drop|Shot|Explosion|Explode|Fist|Vine$|Overload|Leafair') { 1.1 } elseif ($name -match 'Golem|Dragon|Serpent|Lord|Titan|Nest') { 2.1 } else { 1.5 }
        $segment = if ($name -match '^EvilEnt.*Arm$|^EvilEntFireFist$|^SeaSerpentHydroPump$|^SpiritBombBeam$') { 1 } else { 0 }
        $attack = '{fileID: 0}'
        foreach ($suffix in @('Attack','Attacking','2')) {
            if (Test-Path "Assets/Resources/Game/sprites/$name$suffix.png.meta") { $attack = SpriteReference "$name$suffix" 'sharedVisuals'; break }
        }
        foreach ($line in @("  - prefabType: $name", "    sprite: $sprite", "    attackSprite: $attack", '    destroySprite: {fileID: 0}', "    height: $height", '    sortingOrder: 2', '    flipForRight: 1', '    lingerOnDestroy: 0', "    segment: $segment")) { '+' + $line }
    }
}
$sharedRows = @(StyleRows $types 'sharedVisuals')
$projectileRows = @(StyleRows $projectiles 'projectileVisuals')
$effectRows = @(StyleRows $effects 'effectVisuals')
if ($clipRows.Count -or $sharedRows.Count -or $projectileRows.Count -or $effectRows.Count) { $patch.Add("*** Update File: $root/$prefabPath") }
if ($clipRows.Count) { $patch.Add('@@'); foreach ($line in $clipRows) { $patch.Add($line) }; $patch.Add('   scenarioCaption: {fileID: 5424425187506454539}') }
if ($sharedRows.Count) { $patch.Add('@@'); foreach ($line in $sharedRows) { $patch.Add($line) }; $patch.Add('   projectileVisuals:') }
if ($projectileRows.Count) { $patch.Add('@@'); foreach ($line in $projectileRows) { $patch.Add($line) }; $patch.Add('   effectVisuals:') }
if ($effectRows.Count) { $patch.Add('@@'); foreach ($line in $effectRows) { $patch.Add($line) }; $patch.Add(' --- !u!1 &1704370577018710923') }
$patch.Add('*** End Patch')
$patch -join "`n"
