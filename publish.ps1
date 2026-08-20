param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$projectRoot = $PSScriptRoot
$publishRoot = Join-Path $projectRoot "artifacts\publish\win-x64"
$packageRoot = Join-Path $projectRoot "artifacts\package"
$stagingRoot = Join-Path $packageRoot "WorkbenchLauncher-v$Version-win-x64"
$zipPath = Join-Path $packageRoot "WorkbenchLauncher-v$Version-win-x64.zip"

dotnet publish (Join-Path $projectRoot "WorkbenchLauncher.csproj") `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishRoot

if (Test-Path $stagingRoot) { Remove-Item -LiteralPath $stagingRoot -Recurse -Force }
New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $publishRoot "WorkbenchLauncher.exe") -Destination $stagingRoot
Copy-Item -LiteralPath (Join-Path $projectRoot "README.md") -Destination $stagingRoot
Copy-Item -LiteralPath (Join-Path $projectRoot "Assets\workbench-icon.png") -Destination $stagingRoot
if (Test-Path $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
Compress-Archive -Path (Join-Path $stagingRoot "*") -DestinationPath $zipPath -CompressionLevel Optimal

Write-Host "Package created: $zipPath"
