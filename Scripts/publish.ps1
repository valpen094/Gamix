# Gamix Publish Script for Installer creation

$projectPath = Join-Path $PSScriptRoot "..\Gamix.UI\Gamix.UI.csproj"
$outputPath = Join-Path $PSScriptRoot "..\publish"

Write-Host "Publishing Gamix.UI..." -ForegroundColor Cyan

# Clean up previous publish folder
if (Test-Path $outputPath) {
    Remove-Item -Path $outputPath -Recurse -Force
}

# Run dotnet publish
# -c Release: リリース構成
# -r win-x64: Windows 64bit向け
# --self-contained false: .NETランタイムを含めない（ランタイムはパッケージ側で配布するか、ユーザーにインストールしてもらう前提）
# -p:PublishReadyToRun=true: 起動速度向上のための最適化
# -p:DebugType=None -p:DebugSymbols=false: PDBファイルの生成を抑制
dotnet publish $projectPath -c Release -r win-x64 --self-contained true -o $outputPath -p:PublishReadyToRun=true -p:DebugType=None -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish failed!"
    exit 1
}

Write-Host "Publish completed. Files are in: $outputPath" -ForegroundColor Green
