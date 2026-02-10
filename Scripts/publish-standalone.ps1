# Build standalone EXE (no .NET runtime required)
# Output: project root\src\dist\
# Usage (recommended; does not change ExecutionPolicy):
#   powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\Scripts\publish-standalone.ps1"
# Or from project root: .\Scripts\publish-standalone.cmd

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $scriptDir
Push-Location $root

try {
    $proj = Join-Path $root "ContextMenuManager.csproj"
    if (-not (Test-Path $proj)) {
        Write-Error "Project file ContextMenuManager.csproj not found."
        exit 1
    }

    $dist = Join-Path $root "src\\dist"
    if (Test-Path $dist) {
        Remove-Item -Recurse -Force $dist
    }

    dotnet publish $proj -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true `
        -p:PublishTrimmed=false `
        -o $dist

    $exe = Get-ChildItem -Path $dist -Filter "*.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($exe) {
        Write-Host "Built: $($exe.FullName)" -ForegroundColor Green
    }
} finally {
    Pop-Location
}
