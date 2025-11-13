# BlueMeter Setup Script
# This script automatically installs .NET 8.0 SDK, builds, and runs BlueMeter

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "BlueMeter Setup & Launch" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Check if .NET SDK is installed
Write-Host "Checking .NET SDK installation..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>$null

if ($null -eq $dotnetVersion) {
    Write-Host ".NET SDK not found. Installing .NET 8.0 SDK..." -ForegroundColor Yellow

    # Download and install .NET 8.0 SDK
    $dotnetInstallerUrl = "https://dot.net/v1/dotnet-install.ps1"
    $dotnetInstallerPath = "$env:TEMP\dotnet-install.ps1"

    try {
        Invoke-WebRequest -Uri $dotnetInstallerUrl -OutFile $dotnetInstallerPath -UseBasicParsing
        & $dotnetInstallerPath -Channel 8.0 -InstallDir "$env:ProgramFiles\dotnet"
        Write-Host ".NET 8.0 SDK installed successfully!" -ForegroundColor Green
    }
    catch {
        Write-Host "Failed to install .NET SDK. Please visit https://dotnet.microsoft.com/download" -ForegroundColor Red
        Write-Host "Error: $_" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host ".NET SDK found: $dotnetVersion" -ForegroundColor Green
}

Write-Host ""
Write-Host "Building BlueMeter..." -ForegroundColor Yellow

# Navigate to WPF project and build
$wpfPath = Join-Path $PSScriptRoot "BlueMeter.WPF"

try {
    Set-Location $wpfPath

    # Clean previous builds
    dotnet clean -c Release 2>$null

    # Build the project
    Write-Host "Building in Release mode..." -ForegroundColor Yellow
    dotnet build -c Release

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }

    Write-Host "Build completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "Build error: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Launching BlueMeter..." -ForegroundColor Yellow

try {
    dotnet run --no-build -c Release
}
catch {
    Write-Host "Launch error: $_" -ForegroundColor Red
    exit 1
}
