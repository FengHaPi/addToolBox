[CmdletBinding()]
param([switch]$VerifyOnly)

$ErrorActionPreference = 'Stop'
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'testset.json') -Raw | ConvertFrom-Json
$imageRoot = Join-Path $PSScriptRoot 'images'
if (-not $VerifyOnly) { [void][IO.Directory]::CreateDirectory($imageRoot) }
$seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($item in $manifest.images) {
    if ([IO.Path]::GetFileName($item.filename) -cne $item.filename -or
        $item.filename -match '[\\/:]' -or $item.sha256 -notmatch '^[0-9a-f]{64}$' -or
        -not $seen.Add($item.filename) -or ([uri]$item.downloadUrl).Scheme -ne 'https') {
        throw "Invalid manifest entry: $($item.id)"
    }
    $target = Join-Path $imageRoot $item.filename
    if (Test-Path -LiteralPath $target) {
        $actual = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
        if ($actual -ine $item.sha256) {
            throw "SHA256 mismatch: $target. Existing file preserved; investigate before replacing it."
        }
        Write-Output "VERIFIED / SKIP $($item.id)"
        continue
    }
    if ($VerifyOnly) { throw "Missing image: $target" }
    # Commons may rate-limit. Space requests; a failure stops, without automatic retries.
    Start-Sleep -Seconds 20
    $partial = Join-Path $imageRoot ($item.filename + '.' + [guid]::NewGuid().ToString('N') + '.partial')
    try {
        Invoke-WebRequest -Uri $item.downloadUrl -OutFile $partial -TimeoutSec 180 `
            -Headers @{'User-Agent'='addToolBox-Testset/0.1 (public licensed research fixtures)'}
        $actual = (Get-FileHash -LiteralPath $partial -Algorithm SHA256).Hash
        if ($actual -ine $item.sha256) { throw "Downloaded SHA256 mismatch; expected $($item.sha256), received $actual" }
        [IO.File]::Move($partial, $target, $false)
        Write-Output "DOWNLOADED / VERIFIED $($item.id)"
    }
    catch {
        throw "Download failed for $($item.id): $($_.Exception.Message). Any partial file is preserved at $partial. No license or hash was changed."
    }
}
Write-Output "Verified $($manifest.images.Count) images. No model execution or production files changed."
