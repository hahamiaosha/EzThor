@echo off
setlocal

echo ============================================
echo   ThorFlasher — Build Installer
echo ============================================
echo.

:: Step 1: Publish self-contained single-file
echo [1/3] Publishing ThorFlasher (Release, win-x64, single-file) ...
dotnet publish src\ThorFlasher.UI\ThorFlasher.UI.csproj -c Release -o publish\app
if errorlevel 1 (
    echo ERROR: dotnet publish failed.
    exit /b 1
)
echo      Published to publish\app\
echo.

:: Step 2: Verify key files exist
echo [2/3] Verifying publish output ...
if not exist "publish\app\ThorFlasher.exe" (
    echo ERROR: ThorFlasher.exe not found in publish output.
    exit /b 1
)
if not exist "publish\app\appsettings.json" (
    echo ERROR: appsettings.json not found in publish output.
    exit /b 1
)
if not exist "publish\app\Thor_Script\flash.sh" (
    echo ERROR: Thor_Script\flash.sh not found in publish output.
    exit /b 1
)
echo      All required files present.
echo.

:: Step 3: Build installer with Inno Setup (if available)
echo [3/3] Building installer ...
where iscc >nul 2>&1
if errorlevel 1 (
    echo      Inno Setup compiler (iscc) not found on PATH.
    echo      Install Inno Setup from https://jrsoftware.org/isinfo.php
    echo      Then run:  iscc installer.iss
    echo.
    echo      Alternatively, distribute the publish\app\ folder as-is.
    echo      The user only needs to run ThorFlasher.exe from that folder.
) else (
    iscc installer.iss
    if errorlevel 1 (
        echo ERROR: Inno Setup compilation failed.
        exit /b 1
    )
    echo      Installer created: publish\ThorFlasher_Setup.exe
)

echo.
echo ============================================
echo   Build complete.
echo ============================================
