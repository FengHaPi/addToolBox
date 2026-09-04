[CmdletBinding()]
param([Parameter(Mandatory)][string]$BuildRoot, [Parameter(Mandatory)][string]$OutputDirectory)

$ErrorActionPreference = 'Stop'
$moduleRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$buildRootPath = [IO.Path]::GetFullPath($BuildRoot)
$packagePath = [IO.Path]::GetFullPath($OutputDirectory)
$distributionRoot = [IO.Path]::GetDirectoryName($packagePath)
$archiveName = 'AddToolBox.BackgroundRemover-0.2.1.atbmod'
$archivePath = Join-Path $distributionRoot $archiveName
$checksumPath = "$archivePath.sha256"
$roundtripPath = Join-Path $distributionRoot 'roundtrip'
$modelPath = Join-Path $moduleRoot 'Models/model.onnx'
$expectedHash = '50a57872cc739192446da2a934159f957c81af8b5a161dfda8e3daa51660ca67'
$expectedManifest = @{
    schemaVersion = 'addtoolbox-module-v1'; id = 'addtoolbox.background-remover'; displayName = '去背景'
    version = '0.2.1'; kind = 'tool'; entryAssembly = 'AddToolBox.BackgroundRemover.dll'
    entryType = 'AddToolBox.BackgroundRemover.BackgroundRemoverModule'
}
# Explicitly enumerate this module's release assets; unknown DLLs/data must not become distributable.
$expectedPaths = @(
    'module.json', 'AddToolBox.BackgroundRemover.dll', 'AddToolBox.BackgroundRemover.deps.json',
    'AddToolBox.BackgroundRemover.runtimeconfig.json', 'Microsoft.ML.OnnxRuntime.dll',
    'System.Numerics.Tensors.dll', 'onnxruntime.dll', 'onnxruntime_providers_shared.dll', 'DirectML.dll',
    'Models/model.onnx', 'Models/README.md', 'README.md', 'THIRD_PARTY_NOTICES.md',
    'Licenses/ONNXRuntime-LICENSE.txt', 'Licenses/ONNXRuntime-ThirdPartyNotices.txt',
    'Licenses/DirectML-LICENSE.txt', 'Licenses/DirectML-ThirdPartyNotices.txt',
    'Licenses/System.Numerics.Tensors-LICENSE.txt', 'Licenses/System.Numerics.Tensors-ThirdPartyNotices.txt'
)

function Assert-Manifest([string]$Json) {
    $document = [System.Text.Json.JsonDocument]::Parse($Json)
    try {
        $names = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($property in $document.RootElement.EnumerateObject()) {
            if (-not $names.Add($property.Name) -or $property.Name -cnotin $expectedManifest.Keys -or
                $property.Value.ValueKind -ne [System.Text.Json.JsonValueKind]::String -or
                $property.Value.GetString() -cne $expectedManifest[$property.Name]) { throw 'Unexpected module manifest.' }
        }
        if ($names.Count -ne $expectedManifest.Count) { throw 'Incomplete module manifest.' }
    }
    finally { $document.Dispose() }
}

function Assert-EntryPath([string]$Name) {
    if ([string]::IsNullOrWhiteSpace($Name) -or $Name.Contains('\') -or $Name.Contains(':') -or
        [IO.Path]::IsPathRooted($Name) -or @($Name.Split('/') | Where-Object { $_ -in @('', '.', '..') }).Count -gt 0) {
        throw "Unsafe archive entry: $Name"
    }
    if ($Name -cnotin $expectedPaths) { throw "Unexpected release asset: $Name" }
}

if (-not (Test-Path -LiteralPath $modelPath) -or (Get-Item -LiteralPath $modelPath).Length -ne 199681624 -or
    (Get-FileHash -LiteralPath $modelPath).Hash -ine $expectedHash) {
    throw 'Prepare the approved B model explicitly first. Packaging never downloads a model.'
}
if ($packagePath -ieq $roundtripPath) { throw 'Staging and roundtrip directories must differ.' }
foreach ($target in @($packagePath, $archivePath, $checksumPath, $roundtripPath)) {
    if (Test-Path -LiteralPath $target) { throw "Output already exists; left untouched: $target" }
}
# Restore must have been explicitly completed for this artifacts path before packaging.
dotnet build (Join-Path $moduleRoot 'AddToolBox.BackgroundRemover.csproj') -c Release --no-restore --artifacts-path $buildRootPath
if ($LASTEXITCODE -ne 0) { throw 'Release build failed. Restore dependencies explicitly for this BuildRoot first.' }
$source = Join-Path $buildRootPath 'bin/AddToolBox.BackgroundRemover/release_win-x64'
Assert-Manifest (Get-Content -LiteralPath (Join-Path $source 'module.json') -Raw)
$sourceItems = @(Get-Item -LiteralPath $source) + @(Get-ChildItem -LiteralPath $source -Recurse -Force)
if (@($sourceItems | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count -gt 0) {
    throw 'Release source must not contain symbolic links or junctions.'
}
$files = @($sourceItems | Where-Object { -not $_.PSIsContainer -and $_.Extension -notin @('.pdb', '.lib') })
$byPath = [Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
foreach ($file in $files) {
    $relative = [IO.Path]::GetRelativePath($source, $file.FullName).Replace('\', '/')
    Assert-EntryPath $relative
    $byPath.Add($relative, $file)
}
if ($byPath.Count -ne $expectedPaths.Count) { throw 'Incomplete release asset set.' }
$builtModel = $byPath['Models/model.onnx']
if ($builtModel.Length -ne 199681624 -or (Get-FileHash -LiteralPath $builtModel.FullName).Hash -ine $expectedHash) {
    throw 'Package must contain only the approved B model.'
}
$orderedPaths = [string[]]@($byPath.Keys)
[Array]::Sort($orderedPaths, [StringComparer]::Ordinal)
$dependencies = Get-Content -LiteralPath (Join-Path $source 'AddToolBox.BackgroundRemover.deps.json') -Raw | ConvertFrom-Json
if ($null -eq $dependencies.libraries.'Microsoft.ML.OnnxRuntime.DirectML/1.24.4' -or
    $null -ne $dependencies.libraries.'Microsoft.ML.OnnxRuntime/1.29.0') { throw 'Incoherent ONNX Runtime dependencies.' }
# A build must not have populated one of our reserved destinations.
foreach ($target in @($packagePath, $archivePath, $checksumPath, $roundtripPath)) {
    if (Test-Path -LiteralPath $target) { throw "Output became occupied: $target" }
}
[void][IO.Directory]::CreateDirectory($packagePath)
$rows = foreach ($relative in $orderedPaths) {
    $file = $byPath[$relative]
    $destination = Join-Path $packagePath $relative
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination))
    [IO.File]::Copy($file.FullName, $destination, $false)
    $hash = (Get-FileHash -LiteralPath $file.FullName).Hash
    if ((Get-Item -LiteralPath $destination).Length -ne $file.Length -or
        (Get-FileHash -LiteralPath $destination).Hash -ne $hash) { throw "Package copy mismatch: $relative" }
    [pscustomobject]@{ Path=$relative; Bytes=$file.Length; SHA256=$hash }
}
Assert-Manifest (Get-Content -LiteralPath (Join-Path $packagePath 'module.json') -Raw)
$fixedTimestamp = [DateTimeOffset]::new(2000, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
$archiveFile = [IO.FileStream]::new($archivePath, [IO.FileMode]::CreateNew)
try {
    $archive = [IO.Compression.ZipArchive]::new($archiveFile, [IO.Compression.ZipArchiveMode]::Create, $true, [Text.Encoding]::UTF8)
    try {
        foreach ($row in $rows) {
            $entry = $archive.CreateEntry($row.Path, [IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = $fixedTimestamp
            $entry.ExternalAttributes = 0
            $entryStream = $entry.Open()
            try {
                $sourceStream = [IO.File]::OpenRead((Join-Path $packagePath $row.Path))
                try { $sourceStream.CopyTo($entryStream) }
                finally { $sourceStream.Dispose() }
            }
            finally { $entryStream.Dispose() }
        }
    }
    finally { $archive.Dispose() }
}
finally { $archiveFile.Dispose() }

# Validate the entire reopened archive before extracting any entry. No installed path is used.
$archiveFile = [IO.File]::OpenRead($archivePath)
try {
    $archive = [IO.Compression.ZipArchive]::new($archiveFile, [IO.Compression.ZipArchiveMode]::Read, $true)
    try {
        if ($archive.Entries.Count -eq 0 -or $archive.Entries.Count -ne $rows.Count) { throw 'Archive entry count mismatch.' }
        $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        for ($i = 0; $i -lt $archive.Entries.Count; $i++) {
            $entry = $archive.Entries[$i]
            Assert-EntryPath $entry.FullName
            if (-not $seen.Add($entry.FullName)) { throw 'Duplicate archive entry.' }
            $row = $rows[$i]
            if ($entry.FullName -cne $row.Path -or $entry.Length -ne $row.Bytes -or
                $entry.LastWriteTime.DateTime -ne $fixedTimestamp.DateTime) { throw 'Archive order, size or timestamp mismatch.' }
            $entryStream = $entry.Open()
            try { $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($entryStream)) }
            finally { $entryStream.Dispose() }
            if ($hash -ne $row.SHA256) { throw "Archive content hash mismatch: $($entry.FullName)" }
            if ($entry.FullName -ceq 'Models/model.onnx' -and $hash -ine $expectedHash) { throw 'Archive model SHA mismatch.' }
        }
        $manifestEntry = $archive.GetEntry('module.json')
        if ($null -eq $manifestEntry) { throw 'Archive root module.json is required.' }
        $reader = [IO.StreamReader]::new($manifestEntry.Open())
        try { Assert-Manifest $reader.ReadToEnd() }
        finally { $reader.Dispose() }
        [void][IO.Directory]::CreateDirectory($roundtripPath)
        foreach ($entry in $archive.Entries) {
            $destination = [IO.Path]::GetFullPath((Join-Path $roundtripPath $entry.FullName))
            if (-not $destination.StartsWith($roundtripPath + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
                throw 'Extraction path escaped roundtrip directory.'
            }
            [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination))
            $entryStream = $entry.Open()
            try {
                $outputStream = [IO.FileStream]::new($destination, [IO.FileMode]::CreateNew)
                try { $entryStream.CopyTo($outputStream) }
                finally { $outputStream.Dispose() }
            }
            finally { $entryStream.Dispose() }
        }
    }
    finally { $archive.Dispose() }
}
finally { $archiveFile.Dispose() }
$extracted = @(Get-ChildItem -LiteralPath $roundtripPath -Recurse -File -Force)
if ($extracted.Count -ne $rows.Count) { throw 'Roundtrip file count mismatch.' }
foreach ($row in $rows) {
    $file = Get-Item -LiteralPath (Join-Path $roundtripPath $row.Path)
    if ($file.Length -ne $row.Bytes -or (Get-FileHash -LiteralPath $file.FullName).Hash -ne $row.SHA256) {
        throw "Roundtrip content mismatch: $($row.Path)"
    }
}
$archiveHash = (Get-FileHash -LiteralPath $archivePath).Hash.ToLowerInvariant()
$checksumFile = [IO.FileStream]::new($checksumPath, [IO.FileMode]::CreateNew)
try {
    $checksumBytes = [Text.Encoding]::UTF8.GetBytes("$archiveHash  $archiveName`n")
    $checksumFile.Write($checksumBytes, 0, $checksumBytes.Length)
}
finally { $checksumFile.Dispose() }
$totalBytes = ($rows | Measure-Object Bytes -Sum).Sum
$archiveBytes = (Get-Item -LiteralPath $archivePath).Length
[pscustomobject]@{
    Package=$packagePath; Files=$rows.Count; TotalBytes=$totalBytes; Entries=$rows
    Archive=$archivePath; ArchiveBytes=$archiveBytes; ArchiveSHA256=$archiveHash; Checksum=$checksumPath
    CompressionRatio=$archiveBytes / $totalBytes; SavedMB=($totalBytes - $archiveBytes) / 1e6
    SavedPercent=(1 - $archiveBytes / $totalBytes) * 100; Roundtrip=$roundtripPath; RoundtripMatch=$true
    ManifestVersion=$expectedManifest.version; ModelSHA256=$expectedHash; FixedEntryTimestamp='2000-01-01T00:00:00+00:00'
} | ConvertTo-Json -Depth 4
