$ErrorActionPreference = 'Stop'
$auditData = Join-Path $PSScriptRoot 'audit-data'
$mods = Join-Path $auditData 'Mods'
$fix = Join-Path $mods 'TemporalStorageFilterFix'
$audit = Join-Path $mods 'TemporalFilterAudit'
New-Item -ItemType Directory -Path $fix,$audit -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'build\TemporalStorageFilterFix.dll') -Destination $fix -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'build\TemporalFilterAudit.dll') -Destination $audit -Force
Copy-Item -LiteralPath (Join-Path $env:APPDATA 'VintagestoryData\Mods\TemporalStorage_2.1.3.zip') -Destination $mods -Force
@{
    type='code'; modid='temporalstoragefilterfix'; name='Temporal Storage Filter Fix';
    version='1.0.0'; authors=@('Local customization'); side='Universal';
    description='Corrects item filter classification in Temporal Storage 2.1.3.';
    dependencies=@{game='1.22.7';storagehub='2.1.3'}
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $fix 'modinfo.json')
@{
    type='code'; modid='temporalfilteraudit'; name='Temporal Filter Audit (test only)';
    version='1.0.0'; authors=@('Local audit'); side='Server';
    dependencies=@{game='1.22.7';storagehub='2.1.3';temporalstoragefilterfix='1.0.0'}
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $audit 'modinfo.json')
Write-Output $auditData
