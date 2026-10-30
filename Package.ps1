$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$output = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$vanilla = Get-Content -LiteralPath (Join-Path $root 'vanilla-audit.json') -Raw | ConvertFrom-Json
$modpack = Get-Content -LiteralPath (Join-Path $root 'modpack-audit.json') -Raw | ConvertFrom-Json
foreach ($run in @($vanilla,$modpack)) {
    if ($run.Errors.Count -gt 0 -or @($run.Tests | Where-Object { $_ -like 'FAIL:*' }).Count -gt 0) { throw 'Refusing to package failed validation.' }
}
$buildHash = (Get-FileHash -LiteralPath (Join-Path $root 'build\TemporalStorageFilterFix.dll') -Algorithm SHA256).Hash
foreach ($folder in @('audit-data','audit-modpack-data')) {
    $testHash = (Get-FileHash -LiteralPath (Join-Path $root ($folder + '\Mods\TemporalStorageFilterFix\TemporalStorageFilterFix.dll')) -Algorithm SHA256).Hash
    if ($buildHash -ne $testHash) { throw 'The tested DLL differs from the packaged DLL.' }
}
$staging = Join-Path $root 'release'
New-Item -ItemType Directory -Path $staging -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'build\TemporalStorageFilterFix.dll') -Destination $staging -Force
Copy-Item -LiteralPath (Join-Path $root 'audit-data\Mods\TemporalStorageFilterFix\modinfo.json') -Destination $staging -Force
Copy-Item -LiteralPath (Join-Path $root 'README.md') -Destination (Join-Path $staging 'README.md') -Force
$zipPath = Join-Path $output 'TemporalStorageFilterFix_1.0.0.zip'
Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $zipPath -Force

$source = Join-Path $root 'source-release'
New-Item -ItemType Directory -Path $source -Force | Out-Null
foreach ($file in @('FilterFix.cs','AuditMod.cs','Build.ps1','Prepare-Audit.ps1','Run-Audit.ps1','Package.ps1')) {
    Copy-Item -LiteralPath (Join-Path $root $file) -Destination $source -Force
}
Copy-Item -LiteralPath (Join-Path $root 'README.md') -Destination (Join-Path $source 'README.md') -Force
Compress-Archive -Path (Join-Path $source '*') -DestinationPath (Join-Path $output 'TemporalStorageFilterFix-source.zip') -Force

$changed = @($modpack.Rows | Where-Object { ($_.Before -join ',') -ne ($_.After -join ',') })
$lines = [Collections.Generic.List[string]]::new()
$lines.Add('# Temporal Storage filter audit and validation')
$lines.Add('')
$lines.Add('Date: October 24, 2026. Game: 1.22.7. Temporal Storage: 2.1.3. Companion patch: 1.0.0.')
$lines.Add('')
$lines.Add('## Runtime results')
$lines.Add('')
$lines.Add('| Run | Collectible definitions | Changed classifications | Explicit checks passed | Filter exceptions |')
$lines.Add('|---|---:|---:|---:|---:|')
$lines.Add('| Vanilla + Temporal Storage | ' + $vanilla.Count + ' | ' + $vanilla.Changed + ' | ' + $vanilla.Tests.Count + ' | ' + $vanilla.Errors.Count + ' |')
$lines.Add('| Installed mod set + Temporal Storage | ' + $modpack.Count + ' | ' + $modpack.Changed + ' | ' + $modpack.Tests.Count + ' | ' + $modpack.Errors.Count + ' |')
$lines.Add('')
$lines.Add('Counts include block states and item variants; they are not unique item-family counts. Category expansion includes deliberate storage choices as well as missing-category corrections. The modded run loaded the server-compatible portion of the installed mod set with fresh default configs in an isolated data folder, not the existing save or its configs.')
$lines.Add('')
$lines.Add('Creative sample stacks with embedded contents were also classified: vanilla ' + $vanilla.CreativeSamples.Count + ', modded ' + $modpack.CreativeSamples.Count + '.')
$lines.Add('')
$lines.Add('## Corrected families')
$lines.Add('')
$lines.Add('| Domain and family | Variants changed | Previous categories | New categories |')
$lines.Add('|---|---:|---|---|')
foreach ($group in ($changed | Group-Object { ($_.Code -split ':')[0] + ':' + (($_.Code -split ':')[1] -replace '-.*$','') } | Sort-Object Name)) {
    $before = @($group.Group | ForEach-Object { if ($_.Before.Count -eq 0) { '(none)' } else { $_.Before -join ', ' } } | Sort-Object -Unique) -join '; '
    $after = @($group.Group | ForEach-Object { $_.After -join ', ' } | Sort-Object -Unique) -join '; '
    $lines.Add('| ' + $group.Name + ' | ' + $group.Count + ' | ' + $before + ' | ' + $after + ' |')
}
$lines.Add('')
$lines.Add('## Explicit checks, modded run')
$lines.Add('')
foreach ($test in $modpack.Tests) { $lines.Add('- ' + $test) }
$lines.Add('')
$lines.Add('## Limits')
$lines.Add('')
$lines.Add('Tests exercised the real loaded game API and Temporal Storage category/acceptance methods. They did not move items between chests, open a player GUI, exercise every custom inventory, or load the existing player save. Four serialized-stack fixtures verified item data stays unchanged by classification. Existing world filters and links require no migration because the category IDs are unchanged.')
$lines.Add('')
$lines.Add('## Build identity')
$lines.Add('')
$lines.Add('Patch DLL SHA-256: `' + $buildHash + '`')
$lines.Add('')
$lines.Add('Original Temporal Storage DLL SHA-256: `9f0a9347095e78d7e99cf5cdd96259ea0db90ff909853021987e4b9de5186f1e`.')
$lines.Add('')
$lines.Add('Original classifier inspection evidence: `ev_0acc06811cfd4e4b366deec83f3e36cfbf745ed58fb244c2e6e4291b975c4ffe` and `ev_8c606db6b418d0faa10ba3f33f04f0266a69606defb4d4f8bba818daa405d825`. Runtime results supersede the earlier static-only findings for tested loaded assets.')
$lines | Set-Content -LiteralPath (Join-Path $output 'TemporalStorageFilterFix-audit.md')
Get-FileHash -LiteralPath $zipPath -Algorithm SHA256 | Format-List
Write-Output ('Ready: ' + $zipPath)
