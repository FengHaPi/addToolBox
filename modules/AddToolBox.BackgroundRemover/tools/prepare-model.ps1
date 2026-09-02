$ErrorActionPreference = 'Stop'
$modelDirectory = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\Models'))
$modelPath = Join-Path $modelDirectory 'model.onnx'
$expectedHash = '5600024376f572a557870a5eb0afb1e5961636bef4e1e22132025467d0f03333'
$modelUrl = 'https://huggingface.co/onnx-community/BiRefNet_lite-ONNX/resolve/main/onnx/model.onnx'

if (Test-Path -LiteralPath $modelPath) {
    if ((Get-FileHash -LiteralPath $modelPath -Algorithm SHA256).Hash -ine $expectedHash) {
        throw "Existing model has an unexpected SHA256; left untouched: $modelPath"
    }
    Write-Output "Verified existing model: $modelPath"
    exit 0
}

New-Item -ItemType Directory -Path $modelDirectory -Force | Out-Null
$downloadPath = Join-Path $modelDirectory ("download-{0}.onnx" -f [Guid]::NewGuid().ToString('N'))
try {
    Invoke-WebRequest -Uri $modelUrl -OutFile $downloadPath
    $actualHash = (Get-FileHash -LiteralPath $downloadPath -Algorithm SHA256).Hash
    if ($actualHash -ine $expectedHash) {
        throw "Downloaded model SHA256 mismatch. Expected $expectedHash; got $actualHash"
    }
    Move-Item -LiteralPath $downloadPath -Destination $modelPath
    Write-Output "Downloaded and SHA256 verified: $modelPath"
}
finally {
    # Only this invocation's explicitly named temporary download can be removed.
    if (Test-Path -LiteralPath $downloadPath) {
        Remove-Item -LiteralPath $downloadPath -Force
    }
}
