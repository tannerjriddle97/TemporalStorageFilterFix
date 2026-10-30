param([switch]$Modpack)
$ErrorActionPreference = 'Stop'
$kind = if ($Modpack) { 'modpack' } else { 'vanilla' }
$data = Join-Path $PSScriptRoot $(if ($Modpack) { 'audit-modpack-data' } else { 'audit-data' })
if ($Modpack) {
    New-Item -ItemType Directory -Path (Join-Path $data 'Mods') -Force | Out-Null
    foreach ($folder in @('TemporalStorageFilterFix','TemporalFilterAudit')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('audit-data\Mods\' + $folder)) -Destination (Join-Path $data 'Mods') -Recurse -Force
    }
}
$result = Join-Path $PSScriptRoot ($kind + '-audit.json')
if (Test-Path -LiteralPath $result) { Remove-Item -LiteralPath $result }
$env:TEMPORAL_FILTER_AUDIT_OUTPUT = $result
$arguments = @('--dataPath', ('"' + $data + '"'), '--ip', '127.0.0.1', '--port', $(if ($Modpack) { '42440' } else { '42439' }), '--withconfig', '"{AdvertiseServerUrl: null, Upnp: false, MaxClients: 1}"')
if ($Modpack) { $arguments += @('--addModPath',('"' + (Join-Path $env:APPDATA 'VintagestoryData\Mods') + '"')) }
$proc = Start-Process -FilePath 'E:\Games\Vintagestory\VintagestoryServer.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $PSScriptRoot ($kind + '-server-console.log')) -RedirectStandardError (Join-Path $PSScriptRoot ($kind + '-server-error.log'))
Write-Output ('Isolated audit PID: ' + $proc.Id)
if (-not $proc.WaitForExit(90000)) {
    $proc.Kill()
    $proc.WaitForExit()
    throw 'Isolated audit exceeded 90 seconds and was stopped.'
}
Write-Output ('Server exit: ' + $proc.ExitCode)
if (!(Test-Path -LiteralPath $result)) {
    Get-Content -LiteralPath (Join-Path $PSScriptRoot ($kind + '-server-console.log')) -Tail 20
    throw 'Audit did not produce a result.'
}
$report = Get-Content -LiteralPath $result -Raw | ConvertFrom-Json
$report | Select-Object Count,Changed,Unchanged
$report.Tests
$report.Errors | Select-Object -First 10
if ($proc.ExitCode -ne 0) { throw 'Audit reported a failure.' }
