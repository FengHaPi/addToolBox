$ErrorActionPreference = 'Stop'
$modelDirectory = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\Models'))
$modelPath = Join-Path $modelDirectory 'model.onnx'
$expectedHash = '50a57872cc739192446da2a934159f957c81af8b5a161dfda8e3daa51660ca67'
$modelRevision = 'dc06453148f01ef4131f17e9b791345e32e8ee78'
$modelUrl = "https://huggingface.co/CoderViking/birefnet-lite-onnx/resolve/$modelRevision/birefnet-lite-1024.onnx"

if (Test-Path -LiteralPath $modelPath) {
    if ((Get-FileHash -LiteralPath $modelPath -Algorithm SHA256).Hash -ine $expectedHash) {
        throw "Existing model has an unexpected SHA256; left untouched: $modelPath"
    }
    if ((Get-Item -LiteralPath $modelPath).Length -ne 199681624) { throw 'Unexpected model size.' }
    Write-Output "Verified B Static BiRefNet: $modelPath"
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
    if ((Get-Item -LiteralPath $downloadPath).Length -ne 199681624) { throw 'Unexpected model size.' }
    Move-Item -LiteralPath $downloadPath -Destination $modelPath
    Write-Output "Downloaded and SHA256 verified: $modelPath"
}
finally {
    # Only this invocation's explicitly named temporary download can be removed.
    if (Test-Path -LiteralPath $downloadPath) {
        Remove-Item -LiteralPath $downloadPath -Force
    }
}
