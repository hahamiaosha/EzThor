@echo off
setlocal

set "ROOT=%~dp0"
set "PROJECT=%ROOT%src\ThorFlasher.UI\ThorFlasher.UI.csproj"
set "OUTDIR=%ROOT%dist"

where dotnet >nul 2>&1
if errorlevel 1 goto missing_dotnet

echo Building standalone single-file ThorFlasher.exe...
dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "%OUTDIR%"

if errorlevel 1 (
    echo Build failed. Review the output above for details.
    goto end
)

echo Copying Thor_Script to dist directory...
xcopy /Y /S /I "%ROOT%Thor_Script" "%OUTDIR%\Thor_Script"

echo.
echo Build complete! The standalone executable is located at:
echo %OUTDIR%\ThorFlasher.exe
goto end

:missing_dotnet
echo dotnet was not found on PATH.
echo Install the .NET 8 SDK and try again.
echo https://dotnet.microsoft.com/download/dotnet/8.0

:end
endlocal
pause
