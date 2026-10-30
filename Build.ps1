$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$gameRoot = 'E:\Games\Vintagestory'
$buildRoot = Join-Path $taskRoot 'build'
New-Item -ItemType Directory -Path $buildRoot -Force | Out-Null
if (!(Test-Path -LiteralPath (Join-Path $buildRoot 'storagehub.dll'))) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archivePath = Join-Path $env:APPDATA 'VintagestoryData\Mods\TemporalStorage_2.1.3.zip'
    $archive = [IO.Compression.ZipFile]::OpenRead($archivePath)
    try { [IO.Compression.ZipFileExtensions]::ExtractToFile($archive.GetEntry('storagehub.dll'), (Join-Path $buildRoot 'storagehub.dll'), $false) }
    finally { $archive.Dispose() }
}
$refs = @(
    (Join-Path $gameRoot 'VintagestoryAPI.dll'),
    (Join-Path $gameRoot 'Lib\Newtonsoft.Json.dll'),
    (Join-Path $gameRoot 'Mods\VSSurvivalMod.dll'),
    (Join-Path $buildRoot 'storagehub.dll')
)
$runtimeRoot = [System.Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()
$refs += Get-ChildItem -LiteralPath $runtimeRoot -File -Filter '*.dll' | Where-Object {
    $stream = [IO.File]::OpenRead($_.FullName)
    try { $pe = [System.Reflection.PortableExecutable.PEReader]::new($stream); $pe.HasMetadata }
    finally { if ($pe) { $pe.Dispose() }; $stream.Dispose() }
} | Select-Object -ExpandProperty FullName
$fixDll = Join-Path $buildRoot 'TemporalStorageFilterFix.dll'
$auditDll = Join-Path $buildRoot 'TemporalFilterAudit.dll'
if (Test-Path -LiteralPath $fixDll) { Remove-Item -LiteralPath $fixDll }
if (Test-Path -LiteralPath $auditDll) { Remove-Item -LiteralPath $auditDll }
$compiler = 'C:\Program Files\dotnet\sdk\9.0.302\Roslyn\bincore\csc.dll'
function Compile-TaskAssembly($source, $output, $references, $responseName) {
    $response = Join-Path $buildRoot $responseName
    $arguments = @('/nostdlib+','/target:library','/optimize+','/deterministic+','/langversion:latest',('/out:"' + $output + '"'))
    $arguments += $references | Sort-Object -Unique | ForEach-Object { '/reference:"' + $_ + '"' }
    $arguments += '"' + $source + '"'
    $arguments | Set-Content -LiteralPath $response
    & dotnet $compiler ('@' + $response)
    if ($LASTEXITCODE -ne 0) { throw "Compiler failed: $source" }
}
Compile-TaskAssembly (Join-Path $taskRoot 'FilterFix.cs') $fixDll $refs 'fix.rsp'
Compile-TaskAssembly (Join-Path $taskRoot 'AuditMod.cs') $auditDll ($refs + $fixDll) 'audit.rsp'
Write-Output 'Compiled filter fix and isolated audit mod.'
