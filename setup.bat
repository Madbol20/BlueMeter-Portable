@echo off
REM BlueMeter Setup Script (Batch version for easier execution)
REM This script automatically installs .NET 8.0 SDK, builds, and runs BlueMeter

setlocal enabledelayedexpansion
cd /d "%~dp0"

echo.
echo ======================================
echo BlueMeter Setup ^& Launch
echo ======================================
echo.

REM Check if .NET SDK is installed
echo Checking .NET SDK installation...
dotnet --version >nul 2>&1

if errorlevel 1 (
    echo .NET SDK not found. Installing .NET 8.0 SDK...
    echo Please wait, this may take a few minutes...

    powershell -NoProfile -ExecutionPolicy Bypass -Command ^
        "Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile '$env:TEMP\dotnet-install.ps1'; ^
         & $env:TEMP\dotnet-install.ps1 -Channel 8.0 -InstallDir '$env:ProgramFiles\dotnet'"

    if errorlevel 1 (
        echo.
        echo Failed to install .NET SDK.
        echo Please download and install .NET 8.0 SDK from: https://dotnet.microsoft.com/download
        pause
        exit /b 1
    )

    echo .NET 8.0 SDK installed successfully!
) else (
    for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
    echo .NET SDK found: !DOTNET_VERSION!
)

echo.
echo Building BlueMeter...

REM Navigate to WPF project and build
cd BlueMeter.WPF

REM Clean previous builds
dotnet clean -c Release >nul 2>&1

REM Build the project
echo Building in Release mode...
dotnet build -c Release

if errorlevel 1 (
    echo.
    echo Build failed!
    pause
    exit /b 1
)

echo Build completed successfully!

echo.
echo Launching BlueMeter...
echo.

REM Run the application
dotnet run --no-build -c Release

pause
