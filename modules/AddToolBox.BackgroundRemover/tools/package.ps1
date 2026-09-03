[CmdletBinding()]
param([Parameter(Mandatory)][string]$BuildRoot, [Parameter(Mandatory)][string]$OutputDirectory)

$ErrorActionPreference = 'Stop'
$moduleRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$buildRootPath = [IO.Path]::GetFullPath($BuildRoot)
$packagePath = [IO.Path]::GetFullPath($OutputDirectory)
$modelPath = Join-Path $moduleRoot 'Models/model.onnx'
$expectedHash = '50a57872cc739192446da2a934159f957c81af8b5a161dfda8e3daa51660ca67'
if (-not (Test-Path -LiteralPath $modelPath) -or (Get-FileHash -LiteralPath $modelPath).Hash -ine $expectedHash) {
    throw 'Prepare the approved B model explicitly first. Packaging never downloads a model.'
}
if (Test-Path -LiteralPath $packagePath) { throw 'Output directory already exists; package output is never overwritten.' }
# Restore must have been explicitly completed for this artifacts path before packaging.
dotnet build (Join-Path $moduleRoot 'AddToolBox.BackgroundRemover.csproj') -c Release --no-restore --artifacts-path $buildRootPath
if ($LASTEXITCODE -ne 0) { throw 'Release build failed. Restore dependencies explicitly for this BuildRoot first.' }
$source = Join-Path $buildRootPath 'bin/AddToolBox.BackgroundRemover/release_win-x64'
$manifest = Get-Content -LiteralPath (Join-Path $source 'module.json') -Raw | ConvertFrom-Json
if ($manifest.id -ne 'addtoolbox.background-remover' -or $manifest.version -ne '0.2.1') { throw 'Unexpected module manifest.' }
$files = @(Get-ChildItem -LiteralPath $source -Recurse -File | Where-Object { $_.Extension -notin @('.pdb','.lib') })
$modelFiles = @($files | Where-Object { $_.Extension -eq '.onnx' })
if ($modelFiles.Count -ne 1 -or $modelFiles[0].Name -ne 'model.onnx' -or
    (Get-FileHash -LiteralPath $modelFiles[0].FullName).Hash -ine $expectedHash) { throw 'Package must contain only the approved B model.' }
if (@($files | Where-Object { $_.Name -match '^AddToolBox\.(SDK|App|Core|UI|Infrastructure)\.' -or
    $_.Name -like 'DirectML.Debug*' -or $_.Extension -notin @('.dll','.json','.md','.txt','.onnx') }).Count -ne 0) {
    throw 'Unexpected development, host or private SDK assets in package source.'
}
$dependencies = Get-Content -LiteralPath (Join-Path $source 'AddToolBox.BackgroundRemover.deps.json') -Raw | ConvertFrom-Json
if ($null -eq $dependencies.libraries.'Microsoft.ML.OnnxRuntime.DirectML/1.24.4' -or
    $null -ne $dependencies.libraries.'Microsoft.ML.OnnxRuntime/1.29.0') { throw 'Incoherent ONNX Runtime dependencies.' }
[void][IO.Directory]::CreateDirectory($packagePath)
$rows = foreach ($file in $files) {
    $relative = [IO.Path]::GetRelativePath($source, $file.FullName)
    $destination = Join-Path $packagePath $relative
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination))
    [IO.File]::Copy($file.FullName, $destination, $false)
    $hash = (Get-FileHash -LiteralPath $file.FullName).Hash
    if ((Get-FileHash -LiteralPath $destination).Hash -ne $hash) { throw "Package copy hash mismatch: $relative" }
    [pscustomobject]@{ Path=$relative; Bytes=$file.Length; SHA256=$hash }
}
[pscustomobject]@{ Package=$packagePath; Files=$rows.Count; TotalBytes=($rows | Measure-Object Bytes -Sum).Sum; Entries=$rows } | ConvertTo-Json -Depth 4
